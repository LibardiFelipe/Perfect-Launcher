using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perfect_Launcher.Properties;

namespace Perfect_Launcher
{
    class ManageUsers
    {
        WarningMessages WM = new WarningMessages();

        public bool AddUser(string User, string Passwd, string Classe)
        {
            // Checa se o user e a senha são válidos
            if (User == "" || Passwd == "")
            {
                WM.ShowMessage("Usuário ou senha inválido(s)!", 2);
                return false;
            }

            // Checa se já existe algum user igual
            foreach (string s in Settings.Default.User)
            {
                if (User == s)
                {
                    WM.ShowMessage("Usuário já existente!", 2);
                    return false;
                }
            }

            // Se tudo estiver certo, adiciona a conta
            Settings.Default.User.Add(User);
            // TODO: Adicionar um salted hash antes de salvar a senha
            Settings.Default.Passwd.Add(Passwd);
            // Adiciona a classe
            Settings.Default.Classe.Add(Classe);

            return true;
        }

        public bool RemoveUser(int UserId)
        {
            if (Settings.Default.User.Count >= UserId && Settings.Default.Passwd.Count >= UserId)
            {
                // Remove das settings
                Settings.Default.User.RemoveAt(UserId);
                Settings.Default.Passwd.RemoveAt(UserId);
                Settings.Default.Classe.RemoveAt(UserId);
                return true;
            }
            return false;
        }
    }
}
