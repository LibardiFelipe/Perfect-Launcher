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
using System.IO;
using Perfect_Launcher.Properties;
using System.Runtime.InteropServices;
using System.Net;

namespace Perfect_Launcher
{
    public partial class Form1 : Form
    {
        // Form para combos
        public Combo ComboForm;

        // Define se as settings deverão ser salvas no fechamento do programa
        bool bCanSave = true;

        // Controla as mensagens exibidas na tela do usuário
        WarningMessages WM = new WarningMessages();

        // Armazena os jogos abertos para que o crashwatch possa os observar
        List<RunningGames> RGames = new List<RunningGames>();

        // Fechar ou minizar? (minimiza antes de fechar primeiro)
        bool bShouldClose = false;

        // Contas abertas recentemente
        List<string> OpenRecently = new List<string>();

        // Mensagens globais
        string[] MsgGlobal;

        // Para de rolar o global
        bool bBlockRoll = false;

        int ScrollTextDefaultValue = 0;

        // Será usado para impetir que as contas abram caso o client esteja desatualizado
        bool bHasUpdate = false;

        // Bloqueia a troca de arquitetura caso algum jogo esteja aberto
        public bool bBlockArchChange = false;

        const string Exe32 = "ELEMENTCLIENT.EXE";
        const string Exe64 = "elementclient_64.exe";

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public Form1()
        {
            InitializeComponent();

            ScrollTextDefaultValue = labelGlobal.Top;

            // Checa se o programa está na pasta correta
            if (!File.Exists(Application.StartupPath + "\\" + Exe64) && !File.Exists(Application.StartupPath + "\\" + Exe32))
            {
                WM.ShowMessage("ElementClient.exe não foi encontrado!\nPor favor, coloque este launcher dentro da pasta \"element\" do seu PW.");

                // Procura a pasta elements, e abre-a
                char[] Disk = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

                foreach (char c in Disk)
                    if (Directory.Exists(c + ":\\Level Up\\Perfect World\\element"))
                    {
                        Process.Start(c + ":\\Level Up\\Perfect World\\element");
                        break;
                    }

                // Força o fechamento do programa
                Environment.Exit(0);
            }

            // Verifica se o programa já se encontra aberto
            string PName = Process.GetCurrentProcess().ProcessName;
            Process[] p = Process.GetProcessesByName(PName);
            // TODO: Maximizar a janela do programa aberto ao invés de fecha-lá
            if (p.Length > 1)
                WM.ShowMessage("O programa já está em execução.", 0, false, true);


            // Prepara o programa para o primeiro uso
            if (Settings.Default.bFirstRun)
            {
                // Limpa as stringsCollection
                Settings.Default.User.Clear();
                Settings.Default.Passwd.Clear();
                Settings.Default.Classe.Clear();
                Settings.Default.ComboQueue.Clear();
                Settings.Default.UsersBeforeClosing.Clear();

                // Verificar se tem suporte à 64 bits e perguntar se deseja utilizar
                if (Environment.Is64BitOperatingSystem && File.Exists(Application.StartupPath + "\\" + Exe64))
                {
                    DialogResult dr = WM.ShowMessage("Seu sistema e seu jogo suportam 64 bits.\nDeseja usa-lo?", 1, true);

                    if (dr == DialogResult.Yes)
                        Settings.Default.bUse64 = true;
                }
            }

            // Carrega a aparência do form
            UpdateBackgroundImage();
            UpdateBackColor();
            UpdateFontColor();
            UpdateArchChecks();

            toolStripComboBox1.SelectedItem = Settings.Default.ForceServer;
        }

        private void TemporaryTopMost(int Delay)
        {
            tempTopMost.Stop();
            TopMost = true;
            tempTopMost.Interval = Delay;
            tempTopMost.Start();
        }

        public void RefreshUsernamesOnComboBox(int customIndex = -1, bool bSelectLastAdded = false)
        {
            toolStripComboBox2.Items.Clear();
            // Adiciona o item 0 no combobox
            toolStripComboBox2.Items.Add("Abrir uma conta...");
            toolStripComboBox2.SelectedIndex = 0;

            // Limpa o comboBox antes de tudo
            usersComboBox.Items.Clear();

            // Carrega os usuários salvos no combobox (caso haja algum)
            if (Settings.Default.User.Count > 0)
                foreach (string s in Settings.Default.User)
                {
                    usersComboBox.Items.Add(s);
                    toolStripComboBox2.Items.Add(s);
                }

            if (bSelectLastAdded)
            {
                // Sem verificação pois se o lastAdded for true, é certo que já tem alguma conta adicionada
                usersComboBox.SelectedIndex = usersComboBox.Items.Count - 1;
            }
            else
            {
                if (customIndex > -1)
                {
                    usersComboBox.SelectedIndex = customIndex;
                    return;
                }

                if (usersComboBox.Items.Count > Settings.Default.LastIndexUsed)
                    usersComboBox.SelectedIndex = Settings.Default.LastIndexUsed;
                else
                    if (usersComboBox.Items.Count > 0)
                        usersComboBox.SelectedIndex = 0;
            }

            // Chama a função de update nos forms necessários (caso estejam abertos)
            if (ComboForm != null)
                ComboForm.UpdateUsers();
        }

