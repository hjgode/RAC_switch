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

        public Form1()
        {
            InitializeComponent();
            try
            {
                _wifi = new wifi();
                isRAC = true;
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(ex.Message);                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            _network = new network();
            
            txtLog.Text += "\r\nNetwork start: " + (network._getConnected()?"connected":"disconnected");

            //_network.networkChangedEvent += new network.networkChangeEventHandler(_network_networkChangedEvent);
        }

        void startConnector()
        {
            if (isRAC)
            {
                if (_connector != null)
                    stopConnector();
                _connector = new connector(new string[] { "SUPPORT", "Intermec" });
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
                txtLog.Text += "\r\nNo RAC installed";
            }
        }
        void stopConnector()
        {
            if (_connector == null)
                return;
            _connector.Dispose();
            _connector = null;
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
            if(_wifi!=null)
                _wifi.Dispose();
            if (_network != null)
                _network.Dispose();
            if (_connector != null)
                _connector.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_wifi.setRAC("INTERMEC") == 1)
                txtLog.Text += "\r\nProfile with SSID INTERMEC enabled";
            else
                txtLog.Text += "\r\nenable profile FAILED";
            updateGrid();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(_wifi.setRAC("SUPPORT")==1)
                txtLog.Text += "\r\nProfile with SSID SUPPORT enabled";
            else
                txtLog.Text += "\r\nenable profile FAILED";
            updateGrid();
        }

        private void btnTrySwitch_Click(object sender, EventArgs e)
        {
            if(_connector!=null)
                _connector.trySwitch();
        }

        private void mnuGetProfiles_Click(object sender, EventArgs e)
        {
            updateGrid();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            startConnector();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            stopConnector();
        }
    }
}