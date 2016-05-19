using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Threading;
using System.Runtime.InteropServices;

namespace RAC_switch
{
    public class PowerSourceChanges:IDisposable
    {
        ACLineStatus oldStatus = ACLineStatus.AC_LINE_UNKNOWN;
        System.Threading.Timer _timer;

        public PowerSourceChanges()
        {
            TimerCallback callBack = new TimerCallback(BatteryCheck);
#if DEBUG
            _timer = new Timer(callBack, new object(), 1000, 5000);
#else
            _timer = new Timer(callBack, new object(), 1000, 20000);
#endif
        }

        public void Dispose()
        {
            //stop timer
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        
        void BatteryCheck (object state)
        {
            SYSTEM_POWER_STATUS_EX status=new SYSTEM_POWER_STATUS_EX();
            if (GetSystemPowerStatusEx(status, true))
            {
                if (status.ACLineStatus!= oldStatus)
                {
                    OnPowerMessage("power source: " + status.ACLineStatus.ToString(), status.ACLineStatus == ACLineStatus.AC_LINE_ONLINE ? true : false);
                    oldStatus = status.ACLineStatus;
                }
            }
        }

        public enum ACLineStatus : byte
        {
            AC_LINE_OFFLINE  = 0x00,
            AC_LINE_ONLINE  = 0x01,
            AC_LINE_BACKUP_POWER  = 0x02,
            AC_LINE_UNKNOWN  = 0xFF,
        }

        enum BatteryFlag:uint{
            BATTERY_FLAG_HIGH  = 0x01,
            BATTERY_FLAG_LOW  = 0x02,
            BATTERY_FLAG_CRITICAL  = 0x04,
            BATTERY_FLAG_CHARGING  = 0x08,
            BATTERY_FLAG_NO_BATTERY  = 0x80,
            BATTERY_FLAG_UNKNOWN  = 0xFF,
            BATTERY_PERCENTAGE_UNKNOWN  = 0xFF,
        }

        public delegate void powerChangeEventHandler(object sender, PowerEventArgs args);
        public event powerChangeEventHandler powerChangedEvent;
        public class PowerEventArgs : EventArgs
        {
            public string message;
            public bool docked;
            public PowerEventArgs()
            {
            }
            public PowerEventArgs(string msg, bool bDocked)
            {
                message = msg;
                docked = bDocked;
            }
        }
        void OnPowerMessage(string msg, bool bDocked)
        {
            PowerEventArgs e = new PowerEventArgs(msg, bDocked);
            Logger.WriteLine(e.message);
            if (powerChangedEvent != null)
            {
                powerChangedEvent.Invoke(this, e);
            }
        }

        [DllImport("coredll.dll")]
        static extern bool GetSystemPowerStatusEx(SYSTEM_POWER_STATUS_EX pStatus, bool fUpdate);

        [StructLayout(LayoutKind.Sequential)]
        internal class SYSTEM_POWER_STATUS_EX
        {
            public ACLineStatus ACLineStatus = 0;
            public byte BatteryFlag = 0;
            public byte BatteryLifePercent = 0;
            public byte Reserved1 = 0;
            public uint BatteryLifeTime = 0;
            public uint BatteryFullLifeTime = 0;
            public byte Reserved2 = 0;
            public byte BackupBatteryFlag = 0;
            public byte BackupBatteryLifePercent = 0;
            public byte Reserved3 = 0;
            public uint BackupBatteryLifeTime = 0;
            public uint BackupBatteryFullLifeTime = 0;
        }
    }
}