        public void UpdateBackgroundImage()
        {
           if (File.Exists(Settings.Default.BackgroundImg)) 
                BackgroundImage = Image.FromFile(Settings.Default.BackgroundImg);
        }

        public void UpdateBackColor()
        {
            Color NewColor = Color.FromArgb(Settings.Default.BackgroundTransparency, Settings.Default.BackgroundColor.R, Settings.Default.BackgroundColor.G, Settings.Default.BackgroundColor.B);
            panel1.BackColor = NewColor;
            openButton.BackColor = NewColor;
            AddUserButton.BackColor = NewColor;
            RemoveUserButton.BackColor = NewColor;
            menuStrip1.BackColor = NewColor;
        }

        public void UpdateFontColor()
        {
            Color NewColor = Settings.Default.FontColor;
            panel1.ForeColor = NewColor;
            openButton.ForeColor = NewColor;
            AddUserButton.ForeColor = NewColor;
            RemoveUserButton.ForeColor = NewColor;
            border1.ForeColor = NewColor;
            menuStrip1.ForeColor = NewColor;
            label1.ForeColor = NewColor;
        }

        public void OpenGame(int UserId, bool bOnlyAdd = false, int ProcessId = -1)
        {
            // Login e senha
            string User = Settings.Default.User[UserId];
            // TODO: Desfazer o hash da senha ao abrir
            string Passwd = Settings.Default.Passwd[UserId];

            // Verifica se a conta já está aberta (só se o OnlyAdd for false)
            for (int i = 0; i < RGames.Count && !bOnlyAdd; i++)
            {
                try
                {
                    if (RGames[i].User == User)
                    {
                        DialogResult dr = WM.ShowMessage("A conta '" + User + "' já está aberta.\nDeseja abri-la mesmo assim?", 1, true);
                        if (dr != DialogResult.Yes)
                        {
                            return;
                        }
                        else
                        {
                            // Remove a conta da lista de jogos abertos
                            RGames.RemoveAt(i);
                            i--;
                        }
                    }
                }
                catch
                {

                }
            }

            // Registra o username no arquivo de texto para forçar a entrada no servidor
            // O login e o server são armazenados nesse arquivo em base64
            // D:\Level Up\Perfect World\element\userdata\accounts.txt
            // LOGIN 29000:gateway2.perfectworld.com.br,Ophiuchus(PvE),0
            // cada linha é pra um login e um server
            // Criar uma linha inteira em base64 e verificar se ela já existe no arquivo,
            // se ela não existir, adiciona ela.

            if (Settings.Default.ForceServer != "NENHUM")
            {
                try
                {
                    List<string> Content = new List<string>();
                    foreach (string s in File.ReadAllLines(Application.StartupPath + "\\userdata\\accounts.txt"))
                        Content.Add(s);

                    // Se a primeira linha NÃO existir ou for diferente de true
                    try
                    {
                        if (!Content[0].Contains("true"))
                            Content[0] = "true";
                    }
                    catch
                    {
                        // Se deu exception, é pq o arquivo tava vazio, então
                        // adiciona o true normalmente pra lista
                        Content.Add("true");
                    }

                    // Converte o usuário e o servidor para bas64
                    int GatewayN;
                    switch (Settings.Default.ForceServer)
                    {
                        //Ophiuchus (PvE)
                        //Phoenix (PvP)
                        //Taurus (PvP)
                        case "Ophiuchus (PvE)": GatewayN = 2; break;
                        case "Phoenix (PvP)": GatewayN = 3; break;
                        default:
                            GatewayN = 7; break;
                    }
                    string UserBase64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(User));
                    string ServerBase64 = Convert.ToBase64String(Encoding.Unicode.GetBytes("29000:gateway" + GatewayN.ToString() + ".perfectworld.com.br,"
                        + Settings.Default.ForceServer + ",0"));

                    string StringFinal = UserBase64 + " " + ServerBase64;

                    // Depois verifica se já existe no arquivo
                    bool bExiste = false;
                    for (int i = 0; i < Content.Count; i++)
                    {
                        // Se tiver o usuário em base64, remove a linha toda e reescreve ela
                        // Pq pode ser q ela esteja com outro server em base64
                        if (Content[i].Contains(UserBase64))
                        {
                            Content[i] = StringFinal;
                            bExiste = true;
                            break;
                        }
                    }

                    // Se não existir, adiciona no último slot do array
                    if (!bExiste)
                        Content.Add(StringFinal);

                    string Result = Content[0];
                    for (int i = 0; i < Content.Count; i++)
                    {
                        if (i == 0)
                            continue;

                        Result += "\n" + Content[i];
                    }

                    // Depois escreve de volta no arquivo
                    File.WriteAllText(Application.StartupPath + "\\userdata\\accounts.txt", Result);
                }
                catch (Exception x)
                {
                    WM.ShowMessage(x.ToString(), 3);
                }
            }
            else
            {
                // Apaga tudo que está no arquivo e deixa como false
                // Assim, aparecerá a lista de servidores pro jogador escolher
                File.WriteAllText(Application.StartupPath + "\\userdata\\accounts.txt", "false");
            }

