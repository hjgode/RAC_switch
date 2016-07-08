using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RAC_switch
{
    public partial class Form1 : Form
    {
        wifi _wifi;
        System.Windows.Forms.Timer timer1;
        network _network;
        bool isRAC = false;

        connector _connector;

        System.Windows.Forms.Timer timerMinimize = new Timer();

        public Form1()
        {
            InitializeComponent();
            
            //we have to wait for the Windows APIs being loaded...
            WinAPIReady winReady = new WinAPIReady();
            int maxWait = 20;
            do
            {
                System.Threading.Thread.Sleep(1000);
                maxWait--;
            } while (winReady.ApiIsReay == false && maxWait>0);
            winReady.Dispose();

            try
            {
                _wifi = new wifi();
                isRAC = true;
                MobileConfiguration myConfig = new MobileConfiguration();
                button1.Text = myConfig._profile1;
                button2.Text = myConfig._profile2;
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(ex.Message);                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            if (isRAC)
            {
                btnStart.Enabled = true;
            }

            _network = new network();
            
            addLog("Network start: " + (network._getConnected()?"connected":"disconnected"));

            timerMinimize.Interval = 5000;
            timerMinimize.Tick += new EventHandler(timerMinimize_Tick);
            timerMinimize.Enabled = true;

            startConnector();
            updateButtons();

            //_network.networkChangedEvent += new network.networkChangeEventHandler(_network_networkChangedEvent);
        }

        void winReady_apiChangedEvent(object sender, WinAPIReady.ApiEventArgs args)
        {
            throw new NotImplementedException();
        }

        void timerMinimize_Tick(object sender, EventArgs e)
        {
            timerMinimize.Enabled = false;
#if DEBUG
#else
            win32native.Minimize(this);
#endif
        }

        void startConnector()
        {
            if (isRAC)
            {
                if (_connector != null)
                    stopConnector();
                //_connector = new connector(new string[] { "SUPPORT", "Intermec" });
                addLog("starting connector...");
                _connector = new connector();
                _connector.connectorChangedEvent += new connector.connectorChangeEventHandler(_connector_connectorChangedEvent);

                updateGrid();

                dataGrid2.DataSource = _wifi._accesspoints;

                timer1 = new Timer();
                timer1.Tick += new EventHandler(timer1_Tick);
                timer1.Interval = 30000; //all 30 seconds
                //                timer1.Enabled = true;
            }
            else
            {
                addLog("No RAC installed");
            }
        }
        void stopConnector()
        {
            if (_connector == null)
                return;
            addLog("STOP connector...");
            _connector.Dispose();
            _connector = null;
            addLog("connector stopped.");
        }

        void _connector_connectorChangedEvent(object sender, connector.ConnectorEventArgs args)
        {
            addLog(args.message);
        }

        void _network_networkChangedEvent(object sender, network.NetworkEventArgs eventArgs)
        {
            addLog(eventArgs.ipAddress.ToString()+", "+eventArgs.EventReason.ToString());
        }

        delegate void SetTextCallback(string text);
        public void addLog(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.txtLog.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(addLog);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (txtLog.Text.Length > 2000)
                    txtLog.Text = "";
                txtLog.Text += text + "\r\n";
                txtLog.SelectionLength = 0;
                txtLog.SelectionStart = txtLog.Text.Length - 1;
                txtLog.ScrollToCaret();
            }
        }
        void timer1_Tick(object sender, EventArgs e)
        {
            dataGrid2.DataSource = null;
            dataGrid2.DataSource = _wifi._accesspoints;
            dataGrid2.Refresh();
        }

        void updateGrid()
        {
            dataGrid1.DataSource = null;
            dataGrid1.DataSource = _wifi.racProfiles;
            dataGrid1.Refresh();
        }

        private void Form1_Closed(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            this.Refresh();          
            if(_wifi!=null)
                _wifi.Dispose();
            if (_network != null)
                _network.Dispose();
            if (_connector != null)
                _connector.Dispose();
            Cursor.Current = Cursors.Default;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_wifi.setRAC(button1.Text) == 1)
                addLog ("\r\nsetting Profile "+button1.Text+ " enabled");
            else
                addLog("\r\nenable Profile " + button1.Text + " FAILED");
            updateGrid();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(_wifi.setRAC(button2.Text)==1)
                addLog("setting Profile "+button2.Text+ " enabled");
            else
                addLog("enable profile FAILED");
            updateGrid();
        }

        private void btnTrySwitch_Click(object sender, EventArgs e)
        {
            if (_connector != null)
            {
                addLog("trying to switch to preferred Profile");
                _connector.trySwitch();
            }
            else
            {
                addLog("connector not running.");
            }
        }

        private void mnuGetProfiles_Click(object sender, EventArgs e)
        {
            updateGrid();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            startConnector();
            updateButtons();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            stopConnector();
            updateButtons();
        }

        void updateButtons()
        {
            if (_connector != null)
            {
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            else
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void mnuMinimize_Click(object sender, EventArgs e)
        {
            win32native.Minimize(this);
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("RAC_Switch version " + Logger.getVersion());
        }

    }
}