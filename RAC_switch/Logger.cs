using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.IO;

namespace RAC_switch
{
    public class Logger
    {
        public static bool bEnableLogging = true;
        public static string sLogFile="\\RAC_switch.log.txt";
        public static void WriteLine(string s)
        {
            System.Diagnostics.Debug.WriteLine(s);
            if (bEnableLogging)
            {
                try{
                    using(StreamWriter sw = new StreamWriter(sLogFile, true)){
                        sw.WriteLine(DateTime.Now.ToShortTimeString() + "\t" + s);
                    }
                }catch(Exception ex){
                    System.Diagnostics.Debug.WriteLine("RAC_switch Exception: " + s + "\r\n" +ex.Message);
                }
            }
        }
    }
}