            // Argumentos que serão usados
            string args = " startbypatcher " + Settings.Default.ExtraArgs + " user:" + User + " pwd:" + Passwd;

            // Cria uma classe temporária para ser armazenada na lista
            RunningGames rg = new RunningGames();

            // Cria o processo, seta seu id e o usuário                    // Abre conforme a setting
            if (!bOnlyAdd)
                rg.ProcessId = Process.Start(Application.StartupPath + "\\" + (Settings.Default.bUse64 ? Exe64 : Exe32), args).Id;
            else
                rg.ProcessId = ProcessId;

            rg.User = User;

            // Adiciona a classe para a lista (o crashwatch ficará responsável por remove-lá depois)
            RGames.Add(rg);

            // Seta como o último index aberto
            Settings.Default.LastIndexUsed = UserId;

            // Verifica se a conta aberta já está presente na lista de recentes
            // (caso esteja, remove-a)
            if (OpenRecently.Contains(User))
                OpenRecently.Remove(User);

            // Seta como uma conta aberta recentemente
            OpenRecently.Add(User);

            // Caso tenha mais de 5 contas recentes, remove a primeira
            if (OpenRecently.Count > 5)
                OpenRecently.RemoveAt(0);

            // Atualiza os menus com as contas recentes
            UpdateOpenRecentlyMenu();

            // Chama a função de update nos forms necessários (caso estejam abertos)
            if (ComboForm != null)
                ComboForm.UpdateUsers();

            // Ativa o crashwatch caso ele esteja desligado
            if (!CrashWatcherTimer.Enabled)
                CrashWatcherTimer.Start();

            // Desativa/ativa a troca de arquitetura
            UpdateArchChecks();

            // Salva a conta aberta no log
            WriteToLog(User);
        }

        private void WriteToLog(string User)
        {
            if (File.Exists(Application.StartupPath + "\\Perfect Launcher\\Logs.txt"))
            {
                string horario = "[" + DateTime.Now.ToString("d") + " às " + DateTime.Now.ToString("T") + "]: ";
                string frase = "A conta '" + User + "' foi aberta.\n";
                string completo = horario + frase + File.ReadAllText(Application.StartupPath + "\\Perfect Launcher\\Logs.txt");

                File.WriteAllText(Application.StartupPath + "\\Perfect Launcher\\Logs.txt", completo);

                // Verifica a quantidade de linhas que o log já tem,
                // se passar de 1000, começa a deletar a última linha
                if (CountLinesReader() > 1000)
                {
                    var lines = File.ReadAllLines(Application.StartupPath + "\\Perfect Launcher\\Logs.txt");
                    File.WriteAllLines(Application.StartupPath + "\\Perfect Launcher\\Logs.txt", lines.Take(lines.Length - 1).ToArray());
                }
            }
        }

        private int CountLinesReader()
        {
            int lineCounter = 0;
            using (var reader = new StreamReader(Application.StartupPath + "\\Perfect Launcher\\Logs.txt"))
            {
                while (reader.ReadLine() != null)
                    lineCounter++;
                
                return lineCounter;
            }
        }

        public List<RunningGames> GetRunningGames() { return RGames; }

        private async void RollGlobalMessages()
        {
            if (bBlockRoll)
                return;

            if (labelGlobal.Top == ScrollTextDefaultValue)
            {
                await Task.Delay(3500);
            }

            await Task.Delay(10);

            labelGlobal.Top -= 1;

            if (labelGlobal.Top <= -25)
            {
                // Move pra baixo
                labelGlobal.Top = 25;

                bool bFound = false;

                // Procura a frase atual no array
                for (int i = 0; i < MsgGlobal.Length; i++)
                {
                    // Acha a mensagem na lista
                    // A msg tá no index 0
                    // o Lenght é 2
                    // último index é 1
                    if (labelGlobal.Text == MsgGlobal[i])
                    {
                        if ((i + 1) <= (MsgGlobal.Length - 1))
                            labelGlobal.Text = MsgGlobal[i + 1];
                        else
                            labelGlobal.Text = MsgGlobal[0];

                        bFound = true;
                        break;
                    }
                }
                if (!bFound)
                {
                    if (MsgGlobal.Length > 0)
                        labelGlobal.Text = MsgGlobal[0];
                }

            }

            // Chama a func dnv (se puder)
            if (!bBlockRoll)
                RollGlobalMessages();
        }

