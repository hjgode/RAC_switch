using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Funk_switch
{
    public interface IProfiles
    {
        List<Profile> profiles
        {
            get;
        }
        int readProfiles();
        Profile getCurrentProfile();
        bool setCurrentProfile(string sLabel);
    }
}
