using System;
using System.Text;

using System.Runtime.InteropServices;

namespace RAC_switch
{
    class win32native
    {
        [DllImport("coredll.dll")]
        static extern int ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_MINIMIZED = 6;



        public static void Minimize(System.Windows.Forms.Form frm)
        {
            // The Taskbar must be enabled to be able to do a Smart Minimize
            frm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            frm.WindowState = System.Windows.Forms.FormWindowState.Normal;
            //frm.ControlBox = true;
            //frm.MinimizeBox = true;
            //frm.MaximizeBox = true;

            // Since there is no WindowState.Minimize, we have to P/Invoke ShowWindow
            ShowWindow(frm.Handle, SW_MINIMIZED);
        }
    }
}
