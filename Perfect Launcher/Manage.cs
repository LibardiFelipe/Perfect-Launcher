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
    public partial class Manage : Form
    {
        Form1 f1;

        int newindex = 0;

        public Manage(Form1 frm1)
        {
            InitializeComponent();

            f1 = frm1;
        }

        private void Manage_Load(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void UpdateList()
        {
            listBox1.Items.Clear();
            foreach (string s in Settings.Default.User)
                listBox1.Items.Add(s);

            if (listBox1.Items.Count > newindex)
                listBox1.SelectedIndex = newindex;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex <= 0)
                return;

            // Salva o item selected index - 1
            string prevuser = Settings.Default.User[listBox1.SelectedIndex - 1];
            string prevpass = Settings.Default.Passwd[listBox1.SelectedIndex - 1];
            string prevclass = Settings.Default.Classe[listBox1.SelectedIndex - 1];

            Settings.Default.User[listBox1.SelectedIndex - 1] = Settings.Default.User[listBox1.SelectedIndex];
            Settings.Default.Passwd[listBox1.SelectedIndex - 1] = Settings.Default.Passwd[listBox1.SelectedIndex];
            Settings.Default.Classe[listBox1.SelectedIndex - 1] = Settings.Default.Classe[listBox1.SelectedIndex];

            Settings.Default.User[listBox1.SelectedIndex] = prevuser;
            Settings.Default.Passwd[listBox1.SelectedIndex] = prevpass;
            Settings.Default.Classe[listBox1.SelectedIndex] = prevclass;

            newindex = listBox1.SelectedIndex - 1;

            UpdateList();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
                return;

            AddEditUser addUser = new AddEditUser(f1, listBox1.SelectedIndex, true);
            addUser.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null || listBox1.SelectedIndex >= listBox1.Items.Count - 1)
                return;

            // Salva o item selected index + 1
            string nextuser = Settings.Default.User[listBox1.SelectedIndex + 1];
            string nextpass = Settings.Default.Passwd[listBox1.SelectedIndex + 1];
            string nextclass = Settings.Default.Classe[listBox1.SelectedIndex + 1];

            Settings.Default.User[listBox1.SelectedIndex + 1] = Settings.Default.User[listBox1.SelectedIndex];
            Settings.Default.Passwd[listBox1.SelectedIndex + 1] = Settings.Default.Passwd[listBox1.SelectedIndex];
            Settings.Default.Classe[listBox1.SelectedIndex + 1] = Settings.Default.Classe[listBox1.SelectedIndex];

            Settings.Default.User[listBox1.SelectedIndex] = nextuser;
            Settings.Default.Passwd[listBox1.SelectedIndex] = nextpass;
            Settings.Default.Classe[listBox1.SelectedIndex] = nextclass;

            newindex = listBox1.SelectedIndex + 1;

            UpdateList();
        }

        private void Manage_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Atualiza o form1 ao fechar
            f1.RefreshUsernamesOnComboBox();
        }
    }
}
