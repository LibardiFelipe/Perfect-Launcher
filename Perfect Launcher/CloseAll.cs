using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Perfect_Launcher
{
    public partial class CloseAll : Form
    {
        List<RunningGames> RGames = new List<RunningGames>();
        public CloseAll(List<RunningGames> rg)
        {
            InitializeComponent();

            RGames = rg;
        }

        private void CloseAll_Load(object sender, EventArgs e)
        {
            // Sempre vai ser 0
            comboBox1.SelectedIndex = 0;

            foreach (RunningGames rg in RGames)
            {
                comboBox1.Items.Add(rg.User);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string naoFechar = comboBox1.SelectedItem.ToString();

            foreach(RunningGames rg in RGames)
            {
                try
                {
                    if (rg.User != naoFechar)
                    {
                        Process p = Process.GetProcessById(rg.ProcessId);
                        p.Kill();
                    }
                }
                catch
                {

                }
            }

            Close();
        }
    }
}
