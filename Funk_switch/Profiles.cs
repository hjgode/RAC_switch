using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Funk_switch
{
    public class Profile
    {
        string _regKey;
        string _profileLabel;
        string _ssid;
        string _enabled;

        /// <summary>
        /// registry key where the profile is located
        /// </summary>
        public string sProfileRegKey
        {
            get { return _regKey; }
        }

        [Obsolete("Do NOT USE!")]
        /// <summary>
        /// The label used for the profile
        /// </summary>
        public string sProfileLabel
        {
            get { return _profileLabel; }
        }
        /// <summary>
        /// the SSID defined in the profile
        /// </summary>
        public string sSSID
        {
            get { return _ssid; }
        }
        /// <summary>
        /// string "0" or "1" for this profile being enabled
        /// </summary>
        public string sEnabled
        {
            get { return _enabled; }
        }
        /// <summary>
        /// bool for profile is enabled
        /// </summary>
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
        /// <summary>
        /// create a new profile object
        /// </summary>
        /// <param name="profileRegKey"></param>
        /// <param name="profilelabel"></param>
        /// <param name="ssid"></param>
        /// <param name="disabled"></param>
        public Profile(string profileRegKey, string profilelabel, string ssid, string disabled)
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
}
