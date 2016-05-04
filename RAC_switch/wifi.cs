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

        AdapterCollection m_adapters;

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
            
            preferedSSIDs.Add("SUPPORT");
            preferedSSIDs.Add("INTERMEC");
            
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
            UpdateAdapters();
            
            _ssAPI.listRACprofiles();

            //_ssAPI.setRACprofile("INTERMEC");

            //_ssAPI.listRACprofiles();
        }

        public int setRAC(string sSSID)
        {
            return _ssAPI.setRACprofile(sSSID);
        }

        public List<itc_ssapi.racProfile> racProfiles
        {
            get
            {
                return _ssAPI.racProfiles;
            }
        }

        public void UpdateAdapters()
        {
            // Get the available adapters
            m_adapters = Networking.GetAdapters();
            accesspointlist.Clear();

            // Add the adapters
            foreach (Adapter adapter in m_adapters)
            {
                Logger.WriteLine("Adapter: " + adapter.Name);
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
    }
}
