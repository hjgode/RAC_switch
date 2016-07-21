using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Intermec.DeviceManagement.SmartSystem;
using Microsoft.Win32;

namespace RAC_switch
{
    public class itc_ssapi2 : IDisposable
    {
        static ITCSSApi _ssAPI = null;

        #region XML_STUFF
        /*
            <Subsystem Name="Funk Security">
             <Field Name="ActiveProfile">Profile_1</Field>
              <Group Name="Profile" Instance="Profile_1">
               <Field Name="ProfileLabel">Profile_1</Field> 
               <Field Name="SSID">Fuhrpark_PDA</Field> 
               <Field Name="Association">RSN</Field> 
               <Field Name="Encryption">AES</Field> 
               <Field Name="PreSharedKey" Encrypt="binary.base64">.lkjsaflkjsafdlkj==</Field> 
              </Group>
              <Group Name="Profile" Instance="Profile_2">
               <Field Name="ProfileLabel">Profile_2</Field> 
               <Field Name="SSID">MI-RC.51</Field>
               <Field Name="Association">Open</Field> 
               <Field Name="Encryption">None</Field>  
              </Group>
             </Subsystem>
            */
        #endregion

        public static bool isFunkClient()
        {
            if (_ssAPI == null)
                _ssAPI = new ITCSSApi();
            bool bRet = false;
            const string isFunkXML =
                "<Subsystem Name=\"Communications\">\r\n" +
                "  <Group Name=\"802.11 Radio\">\r\n" +
                "     <Field Name=\"ZeroConfig\"></Field> \r\n" +
                "  </Group>\r\n" +
                "</Subsystem>\r\n";
            StringBuilder sb = new StringBuilder(1024);
            int aSize = 1024;
            try
            {
                if (_ssAPI.Get(isFunkXML, sb, ref aSize, 2000) == ITCSSErrors.E_SS_SUCCESS)
                {
                    int iPos = sb.ToString().IndexOf("\"ZeroConfig\">") + "\"ZeroConfig\">".Length;
                    string sValue = sb.ToString().Substring(iPos, sb.ToString().IndexOf("<"));
                    if (sValue.Equals("Off", StringComparison.OrdinalIgnoreCase))
                        bRet = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine("isFunkClient exception: " + ex.Message);
            }
            return bRet;
        }

        private List<funkProfile> _myFunkProfiles = new List<funkProfile>();
        /// <summary>
        /// returns the current list of RAC Profiles as defined in Registry
        /// </summary>
        public List<funkProfile> _funkProfiles
        {
            get
            {
                if(_myFunkProfiles.Count>0)
                    return _myFunkProfiles;
                listFUNKprofiles(); //read current list, updates _myRacProfiles
                return _myFunkProfiles;
            }
            set
            {
                _myFunkProfiles = value;
            }
        }

        const string setFUNKprofileXml =
            "<Subsystem Name=\"Funk Security\">\r\n" +
                "<Field Name=\"ActiveProfile\">{0}</Field>\r\n" + //put Profile Name for {0}, ie "Profile_1"
            "</Subsystem>";

        const string getRadioEnabledXml =
            "<Subsystem Name=\"Communications\"> \r\n" +
            " <Group Name=\"802.11 Radio\"> \r\n" +
            "     <Field Name=\"Radio Enabled\"></Field>  \r\n" +
            " </Group> \r\n" +
            "</Subsystem> \r\n";
        const string setRadioEnabledXml =
            "<Subsystem Name=\"Communications\"> \r\n" +
            " <Group Name=\"802.11 Radio\"> \r\n" +
            "     <Field Name=\"Radio Enabled\">{0}</Field>  \r\n" +
            " </Group> \r\n" +
            "</Subsystem> \r\n";

        public itc_ssapi2()
        {
            if (_ssAPI == null)
                _ssAPI = new ITCSSApi();
            //_racProfiles = listRACprofiles();
        }

        public bool getRadioEnabled()
        {
            bool bRet = false;
            Logger.WriteLine("getRadioEnabled...");
            StringBuilder sb = new StringBuilder(1024);
            int dSize = 1024;
            string sXML = getRadioEnabledXml;
            Logger.WriteLine(sXML);
            try
            {
                if (_ssAPI == null)
                    _ssAPI = new ITCSSApi();
                uint uError = _ssAPI.Get(sXML, sb, ref dSize, 2000);
                if (uError == ITCSSErrors.E_SS_SUCCESS)
                {
                    Logger.WriteLine("getRadioEnabled success");
                    string sTest = sb.ToString().Substring(sb.ToString().IndexOf("<Field Name=\"Radio Enabled\">") + "<Field Name=\"Radio Enabled\">".Length, 1);
                    //Logger.WriteLine("getRadioEnabled success: sTest="+sTest);
                    if (sTest == "1")
                        bRet = true;
                    else
                        bRet = false;
                }
                else
                {
                    Logger.WriteLine("SSAPI error: " + uError.ToString() + "\n" + sb.ToString().Substring(0, dSize));
                }
            }
            catch (Exception ex) {
                Logger.WriteLine("getRadioEnabled Exception: " + ex.Message);
            }
            return bRet;
        }
        public int setRadioEnabled(bool bEnable)
        {
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

        funkProfile _currentFunkProfile=new funkProfile("Profile_1","Profile_1","Profile_1","INTERMEC");
        public funkProfile getCurrentProfile()
        {
            const string subKey = @"HKEY_CURRENT_USER\Software\Intermec\80211Conf\Profiles";
            int currentProfileInt = (int)Registry.GetValue(subKey, "ActiveProfile", -1);
            string currentProfileString = (string)Registry.GetValue(subKey, currentProfileInt.ToString(), "");

            foreach (funkProfile r in _funkProfiles)
            {
                if (r.sProfileRekKey == currentProfileString)
                {
                    _currentFunkProfile = r;
                    break;
                }
            }
            return _currentFunkProfile;
        }

        public class funkProfile
        {
            string _regKey;
            string _profileLabel;
            string _ssid;

            public string sProfileRekKey
            {
                get { return _regKey; }
            }
            public string sProfileLabel
            {
                get { return _profileLabel; }
            }
            public string sSSID
            {
                get { return _ssid; }
            }

            private string _profileName = "";
            public string profileName
            {
                get { return _profileName; }
            }
            public funkProfile(string profileRegKey, string profilelabel, string profilename, string ssid)
            {
                _regKey = profileRegKey;
                _profileLabel = profilelabel;
                _profileName = profilename;
                _ssid = ssid;
            }
            public override string ToString()
            {
                return "sProfileRekKey: " + _regKey +
                       "/ sProfileLabel :" + _profileLabel +
                       "/ sSSID :" + _ssid.Trim() +
                       "/ sProfileName :" + _profileName;
            }
        }

        public List<funkProfile> listFUNKprofiles()
        {
            _myFunkProfiles.Clear();
            List<string> lRet = new List<string>();
            //RegistryKey rKey = Registry.LocalMachine.OpenSubKey(@"Software\wpa_supplicant\configs\Intermec\networks",false);
            const string subKey = @"HKEY_CURRENT_USER\Software\Intermec\80211Conf\Profiles";
            int currentProfileInt = (int)Registry.GetValue(subKey, "ActiveProfile", -1);
            string currentProfileString = (string)Registry.GetValue(subKey, currentProfileInt.ToString(), "");
            //try 0 to 19
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    //string sVal = (string)rKey.GetValue(i.ToString());
                    string sProfileSubKey = (string)Registry.GetValue(subKey, i.ToString(), ""); //this will be the name of the subkey, ie Profile_1 to Profile_4
                    if (sProfileSubKey.Length > 0)
                    {
                        //read the Profile settings subkey
                        string sLabel = (string)Registry.GetValue(subKey + "\\" + sProfileSubKey, "ProfileLabel", "");
                        string sSSID = (string)Registry.GetValue(subKey + "\\" + sProfileSubKey, "SSID", "");
                        string sName = sProfileSubKey;
                        sSSID = sSSID.Trim(new char[] { '"' });

                        funkProfile funkProf = new funkProfile(sProfileSubKey, sLabel, sName, sSSID);
                        Logger.WriteLine("listFunkprofiles found: " + funkProf.ToString());
                        _myFunkProfiles.Add(funkProf);
                        if (funkProf.sProfileRekKey == currentProfileString)
                        {
                            _currentFunkProfile = funkProf;
                            Logger.WriteLine("## Current Profile: " + currentProfileString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("Exception in listFunkProfiles read networks. " + ex.Message);
                }
            }
            //sort rac profiles by profile1 and profile2
            _myFunkProfiles.Sort(new MobileConfiguration.myCompareString());

            return _myFunkProfiles;
        }

        /// <summary>
        /// enable Profile with ssid
        /// disable others!
        /// </summary>
        /// <param name="ssid"></param>
        /// <returns>1 for OK</returns>
        public int setFUNKprofile(string sProfile)
        {
            Logger.WriteLine("setFunkprofile: " + sProfile);
            int iRet = 0;
            //find profile with ssid
            if (enableProfile(sProfile) != 0)
                iRet = -2;
            else
                iRet = 1;
            //listRACprofiles();      
            this.setRadioEnabled(true);
            return iRet;
        }


        public int setFUNKprofileBySSID(string sSSID){
            int iRet=-1;
            Logger.WriteLine("setFunkprofileBySSID: " + sSSID);
            foreach (funkProfile f in _funkProfiles){
                if(f.sSSID==sSSID){
                    if(enableProfile(f.sProfileRekKey)==1){
                        iRet = 1;
                        break;
                    }
                }
            }
            this.setRadioEnabled(true);
            return iRet;
        }

        /// <summary>
        /// for funk only one profile is enabled
        /// by setting ActiveProfile
        /// </summary>
        /// <param name="profilelabel"></param>
        /// <param name="bEnable"></param>
        /// <returns></returns>
        public int enableProfile(string profile)
        {
            Logger.WriteLine("enableProfile: " + profile );
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            int dSize = 1024;
            //set profile_x for profile with profileLabel
            string sXML = String.Format(setFUNKprofileXml, profile); //enable = 1, disabled=0, the xml say disabled instead of enabled !!!
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
