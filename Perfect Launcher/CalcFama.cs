using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Perfect_Launcher
{
    public partial class CalcFama : Form
    {
        public CalcFama()
        {
            InitializeComponent();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Value > numericUpDown2.Value)
                labelResultado.Text = "Nenhuma insígnia.";
            else
                CalculaValor();
        }

        private void CalculaValor()
        {
            try
            {
                int FamaDesejada = Convert.ToInt32(numericUpDown2.Value) - Convert.ToInt32(numericUpDown1.Value);
                int FamaTotal = 0;
                int InsigniasNecessarias = 0;
                
                while(FamaTotal < FamaDesejada)
                {
                    FamaTotal += 25;
                    InsigniasNecessarias += 2;
                }

                labelResultado.Text = InsigniasNecessarias.ToString() + " Insígnia Brado de Batalha ou " + (InsigniasNecessarias / 2) + " Insígia do Oficial Fantasma\npara atingir " + (Convert.ToInt32(numericUpDown1.Value) + FamaTotal).ToString() + " de fama.";
            }
            catch
            {

            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown2.Value < numericUpDown1.Value)
                labelResultado.Text = "Nenhuma insígnia.";
            else
                CalculaValor();
        }

        private void CalcFama_Load(object sender, EventArgs e)
        {

        }

        private void numericUpDown1_Click(object sender, EventArgs e)
        {

        }
    }
}
