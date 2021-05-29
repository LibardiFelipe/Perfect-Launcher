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
    public partial class FormAtalhos : Form
    {
        public FormAtalhos()
        {
            InitializeComponent();
        }

        private void FormAtalhos_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = Settings.Default.RevezarContasBtn;
            comboBox2.SelectedIndex = Settings.Default.ProximaContaBtn;
            comboBox3.SelectedIndex = Settings.Default.ContaAnteriorBtn;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != comboBox2.SelectedIndex && comboBox2.SelectedIndex != comboBox3.SelectedIndex
                && comboBox1.SelectedIndex != comboBox3.SelectedIndex)
            {
                Settings.Default.RevezarContasBtn = (sbyte)comboBox1.SelectedIndex;
                Settings.Default.ProximaContaBtn = (sbyte)comboBox2.SelectedIndex;
                Settings.Default.ContaAnteriorBtn = (sbyte)comboBox3.SelectedIndex;
                Close();
            }
            else
            {
                WarningMessages wm = new WarningMessages();
                wm.ShowMessage("Não é possível utilizar o mesmo atalho para duas (ou mais) coisas!", 2);
            }
        }
    }
}
