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
    public partial class AddEditUser : Form
    {
        Form1 f1;

        bool bEditar;
        int Id;

        public AddEditUser(Form1 frm1, int Index = -1, bool bEdit = false)
        {
            InitializeComponent();

            f1 = frm1;

            bEditar = bEdit;
            Id = Index;

            comboBox1.SelectedIndex = 0;
        }

        private void AddEditUser_Load(object sender, EventArgs e)
        {
            if (bEditar)
            {
                Text = "EDITAR";
                button1.Text = "SALVAR";

                // Exibe as informações da conta carregada
                textBox1.Text = Settings.Default.User[Id];
                textBox2.Text = Settings.Default.Passwd[Id];

                // Verifica se a descrição bate com alguma da combobox
                if (comboBox1.Items.Contains(Settings.Default.Classe[Id]))
                {
                    checkBox1.Checked = false;
                    comboBox1.SelectedIndex = comboBox1.Items.IndexOf(Settings.Default.Classe[Id]);
                }
                else
                {
                    // Carrega a descrição na textbox
                    checkBox1.Checked = true;
                    textBox3.Text = Settings.Default.Classe[Id];
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "")
                return;

            // Se a descrição customizada estiver ativada, retorna caso esjeta vazia
            if (checkBox1.Checked)
                if (textBox3.Text == "")
                    return;

            if (!bEditar)
            {
                ManageUsers m = new ManageUsers();
                bool bAdded = m.AddUser(textBox1.Text, textBox2.Text, checkBox1.Checked ? textBox3.Text : comboBox1.SelectedItem.ToString());

                if (bAdded)
                {
                    f1.RefreshUsernamesOnComboBox(Id);
                    Close();
                }
            }
            else
            {
                Settings.Default.User[Id] = textBox1.Text;
                Settings.Default.Passwd[Id] = textBox2.Text;
                Settings.Default.Classe[Id] = checkBox1.Checked ? textBox3.Text : comboBox1.SelectedItem.ToString();

                f1.RefreshUsernamesOnComboBox(Id);
                Close();
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                button1_Click(this, e);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = !checkBox1.Checked;
            textBox3.Enabled = checkBox1.Checked;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            textBox2.UseSystemPasswordChar = !textBox2.UseSystemPasswordChar;
        }
    }
}
