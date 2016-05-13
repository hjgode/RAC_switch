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

            [DllImport("coredll", SetLastError = true)]
            internal static extern bool CloseHandle(IntPtr hObject);

            [DllImport("coredll", SetLastError = true)]
            internal static extern int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

            [DllImport("coredll", SetLastError = true)]
            internal static extern int WaitForMultipleObjects(int nCount, IntPtr[] lpHandles, bool fWaitAll, int dwMilliseconds);
        }
    }
}
