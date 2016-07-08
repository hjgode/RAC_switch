using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;
using System.Threading;

namespace RAC_switch
{
    #region Event Reset Mode

    public enum EventResetMode
    {
        AutoReset = 0,
        ManualReset = 1,
    }

    #endregion

    class EventWaitHandle : WaitHandle
    {
        public const int WaitTimeout = 250;
        public const Int32 WAIT_FAILED = -1;
        public const Int32 WAIT_TIMEOUT = 0x102;

        public EventWaitHandle(bool initialState, EventResetMode mode, string name)
        {
            this.Handle = NativeMethods.CreateEvent(IntPtr.Zero, mode == EventResetMode.ManualReset, initialState, name);
        }

        /// <summary>
        /// create a handle to existing system event
        /// </summary>
        /// <param name="name">name of system event</param>
        public EventWaitHandle(string name)
        {
            try
            {
                this.Handle = NativeMethods.OpenEvent(NativeMethods.EventAccess.EVENT_ALL_ACCESS, false, name);
                if (this.Handle == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("EventWaitHandle: OpenSystemEvent Error: " + Marshal.GetLastWin32Error().ToString());
                }
            }
            catch (Exception)
            {
                this.Handle = IntPtr.Zero;
            }
        }

        public bool Set()
        {
            return NativeMethods.EventModify(this.Handle, NativeMethods.EVENT.SET);
        }

        public bool Reset()
        {
            return NativeMethods.EventModify(this.Handle, NativeMethods.EVENT.RESET);
        }

        public static int WaitAny(WaitHandle[] waitHandles)
        {
            return WaitAny(waitHandles, Timeout.Infinite, false);
        }

        public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            IntPtr[] handles = new IntPtr[waitHandles.Length];
            for (int i = 0; i < handles.Length; i++)
            {
                handles[i] = waitHandles[i].Handle;
            }

            return NativeMethods.WaitForMultipleObjects(handles.Length, handles, false, millisecondsTimeout);
        }

        //[Obsolete("NOT SUPPORTED on Windows CE")]
        public static bool WaitAll(WaitHandle[] waitHandles, int secondsTimeout, bool exitContext)
        {
            IntPtr[] handles = new IntPtr[waitHandles.Length];
            for (int i = 0; i < handles.Length; i++)
            {
                handles[i] = waitHandles[i].Handle;
            }
            int maxTries = secondsTimeout;// 30;
            bool bAllSet = false;
            do
            {
                int iWaitResult = NativeMethods.WaitForMultipleObjects(handles.Length, handles, false, 1000);
                //WaitForMultipleObjects will be released for any handle being signaled
                //the call will release for the first set event in the array and this will repeat
                //need to iterate thru all events to check if all events are set.
                //iWaitResult will indicate the position of the signaled event inside the array
                if (iWaitResult == WAIT_FAILED)
                    System.Diagnostics.Debug.WriteLine("WaitAll FAIL");
                if (iWaitResult == WAIT_TIMEOUT)
                    System.Diagnostics.Debug.WriteLine("WaitAll timed out");

                if (iWaitResult < handles.Length)
                {
                    System.Diagnostics.Debug.WriteLine("WaitAll released for event handle " + handles[iWaitResult].ToString());
                    //create temp List
                    List<IntPtr> temp = new List<IntPtr>(handles);
                    //remove handle from List
                    System.Diagnostics.Debug.WriteLine("WaitAll removed signaled event handle");
                    temp.Remove(handles[iWaitResult]);
                    //convert List back to array
                    handles = temp.ToArray();
                    System.Diagnostics.Debug.WriteLine("WaitAll: num of remaining event handles: "+temp.Count.ToString());
                    if (temp.Count == 0)
                    {
                        bAllSet = true;
                        System.Diagnostics.Debug.WriteLine("WaitAll all event handles signaled");
                        break;
                    }
                }
                maxTries--;
            } while (handles.Length > 0 && maxTries!=0 && bAllSet==false);
            if(maxTries==0)
                System.Diagnostics.Debug.WriteLine("WaitAll timed out - not all event handles signaled");
            return bAllSet;

            //throw new NotSupportedException("WaitForMultipleObjects with fWaitAll=true is NOT SUPPORTED on Windows CE");
            //return NativeMethods.WaitForMultipleObjects(handles.Length, handles, true, millisecondsTimeout);
        }


        public override bool WaitOne()
        {
            return WaitOne(Timeout.Infinite, false);
        }

        public override bool WaitOne(int millisecondsTimeout, bool exitContext)
        {
            return NativeMethods.WaitForSingleObject(this.Handle, millisecondsTimeout) == 0;
        }


        public override void Close()
        {
            if (this.Handle != WaitHandle.InvalidHandle)
            {
                NativeMethods.CloseHandle(this.Handle);
                this.Handle = WaitHandle.InvalidHandle;
            }
        }

        internal static class NativeMethods
        {
            internal enum EVENT
            {
                PULSE = 1,
                RESET = 2,
                SET = 3,
            }

            [DllImport("coredll.dll", SetLastError = true)]
            internal static extern bool EventModify(IntPtr hEvent, EVENT ef);

            [DllImport("coredll", SetLastError = true)]
            internal static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

            [DllImport("coredll.dll", SetLastError = true)]
            internal static extern IntPtr OpenEvent(uint desiredAccess, bool inheritHandle, string name);

            internal static class EventAccess
            {
                public static uint SYNCHRONIZE { get { return 0x00100000; } }
                public static uint STANDARD_RIGHTS_REQUIRED { get { return 0x000F0000; } }
                public static uint EVENT_ALL_ACCESS { get { return STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x3; } }
            }

            [DllImport("coredll", SetLastError = true)]
            internal static extern bool CloseHandle(IntPtr hObject);

            [DllImport("coredll", SetLastError = true)]
            internal static extern int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

            [DllImport("coredll", SetLastError = true)]
            internal static extern int WaitForMultipleObjects(int nCount, IntPtr[] lpHandles, bool fWaitAll, int dwMilliseconds);
        }
    }
}
