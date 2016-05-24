#define USE_OLD_SDF

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using OpenNETCF.Net;


namespace RAC_switch
{
    public class wifi:IDisposable
    {
        itc_ssapi _ssAPI = new itc_ssapi();
#if USE_OLD_SDF
        AdapterCollection m_adapters;
#else
        OpenNETCF.Net.NetworkInformation.NetworkInterface[] m_adapters;
#endif
        List<string> preferedSSIDs = new List<string>();

        List<NearAccesPoints> accesspointlist;

        public List<NearAccesPoints> _accesspoints
        {
            get
            {
                UpdateAdapters();
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
            if (!isRACclient())
            {
                throw new NotSupportedException("No RAC client installation");
            }
            accesspointlist = new List<NearAccesPoints>();
            
            start();
        }
        public void Dispose()
        {
            _ssAPI.Dispose();
        }
        
        bool isRACclient()
        {
            if (System.IO.File.Exists(@"\windows\WPA_Configlet.dll"))
                return true;
            else
                return false;
        }

        void doAutomateSwitch()
        {
        }

        void start()
        {
            
            //_ssAPI.listRACprofiles();

            foreach (itc_ssapi.racProfile R in _ssAPI._racProfiles)
            {
                preferedSSIDs.Add(R.sSSID);
                //preferedSSIDs.Add("SUPPORT");
                //preferedSSIDs.Add("INTERMEC");
            }

            //_ssAPI.setRACprofile("INTERMEC");

            //_ssAPI.listRACprofiles();

            UpdateAdapters();
        }

        public int setRAC(string sProfile)
        {
            return _ssAPI.setRACprofile(sProfile);
        }

        public List<itc_ssapi.racProfile> racProfiles
        {
            get
            {
                return _ssAPI._racProfiles;
            }
        }

        public static string getAssociatedAP()
        {
            string currentSSID = "";
            OpenNETCF.Net.NetworkInformation.NetworkInterface[] iFaces = (OpenNETCF.Net.NetworkInformation.NetworkInterface[])OpenNETCF.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (OpenNETCF.Net.NetworkInformation.NetworkInterface ni in iFaces)
            {
                if (ni.NetworkInterfaceType == OpenNETCF.Net.NetworkInformation.NetworkInterfaceType.Wireless80211 ||
                    ni.NetworkInterfaceType == OpenNETCF.Net.NetworkInformation.NetworkInterfaceType.Ethernet)
                {
                    if (ni.Name == "TIWLN1")
                    {
                        OpenNETCF.Net.NetworkInformation.WirelessNetworkInterface wIface = (OpenNETCF.Net.NetworkInformation.WirelessNetworkInterface)ni;
                        currentSSID = wIface.AssociatedAccessPoint.Trim();
                        Logger.WriteLine("Associated with: " + currentSSID);
                        break;
                    }
                }
            }
            return currentSSID;
        }

#if USE_OLD_SDF
        public void UpdateAdapters()
        {
            // Get the available adapters
            m_adapters = Networking.GetAdapters();
            accesspointlist.Clear();

            // Add the adapters
            foreach (Adapter adapter in m_adapters)
            {
                Logger.WriteLine("Adapter: " + adapter.Name);
                adapter.RebindAdapter();
                if (adapter.Name == "TIWLN1")
                {
                    Logger.WriteLine("found TIWLN1");
                    Logger.WriteLine("Associated with: " + adapter.AssociatedAccessPoint);
                    if (preferedSSIDs.Contains(adapter.AssociatedAccessPoint))
                    {
                        Logger.WriteLine("device already connected to preferred SSID");
                        continue; //exit foreach adapter
                    }
                    foreach (AccessPoint ap in adapter.NearbyAccessPoints)
                    {
                        //Logger.WriteLine("AP: " + ap.Name +":"+ ap.SignalStrengthInDecibels.ToString() + ", " + 
                        //    ap.InfrastructureMode.ToString() + ", " + ap.Privacy.ToString());
                        
                        accesspointlist.Add(new NearAccesPoints(ap.Name, ap.SignalStrengthInDecibels, ap.Privacy));
                        
                        Logger.WriteLine("Found AP: " + ap.Name);
                        
                        foreach (string s in preferedSSIDs)
                        {
                            if (ap.Name == s) // && adapter.AssociatedAccessPoint != s) 
                            {                                                       
                                //a preferred AP is in sight and we are not connected
                                //switch network?
                                Logger.WriteLine("will switch to '" + s + "'");
                                _ssAPI.setRACprofile(s);//switch
                            }
                        }//list of preferred APs
                    }//access points
                }// TIWLN1
            }//adapters
        }
#else
        public void UpdateAdapters()
        {
            // Get the available adapters
            m_adapters = (OpenNETCF.Net.NetworkInformation.NetworkInterface[])OpenNETCF.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            accesspointlist.Clear();

            // Add the adapters
            foreach (OpenNETCF.Net.NetworkInformation.NetworkInterface adapter in m_adapters)
            {
                Logger.WriteLine("Adapter: " + adapter.Name);
                if (adapter.Name == "TIWLN1")
                {
                    Logger.WriteLine("found TIWLN1");
                    if (adapter.NetworkInterfaceType == OpenNETCF.Net.NetworkInformation.NetworkInterfaceType.Wireless80211)
                        ;
                    adapter.bind();
                    //the following may work or not
                    OpenNETCF.Net.NetworkInformation.WirelessNetworkInterface wni=(OpenNETCF.Net.NetworkInformation.WirelessNetworkInterface)adapter;
                    string currentAP=wni.AssociatedAccessPoint;
                    Logger.WriteLine("Associated with: " + currentAP);
                    accesspointlist.Add(new NearAccesPoints(wni.Name, wni.SignalStrength.Decibels, (int)(wni.WEPStatus)));
                    if (preferedSSIDs.Contains(currentAP))
                    {
                        Logger.WriteLine("device already connected to defined SSID");
                        continue; //exit foreach adapter
                    }
                }// TIWLN1
            }//adapters
        }
#endif
    }
}
