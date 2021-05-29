using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using Perfect_Launcher.Properties;

namespace Perfect_Launcher
{
    public partial class Global : Form
    {
        string[] MsgGlobal;

        public Global(string[] msg)
        {
            InitializeComponent();
            MsgGlobal = msg;
        }

        private void Global_Load(object sender, EventArgs e)
        {
            LoadMessages();
        }

        private void LoadMessages(bool bFromFile = false)
        {
            for (int i = MsgGlobal.Length - 1; i >= 0; i--)
                listBox1.Items.Add(MsgGlobal[i]);

            // Seleciona o primeiro index
            if (listBox1.Items.Count > 0)
            {
                listBox1.SelectedIndex = 0;
            }
            else
            {
                WarningMessages wm = new WarningMessages();
                wm.ShowMessage("Não foi possível encontrar as mensagens!", 2);
                Close();
            }
        }
    }
}
