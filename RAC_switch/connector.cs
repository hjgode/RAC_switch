﻿using System;
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
        object syncObject = new object();
        bool bInsideSwitch = false;

        bool _bStopThread = false;
        Thread _workThread=null;
        Thread _connectThread = null;
        Thread _timerThread = null;
        //PowerMessages _powerMessages = null;
        PowerSourceChanges _PowerSourceMessages = null;
        PowerMessages _PowerMessages = null;

        itc_ssapi _ssRACapi;

        #region EVENTS
        /// <summary>
        /// event signaled periodically, used by
        /// </summary>
        EventWaitHandle evtTime = new EventWaitHandle(false, EventResetMode.AutoReset, "evtTime");
        EventWaitHandle evtDisconnect = new EventWaitHandle(false, EventResetMode.AutoReset, "evtDisconnect");
        EventWaitHandle evtPower = new EventWaitHandle(false, EventResetMode.AutoReset, "evtPower");
        EventWaitHandle evtUndocked = new EventWaitHandle(false, EventResetMode.AutoReset, "evtUndocked");
        /// <summary>
        /// even to stop all threads. Manually reset event!
        /// </summary>
        EventWaitHandle evtStopThreads = new EventWaitHandle(false, EventResetMode.ManualReset, "evtStopThreads");
        #endregion

        public connector(string[] profiles)
        {
            _profiles = profiles;
            evtStopThreads.Reset(); // clear event

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
            _workThread.Name = "myWorkerThread";
            _workThread.Start();

            _connectThread = new Thread(new ThreadStart(connectWatchThread));
            _connectThread.Name = "connectWatchThread";
            _connectThread.Start();

            _timerThread=new Thread(new ThreadStart(timerThread));
            _timerThread.Name = "timerThread";
            _timerThread.Start();

            _PowerSourceMessages = new PowerSourceChanges();
            _PowerSourceMessages.powerChangedEvent += new PowerSourceChanges.powerChangeEventHandler(_powerSourceMessages_powerChangedEvent);

            _PowerMessages = new PowerMessages();
            _PowerMessages.powerChangedEvent += new PowerMessages.powerChangeEventHandler(_PowerMessages_powerChangedEvent);
        }

        void _PowerMessages_powerChangedEvent(object sender, PowerMessages.PowerEventArgs args)
        {
            if(args.powerOn)
                evtPower.Set();            
        }


        void _powerSourceMessages_powerChangedEvent(object sender, PowerSourceChanges.PowerEventArgs args)
        {
            if(!(args.docked))
                evtUndocked.Set();            
        }

        public void Dispose()
        {
            if (_workThread != null)
            {
                evtStopThreads.Set(); // stop all threads
                _bStopThread = true;
                Thread.Sleep(2000);
                if (_workThread != null) 
                    _workThread.Abort();
                if (_connectThread != null)
                    _connectThread.Abort();
                if (_timerThread != null)
                    _timerThread.Abort();
                if (_PowerSourceMessages != null)
                    _PowerSourceMessages.Dispose();
            }
        }

        public void trySwitch()
        {
            doSwitch();
        }

        /// <summary>
        /// this function tries to switch to the preferred network
        /// </summary>
        void doSwitch()
        {
            if (bInsideSwitch)
            {
                Logger.WriteLine("doSwitch called although already insideSwitch");
                return;
            }
            lock (syncObject)
                bInsideSwitch=true;

            int iConnectTry = 0;
            //is first profile active?
            string currentProfile = _ssRACapi.getCurrentProfile().sProfileLabel;
            OnConnecterMessage("current profile=" + currentProfile);
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
                //try for 40 seconds or so
                while (!_bStopThread && iConnectTry < 30)
                {
                    Thread.Sleep(1000);
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

            lock (syncObject)
                bInsideSwitch = false;
        }

        /// <summary>
        /// main worker thread
        /// can be 'released' by
        ///  timer event
        ///  stop event
        ///  disconnect event
        ///  power event
        ///  dock event
        /// end then calls other functions
        /// </summary>
        void myWorkerThread()
        {
            OnConnecterMessage("myWorkerThread start");
            batterylog2.BatteryStatusEx batteryStatus=new batterylog2.BatteryStatusEx();
            bool isDocked = batteryStatus._isACpowered;
            bool stopp = false;
            int indx = -1;
            EventWaitHandle[] handles = new EventWaitHandle[] { evtStopThreads, evtTime, evtDisconnect, evtPower, evtUndocked };
            try
            {
                do
                {
                    indx = EventWaitHandle.WaitAny(handles, 5000, false);
                    switch (indx)
                    {
                        case 0:
                            OnConnecterMessage("myWorkerThread stopp signaled");
                            stopp = true;
                            _bStopThread = true;
                            break;
                        case 1: //timer
                            OnConnecterMessage("myWorkerThread timer signaled");
                            break;
                        case 2: //disconnect
                            OnConnecterMessage("myWorkerThread disconnect signaled");
                            //try primary profile
                            //try secondary profile
                            doSwitch();
                            break;
                        case 3: //PowerOn
                            OnConnecterMessage("myWorkerThread powerOn signaled");
                            //try primary profile
                            //try secondary profile
                            doSwitch();
                            break;
                        case 4: //undocked
                            OnConnecterMessage("myWorkerThread undocked signaled");
                            //try primary profile
                            //try secondary profile
                            doSwitch();
                            break;
                        case EventWaitHandle.WAIT_FAILED:
                            break;
                        case EventWaitHandle.WAIT_TIMEOUT:
                            //OnConnecterMessage("myWorkerThread WAIT_TIMEOUT");
                            break;
                    }
                } while (!stopp);
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

        /// <summary>
        /// this thread checks for an existing connection
        /// will fire disconnect event on disconnect
        /// </summary>
        void connectWatchThread()
        {
            OnConnecterMessage("connectWatchThread starting");
            bool newState, oldState = true;
            WaitHandle[] handles = new WaitHandle[] { evtStopThreads };
            int indx = -1;
            bool stopp=false;
            bool bRadioPower = false;
            string associatedAP = "";
            bool bDoNotCheck=false;

            try
            {
                do
                {
#if DEBUG
                    indx = EventWaitHandle.WaitAny(handles, 20000, false);
#else
                    indx = EventWaitHandle.WaitAny(handles, 60000, false);
#endif
                    switch (indx)
                    {
                        case 0:
                            stopp = true;
                            OnConnecterMessage("connectWatchThread stopp signaled");
                            break;
                        case EventWaitHandle.WAIT_TIMEOUT:
                            lock (syncObject)
                            {
                                bDoNotCheck = bInsideSwitch;
                            }
                            if (bDoNotCheck)
                            {
                                OnConnecterMessage("connectWatchThread does not check!");
                                break;
                            }
                            //OnConnecterMessage("connectWatchThread WAIT_TIMEOUT");
                            bRadioPower = _ssRACapi.getRadioEnabled();
                            if (!bRadioPower){
                                OnConnecterMessage("Radio is disabled!");
                                break;
                            }
                            associatedAP=wifi.getAssociatedAP();
                            OnConnecterMessage("connectWatchThread: associatedAP AP=" + associatedAP);
                            if (associatedAP == this._ssRACapi._racProfiles[0].sSSID)
                            {
                                OnConnecterMessage("connectWatchThread: Radio already connected to preferred profile " + _ssRACapi._racProfiles[0].sProfileLabel);
                                break;
                            }
                            newState = network._getConnected();
                            if (!newState)
                            {
                                OnConnecterMessage("connectWatchThread: fire Disconnect event");
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
        /// fires timer event periodically
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
                    indx = EventWaitHandle.WaitAny(handles, 30000, false);
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
                            //Thread.Sleep(1000);
                            //evtTime.Reset();
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
