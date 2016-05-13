using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Threading;

namespace RAC_switch
{
    public class connector:IDisposable
    {
        /// <summary>
        /// list of profile in order of priority!
        /// </summary>
        string[] _profiles;
        Thread _workThread=null;
        bool _bStopThread = false;

        itc_ssapi _ssRACapi;

        public connector(string[] profiles)
        {
            _profiles = profiles;
            OnConnecterMessage("connector initialized with profiles: ");
            try
            {
                _ssRACapi = new itc_ssapi();
            }
            catch (Exception ex)
            {
                throw new NotSupportedException("RAC not active");
            }
            foreach (string s in _profiles)
                OnConnecterMessage("\t" + s);

            _workThread = new Thread(new ThreadStart(myWorkerThread));
            _workThread.Start();

        }

        public void Dispose()
        {
            if (_workThread != null)
            {
                _bStopThread = true;
                Thread.Sleep(1000);
                if (_workThread != null) 
                    _workThread.Abort();
            }
        }


        void myWorkerThread()
        {
            OnConnecterMessage("myWorkerThread start");
            int iDelay = 0;
            int iConnectTry=0;
            batterylog2.BatteryStatusEx batteryStatus=new batterylog2.BatteryStatusEx();
            bool isDocked = batteryStatus._isACpowered;
            bool stopp = false;
            int indx = -1;
            EventWaitHandle[] handles = new EventWaitHandle[] { evtStopThreads, evtTime, evtDisconnect, evtPower, evtUndocked };
            try
            {
                do
                {
                    indx = EventWaitHandle.WaitAny(handles, 500, false);
                    switch (indx)
                    {
                        case 0:
                            stopp = true;
                            _bStopThread = true;
                            break;
                        case 1: //timer
                            break;
                        case 2: //disconnect
                            //try primary profile
                            //try secondary profile
                            break;
                        case 3: //PowerOn
                            //try primary profile
                            //try secondary profile
                            break;
                        case 4: //undocked
                            //try primary profile
                            //try secondary profile
                            break;
                        case EventWaitHandle.WAIT_FAILED:
                            break;
                        case EventWaitHandle.WAIT_TIMEOUT:
                            break;
                    }
#if DEBUG
                    if (iDelay == 2 * 5) //every 5 seconds
#else
                    if (iDelay == 2 * 30) //every 30 seconds
#endif
                    {
                        iDelay = 0;
                        //check dock/undock
#if DEBUG
                        if (batteryStatus._isACpowered)
#else
                        if (batteryStatus._isACpowered != isDocked)
#endif
                        {
                            OnConnecterMessage("docked change recognized");
                            //is first profile active?
                            string currentProfile=_ssRACapi.getCurrentProfile().sProfileLabel;
                            OnConnecterMessage("current profile="+currentProfile);
                            if (currentProfile == _profiles[0])
                            {
                                OnConnecterMessage("Current Profile = First profile");
                                if (network._getConnected() == false)
                                { //not connected
                                    OnConnecterMessage("network not connected. Switching profiles...");
                                    _ssRACapi.enableProfile(_profiles[0], false); //disable first profile
                                    _ssRACapi.enableProfile(_profiles[1], true); //enable second profile
                                }
                                else
                                {
                                    OnConnecterMessage("network connected. No profile change.");
                                }                                
                            }
                            else if (_ssRACapi.getCurrentProfile().sProfileLabel == _profiles[1])
                            {
                                OnConnecterMessage("Current Profile = Second profile");
                                if (network._getConnected() == false)
                                    OnConnecterMessage("secondary profile not connected");
                                else
                                    OnConnecterMessage("secondary profile connected");
                                //try first profile, regardless of connect state
                                OnConnecterMessage("Trying first Profile. Switching ...");
                                _ssRACapi.enableProfile(_profiles[1], false); //disable second profile
                                _ssRACapi.enableProfile(_profiles[0], true); //enable first profile
                                iConnectTry = 0;
                                while (!_bStopThread && iConnectTry < 10)
                                {
                                    Thread.Sleep(500);
                                    iConnectTry++;
                                    if (network._getConnected() == true)
                                        break;
                                }
                                if (network._getConnected() == false)
                                {
                                    OnConnecterMessage("First Profile did not connect. Switching to secondary profile...");
                                    //switch back
                                    _ssRACapi.enableProfile(_profiles[0], false); //enable first profile
                                    _ssRACapi.enableProfile(_profiles[1], true); //disable second profile
                                }
                                else
                                {
                                    OnConnecterMessage("primary network did connect.");
                                }
                            }
                            else
                            {
                                OnConnecterMessage("Current profile not in list!");
                            }
                        }
                        isDocked = batteryStatus._isACpowered;
                    }
                    Thread.Sleep(500);
                    iDelay++;
                } while (!_bStopThread);
            }
            catch (ThreadAbortException ex)
            {
                OnConnecterMessage("myWorkerThread ThreadAbortException: " + ex.Message);
            }
            catch (Exception ex)
            {
                OnConnecterMessage("myWorkerThread Exception: " + ex.Message);
            }
            OnConnecterMessage("myWorkerThread ended");
        }

