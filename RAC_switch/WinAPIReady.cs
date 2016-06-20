using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;
using System.Threading;

namespace RAC_switch
{
    public class WinAPIReady:IDisposable
    {
        [Obsolete(@"use OpenEvent with registry HKLM\System\Events names")]
        [DllImport("coredll.dll")]
        static extern bool IsAPIReady(UInt32 hAPI);

        [DllImport("coredll.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr OpenEvent(uint desiredAccess, bool inheritHandle, string name);
        //const int EVENT_ALL_ACCESS = 0;
        static class EventAccess
        {
            public static uint SYNCHRONIZE { get { return 0x00100000; } }
            public static uint STANDARD_RIGHTS_REQUIRED { get { return 0x000F0000; } }
            public static uint EVENT_ALL_ACCESS { get { return STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x3; } }
        }
        /* see https://msdn.microsoft.com/en-us/library/aa450537.aspx
        [HKEY_LOCAL_MACHINE\System\Events]
        "SYSTEM/ShellInit"=""
        "SYSTEM/ShellAPIReady"=""
        "SDP_DATA_AVAILABLE"="SDP Data is available"
        "system/events/notify/APIReady"="Notifications API set ready"
        "SYSTEM/BatteryAPIsReady"="Battery Interface APIs"
        "SYSTEM/NLedAPIsReady"="Notification LED APIs"
        "LASS_SRV_STARTED"="LASS APIs ready"
        "SYSTEM/WZCApiSetReady"="Event triggered after WZC is ready to accept and process API calls"
        "SYSTEM/GweApiSetReady"="Event triggered after GWES registers its API sets"
        "SYSTEM/PowerManagerReady"="Power Manager APIs ready"
        "SYSTEM/DevMgrApiSetReady"="Device Manager APIs ready"
        */

        [DllImport("coredll", SetLastError = true)]
        internal static extern long WaitForMultipleObjects(int nCount, IntPtr[] lpHandles, bool fWaitAll, int dwMilliseconds);
        [DllImport("coredll", SetLastError = true)]
        internal static extern long WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);
        class WaitObjectReturnValue
        {
            public static long WAIT_OBJECT_0 {get{return 0x00000000L;}}
            public static long WAIT_TIMEOUT {get{return 0x000000102L;}}
            public static long WAIT_ABANDONED = 0x00000080L;
            public static long WAIT_FAILED = 0xffffffffL;
        }
        [DllImport("coredll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hObject);

        internal enum EVENT
        {
            PULSE = 1,
            RESET = 2,
            SET = 3,
        }

        [DllImport("coredll.dll", SetLastError = true)]
        internal static extern bool EventModify(IntPtr hEvent, EVENT ef);

        //[DllImport("coredll", SetLastError = true)]
        //internal static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("coredll.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        public static extern System.IntPtr CreateEvent(System.IntPtr lpEventAttributes, [In, MarshalAs(UnmanagedType.Bool)] bool bManualReset, [In, MarshalAs(UnmanagedType.Bool)] bool bIntialState, [In, MarshalAs(UnmanagedType.BStr)] string lpName);

        string[] eventNames = new string[] { 
            "SYSTEM/ShellInit", 
            "SYSTEM/ShellAPIReady", 
            "system/events/notify/APIReady", 
            "SYSTEM/GweApiSetReady", 
            "SYSTEM/PowerManagerReady", 
            "SYSTEM/DevMgrApiSetReady" };
        EventWaitHandle[] hEvents;

        string szStopThread = "stopWinAPIready";
        EventWaitHandle hStopThread = null;

        static IntPtr OpenSystemEvent(string name)
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = OpenEvent(EventAccess.EVENT_ALL_ACCESS, false, name);
                if (handle == IntPtr.Zero)
                {
                    Logger.WriteLine("OpenSystemEvent Error: " + Marshal.GetLastWin32Error().ToString());
                }
            }
            catch (Exception)
            {
            }
            return handle;
        }

        Thread _waitThread = null;
        bool _ApiIsReady=false;
        public bool ApiIsReay
        {
            get { return _ApiIsReady; }
        }

        public WinAPIReady()
        {
            //load handles
            hEvents = new EventWaitHandle[eventNames.Length];            
            for (int i = 0; i < eventNames.Length; i++)
            {
                hEvents[i] = new EventWaitHandle(eventNames[i]);
                Logger.WriteLine("Event '" + eventNames[i] + "' has handle " + hEvents[i].Handle.ToString());
            }
            hStopThread = new EventWaitHandle(false,EventResetMode.ManualReset, szStopThread);

            _waitThread = new Thread(new ThreadStart(myWorkerThread));
            _waitThread.Name = "myWaitThread";
            _waitThread.Start();
        }
        
        public void Dispose()
        {
            if (_waitThread != null)
            {
                if (hStopThread != null)
                {
                    hStopThread.Set();
                    Thread.Sleep(1500);
                }
                try
                {
                    _waitThread.Join(500);
                }
                catch (Exception)
                {
                }
                _waitThread.Abort();
            }

            if (hStopThread != null)
            {
                hStopThread.Reset();
                hStopThread = null;
            }
        }

        void myWorkerThread()
        {
            Logger.WriteLine("WinApiReady-myWorkerThread starting...");
            bool bStop=false;
            try
            {
                do
                {
                    bool uWaitStop = hStopThread.WaitOne(500, false);// WaitForSingleObject(hStopThread, 500);
                    if (uWaitStop)
                    {
                        bStop = true;
                        break;
                    }

                    //only first event is reported all the time!
                    long uWaitApiReady = EventWaitHandle.WaitAny(hEvents, 500, false);// WaitForMultipleObjects(hEvents.Length, hEvents, true, 500); //wait for all events being set
                    switch (uWaitApiReady)
                    {
                        case 0:
                            Logger.WriteLine("API " + eventNames[uWaitApiReady] + " is set");
                            break;
                        case 1:
                            Logger.WriteLine("API " + eventNames[uWaitApiReady] + " is set");
                            break;
                        case 2:
                            Logger.WriteLine("API " + eventNames[uWaitApiReady] + " is set");
                            break;
                        case 3:
                            Logger.WriteLine("API " + eventNames[uWaitApiReady] + " is set");
                            break;
                        case 4:
                            Logger.WriteLine("API " + eventNames[uWaitApiReady] + " is set");
                            break;
                        case 5:
                            Logger.WriteLine("API " + eventNames[uWaitApiReady] + " is set");
                            break;
                    }
                    //if (uWaitApiReady == WaitObjectReturnValue.WAIT_OBJECT_0)
                    //{ //all APIs set
                    //    _ApiIsReady = true;
                    //    OnApiChangeMessage(true);
                    //    bStop = true;
                    //}
                } while (!bStop);
            }
            catch (ThreadAbortException ex)
            {
                Logger.WriteLine("WinApiReady-myWorkerThread ThreadAbortException: " + ex.Message);
            }
            catch (Exception ex)
            {
                Logger.WriteLine("WinApiReady-myWorkerThread Exception: " + ex.Message);
            }
            Logger.WriteLine("WinApiReady-myWorkerThread ended.");
        }

        public delegate void apiChangeEvent(object sender, ApiEventArgs args);
        public event apiChangeEvent apiChangedEvent;
        public class ApiEventArgs : EventArgs
        {
            public bool _API_READY;
            public ApiEventArgs()
            {
                _API_READY=false;
            }
            public ApiEventArgs(bool ready)
            {
                _API_READY=ready;
            }
        }
        void OnApiChangeMessage(bool apiReady)
        {
            ApiEventArgs e = new ApiEventArgs(apiReady);
            Logger.WriteLine("Firing ApiReady event");
            if (apiChangedEvent != null)
            {
                apiChangedEvent.Invoke(this, e);
            }
        }
    }
}
