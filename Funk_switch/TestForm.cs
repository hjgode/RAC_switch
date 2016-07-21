using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Funk_switch
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                FunkProfiles funk = new FunkProfiles();
                Logger.WriteLine("current profile: " + funk.getCurrentProfile().ToString());

                funk.setCurrentProfile("Profile_2");
                Logger.WriteLine("current profile: " + funk.getCurrentProfile().ToString());
            }catch(NotSupportedException ex){
                MessageBox.Show(ex.Message);
            }
        }
    }
}