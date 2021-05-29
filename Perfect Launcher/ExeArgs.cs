using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Perfect_Launcher.Properties;
using System.IO;

namespace Perfect_Launcher
{
    public partial class ExeArgs : Form
    {
        public ExeArgs()
        {
            InitializeComponent();
        }

        private void ExeArgs_Load(object sender, EventArgs e)
        {
            textBox3.Text = Settings.Default.ExtraArgs;
        }

        private void ExeArgs_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.ExtraArgs = textBox3.Text;
        }
    }
}
