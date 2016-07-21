using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Xml;
using System.Reflection;
using System.Collections.Specialized;
using System.Collections;

namespace Funk_switch
{
    class MobileConfiguration
    {
        public class myCompareString:IComparer<Profile>
        {
            // TODO: Comparison logic :)
            MobileConfiguration _myConfig=null;
            List<string> orderedStrings=null;

            public myCompareString()
            {
                _myConfig = new MobileConfiguration();
                orderedStrings = new List<string>();
                orderedStrings.Add(_myConfig._profile1);
                orderedStrings.Add(_myConfig._profile2);
            }
            public int Compare(Profile x, Profile y)
            {
                int xi = orderedStrings.IndexOf(x.sProfileLabel); // -1 if not found!
                int yi = orderedStrings.IndexOf(y.sProfileLabel);
                if (xi > yi)
                    return 1;
                if (xi < yi)
                    return -1;
                return 0;
            }
        }

        static NameValueCollection _defaultSettings = new NameValueCollection();
        static NameValueCollection defaultConfig
        {
            get
            {
                _defaultSettings.Clear();
                _defaultSettings.Add("profile1", "SUPPORT");
                _defaultSettings.Add("profile2", "Intermec");
                _defaultSettings.Add("checkOnUndock", false.ToString());
                _defaultSettings.Add("checkOnResume", true.ToString());
                _defaultSettings.Add("switchTimeout", "30");
                _defaultSettings.Add("checkConnectIP", "false");
                _defaultSettings.Add("enableLogging", false.ToString());
                _defaultSettings.Add("switchOnDisconnect", false.ToString());
                return _defaultSettings;
            }
        }

        public bool _checkConnectIP
        {
            get
            {
                readConfig();
                bool chk_connect = false;
                try
                {
                    chk_connect = bool.Parse(_Settings["checkConnectIP"]);
                }
                catch (Exception) { }
                return chk_connect;
            }
        }
        public string _profile1
        {
            get
            {
                readConfig();
                return _Settings["profile1"];
            }
        }
        public string _profile2
        {
            get
            {
                readConfig();
                return _Settings["profile2"];
            }
        }
        public bool _checkOnUndock
        {
            get
            {
                readConfig();
                return bool.Parse(_Settings["checkOnUndock"]);
            }
        }
        public bool _checkOnResume
        {
            get
            {
                readConfig();
                return bool.Parse(_Settings["checkOnResume"]);
            }
        }
        public bool _switchOnDisconnect
        {
            get
            {
                readConfig();
                return bool.Parse(_Settings["switchOnDisconnect"]);
            }
        }
        public int _switchTimeout{
            get
            {
                readConfig();
                return int.Parse(_Settings["switchTimeout"]);
            }
        }
        public bool _enableLogging
        {
            get
            {
                readConfig();
                return bool.Parse(_Settings["enableLogging"]);
            }
        }

        // usage= string profile1 = MobileConfiguration.Settings["profile1"];
        static NameValueCollection _Settings=null;

        public MobileConfiguration()
        {
            readConfig();
        }

        void readConfig()
        {
            if (_Settings != null)
                return;
            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            string configFile = Path.Combine(appPath, "App.config");

            if (!File.Exists(configFile))
            {
                _Settings = defaultConfig;
                Logger.WriteLine(string.Format("Application configuration file '{0}' not found.", configFile));
            }

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(configFile);
            XmlNodeList nodeList = xmlDocument.GetElementsByTagName("appSettings");
            _Settings = new NameValueCollection();

            foreach (XmlNode node in nodeList)
            {
                foreach (XmlNode key in node.ChildNodes)
                {
                    _Settings.Add(key.Attributes["key"].Value, key.Attributes["value"].Value);
                }
            }
        }

        //static void readSettings(){
        //    _profile1 = _Settings["profile1"];
        //    _profile2 = _Settings["profile2"];
        //    _checkOnUndock=bool.Parse(_Settings["checkOnUndock"]);
        //    _checkOnResume = bool.Parse(_Settings["checkOnResume"]);
        //    _switchTimeout = int.Parse(_Settings["switchTimeout"]);
        //}
    }
}
