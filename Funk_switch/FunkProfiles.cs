using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Microsoft.Win32;
using Intermec.DeviceManagement.SmartSystem;

namespace Funk_switch
{
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

    /// <summary>
    /// throws NotSupportedException if not Funk Client
    /// </summary>
    public class FunkProfiles:IProfiles,IDisposable
    {
        const string subKey = @"HKEY_CURRENT_USER\Software\Intermec\80211Conf\Profiles";
        static ITCSSApi _ssAPI = new Intermec.DeviceManagement.SmartSystem.ITCSSApi();
        const string setFUNKprofileXml =
            "<Subsystem Name=\"Funk Security\">\r\n" +
                "<Field Name=\"ActiveProfile\">{0}</Field>\r\n" + //put Profile Name for {0}, ie "Profile_1"
            "</Subsystem>";

        static List<Profile> _profiles = new List<Profile>();
        public List<Profile> profiles{
            get{return _profiles;}
        }

        public FunkProfiles()
        {
            if (!isFunkClient())
                throw new NotSupportedException("Funk client is not active");
            readProfiles();
        }

        public void Dispose(){
            if (_ssAPI != null)
            {
                _ssAPI.Dispose();
                _ssAPI = null;
            }
        }


        public static bool isFunkClient()
        {
            /*
            [HKEY_LOCAL_MACHINE\Drivers\BuiltIn\NDISUIO]
            "Dll"="zniczio.dll" ; for FUNK active
            "Dll"="ndisuio.dll" ; for ZeroConfig active
             * or
            [HKEY_LOCAL_MACHINE\Drivers\BuiltIn\ZeroConfig]
            "Dll"="WZCSVCxxx.dll" ; for FUNK active
            "Dll"="WZCSVC.dll" ; for ZeroConfig active
             */
            int maxtry = 5;
            int currtry=0;
            bool bRet = false;
            Logger.WriteLine("isFunkClient() ...");
            do
            {
                try
                {
                    currtry++;
                    Logger.WriteLine("isFunkClient(): attempt "+ currtry.ToString());
                    string ZeroConfigDll = (string) Registry.LocalMachine.OpenSubKey(@"Drivers\BuiltIn\ZeroConfig", false).GetValue("Dll", "");
                    if ("WZCSVCxxx.dll".Equals(ZeroConfigDll, StringComparison.OrdinalIgnoreCase))
                    {
                        bRet = true;
                        break;
                    }
                    //using SSAPI is a mess on warmboot!
                    //if (_ssAPI == null)
                    //{
                    //    Logger.WriteLine("isFunkClient(): new ITCSSApi()...");
                    //    _ssAPI = new ITCSSApi();
                    //}
                    //if (_ssAPI != null)
                    //{
                    //    Logger.WriteLine("isFunkClient(): ITCSSApi() loaded");
                    //    const string isFunkXML =
                    //         "<Subsystem Name=\"Communications\">\r\n" +
                    //         "  <Group Name=\"802.11 Radio\">\r\n" +
                    //         "     <Field Name=\"ZeroConfig\"></Field> \r\n" +
                    //         "  </Group>\r\n" +
                    //         "</Subsystem>\r\n";
                    //    StringBuilder sb = new StringBuilder(1024);
                    //    int aSize = 1024;
                    //    if (_ssAPI.Get(isFunkXML, sb, ref aSize, 2000) == ITCSSErrors.E_SS_SUCCESS)
                    //    {
                    //        int iPos = sb.ToString().IndexOf("\"ZeroConfig\">") + "\"ZeroConfig\">".Length;
                    //        string sValue = sb.ToString().Substring(iPos, sb.ToString().IndexOf("<"));
                    //        if (sValue.Equals("Off", StringComparison.OrdinalIgnoreCase))
                    //            bRet = true;
                    //        break;
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("isFunkClient exception: " + ex.Message);
                }
                System.Threading.Thread.Sleep(5000);
            } while (currtry < maxtry);
            return bRet;
        }

        public Profile getCurrentProfile()
        {
            if (_profiles.Count == 0)
                readProfiles();
           
            int currentProfileInt = (int)Registry.GetValue(subKey, "ActiveProfile", -1);
            string currentProfileString = (string)Registry.GetValue(subKey, currentProfileInt.ToString(), "");

            foreach (Profile p in _profiles)
            {
                if (p.sProfileRekKey == currentProfileString)
                {
                    return p;
                }
            }
            return new Profile("", "", "", "1");
        }

        public bool setCurrentProfile(string sInstanceName)
        {
            if (getCurrentProfile().sProfileLabel == sInstanceName)
                return true;
            Logger.WriteLine("enableProfile: " + sInstanceName);
            int iRet = 0;
            StringBuilder sb = new StringBuilder(1024);
            int dSize = 1024;
            //set profile_x for profile with profileLabel
            string sXML = String.Format(setFUNKprofileXml, sInstanceName); //enable = 1, disabled=0, the xml say disabled instead of enabled !!!
            Logger.WriteLine(sXML);
            uint uError = _ssAPI.Set(sXML, sb, ref dSize, 2000);
            if (uError != ITCSSErrors.E_SS_SUCCESS)
            {
                Logger.WriteLine("SSAPI error: " + uError.ToString() + "\n");
                iRet = -1;
            }
            else
            {
                Logger.WriteLine("enableProfile success");
                iRet = 0;
            }
            return iRet==0;
        }

        public int readProfiles()
        {
            _profiles.Clear();
            List<string> lRet = new List<string>();
            //RegistryKey rKey = Registry.LocalMachine.OpenSubKey(@"Software\wpa_supplicant\configs\Intermec\networks",false);

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

                        string sDisabled = "1";
                        Profile funkProf;
                        if (sName == currentProfileString)
                        {
                            sDisabled="0";
                            Logger.WriteLine("## Current Profile: " + currentProfileString);
                        }

                        funkProf = new Profile(sProfileSubKey, sLabel, sSSID, sDisabled);
                        Logger.WriteLine("listFunkprofiles found: " + funkProf.ToString());
                        _profiles.Add(funkProf);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("Exception in listFunkProfiles read networks. " + ex.Message);
                }
            }
            //sort rac profiles by profile1 and profile2
            _profiles.Sort(new MobileConfiguration.myCompareString());

            return _profiles.Count;
        }
    }
}
