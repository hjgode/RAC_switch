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
                    System.IO.FileInfo fi = new FileInfo(sLogFile);
                    if (fi.Length > 2000000)
                    {
                        System.IO.File.Move(sLogFile, sLogFile + ".bak");
                    }
                    using(StreamWriter sw = new StreamWriter(sLogFile, true)){
                        sw.WriteLine(timestamp + "\t" + s);
                    }
                }catch(Exception ex){
                    System.Diagnostics.Debug.WriteLine("RAC_switch Exception: " + s + "\r\n" +ex.Message);
                }
            }
        }
        static string timestamp
        {
            get
            {
                DateTime now = DateTime.Now;
                return String.Format("{0:04}{1:02}{2:02} {3:02}{4:02}{5:02}",
                    now.Year, now.Month, now.Day,
                    now.Hour, now.Minute, now.Second);
            }
        }
    }
}