        void connectWatchThread()
        {
            OnConnecterMessage("connectWatchThread starting");
            bool newState, oldState = true;
            WaitHandle[] handles = new WaitHandle[] { evtStopThreads };
            int indx = -1;
            bool stopp=false;
            try
            {
                do
                {
                    indx = EventWaitHandle.WaitAny(handles, 5000, false);
                    switch (indx)
                    {
                        case 0:
                            stopp = true;
                            OnConnecterMessage("connectWatchThread stopp signaled");
                            break;
                        case EventWaitHandle.WAIT_TIMEOUT:
                            OnConnecterMessage("connectWatchThread WAIT_TIMEOUT");
                            newState = network._getConnected();
                            if (!newState)
                            {
                                OnConnecterMessage("connectWatchThread fires Disconnect event");
                                //fire event
                                evtDisconnect.Set();
                            }
                            oldState = newState;
                            break;
                    }
                } while (!stopp);
            }
            catch (ThreadAbortException ex)
            {
                OnConnecterMessage("connectWatchThread ThreadAbortException: " + ex.Message);
            }
            catch (Exception ex)
            {
                OnConnecterMessage("connectWatchThread Exception: " + ex.Message);
            }
            OnConnecterMessage("connectWatchThread ended");
        }

        /// <summary>
        /// fire timer event periodically
        /// </summary>
        void timerThread()
        {
            OnConnecterMessage("timerThread started");
            EventWaitHandle[] handles=new EventWaitHandle[]{evtStopThreads};
            int indx = -1;
            bool stopp = false;
            try
            {
                do
                {
                    indx = EventWaitHandle.WaitAny(handles, 5000, false);
                    switch (indx)
                    {
                        case 0:
                            stopp = true;
                            OnConnecterMessage("timerThread stopp signaled");
                            break;
                        case EventWaitHandle.WAIT_FAILED:
                            OnConnecterMessage("timerThread WAIT_FAILED");
                            break;
                        case EventWaitHandle.WAIT_TIMEOUT:
                            OnConnecterMessage("timerThread fires evtTime");
                            evtTime.Set();
                            Thread.Sleep(1000);
                            evtTime.Reset();
                            break;
                    }
                } while (!stopp);
            }
            catch (ThreadAbortException ex)
            {
                OnConnecterMessage("timerThread ThreadAbortException: " + ex.Message);
            }
            catch (Exception ex)
            {
                OnConnecterMessage("timerThread Exception: " + ex.Message);
            }
            OnConnecterMessage("timerThread ended");
        }

        #region EVENTS
        /// <summary>
        /// event to let all waiting threads run one loop. Must be reset manually!
        /// </summary>
        EventWaitHandle evtTime = new EventWaitHandle(false, EventResetMode.ManualReset, "evtTime");
        EventWaitHandle evtDisconnect = new EventWaitHandle(false, EventResetMode.AutoReset, "evtDisconnect");
        EventWaitHandle evtPower = new EventWaitHandle(false, EventResetMode.AutoReset, "evtPower");
        EventWaitHandle evtUndocked = new EventWaitHandle(false, EventResetMode.AutoReset, "evtUndocked");
        /// <summary>
        /// even to stop all threads. Manually reset event!
        /// </summary>
        EventWaitHandle evtStopThreads = new EventWaitHandle(false, EventResetMode.AutoReset, "evtStopThreads");
        #endregion

        #region EvenHandling
        public delegate void connectorChangeEventHandler(object sender, ConnectorEventArgs args);
        public event connectorChangeEventHandler connectorChangedEvent;
        public class ConnectorEventArgs : EventArgs
        {
            public string message;
            public ConnectorEventArgs()
            {
            }
            public ConnectorEventArgs(string msg)
            {
                message = msg;
            }
        }
        void OnConnecterMessage(string msg)
        {
            ConnectorEventArgs e = new ConnectorEventArgs(msg);
            Logger.WriteLine(e.message);
            if (connectorChangedEvent != null)
            {
                connectorChangedEvent.Invoke(this,e);
            }
        }
        #endregion
    }

}
