using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Intermec.DeviceManagement.SmartSystem;

using Microsoft.Win32;

namespace RAC_switch
{
    public class itc_ssapi:IDisposable
    {
        static ITCSSApi _ssAPI = null;

        #region XML_STUFF
        //there is no possible query to query all existing profile via XML
        //use the registry instead:
        //[HKEY_LOCAL_MACHINE\Software\wpa_supplicant\configs\Intermec\networks]
        /*
        [HKEY_LOCAL_MACHINE\Software\wpa_supplicant\configs\Intermec\networks]
        "0"="Profile_1"
        "1"="Profile_2"
        "2"="Profile_3"
        "19"=""
        ## lists defined Profiles
        
        [HKEY_LOCAL_MACHINE\Software\wpa_supplicant\configs\Intermec\networks\Profile_3]
        "disabled"="0"
        "key_mgmt"="NONE"
        ...          
        "ssid"="\"INTERMEC\""
        "ProfileName"="Intermec"

        [HKEY_LOCAL_MACHINE\Software\wpa_supplicant\configs\Intermec\networks\Profile_2]
        "disabled"="0"
        "key_mgmt"="WPA-PSK"
        ...
        "ssid"="\"SUPPORT\""
        "ProfileName"="SUPPORT"
        */

        /* The XML uses the ProfileName, but the registry lists Profile_1 to Profile_x subkeys!
        <?xml version="1.0" encoding="UTF-8" ?> 
         <DevInfo Action="Set" Persist="true">
          <Subsystem Name="Reliable Access Client">
           <Group Name="Profile" Instance="SUPPORT">
            <Field Name="disabled">1</Field> 
           </Group>
          </Subsystem>
         </DevInfo>
        */
        #endregion

        public List<racProfile> _racProfiles = new List<racProfile>();

        const string queryRACxml = "<Subsystem Name=\"Reliable Access Client\">";

        const string setRACprofileXml =
            "<Subsystem Name=\"Reliable Access Client\">\r\n" +
            "  <Group Name=\"Profile\" Instance=\"{0}\">\r\n" + //put Profile Name for {0}, ie "Profile_1"
            "    <Field Name=\"disabled\">{1}</Field>\r\n" +    //put 1 for enabled or 0 for disabled!
            "  </Group>\r\n" +
            "</Subsystem>";

        public itc_ssapi()
        {
            if (_ssAPI == null)
                _ssAPI = new ITCSSApi();
            _racProfiles = listRACprofiles();
        }

        public racProfile getCurrentProfile()
        {
            racProfile racP = new racProfile("Default", "Default", "", "disabled");
            foreach (racProfile r in _racProfiles)
            {
                if (r.bDisabled == false)
                {
                    racP=r ;
                    break;
                }
            }
            return racP;
        }

        public class racProfile
        {
            string _regKey;
            string _profileLabel;
            string _ssid;
            string _disabled;
            
            public string sProfileRekKey{
                get { return _regKey; }
            }
            public string sProfileLabel{
                get {return _profileLabel;}
            }
            public string sSSID{
                get {return _ssid;}
            }
            public string sDisabled
            {
                get { return _disabled; }
            }
            public bool bDisabled
            {
                get
                {
                    if (sDisabled == "0")
                        return false;
                    else
                        return true;
                }
            }
            public racProfile(string profileRegKey, string profilelabel, string ssid, string disabled)
            {
                _regKey = profileRegKey;
                _profileLabel = profilelabel;
                _ssid = ssid;
                _disabled = disabled;
            }
            public override string ToString()
            {
                return "sProfileRekKey: " + _regKey +
                       "/ sProfileLabel :" + _profileLabel +
                       "/ sSSID :" + _ssid + 
                       "/ sDisabled :" + _disabled;
            }
        }

        public List<racProfile> listRACprofiles()
        {
            _racProfiles.Clear();
            List<string> lRet = new List<string>();
            //RegistryKey rKey = Registry.LocalMachine.OpenSubKey(@"Software\wpa_supplicant\configs\Intermec\networks",false);
            const string subKey = @"HKEY_LOCAL_MACHINE\\Software\wpa_supplicant\configs\Intermec\networks";
            //try 0 to 19
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    //string sVal = (string)rKey.GetValue(i.ToString());
                    string sProfileSubKey = (string)Registry.GetValue(subKey, i.ToString(), "");
                    if (sProfileSubKey.Length > 0)
                    {
                        //read the Profile settings subkey
                        string sLabel = (string)Registry.GetValue(subKey +"\\" + sProfileSubKey, "ProfileName", "");
                        string sSSID = (string)Registry.GetValue(subKey + "\\" + sProfileSubKey, "ssid", "");
                        sSSID = sSSID.Trim(new char[] { '"' });
                        string sDisabled = (string)Registry.GetValue(subKey + "\\" + sProfileSubKey, "disabled", "");

                        racProfile racProf = new racProfile(sProfileSubKey, sLabel, sSSID, sDisabled);
                        Logger.WriteLine("listRACprofiles found: " + racProf.ToString());
                        _racProfiles.Add(racProf);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("Exception in listRACprofiles read networks. " + ex.Message);
                }
            }
            return _racProfiles;
        }

        /// <summary>
        /// enable Profile with ssid
        /// disable others!
        /// </summary>
        /// <param name="ssid"></param>
        /// <returns></returns>
        public int setRACprofile(string ssid)
        {
            Logger.WriteLine("setRACprofile: " + ssid);
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            //find profile with ssid
            string profile = "";
            foreach (racProfile rac in _racProfiles)
            {
                if (rac.sProfileLabel == "Default")
                    continue; //next iteration
                if (rac.sSSID == ssid)
                {
                    profile = rac.sProfileLabel;
                    enableProfile(rac.sProfileLabel, true);
                    iRet = 1;
                }
                else
                {
                    profile = rac.sProfileLabel;
                    enableProfile(rac.sProfileLabel, false);
                }
            }
            if (profile == "")
                return -2;
            listRACprofiles();
            return iRet;
        }

        public int enableProfile(string profilelabel, bool bEnable)
        {
            Logger.WriteLine("enableProfile: " + profilelabel + "/" + bEnable);
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            int dSize = 1024;
            string sXML = String.Format(setRACprofileXml, profilelabel, (bEnable?1:0) ); //enable = 1, disabled=0 !!!
            Logger.WriteLine(sXML);
            uint uError = _ssAPI.Set(sXML, sb, ref dSize, 2000);
            if (uError != ITCSSErrors.E_SS_SUCCESS)
            {
                Logger.WriteLine("SSAPI error: " + uError.ToString() + "\n" + sb.ToString().Substring(0, dSize));
                iRet = -1;
            }
            else
            {
                Logger.WriteLine("enableProfile success");
                iRet = 0;
            }
            return iRet;
        }

        public void Dispose()
        {
            if (_ssAPI != null)
            {
                _ssAPI.Dispose();
                _ssAPI = null;
            }
        }
    }
}