        public void UpdateArchChecks()
        {
            arquiteturaToolStripMenuItem.Text = (RGames.Count > 0) ? "Arquitetura (jogo em execução)" : "Arquitetura";
            arquiteturaToolStripMenuItem.Enabled = !(RGames.Count > 0);
            // Só deixa marcar o 64 bits caso o sistema e o jogo suporte
            usar64BitsToolStripMenuItem.Enabled = Environment.Is64BitOperatingSystem && File.Exists(Application.StartupPath + "\\" + Exe64);

            usar32BitsToolStripMenuItem.Checked = !Settings.Default.bUse64;
            usar64BitsToolStripMenuItem.Checked = Settings.Default.bUse64;
        }

        private async Task<bool> HasUpdate()
        {
            try
            {
                string SVersion = "xxx";
                string CVersion = "xxx";

                // Verifica a versão do server
                // Perfect World\patcher\server\updateserver.txt
                // Contém "patch"  "http://fpatch3.perfectworld.com.br/CPW/"
                // Ao adicionar version na url, retorna um file com a versão do servidor
                string TextoComUrl = File.ReadAllText(Application.StartupPath.Replace("\\element", "\\patcher\\server\\updateserver.txt"));
                var start = TextoComUrl.IndexOf("\"");
                var Remover = TextoComUrl.Substring(start, TextoComUrl.IndexOf("\"h") - start); // Pega todo text até o "h(...ttp://)
                TextoComUrl = TextoComUrl.Replace(Remover, ""); // Depois remove
                TextoComUrl = TextoComUrl.Replace("\"", ""); // Remove as "" que sobraram
                TextoComUrl = TextoComUrl.Replace("\r", ""); // Remove os \r (se tiver algum)
                TextoComUrl = TextoComUrl.Replace("\n", ""); // Remove os \n (se tiver algum)

                TextoComUrl += "element/version"; // Adiciona o resto da url necessária para pegar a versão

                // Usa um webclient pra ver o arquivo
                var wc = new WebClient();
                SVersion = await wc.DownloadStringTaskAsync(TextoComUrl);
                SVersion = SVersion.Replace("\n", ""); // Pega a versão já removendo os \n
                SVersion = SVersion.Replace("\r", ""); // Remove os \r caso haja algum

                // Verifica a versão do client
                // Perfect World\config\element\version.sw
                // Contém VERSÃOXX 0

                // Substitui o diretório atual pelo diretório do arquivo na hora de ler o texto
                CVersion = File.ReadAllText(Application.StartupPath.Replace("\\element", "\\config\\element\\version.sw"));
                CVersion = CVersion.Replace(" 0", ""); // Remove o 0 da versão
                CVersion = CVersion.Replace("\r", ""); // Remove os \r caso haja algum
                CVersion = CVersion.Replace("\n", "");// Remove os \n caso haja algum

                if (SVersion == "xxx" || CVersion == "xxx")
                {
                    WM.ShowMessage("Ocorreu um erro ao verificar a versão do server/client.\nPor favor, tente mais tarde.", 3);
                    return false;
                }

                return (SVersion != CVersion);
            }
            catch (Exception x)
            {
                WM.ShowMessage(x.ToString());
                return false;
            }
        }

