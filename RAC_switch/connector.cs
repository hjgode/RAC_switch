using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Threading;

namespace RAC_switch
{
    public class connector:IDisposable
    {
        MobileConfiguration _myConfig = new MobileConfiguration();

        #region CONFIG_VALUES
        /// <summary>
        /// list of profile in order of priority!
        /// </summary>
        string[] _profiles;
        bool _bcheckOnResume = true;
        bool _bcheckOnUndock = false;
        bool _bswitchOnDisconnect = false;
        int _iSwitchTimeout = 30;
        bool _benableLogging = false;
        #endregion

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

        //public connector(string[] profiles)
        public connector()
        {
            //read config values
            _profiles = new string[] { _myConfig._profile1, _myConfig._profile2 };
            _bcheckOnUndock = _myConfig._checkOnUndock;
            _bcheckOnResume = _myConfig._checkOnResume;
            _bswitchOnDisconnect = _myConfig._switchOnDisconnect;
            _iSwitchTimeout = _myConfig._switchTimeout;
            _benableLogging = _myConfig._enableLogging;
#if DEBUG
            Logger.bEnableLogging = true;
#else
            Logger.bEnableLogging = _benableLogging;
#endif
            //_profiles = profiles;
            evtStopThreads.Reset(); // clear event

            OnConnectedMessage("connector initialized with profiles: ");
            try
            {
                _ssRACapi = new itc_ssapi();
            }
            catch (Exception ex)
            {
                throw new NotSupportedException("RAC not active");
            }

            //list profiles and disable all first, only one should be enabled
            foreach (string s in _profiles)
            {
                OnConnectedMessage("\t" + s);
                _ssRACapi.enableProfile(s, false);
            }
            _ssRACapi.enableProfile(_profiles[0], true); //enable primary profile

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
            evtStopThreads.Set(); // stop all threads
            _bStopThread = true;
            Thread.Sleep(5000);
            if (_workThread != null) 
                _workThread.Abort();
            if (_connectThread != null)
                _connectThread.Abort();
            if (_timerThread != null)
                _timerThread.Abort();
            if (_PowerSourceMessages != null)
                _PowerSourceMessages.Dispose();
            evtStopThreads.Reset();
        }

        public void trySwitch()
        {
            doSwitch();
        }

        /// <summary>
        /// this function tries to switch to the preferred network
        /// and falls back to second profile if 1st did not connect
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

            OnConnectedMessage("DoSwitch() start...");
            Logger.WriteLine("DoSwitch() start...");

            int iConnectTry = 0;
            //is first profile active?
            string currentProfile = _ssRACapi.getCurrentProfile().sProfileLabel;
            string desiredSSID = _ssRACapi.getCurrentProfile().sSSID;
            OnConnectedMessage("current profile=" + currentProfile);
            //is the preferred profile active?
            if (currentProfile == _profiles[0])
            {
                OnConnectedMessage("Current Profile = First profile");
                if (network._getConnected() == false)
                { //not connected
                    OnConnectedMessage("network not connected. Switching to 2nd profile...");
                    _ssRACapi.enableProfile(_profiles[0], false); //disable first profile
                    _ssRACapi.enableProfile(_profiles[1], true); //enable second profile
                }
                else
                {
                    OnConnectedMessage("network connected. No profile change.");
                }
            }
            else if (_ssRACapi.getCurrentProfile().sProfileLabel == _profiles[1])
            {
                desiredSSID = _ssRACapi._racProfiles[1].sSSID;
                OnConnectedMessage("Current Profile = Second profile");
                if ( network._getConnected() == false)
                    OnConnectedMessage("secondary profile not connected");
                else
                    OnConnectedMessage("secondary profile connected");
                //try first profile, regardless of connect state
                OnConnectedMessage("Trying first Profile. Switching ...");
                _ssRACapi.enableProfile(_profiles[1], false); //disable second profile
                _ssRACapi.enableProfile(_profiles[0], true); //enable first profile
                desiredSSID = _ssRACapi._racProfiles[0].sSSID;
                iConnectTry = 0;
                //try for 40 seconds or so
                while (!_bStopThread && (iConnectTry < _iSwitchTimeout))
                {
                    Thread.Sleep(1000);
                    iConnectTry++;
                    if (_myConfig._checkConnectIP)
                    {
                        if (network._getConnected() == true)    // do not care about AP association
                            break;
                    }
                    else //check AP association but not IP
                    {
                        if (wifi.isAssociated(desiredSSID) == true) // do not care about IP
                            break;
                    }
                }
                //another test for being connected
                if (network._getConnected() == false)
                {
                    OnConnectedMessage("First Profile did not connect. Switching to secondary profile...");
                    //switch back
                    _ssRACapi.enableProfile(_profiles[0], false); //disable first profile
                    _ssRACapi.enableProfile(_profiles[1], true);  //enable second profile
                }
                else
                {
                    OnConnectedMessage("primary network connected.");
                }
            }
            else
            {
                OnConnectedMessage("Current profile not in list!");
            }

