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

namespace Perfect_Launcher
{
    public partial class CheckForm : Form
    {
        public CheckForm()
        {
            InitializeComponent();
        }

        private void CheckForm_Load(object sender, EventArgs e)
        {
            if (Settings.Default.LastCheckup == DateTime.Today.ToShortDateString())
            {
                checkBox0.Checked = Settings.Default.DailyCheckups[0];
                checkBox1.Checked = Settings.Default.DailyCheckups[1];
                checkBox2.Checked = Settings.Default.DailyCheckups[2];
                checkBox3.Checked = Settings.Default.DailyCheckups[3];
                checkBox4.Checked = Settings.Default.DailyCheckups[4];
                checkBox5.Checked = Settings.Default.DailyCheckups[5];
                checkBox6.Checked = Settings.Default.DailyCheckups[6];
                checkBox7.Checked = Settings.Default.DailyCheckups[7];
                checkBox8.Checked = Settings.Default.DailyCheckups[8];
                checkBox9.Checked = Settings.Default.DailyCheckups[9];
            }

            // Sempre carrega o checkbox da torre, pois ele será resetado toda quarta-feira pelo programa
            // Ao perguntar pro usuário se ele deseja checar por atualizações
            checkBox10.Checked = Settings.Default.DailyCheckups[10];
        }

        private void CheckForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.LastCheckup = DateTime.Today.ToShortDateString();

            Settings.Default.DailyCheckups[0] = checkBox0.Checked;
            Settings.Default.DailyCheckups[1] = checkBox1.Checked;
            Settings.Default.DailyCheckups[2] = checkBox2.Checked;
            Settings.Default.DailyCheckups[3] = checkBox3.Checked;
            Settings.Default.DailyCheckups[4] = checkBox4.Checked;
            Settings.Default.DailyCheckups[5] = checkBox5.Checked;
            Settings.Default.DailyCheckups[6] = checkBox6.Checked;
            Settings.Default.DailyCheckups[7] = checkBox7.Checked;
            Settings.Default.DailyCheckups[8] = checkBox8.Checked;
            Settings.Default.DailyCheckups[9] = checkBox9.Checked;
            Settings.Default.DailyCheckups[10] = checkBox10.Checked;
        }
    }
}