        private async void CheckForClientUpdates()
        {
            Task<bool> update = HasUpdate();
            bHasUpdate = await update;

            if (bHasUpdate)
            {
                DialogResult dr = WM.ShowMessage("A versão do seu client é antiga e precisa ser atualizada.\nDeseja atualizar agora?", 1, true);
                if (dr == DialogResult.Yes)
                {
                    // Só para de perguntar se ele clicar em Yes.
                    Settings.Default.bCheckedForUpdates = true;

                    // Checa se o launcher existe
                    string LauncherPath = Application.StartupPath.Replace("\\element", "\\launcher\\Launcher.exe");
                    if (File.Exists(LauncherPath))
                    {
                        Process.Start(LauncherPath);
                    }
                    else
                    {
                        WM.ShowMessage("O launcher não pôde ser encontrado!\nFavor procurar por atualizações manualmente.", 3);
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Cria a pasta em que os arquivos do programa serão armazenados
            if (!Directory.Exists(Application.StartupPath + "\\Perfect Launcher"))
                Directory.CreateDirectory(Application.StartupPath + "\\Perfect Launcher");

            // Cria a pasta onde as listas de combo serão armazenadas
            if (!Directory.Exists(Application.StartupPath + "\\Perfect Launcher\\Lista de Combos"))
                Directory.CreateDirectory(Application.StartupPath + "\\Perfect Launcher\\Lista de Combos");

            // Cria a pasta de logs
            if (!Directory.Exists(Application.StartupPath + "\\Perfect Launcher\\Erros"))
                Directory.CreateDirectory(Application.StartupPath + "\\Perfect Launcher\\Erros");

            RollGlobalMessages();
            DownloadMessages();
            RefreshUsernamesOnComboBox();

            CheckForClientUpdates();

            // Reseta a checkbox da torre do martírio caso seja quarta-feira
            if (DateTime.Today.ToString("D").Contains("quarta-feira"))
                Settings.Default.DailyCheckups[10] = false;

            // Checar se alguma conta que estava aberta ao fechar o programa
            // continua aberta
            try
            {
                foreach (string s in Settings.Default.UsersBeforeClosing)
                {

                    var start = s.IndexOf("[") + 1;
                    var User = s.Substring(start, s.IndexOf("]") - start);

                    var start2 = s.IndexOf("{") + 1;
                    var processId = s.Substring(start2, s.IndexOf("}") - start2);

                    // Pega os processos do PW abertos, e verifica se o ID bate
                    Process[] pr = Process.GetProcessesByName(Settings.Default.bUse64 ? Exe64.Replace(".exe", "") : Exe32.Replace(".exe", ""));
                    foreach (Process Process in pr)
                    {
                        // Se o id do processo for igual ao id do processo salvo, adiciona a conta como aberta
                        if (Process.Id == Convert.ToInt32(processId))
                        {
                            OpenGame(Settings.Default.User.IndexOf(User), true, Convert.ToInt32(processId));
                        }
                    }
                }
            }
            catch(Exception x)
            {
                WM.ShowMessage(x.ToString(), 3);
            }
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            if (usersComboBox.SelectedItem == null)
                return;

            // Checa por updates
            if (bHasUpdate)
            {
                CheckForClientUpdates();

                // Se continuar tendo update, não abre conta nenhuma
                if (bHasUpdate)
                    return;
            }

            try
            {
                OpenGame(usersComboBox.SelectedIndex);

                // Seta o form temporáriamente como topmost por 2.5 segundos
                TemporaryTopMost(2500);
            }
            catch(Exception x)
            {
                // Caso dê algo errado...
                WM.ShowMessage(x.ToString(), 3);
            }
        }

        private void UpdateOpenRecentlyMenu()
        {
            // Exibe os menus de acordo com a quantidade de contas recentes
            cONTA1ToolStripMenuItem.Visible = (OpenRecently.Count > 4);
            cONTA2ToolStripMenuItem.Visible = (OpenRecently.Count > 3);
            cONTA3ToolStripMenuItem.Visible = (OpenRecently.Count > 2);
            cONTA4ToolStripMenuItem.Visible = (OpenRecently.Count > 1);
            cONTA5ToolStripMenuItem.Visible = (OpenRecently.Count > 0);

            if (OpenRecently.Count > 0)
                cONTA5ToolStripMenuItem.Text = OpenRecently[0];
            if (OpenRecently.Count > 1)
                cONTA4ToolStripMenuItem.Text = OpenRecently[1];
            if (OpenRecently.Count > 2)
                cONTA3ToolStripMenuItem.Text = OpenRecently[2];
            if (OpenRecently.Count > 3)
                cONTA2ToolStripMenuItem.Text = OpenRecently[3];
            if (OpenRecently.Count > 4)
                cONTA1ToolStripMenuItem.Text = OpenRecently[4];
        }

        private void CrashWatcherTimer_Tick(object sender, EventArgs e)
        {
            // TODO: Reformular o crashwatcher pra ele ficar constantemente verificando conta por conta
            // Creio que de x em x segundos do jeito que está, em máquinas mais fracas, dará um lag spike todo ciclo

            // Seta um novo interval todo tick (o interval será armazenado nas settings)
            CrashWatcherTimer.Interval = Settings.Default.CrashWatchInterval;

            // Pausa o crashwatcher todo começo de tick e inicia ele dnv no final
            CrashWatcherTimer.Stop();

            // Se não tiver nenhum jogo aberto ou se o user desativou o crashwatcher, se desativa
            if (RGames.Count <= 0)
            {
                // Ativar a troca de arquitetura (de x64 para x32 vice-versa)
                bBlockArchChange = false;
                return;
            }

            // Desativar a troca de arquitetura caso alguma conta esteja aberta
            bBlockArchChange = true;

            // Verifica se o processo ainda está aberto
            for (int i = 0; i < RGames.Count; i++)
            {
                // Se o processo NÃO estiver mais aberto
                if (!IsProcessRunning(RGames[i].ProcessId))
                {
                    // Verifica se foi um reportbug
                    if (WasReportBug())
                    {
                        // Salva o user
                        string User = RGames[i].User;

                        DialogResult dr = WM.ShowMessage("A conta '" + User + "' crashou.\nDeseja reabri-la?", 1, true);
                        if (dr == DialogResult.Yes)
                        {
                            // Remove a conta da lista,
                            RGames.Remove(RGames[i]);

                            // Fecha UMA janela de report bug (apenas uma, pois se várias contas crasharem ao mesmo tempo, o programa continuará detectando)
                            Process p = Process.GetProcessesByName("creportbugs")[0];
                            if (p != null)
                                p.Kill();

                            // Abre o jogo novamente
                            OpenGame(Settings.Default.User.IndexOf(User));
                        }
                        else
                        {
                            // Caso ele não queira abrir denovo, remove a conta da lista e fecha o reportbug
                            RGames.RemoveAt(i);

                            // Chama a função de update nos forms necessários (caso estejam abertos)
                            if (ComboForm != null)
                                ComboForm.UpdateUsers();

                            Process p = Process.GetProcessesByName("creportbugs")[0];
                            if (p != null)
                                p.Kill();
                        }
                    }
                    else
                    {
                        // Caso não tenha sido, só remove o rg da lista
                        RGames.RemoveAt(i);

                        // Chama a função de update nos forms necessários (caso estejam abertos)
                        if (ComboForm != null)
                            ComboForm.UpdateUsers();

                        // Volta 1 index
                        i--;
                    }

                    // Ativa/desativa a troca de arquiteturas
                    UpdateArchChecks();
                }
            }

            // Inicia o crashwatcher denovo
            CrashWatcherTimer.Start();
        }

        private bool IsProcessRunning(int ProcessId)
        {
            // TODO: Achar uma alternativa melhor (sem usar exception)
            try
            {
                // Checa se o processo existe
                Process p = Process.GetProcessById(ProcessId);

                // Checa se o processo bate com o do jogo                    // .exe minúsculo no caso de ser x64 e maiúsculo no caso de ser x32
                string ProcessName = (Settings.Default.bUse64 ? Exe64 : Exe32).Replace(Settings.Default.bUse64 ? ".exe" : ".EXE", "");
                if (p.ProcessName == ProcessName)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        private bool WasReportBug()
        {
            // Pega todos os processos de reportbug abertos
            Process[] p = Process.GetProcessesByName("creportbugs");

            // Esconde todos os reports bugs
            if (p.Length > 0)
                foreach (Process Process in p)
                    ManageWindow(Process);

            return p.Length > 0;
        }

        private static void ManageWindow(Process Process, int WindowState = 0)
        {
            IntPtr hWnd;
            hWnd = Process.MainWindowHandle;
            ShowWindow(hWnd, WindowState); // 0 = hidden
        }

        private void RemoveUserButton_Click(object sender, EventArgs e)
        {
            if (usersComboBox.SelectedItem == null)
                return;

            // Salva o usuário
            string User = Settings.Default.User[usersComboBox.SelectedIndex];

            DialogResult dr = WM.ShowMessage("Remover a conta '" + User + "'?", 1, true);
            if (dr == DialogResult.Yes)
            {
                ManageUsers m = new ManageUsers();
                bool bRemoved = m.RemoveUser(usersComboBox.SelectedIndex);

                // Se a conta foi removida, atualiza a lista
                if (bRemoved)
                {
                    // Remove da lista de jogos abertos, caso esteja nela
                    for (int i = 0; RGames.Count > 0 && i < RGames.Count; i++)
                    {
                        if (RGames[i].User == User)
                        {
                            RGames.RemoveAt(i);
                            i--;
                        }
                    }
                    // Atualiza a lista com os usuários
                    RefreshUsernamesOnComboBox();
                }
            }
        }

        private void AddUserButton_Click(object sender, EventArgs e)
        {
            AddEditUser addUser = new AddEditUser(this);
            addUser.ShowDialog();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // É pra fechar ou pra minimizar?
            if (!bShouldClose)
            {
                // Cancela o fechamento
                e.Cancel = true;

                // E minimiza pra taskbar
                MinimizeToTaskbar();

                // Desativa o first run
                Settings.Default.bFirstRun = false;
            }

            // Se nada deu errado durante a execução do programa
            if (bCanSave)
            {
                // Salva as contas abertas (se tiver alguma)
                Settings.Default.UsersBeforeClosing.Clear();
                if (RGames.Count > 0)
                {
                    for (int i = 0; i < RGames.Count; i++)
                    {
                        string User = RGames[i].User;
                        int pId = RGames[i].ProcessId;

                        // Adiciona neste formato {User}[ProcessID]
                        Settings.Default.UsersBeforeClosing.Add("[" + User + "]" + "{" + pId.ToString() + "}");
                    }
                }

                Settings.Default.Save();
            }
            else
            {
                WM.ShowMessage("As alterações feitas no programa NÃO foram salvas.", 2);
            }
        }

        private void MinimizeToTaskbar()
        {
            // Para de rodar as mensagens
            bBlockRoll = true;

            // A próxima vez que o usuário pedir pra fechar, ele fechará
            bShouldClose = true;

            // Exibe o ícone na taskbar e esconde o programa
            notifyIcon1.Visible = true;
            Hide();

            // Exibe a mensagem explicando que o programa foi minimizado
            // (caso seja a primeira vez que o programa foi aberto)
            if (Settings.Default.bFirstRun)
                notifyIcon1.ShowBalloonTip(2500);
        }

        private void MaximizeFromTaskbar()
        {
            // Volta à rodar as mensagens
            labelGlobal.Top = ScrollTextDefaultValue;
            bBlockRoll = false;
            RollGlobalMessages();

            // Atualiza o comboBox
            RefreshUsernamesOnComboBox();

            // A próxima vez que o usuário pedir pra fechar, ele minimizará
            bShouldClose = false;

            // Exibe o ícone na taskbar e esconde o programa
            notifyIcon1.Visible = false;
            Show();
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private async void DownloadMessages()
        {
            // Tem internet?
            if (!CheckForInternetConnection())
            {
                labelGlobal.Text = "Sem conexão com a internet.";
                return;
            }

            // Verifica se tem internet e se já passou 1h desde o último request pro global
            string hour = DateTime.Now.ToString("HH");

            if (hour != Settings.Default.LastGlobalUpdate)
            {
                // Pede um novo request, salva no bloco de notas
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                string url = "https://pastebin.com/raw/1B7k6kFa";
                string content = wc.DownloadString(url);

                await Task.Delay(50);

                File.WriteAllText(Application.StartupPath + "\\Perfect Launcher\\Global.txt", content, Encoding.UTF8);
            }

            MsgGlobal = File.ReadAllLines(Application.StartupPath + "\\Perfect Launcher\\Global.txt", Encoding.UTF8);

            // Salva o último horário atualizado
            Settings.Default.LastGlobalUpdate = hour;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ComboForm == null)
                ComboForm = new Combo(this);

            ComboForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Manage M = new Manage(this);
            M.ShowDialog();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MaximizeFromTaskbar();

                // Seta o form temporáriamente como topmost por 1.5 segundos
                TemporaryTopMost(1500);
            }
                
            if (e.Button == MouseButtons.Right)
                notifyIcon1.ContextMenuStrip.Show();
        }

        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cONTA5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OpenRecently.Count <= 0)
                return;

            if (!Settings.Default.User.Contains(OpenRecently[0]))
            {
                WM.ShowMessage("A conta '" + OpenRecently[0] + "' não pôde ser encontrada.", 3);
                return;
            }
            int Index = Settings.Default.User.IndexOf(OpenRecently[0]);

            OpenGame(Index);
        }

        private void cONTA4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OpenRecently.Count <= 1)
                return;

            if (!Settings.Default.User.Contains(OpenRecently[1]))
            {
                WM.ShowMessage("A conta '" + OpenRecently[1] + "' não pôde ser encontrada.", 3);
                return;
            }
            int Index = Settings.Default.User.IndexOf(OpenRecently[1]);

            OpenGame(Index);
        }

