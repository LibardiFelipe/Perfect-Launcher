using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace Perfect_Launcher
{
    class WarningMessages
    {
        public System.Windows.Forms.DialogResult ShowMessage(string Msg, int MsgType = 0, bool bYesNo = false, bool bFatal = false)
        {
            System.Windows.Forms.DialogResult DialogChoice;
            System.Windows.Forms.MessageBoxIcon Icon;

            // Exibir um botão de YesNo ou um OK?
            System.Windows.Forms.MessageBoxButtons Buttons = bYesNo ? System.Windows.Forms.MessageBoxButtons.YesNo : System.Windows.Forms.MessageBoxButtons.OK;

            // 0: Informação, 1: Pergunta, 2: Exclamação, >=3: Erro
            switch (MsgType)
            {
                case 0:
                    Icon = System.Windows.Forms.MessageBoxIcon.Information;
                    DialogChoice = System.Windows.Forms.MessageBox.Show(new System.Windows.Forms.Form { TopMost = true }, Msg, "Perfect Launcher", Buttons, Icon);
                    break;
                case 1:
                    Icon = System.Windows.Forms.MessageBoxIcon.Question;
                    DialogChoice = System.Windows.Forms.MessageBox.Show(new System.Windows.Forms.Form { TopMost = true }, Msg, "Perfect Launcher", Buttons, Icon);
                    break;
                case 2:
                    // TODO: Dar play no som manualmente (geralmente o windows10 não vem como um som para a caixa de exclamação)
                    Icon = System.Windows.Forms.MessageBoxIcon.Exclamation;
                    DialogChoice = System.Windows.Forms.MessageBox.Show(new System.Windows.Forms.Form { TopMost = true }, Msg, "Perfect Launcher", Buttons, Icon);
                    break;
                default:
                    string NomeDoArquivo = DateTime.Now.ToString("G");
                    NomeDoArquivo = NomeDoArquivo.Replace("/", "-");
                    NomeDoArquivo = NomeDoArquivo.Replace(":", ".");
                    File.WriteAllText(Application.StartupPath + "\\Perfect Launcher\\Logs\\" + NomeDoArquivo + ".txt", Msg);

                    Icon = System.Windows.Forms.MessageBoxIcon.Error;
                    DialogChoice = System.Windows.Forms.MessageBox.Show(new System.Windows.Forms.Form { TopMost = true }, Msg, "Perfect Launcher", Buttons, Icon);
                    break;
            }

            // Força o fechamento do programa caso seja fatal
            if (bFatal)
                Environment.Exit(0);

            return DialogChoice;
        }
    }
}
