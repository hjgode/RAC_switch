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

        private List<racProfile> _myRacProfiles = new List<racProfile>();
        /// <summary>
        /// returns the current list of RAC Profiles as defined in Registry
        /// </summary>
        public List<racProfile> _racProfiles
        {
            get
            {
                listRACprofiles(); //read current list, updates _myRacProfiles
                return _myRacProfiles;
            }
            set
            {
                _myRacProfiles = value;
            }
        }

        const string queryRACxml = "<Subsystem Name=\"Reliable Access Client\">";

        const string setRACprofileXml =
            "<Subsystem Name=\"Reliable Access Client\">\r\n" +
            "  <Group Name=\"Profile\" Instance=\"{0}\">\r\n" + //put Profile Name for {0}, ie "Profile_1"
            "    <Field Name=\"disabled\">{1}</Field>\r\n" +    //put 1 for enabled or 0 for disabled, the 'disabled' has to be read as 'enabled'!
            "  </Group>\r\n" +
            "</Subsystem>";

        const string getRadioEnabledXml =
            "<Subsystem Name=\"Communications\"> \r\n" +
            " <Group Name=\"802.11 Radio\"> \r\n" +
            "     <Field Name=\"Radio Enabled\">1</Field>  \r\n" +
            " </Group> \r\n" +
            "</Subsystem> \r\n";
        const string setRadioEnabledXml =
            "<Subsystem Name=\"Communications\"> \r\n" +
            " <Group Name=\"802.11 Radio\"> \r\n" +
            "     <Field Name=\"Radio Enabled\">{0}</Field>  \r\n" +
            " </Group> \r\n" +
            "</Subsystem> \r\n";

        public itc_ssapi()
        {
            if (_ssAPI == null)
                _ssAPI = new ITCSSApi();
            //_racProfiles = listRACprofiles();
        }

        public bool getRadioEnabled()
        {
            bool bRet = false;
            Logger.WriteLine("getRadioEnabled...");
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            int dSize = 1024;
            string sXML = getRadioEnabledXml;
            Logger.WriteLine(sXML);
            uint uError = _ssAPI.Get(sXML, sb, ref dSize, 2000);
            if (uError != ITCSSErrors.E_SS_SUCCESS)
            {
                Logger.WriteLine("SSAPI error: " + uError.ToString() + "\n" + sb.ToString().Substring(0, dSize));
                iRet = -1;
            }
            else
            {
                Logger.WriteLine("getRadioEnabled success");
                string sTest = sb.ToString().Substring(sb.ToString().IndexOf("<Field Name=\"Radio Enabled\">") + "<Field Name=\"Radio Enabled\">".Length, 1);
                //Logger.WriteLine("getRadioEnabled success: sTest="+sTest);
                if (sTest == "1")
                    bRet = true;
                else
                    bRet = false;
                iRet = 0;
            }
            return bRet;
        }
        public int setRadioEnabled(bool bEnable){
            Logger.WriteLine("setRadioEnabled: " + bEnable);
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            int dSize = 1024;
            string sXML = String.Format(setRadioEnabledXml, (bEnable ? 1 : 0)); 
            Logger.WriteLine(sXML);
            uint uError = _ssAPI.Set(sXML, sb, ref dSize, 2000);
            if (uError != ITCSSErrors.E_SS_SUCCESS)
            {
                Logger.WriteLine("SSAPI error: " + uError.ToString() + "\n" + sb.ToString().Substring(0, dSize));
                iRet = -1;
            }
            else
            {
                Logger.WriteLine("setRadioEnabled success");
                iRet = 0;
            }
            return iRet;
        }

        public racProfile getCurrentProfile()
        {
            racProfile racP = new racProfile("Default", "Default", "", "disabled");
            foreach (racProfile r in _racProfiles)
            {
                if (r.bEnabled == true)
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
            string _enabled;
            
            public string sProfileRekKey{
                get { return _regKey; }
            }
            public string sProfileLabel{
                get {return _profileLabel;}
            }
            public string sSSID{
                get {return _ssid;}
            }
            public string sEnabled
            {
                get { return _enabled; }
            }
            public bool bEnabled
            {
                get
                {
                    if (sEnabled == "0")
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
                if (disabled == "0")
                    _enabled = "1";
                else
                    _enabled = "0";
            }
            public override string ToString()
            {
                return "sProfileRekKey: " + _regKey +
                       "/ sProfileLabel :" + _profileLabel +
                       "/ sSSID :" + _ssid.Trim() + 
                       "/ sEnabled :" + _enabled;
            }
        }

        public List<racProfile> listRACprofiles()
        {
            _myRacProfiles.Clear();
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

                        //the registry holds a disbaled value whereas the xml holds an enabled value
                        string sDisabled = (string)Registry.GetValue(subKey + "\\" + sProfileSubKey, "disabled", "");

                        racProfile racProf = new racProfile(sProfileSubKey, sLabel, sSSID, sDisabled);
                        Logger.WriteLine("listRACprofiles found: " + racProf.ToString());
                        _myRacProfiles.Add(racProf);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("Exception in listRACprofiles read networks. " + ex.Message);
                }
            }
            //sort rac profiles by profile1 and profile2
            _myRacProfiles.Sort(new MobileConfiguration.myCompareString());
            
            return _myRacProfiles;
        }

        /// <summary>
        /// enable Profile with ssid
        /// disable others!
        /// </summary>
        /// <param name="ssid"></param>
        /// <returns></returns>
        public int setRACprofile(string sProfile)
        {
            Logger.WriteLine("setRACprofile: " + sProfile);
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            //find profile with ssid
            string profile = "";
            foreach (racProfile rac in _racProfiles)
            {
                if (rac.sProfileLabel == sProfile)
                {
                    profile = rac.sProfileLabel;
                    enableProfile(rac.sProfileLabel, true);
                    iRet = 1;
                }
                else
                {
                    enableProfile(rac.sProfileLabel, false);
                }
            }
            if (profile == "")
                return -2;
            //listRACprofiles();
            return iRet;
        }

        public int enableProfile(string profilelabel, bool bEnable)
        {
            Logger.WriteLine("enableProfile: " + profilelabel + "/" + bEnable);
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            int dSize = 1024;
            string sXML = String.Format(setRACprofileXml, profilelabel, (bEnable?1:0) ); //enable = 1, disabled=0, the xml say disabled instead of enabled !!!
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
