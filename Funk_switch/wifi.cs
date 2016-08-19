using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

//using OpenNETCF.Net;
using Intermec.DeviceManagement.SmartSystem;

namespace Funk_switch
{
    public class wifi:IDisposable
    {
        FunkProfiles _profiles;

        List<string> preferedSSIDs = new List<string>();

        MobileConfiguration _myConfig = new MobileConfiguration();

        /// <summary>
        /// not used with  FUNK
        /// </summary>
        List<NearAccesPoints> accesspointlist;
        ITCSSApi _ssAPI = null;

        public List<NearAccesPoints> _accesspoints
        {
            get
            {
                return accesspointlist;
            }
        }
        public class NearAccesPoints
        {
            public string SSID { get; set; }
            public int dbRSSI { get; set; }
            public int secured { get; set; }
            public NearAccesPoints(string ssid, int rssi, int secure){
                SSID=ssid;
                dbRSSI=rssi;
                secured=secure;
            }
        }

        public wifi()
        {
            _profiles = new FunkProfiles();
            accesspointlist = new List<NearAccesPoints>();
            
            start();
        }
        public void Dispose()
        {
            if (_profiles != null)
            {
                _profiles.Dispose();
                _profiles = null;
            }
            //_ssAPI.Dispose();
        }
        
        void start()
        {
            
            //_ssAPI.listRACprofiles();

            foreach (Profile R in _profiles.profiles)
            {
                preferedSSIDs.Add(R.sSSID);
                if (R.sProfileRegKey == _myConfig._profile1)
                    _SSIDprimary = R.sSSID;
                if (R.sProfileRegKey == _myConfig._profile2)
                    _SSIDseconday = R.sSSID;
                //preferedSSIDs.Add("SUPPORT");
                //preferedSSIDs.Add("INTERMEC");
            }

        }

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

        public bool getRadioEnabled()
        {
            if (_ssAPI == null)
                _ssAPI = new ITCSSApi();
            bool bRet = false;
            Logger.WriteLine("getRadioEnabled...");
            StringBuilder sb = new StringBuilder(1024);
            int dSize = 1024;
            string sXML = getRadioEnabledXml;
            Logger.WriteLine(sXML);
            uint uError = _ssAPI.Get(sXML, sb, ref dSize, 2000);
            if (uError != ITCSSErrors.E_SS_SUCCESS)
            {
                Logger.WriteLine("SSAPI error: " + uError.ToString() + "\n" + sb.ToString().Substring(0, dSize));
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
            }
            return bRet;
        }

        public int setRadioEnabled(bool bEnable)
        {
            //TODO: TEST
            //return 0;
            if (_ssAPI == null)
                _ssAPI = new ITCSSApi();

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

        public Profile getCurrentProfile()
        {
            return _profiles.getCurrentProfile();
        }

        public int setProfile(string sProfile)
        {
            if (_profiles.setCurrentProfile(sProfile))
                return 1;
            else
                return 0;
        }

        public List<Profile> wlanProfiles
        {
            get
            {
                return _profiles.profiles;
            }
        }

        string _SSIDprimary = "Intermec";
        /// <summary>
        /// return the SSID for the primary Profile
        /// </summary>
        public string SSIDprimary
        {
            get { return _SSIDprimary; }
        }
        string _SSIDseconday = "Intermec";
        /// <summary>
        /// return the SSID for the secondary Profile
        /// </summary>
        public string SSIDsecondary
        {
            get { return _SSIDseconday; }
        }

        public static bool isAssociated(string ssid)
        {
            Logger.WriteLine("Testing association against '" + ssid+"'");
            string getssid=getAssociatedAP();
            Logger.WriteLine("getAssociatedAP()='" + getssid + "'\n");
            if (getssid.Length == 0)
                return false;
            if (getssid == ssid)
                return true;
            else
                return false;
        }

        public static string getAssociatedAP()
        {
            int iStatus;
            uint uRes = Intermec.Communication.WLAN.WLAN80211API_v3.GetAssociationStatus(out iStatus);
            if (uRes != Intermec.Communication.WLAN.WLAN80211API_v3.CONST.ERR_SUCCESS)
                return "";
            string sSSID="";
            if (iStatus == Intermec.Communication.WLAN.WLAN80211API_v3.CONST.NDIS_RADIO_ASSOCIATED)
            {
                byte[] tszSsid = new byte[255];
                uRes = Intermec.Communication.WLAN.WLAN80211API_v3.GetSSID(tszSsid);
                if (uRes != Intermec.Communication.WLAN.WLAN80211API_v3.CONST.ERR_SUCCESS)
                    return "";
                List<byte> lBytes = new List<byte>();
                int i=0;
                while(i<254){
                    if (tszSsid[i] + tszSsid[i + 1] != 0)
                    {
                        lBytes.Add(tszSsid[i]);
                        lBytes.Add(tszSsid[i+1]);
                    }
                    else
                        break;
                    i+=2;
                }
                sSSID = Encoding.Unicode.GetString(lBytes.ToArray(), 0, lBytes.ToArray().Length);
                return sSSID;
            }
            else
                return "";

        }

        public void UpdateAdapters()
        {
            if (accesspointlist != null)
                accesspointlist.Clear();
            else
                accesspointlist = new List<NearAccesPoints>();
        }
    }
}
