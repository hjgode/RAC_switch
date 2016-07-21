using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Intermec.DeviceManagement.SmartSystem;
using Microsoft.Win32;

namespace Funk_switch
{
    public class RacProfiles:IProfiles,IDisposable
    {
        static ITCSSApi _ssAPI = null;
        List<Profile> _Profiles = new List<Profile>();

        public List<Profile> profiles
        {
            get { return _Profiles; }
        }

        const string setRACprofileXml =
            "<Subsystem Name=\"Reliable Access Client\">\r\n" +
            "  <Group Name=\"Profile\" Instance=\"{0}\">\r\n" + //put Profile Name for {0}, ie "Profile_1"
            "    <Field Name=\"disabled\">{1}</Field>\r\n" +    //put 1 for enabled or 0 for disabled, the 'disabled' has to be read as 'enabled'!
            "  </Group>\r\n" +
            "</Subsystem>";

        public RacProfiles()
        {
            if(!isRACClient())
                throw new NotSupportedException("RAC client is not active");
            readProfiles();
        }

        public void Dispose()
        {
            if (_ssAPI != null)
            {
                _ssAPI.Dispose();
                _ssAPI = null;
            }
        }

        public int readProfiles()
        {
            _Profiles.Clear();
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
                        string sLabel = (string)Registry.GetValue(subKey + "\\" + sProfileSubKey, "ProfileName", "");
                        string sSSID = (string)Registry.GetValue(subKey + "\\" + sProfileSubKey, "ssid", "");
                        sSSID = sSSID.Trim(new char[] { '"' });

                        //the registry holds a disbaled value whereas the xml holds an enabled value
                        string sDisabled = (string)Registry.GetValue(subKey + "\\" + sProfileSubKey, "disabled", "");

                        Profile funkProf = new Profile(sProfileSubKey, sLabel, sSSID, sDisabled);
                        Logger.WriteLine("listRACprofiles found: " + funkProf.ToString());
                        _Profiles.Add(funkProf);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("Exception in listRACprofiles read networks. " + ex.Message);
                }
            }
            //sort rac profiles by profile1 and profile2
            _Profiles.Sort(new MobileConfiguration.myCompareString());

            return _Profiles.Count;
        }

        public static bool isRACClient()
        {
            if (System.IO.File.Exists(@"\windows\WPA_Configlet.dll"))
                return true;
            else
                return false;
        }

        /// <summary>
        /// set active (enabled) RAC profile
        /// and disables others
        /// </summary>
        /// <param name="sLabel"></param>
        /// <returns></returns>
        public bool setCurrentProfile(string sLabel)
        {
            Logger.WriteLine("setRACprofile: " + sLabel);
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            //find profile with ssid
            string profile = "";
            foreach (Profile prof in _Profiles)
            {
                //disable if not the desired one
                if (prof.sProfileLabel == sLabel)
                {
                    profile = prof.sProfileLabel;
                    enableProfile(prof.sProfileLabel, true);
                    iRet = 1;
                }
                else
                {
                    enableProfile(prof.sProfileLabel, false);
                }
            }
            if (profile == "")
                iRet = -2;
            //listRACprofiles();
            return iRet==0;
        }

        int enableProfile(string profilelabel, bool bEnable)
        {
            Logger.WriteLine("enableProfile: " + profilelabel + "/" + bEnable);
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            int dSize = 1024;
            string sXML = String.Format(setRACprofileXml, profilelabel, (bEnable ? 1 : 0)); //enable = 1, disabled=0, the xml say disabled instead of enabled !!!
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

        public Profile getCurrentProfile()
        {
            Profile racP = new Profile("Default", "Default", "", "1");
            foreach (Profile r in _Profiles)
            {
                if (r.bEnabled == true)
                {
                    racP = r;
                    break;
                }
            }
            return racP;
        }
    }
}