        private void cONTA3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OpenRecently.Count <= 2)
                return;

            if (!Settings.Default.User.Contains(OpenRecently[2]))
            {
                WM.ShowMessage("A conta '" + OpenRecently[2] + "' não pôde ser encontrada.", 3);
                return;
            }
            int Index = Settings.Default.User.IndexOf(OpenRecently[2]);

            OpenGame(Index);
        }

        private void cONTA2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OpenRecently.Count <= 3)
                return;

            if (!Settings.Default.User.Contains(OpenRecently[3]))
            {
                WM.ShowMessage("A conta '" + OpenRecently[3] + "' não pôde ser encontrada.", 3);
                return;
            }
            int Index = Settings.Default.User.IndexOf(OpenRecently[3]);

            OpenGame(Index);
        }

        private void cONTA1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OpenRecently.Count <= 4)
                return;

            if (!Settings.Default.User.Contains(OpenRecently[4]))
            {
                WM.ShowMessage("A conta '" + OpenRecently[4] + "' não pôde ser encontrada.", 3);
                return;
            }
            int Index = Settings.Default.User.IndexOf(OpenRecently[4]);

            OpenGame(Index);
        }

        private void tempTopMost_Tick(object sender, EventArgs e)
        {
            tempTopMost.Stop();
            TopMost = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CheckForm c = new CheckForm();
            c.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Customizar c = new Customizar(this);
            c.ShowDialog();
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void labelGlobal_Click(object sender, EventArgs e)
        {
            Global g = new Global(MsgGlobal);
            g.ShowDialog();
        }

        private void customizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void calculadoraDeFamaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CalcFama c = new CalcFama();
            c.ShowDialog();
        }

        private void combarComAPTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ComboForm == null)
                ComboForm = new Combo(this);

            ComboForm.Show();
        }

        private void checkinDiárioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckForm c = new CheckForm();
            c.ShowDialog();
        }

        private void gerenciarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Manage M = new Manage(this);
            M.ShowDialog();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Global g = new Global(MsgGlobal);
            g.ShowDialog();
        }

        private void twitterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://twitter.com/LibardiFelipe");
        }

        private void customizarToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Customizar c = new Customizar(this);
            c.ShowDialog();
        }

        private void usar32BitsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.bUse64 = false;
            UpdateArchChecks();
        }

        private void usar64BitsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.bUse64 = true;
            UpdateArchChecks();
        }

        private void logsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Se o arquivo de logs não existir, cria um e abre
            if (!File.Exists(Application.StartupPath + "\\Perfect Launcher\\Logs.txt"))
                File.WriteAllText(Application.StartupPath + "\\Perfect Launcher\\Logs.txt", "");

            try
            {
                Process.Start(Application.StartupPath + "\\Perfect Launcher\\Logs.txt");
            }catch(Exception x)
            {
                WM.ShowMessage(x.ToString(), 3);
            }
        }

        private void fecharContasEXCETOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseAll ca = new CloseAll(RGames);
            ca.ShowDialog();
        }

        private void checkinDiárioToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CheckForm c = new CheckForm();
            c.ShowDialog();
        }

        private void combarComAPTToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (ComboForm == null)
                ComboForm = new Combo(this);

            ComboForm.Show();
        }

        private void fecharContasEXCETOToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CloseAll ca = new CloseAll(RGames);
            ca.ShowDialog();
        }

        private void toolStripComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (toolStripComboBox2.SelectedIndex > 0)
            {
                OpenGame(Settings.Default.User.IndexOf(toolStripComboBox2.SelectedItem.ToString()));

                // Volta pro selectedIndex 0 e fecha o menu
                toolStripComboBox2.SelectedIndex = 0;
                contextMenuStrip1.Hide();
            }
        }

        private void atualizarOPWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Checa se o launcher existe
            string LauncherPath = Application.StartupPath.Replace("\\element", "\\launcher\\Launcher.exe");
            if (File.Exists(LauncherPath))
            {
                Process.Start(LauncherPath);
            }
            else
            {
                WM.ShowMessage("O launcher não pôde ser encontrado!\nFavor procurar por atualizações manualmente.", 3);
            }
        }

        private void picPayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PicPay pp = new PicPay();
            pp.ShowDialog(); // pp.show kkkkk
        }

        private void notasDeAtualizaçãoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://pastebin.com/raw/rGnU5DFs");
        }

        private void importarToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog od = new OpenFileDialog();
            od.Filter = "Lista de Contas | *.plcontas";
            od.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            od.FileName = "";
            od.ValidateNames = true;

            if (od.ShowDialog() == DialogResult.OK)
            {
                int Contador = 0;
                string[] Contas = File.ReadAllLines(od.FileName);

                ManageUsers m = new ManageUsers();

                foreach (string s in Contas)
                {

                    // ¹conta² ³senha£ ¢classe¬
                    var start = s.IndexOf("¹") + 1;
                    var User = s.Substring(start, s.IndexOf("²") - start);

                    // Se já tiver a conta adicionada, skippa pra próxima
                    if (Settings.Default.User.Contains(User))
                        continue;

                    var start2 = s.IndexOf("³") + 1;
                    var Passwd = s.Substring(start2, s.IndexOf("£") - start2);

                    var start3 = s.IndexOf("¢") + 1;
                    var Classe = s.Substring(start3, s.IndexOf("¬") - start3);

                    m.AddUser(User, Passwd, Classe);

                    Contador++;
                }

                if (Contador > 0)
                    WM.ShowMessage("Adicionada(s) " + Contador.ToString() + " nova(s) conta(s)!");
                else
                    WM.ShowMessage("Todas as contas encontradas no arquivo já estão no programa.");

                RefreshUsernamesOnComboBox();
            }
        }

        private void exportarToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            // Cria um arquivo com todas as contas, senhas e classes no seguinte esquema:
            // ¹conta² ³senha£ ¢classe¬
            if (Settings.Default.Passwd.Count != Settings.Default.User.Count || Settings.Default.Classe.Count != Settings.Default.User.Count)
            {
                // Se TODAS essa settings não tiverem o mesmo número, tem algo errado ai.
                WM.ShowMessage("O número de usuários/senhas/classes é(são) diferente(s)!", 3);
                return;
            }

            string Tudo = "";
            for (int i = 0; i < Settings.Default.User.Count; i++)
                Tudo += "¹" + Settings.Default.User[i] + "²³" + Settings.Default.Passwd[i] + "£¢" + Settings.Default.Classe[i] + "¬\n";

            // Salva tudo
            SaveFileDialog sd = new SaveFileDialog();
            sd.Filter = "Lista de Contas | *.plcontas";
            sd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            sd.FileName = "Contas";
            sd.ValidateNames = true;

            if (sd.ShowDialog() == DialogResult.OK)
                File.WriteAllText(sd.FileName, Tudo);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            FormAtalhos fa = new FormAtalhos();
            fa.ShowDialog();
        }

        private void resetarOProgramaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dr = WM.ShowMessage("Isso resetará todas as contas e as configurações salvas.\nDeseja continuar?", 1, true);
            if (dr == DialogResult.Yes)
            {
                // Fecha direto ao invés de minimizar
                bShouldClose = true;

                Settings.Default.Reset();
                Settings.Default.Save();
                Application.Restart();
            }
        }

        private void executáveisEArgumentosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExeArgs ea = new ExeArgs();
            ea.ShowDialog();
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings.Default.ForceServer = toolStripComboBox1.SelectedItem.ToString();
        }

        private void configurarAtalhosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAtalhos fa = new FormAtalhos();
            fa.ShowDialog();
        }
    }
}