            lock (syncObject)
                bInsideSwitch = false;
            OnConnectedMessage("DoSwitch() end.");
            Logger.WriteLine("DoSwitch() end.");
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
            OnConnectedMessage("myWorkerThread start");
            batterylog2.BatteryStatusEx batteryStatus=new batterylog2.BatteryStatusEx();
            bool isDocked = batteryStatus._isACpowered;
            bool stopp = false;
            int indx = -1;
            EventWaitHandle[] handles = new EventWaitHandle[] { evtStopThreads, evtTime, evtDisconnect, evtPower, evtUndocked };
            try
            {
                while(!stopp)
                {
                    indx = EventWaitHandle.WaitAny(handles, 5000, false);
                    switch (indx)
                    {
                        case 0:
                            OnConnectedMessage("myWorkerThread stopp signaled");
                            stopp = true;
                            _bStopThread = true;
                            break;
                        case 1: //timer
                            OnConnectedMessage("myWorkerThread timer signaled");
                            OnConnectedMessage("current profile: " + _ssRACapi.getCurrentProfile().sProfileLabel);
                            break;
                        case 2: //disconnect
                            if(_bswitchOnDisconnect){
                                OnConnectedMessage("myWorkerThread disconnect signaled");
                                doSwitch();
                            }
                            break;
                        case 3: //PowerOn
                            if (_bcheckOnResume)
                            {
                                OnConnectedMessage("myWorkerThread powerOn signaled");
                                doSwitch();
                            }
                            break;
                        case 4: //undocked
                            if (_bcheckOnUndock)
                            {
                                OnConnectedMessage("myWorkerThread undocked signaled");
                                //try primary profile
                                //try secondary profile
                                doSwitch();
                            }
                            break;
                        case EventWaitHandle.WAIT_FAILED:
                            break;
                        case EventWaitHandle.WAIT_TIMEOUT:
                            //OnConnecterMessage("myWorkerThread WAIT_TIMEOUT");
                            break;
                    }
                };
            }
            catch (ThreadAbortException ex)
            {
                //OnConnecterMessage("myWorkerThread ThreadAbortException: " + ex.Message);
                ;
            }
            catch (Exception ex)
            {
                OnConnectedMessage("myWorkerThread Exception: " + ex.Message);
            }
            OnConnectedMessage("myWorkerThread ended");
        }

        /// <summary>
        /// this thread checks for an existing connection
        /// will fire disconnect event on disconnect
        /// </summary>
        void connectWatchThread()
        {
            OnConnectedMessage("connectWatchThread starting");
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
                            OnConnectedMessage("connectWatchThread stopp signaled");
                            break;
                        case EventWaitHandle.WAIT_TIMEOUT:
                            lock (syncObject)
                            {
                                bDoNotCheck = bInsideSwitch;
                            }
                            if (bDoNotCheck)
                            {
                                OnConnectedMessage("connectWatchThread does not check!");
                                break;
                            }
                            //OnConnecterMessage("connectWatchThread WAIT_TIMEOUT");
                            bRadioPower = _ssRACapi.getRadioEnabled();
                            if (!bRadioPower){
                                OnConnectedMessage("Radio is disabled!");
                                break;
                            }
                            associatedAP=wifi.getAssociatedAP();
                            //TODO
                            OnConnectedMessage("connectWatchThread: associatedAP AP=" + associatedAP);
                            if (associatedAP == this._ssRACapi._racProfiles[0].sSSID)
                            {
                                OnConnectedMessage("connectWatchThread: Radio already connected to preferred profile " + _ssRACapi._racProfiles[0].sProfileLabel);
                                break;
                            }
                            newState = network._getConnected();
                            if (!newState)
                            {
                                OnConnectedMessage("connectWatchThread: fire Disconnect event");
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
                ;//OnConnecterMessage("connectWatchThread ThreadAbortException: " + ex.Message);
            }
            catch (Exception ex)
            {
                OnConnectedMessage("connectWatchThread Exception: " + ex.Message);
            }
            OnConnectedMessage("connectWatchThread ended");
        }

        /// <summary>
        /// fires timer event periodically
        /// </summary>
        void timerThread()
        {
            OnConnectedMessage("timerThread started");
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
                            OnConnectedMessage("timerThread stopp signaled");
                            break;
                        case EventWaitHandle.WAIT_FAILED:
                            OnConnectedMessage("timerThread WAIT_FAILED");
                            break;
                        case EventWaitHandle.WAIT_TIMEOUT:
                            OnConnectedMessage("timerThread fires evtTime");
                            evtTime.Set();
                            //Thread.Sleep(1000);
                            //evtTime.Reset();
                            break;
                    }
                } while (!stopp);
            }
            catch (ThreadAbortException ex)
            {
                ;// OnConnecterMessage("timerThread ThreadAbortException: " + ex.Message);
            }
            catch (Exception ex)
            {
                OnConnectedMessage("timerThread Exception: " + ex.Message);
            }
            OnConnectedMessage("timerThread ended");
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
        void OnConnectedMessage(string msg)
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
