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

        public Form1()
        {
            InitializeComponent();
            try
            {
                _wifi = new wifi();
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(ex.Message);                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            updateGrid();
            
            dataGrid2.DataSource = _wifi._accesspoints;

            timer1 = new Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 30000; //all 30 seconds
            timer1.Enabled=true;
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
    }
}