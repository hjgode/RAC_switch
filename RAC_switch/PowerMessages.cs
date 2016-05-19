using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;
using System.Threading;

using OpenNETCF.WindowsCE;

namespace RAC_switch
{
    public class PowerMessages:IDisposable
    {

        public PowerMessages()
        {
            OpenNETCF.WindowsCE.PowerManagement.PowerUp += new DeviceNotification(PowerManagement_PowerUp);
            OpenNETCF.WindowsCE.PowerManagement.PowerIdle += new DeviceNotification(PowerManagement_PowerIdle);
            OpenNETCF.WindowsCE.PowerManagement.PowerDown += new DeviceNotification(PowerManagement_PowerDown);
        }

        void PowerManagement_PowerDown()
        {
            Logger.WriteLine("got PowerManagement_PowerDown event");
            OnPowerMessage("got PowerManagement_PowerDown event", false);
        }

        void PowerManagement_PowerIdle()
        {
            Logger.WriteLine("got PowerManagement_PowerIdle event");
            OnPowerMessage("got PowerManagement_PowerIdle event", false);
        }

        void PowerManagement_PowerUp()
        {
            Logger.WriteLine("got powerup event");
            OnPowerMessage("got powerup event", true);
        }

        public void Dispose()
        {
        }

        public delegate void powerChangeEventHandler(object sender, PowerEventArgs args);
        public event powerChangeEventHandler powerChangedEvent;
        public class PowerEventArgs : EventArgs
        {
            public string message;
            public bool powerOn;
            public PowerEventArgs()
            {
            }
            public PowerEventArgs(string msg, bool bPower)
            {
                message = msg;
                powerOn = bPower;
            }
        }
        void OnPowerMessage(string msg, bool powerOn)
        {
            PowerEventArgs e = new PowerEventArgs(msg, powerOn);
            Logger.WriteLine(e.message);
            if (powerChangedEvent != null)
            {
                powerChangedEvent.Invoke(this, e);
            }
        }
    }
}
