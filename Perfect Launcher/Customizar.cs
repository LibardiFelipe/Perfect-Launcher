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
    public partial class Customizar : Form
    {
        Form1 f1;

        public Customizar(Form1 frm1)
        {
            InitializeComponent();

            f1 = frm1;
        }

        private void Customizar_Load(object sender, EventArgs e)
        {
             if (File.Exists(Settings.Default.BackgroundImg))   
                textBox1.Text = Settings.Default.BackgroundImg;

            if (Settings.Default.BackgroundColor != null)
                pictureBox1.BackColor = Settings.Default.BackgroundColor;

            if (Settings.Default.FontColor != null)
                pictureBox2.BackColor = Settings.Default.FontColor;

            trackBar1.Value = Settings.Default.BackgroundTransparency;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "Arquivos de Imagem (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
            fd.FileName = "";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                textBox1.Text = fd.FileName;
                Settings.Default.BackgroundImg = fd.FileName;
                f1.UpdateBackgroundImage();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = Settings.Default.BackgroundColor;

            DialogResult dr = colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                Settings.Default.BackgroundColor = colorDialog1.Color;
                pictureBox1.BackColor = colorDialog1.Color;
                f1.UpdateBackColor();
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = Settings.Default.FontColor;

            DialogResult dr = colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                Settings.Default.FontColor = colorDialog1.Color;
                pictureBox2.BackColor = colorDialog1.Color;
                f1.UpdateFontColor();
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Settings.Default.BackgroundTransparency = trackBar1.Value;

            f1.UpdateBackColor();
        }
    }
}
