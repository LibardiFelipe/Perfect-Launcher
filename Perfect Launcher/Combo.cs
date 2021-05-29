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
using Perfect_Launcher.Properties;
using System.Runtime.InteropServices;
using System.IO;

namespace Perfect_Launcher
{
    public partial class Combo : Form
    {

        private bool bBlockNext = false;
        private bool bBlockNumpad = true;
        private bool bBlockBandF = false;
        private bool bBlockBack = false;

        private bool bWasMinimized = false;

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(HandleRef hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        internal struct INPUT
        {
            public UInt32 Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }
        internal struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }

        // Exibe mensagens
        WarningMessages w = new WarningMessages();

        // Mantém o track do último item selecionado na listbox2
        int SelectedIndex = -1;

        // Mantém o track do penúltimo item selecionado na listbox2
        int PrevSelectedIndex = -1;

        // Keyboard hook
        private GlobalKeyboardHook _globalKeyboardHook;

        // Teclas
        const uint WM_KEYDOWN = 0x100;
        const int VK_F1 = 0x70;
        const int VK_F2 = 0x71;
        const int VK_F3 = 0x72;
        const int VK_F4 = 0x73;
        const int VK_F5 = 0x74;
        const int VK_F6 = 0x75;
        const int VK_F7 = 0x76;
        const int VK_F8 = 0x77;


        public const int SW_RESTORE = 9;

        // Form1 ref
        Form1 f1;

        // Ordem de processo das contas na fila
        List<Process> ProcessQueue = new List<Process>();

        // Lista de index das contas na fila
        List<int> IndexQueue = new List<int>();

        // Lista de contas abertas
        List<RunningGames> RGames = new List<RunningGames>();

        // Força o paramento da sequência
        bool bForceStop = false;

        // Loop do combo
        int Count = 1;

        // Bloqueia ações envolvendo contas quando alterações estiverem sendo feitas
        bool bChangeBlocked = false;

        public Combo(Form1 frm1)
        {
            InitializeComponent();

            f1 = frm1;
        }

        public static void ClickOnPoint(IntPtr wndHandle, Point clientPoint)
        {
            var oldPos = Cursor.Position;

            /// get screen coordinates
            ClientToScreen(wndHandle, ref clientPoint);

            /// set cursor on coords, and press mouse
            Cursor.Position = new Point(clientPoint.X, clientPoint.Y);

            var inputMouseDown = new INPUT();
            inputMouseDown.Type = 0; /// input type mouse
            inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

            var inputMouseUp = new INPUT();
            inputMouseUp.Type = 0; /// input type mouse
            inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up

            var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

            /// return mouse 
            Cursor.Position = oldPos;
        }

        public void UpdateUsers()
        {
            bChangeBlocked = true;

            UpdateListBox1();
            UpdateListBox2();

            // Sempre reseta o selected index anterior
            PrevSelectedIndex = -1;
            SelectedIndex = -1;

            bChangeBlocked = false;
        }

        private static Keys GetKeyFromIndex(sbyte Index)
        {
            /*
            0 Shift Esquerdo
            1 Shift Direito
            2 Ctrl Esquerdo
            3 Ctrl Direito
            4 Alt Esquerdo
            5 Alt Direito
            6 Aspas "
            7 Seta para cima
            8 Seta para baixo
            9 Seta para direita
            10 Seta para esquerda
            11/ do teclado numérico
            12 *(asterísco) do teclado numérico
            13 -(menos) do teclado numérico
            14 + do teclado numérico */

            switch (Index)
            {
                case 0: return Keys.LShiftKey;
                case 1: return Keys.RShiftKey;
                case 2: return Keys.LControlKey;
                case 3: return Keys.RControlKey;
                case 4: return Keys.Alt;
                case 5: return Keys.RMenu;
                case 6: return Keys.Oemtilde;
                case 7: return Keys.Up;
                case 8: return Keys.Down;
                case 9: return Keys.Right;
                case 10: return Keys.Left;
                case 11: return Keys.Divide;
                case 12: return Keys.Multiply;
                case 13: return Keys.Subtract;
                case 14: return Keys.Add;
                default: return Keys.None;
            }
        }

        private void HookKeys()
        {
            // TODO: Hookar somente as teclas utilizadas nos atalhos pra ver se o Windows Defender
            // para de encher o saco...

            Keys[] AllKeys = new Keys[] { Keys.LShiftKey, Keys.RShiftKey,
                Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, Keys.NumPad6,
                Keys.NumPad7, Keys.NumPad8, Keys.NumPad9, Keys.LControlKey, Keys.RControlKey, Keys.Alt, Keys.RMenu, Keys.Oemtilde,
                Keys.Up, Keys.Down, Keys.Right, Keys.Left, Keys.Divide, Keys.Multiply, Keys.Subtract, Keys.Add };

            _globalKeyboardHook = new GlobalKeyboardHook(AllKeys);

            // Assimila o handler
            _globalKeyboardHook.KeyboardPressed += OnKeyPressed;
        }

        private void OnKeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            // EDT: No need to filter for VkSnapshot anymore. This now gets handled
            // through the constructor of GlobalKeyboardHook(...).

            if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
            {
                // Retorna caso a troca de contas esteja bloqueada
                if (bChangeBlocked)
                    return;

                Keys Next = GetKeyFromIndex(Settings.Default.ProximaContaBtn);
                Keys Back = GetKeyFromIndex(Settings.Default.ContaAnteriorBtn);
                Keys BandF = GetKeyFromIndex(Settings.Default.RevezarContasBtn);

                // Sim, eu fiz com um monte de elseif mesmo, me processa.
                if (e.KeyboardData.Key == Next)
                {
                    TryToChangeUser(0);
                }
                else if (e.KeyboardData.Key == Back)
                {
                    TryToChangeUser(2);
                }
                else if (e.KeyboardData.Key == BandF)
                {
                    TryToChangeUser(1);
                }
                else if (e.KeyboardData.Key == Keys.NumPad1)
                {
                    ChangeUserBasedOnIndex(0);
                }
                else if (e.KeyboardData.Key == Keys.NumPad2)
                {
                    ChangeUserBasedOnIndex(1);
                }
                else if (e.KeyboardData.Key == Keys.NumPad3)
                {
                    ChangeUserBasedOnIndex(2);
                }
                else if (e.KeyboardData.Key == Keys.NumPad4)
                {
                    ChangeUserBasedOnIndex(3);
                }
                else if (e.KeyboardData.Key == Keys.NumPad5)
                {
                    ChangeUserBasedOnIndex(4);
                }
                else if (e.KeyboardData.Key == Keys.NumPad6)
                {
                    ChangeUserBasedOnIndex(5);
                }
                else if (e.KeyboardData.Key == Keys.NumPad7)
                {
                    ChangeUserBasedOnIndex(6);
                }
                else if (e.KeyboardData.Key == Keys.NumPad8)
                {
                    ChangeUserBasedOnIndex(7);
                }
                else if (e.KeyboardData.Key == Keys.NumPad9)
                {
                    ChangeUserBasedOnIndex(8);
                }
                else if (e.KeyboardData.Key == Keys.NumPad0)
                {
                    ChangeUserBasedOnIndex(9);
                }

                /*
                switch(e.KeyboardData.Key)
                {
                    case Keys.Oemtilde: TryToChangeUser(0); break;
                    case Keys.LShiftKey: TryToChangeUser(1); break;
                    //case Keys.D1:
                    case Keys.NumPad1:
                        ChangeUserBasedOnIndex(0); break;
                    //case Keys.D2:
                    case Keys.NumPad2:
                        ChangeUserBasedOnIndex(1); break;
                    //case Keys.D3:
                    case Keys.NumPad3:
                        ChangeUserBasedOnIndex(2); break;
                    //case Keys.D4:
                    case Keys.NumPad4:
                        ChangeUserBasedOnIndex(3); break;
                    //case Keys.D5:
                    case Keys.NumPad5:
                        ChangeUserBasedOnIndex(4); break;
                    //case Keys.D6:
                    case Keys.NumPad6:
                        ChangeUserBasedOnIndex(5); break;
                    //case Keys.D7:
                    case Keys.NumPad7:
                        ChangeUserBasedOnIndex(6); break;
                    //case Keys.D8:
                    case Keys.NumPad8:
                        ChangeUserBasedOnIndex(7); break;
                    //case Keys.D9:
                    case Keys.NumPad9:
                        ChangeUserBasedOnIndex(8); break;
                    //case Keys.D0:
                    case Keys.NumPad0:
                        ChangeUserBasedOnIndex(9); break;
                    default: break;
                } */
            }
        }

        private void ChangeUserBasedOnIndex(int Index)
        {
            if (bBlockNumpad)
                return;

            // Seleciona o index na listbox
            if (listBox2.Items.Count > Index)
            {
                listBox2.SelectedIndex = Index;
            }
            else
            {
                return;
            }

            BringFormToTop();
            listBox2_DoubleClick(this, null);

            // Se janela estava minimizada, minimiza ela de volta
            if (bWasMinimized)
                WindowState = FormWindowState.Minimized;
        }

        private void TryToChangeUser(int Order)
        {
            if (listBox2.SelectedItem == null)
                return;

            switch (Order)
            {
                case 0:
                    if (bBlockNext)
                        return;

                    // Seleciona o próximo index da lista fazendo um loop por todos
                    if (listBox2.SelectedIndex >= listBox2.Items.Count - 1)
                    {
                        listBox2.SelectedIndex = 0;
                    }
                    else
                    {
                        listBox2.SelectedIndex += 1;
                    }

                    break;
                case 1:
                    if (bBlockBandF)
                        return;

                    // Index inválido?
                    if (PrevSelectedIndex == -1 || listBox2.Items.Count <= PrevSelectedIndex)
                        return;

                    listBox2.SelectedIndex = PrevSelectedIndex;

                    break;

                default:
                    if (bBlockBack)
                        return;

                    // Seleciona o index anterior
                    if ((listBox2.SelectedIndex-1) >= 0)
                    {
                        listBox2.SelectedIndex -= 1;
                    }
                    else
                    {
                        listBox2.SelectedIndex = listBox2.Items.Count - 1;
                    }

                    break;
            }

            BringFormToTop();
            listBox2_DoubleClick(this, null);
        } //

        private void BringFormToTop()
        {
            try
            {
                IntPtr hWnd = Handle;
                ShowWindowAsync(new HandleRef(null, hWnd), SW_RESTORE);
                SetForegroundWindow(hWnd);
                Activate();
                ClickOnPoint(Handle, new Point(2, 2));
            }
            catch
            {
                // Feedback auditivo pra saber que algo deu errado
                // (não usar messagebox pois pode atrapalhar no combo)
                System.Media.SystemSounds.Beep.Play();
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // Se o item for nulo ou a fila de index já tiver o index selecionado
            if (listBox1.SelectedItem == null)
                return;

            foreach (string s in listBox1.SelectedItems)
            {
                // Só adiciona caso o item ainda não exista na lista
                if (!IndexQueue.Contains(Settings.Default.User.IndexOf(s)))
                    IndexQueue.Add(Settings.Default.User.IndexOf(s));
            }

            // Atualiza a listbox2
            UpdateListBox2();
        }

        private void UpdateListBox2()
        {
            // Atualiza a listbox2 baseada nos index
            listBox2.Items.Clear();
            listBox3.Items.Clear();

            // Só adiciona caso o listbox1 contenha a conta do index queue
            // Caso não tenha, a conta deverá ser removida do indexqueue
            int Contador = 0;
            for (int i = 0; i < IndexQueue.Count; i++)
            {
                try
                {
                    if (listBox1.Items.Contains(Settings.Default.User[IndexQueue[i]]))
                    {
                        int tempOrdem = i + 1;
                        if (tempOrdem >= 10)
                            tempOrdem = 0;
                        listBox2.Items.Add(tempOrdem.ToString() + ") " + Settings.Default.Classe[IndexQueue[i]]);
                        listBox3.Items.Add(tempOrdem.ToString() + ") " + Settings.Default.Classe[IndexQueue[i]]);
                        Contador++;
                    }
                    else
                    {
                        // Caso a listbox1 NÃO TENHA, significa que a conta fechou
                        // Remove ela da lista de index
                        IndexQueue.RemoveAt(i);
                        i--;
                    }
                }
                catch
                {

                }
            }
            labelNoCombo.Text = "CONTAS NO COMBO (" + Contador.ToString() + ")";

            // Atualiza a fila de processos
            ProcessQueue.Clear();
            for (int i = 0; i < IndexQueue.Count; i++)
                foreach (RunningGames rg in RGames)
                    try
                    {
                        if (rg.User == Settings.Default.User[IndexQueue[i]])
                        {
                            ProcessQueue.Add(Process.GetProcessById(rg.ProcessId));
                            break;
                        }
                    }
                    catch (Exception x)
                    {

                    }
            

            // Seleciona o index selecionado anteriormente
            if (listBox2.Items.Count > SelectedIndex)
            {
                listBox2.SelectedIndex = SelectedIndex;
            }
                
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null)
                return;

            try
            {
                // Remove a conta do index queue
                IndexQueue.RemoveAt(listBox2.SelectedIndex);

                // Atualiza a listbox2
                UpdateListBox2();
            }
            catch
            {

            }
        }

        private void Combo_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            _globalKeyboardHook.Dispose();
            f1.ComboForm = null;
        }

        private void UpdateListBox1()
        {
            // Pega os jogos abertos
            RGames = f1.GetRunningGames();

            // Adiciona-os para a listbox1
            int Contador = 0;
            listBox1.Items.Clear();
            foreach (RunningGames rg in RGames)
            {
                Contador++;
                listBox1.Items.Add(rg.User);
            }
            labelAbertas.Text = "CONTAS ABERTAS (" + Contador.ToString() + ")";
        }

        private static void BringToTop(Process Process)
        {
            if (Process == null)
                return;

            IntPtr hWnd = IntPtr.Zero;
            hWnd = Process.MainWindowHandle;
            ShowWindowAsync(new HandleRef(null, hWnd), SW_RESTORE);
            SetForegroundWindow(hWnd);
        }

        private static void SendKeyPress(Process Process, int KEY)
        {
            IntPtr hWnd;
            hWnd = Process.MainWindowHandle;
            PostMessage(hWnd, WM_KEYDOWN, KEY, 1);
        }

        private void UpdatePrevAndNewSelectedIndex(int NewIndex)
        {
            if (SelectedIndex != NewIndex)
            {
                PrevSelectedIndex = SelectedIndex;
                SelectedIndex = NewIndex;
            }
        }

        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (listBox2.SelectedItem != null || !bChangeBlocked)
                {
                    UpdatePrevAndNewSelectedIndex(listBox2.SelectedIndex);

                    if (ProcessQueue.Count > listBox2.SelectedIndex)
                        BringToTop(ProcessQueue[listBox2.SelectedIndex]);
                }
            }
            catch
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null || listBox2.SelectedIndex <= 0 || bChangeBlocked)
                return;

            try
            {
                // Salva o index anterior ao selecionado
                int PrevIndex = IndexQueue[listBox2.SelectedIndex - 1];

                // Seta o index anterior para o valor do index selecionado
                IndexQueue[listBox2.SelectedIndex - 1] = IndexQueue[listBox2.SelectedIndex];

                // Seta o index selecionado para o valor salvo
                IndexQueue[listBox2.SelectedIndex] = PrevIndex;

                // Diminui o index selecionado
                SelectedIndex = listBox2.SelectedIndex - 1;

            }
            catch
            {

            }

            // Atualiza a listbox2
            UpdateListBox2();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null || listBox2.SelectedIndex >= listBox2.Items.Count - 1 || bChangeBlocked)
                return;

            try
            {
                // Salva o próximo index do selecionado
                int NextIndex = IndexQueue[listBox2.SelectedIndex + 1];

                // Seta o próximo index para o valor do index selecionado
                IndexQueue[listBox2.SelectedIndex + 1] = IndexQueue[listBox2.SelectedIndex];

                // Seta o index selecionado para o valor salvo
                IndexQueue[listBox2.SelectedIndex] = NextIndex;

                // Aumenta o index selecionado
                SelectedIndex = listBox2.SelectedIndex + 1;
            }
            catch
            {

            }


            // Atualiza a listbox2
            UpdateListBox2();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (bChangeBlocked || ProcessQueue.Count <= 0)
            {
                Stop();
                return;
            }

            Start(false);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void Start(bool OnlyF4)
        {
            bChangeBlocked = true;
            bForceStop = false;
            Count = 1;
            label5.Text = "Iniciando...";
            numericUpDown1.Enabled = false;
            numericUpDown2.Enabled = false;
            button5.Enabled = false;
            button14.Enabled = false;
            button15.Enabled = true;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            button6.Enabled = true;
            button11.Enabled = false;
            checkBox1.Enabled = false;
            button10.Enabled = false;
            button9.Enabled = false;

            // Chama o loop de combo
            ComboLoop(OnlyF4);
        }

        private void Stop()
        {
            bForceStop = true;
            Count = 1;
            label5.Text = "Esperando...";
            numericUpDown1.Enabled = true;
            numericUpDown2.Enabled = true;
            button5.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button7.Enabled = true;
            button8.Enabled = true;
            button6.Enabled = false;
            button14.Enabled = true;
            button15.Enabled = false;
            button10.Enabled = true;
            button11.Enabled = true;
            bChangeBlocked = false;
            button9.Enabled = true;
            checkBox1.Enabled = true;

            if (ProcessQueue.Count > 0)
            {
                BringToTop(ProcessQueue[0]);

                if (listBox2.Items.Count > 0)
                {
                    listBox2.SelectedIndex = 0;
                }
                    
            }
        }

        private async void ComboLoop(bool OnlyF4)
        {
            // Se o forcestop for true ou o count >= 8 (chegou ja última tecla pressionada)
            if (bForceStop || ProcessQueue.Count <= 0 || OnlyF4 ? (Count > 4) : (Count > 8))
            {
                Stop();
                return;
            }

            int KEY;

            switch (Count)
            {
                case 1: KEY = VK_F1; label5.Text = "Pressionando F1"; break;
                case 2: KEY = VK_F2; label5.Text = "Pressionando F2"; break;
                case 3: KEY = VK_F3; label5.Text = "Pressionando F3"; break;
                case 4: KEY = VK_F4; label5.Text = "Pressionando F4"; break;
                case 5: KEY = VK_F5; label5.Text = "Pressionando F5"; break;
                case 6: KEY = VK_F6; label5.Text = "Pressionando F6"; break;
                case 7: KEY = VK_F7; label5.Text = "Pressionando F7"; break;
                default: KEY = VK_F8; label5.Text = "Pressionando F8"; break;
            }

            // Se o onlyf4 for true, força a key pra ser o F4
            if (OnlyF4)
                KEY = VK_F4;

            // Pra cada conta na lista, joga a janela pra frente e pressiona a tecla
            try
            {
                for (int i = 0; i < ProcessQueue.Count; i++)
                {
                    // Se o forcestop for true ou o count >= 8 (chegou ja última tecla pressionada)
                    if (bForceStop || ProcessQueue.Count <= 0 || OnlyF4 ? (Count > 4) : (Count > 8))
                    {
                        Stop();
                        return;
                    }

                    // Mostra na lista a conta que está sendo exibida
                    listBox2.SelectedIndex = i;

                    // Traz a janela pra frente e manda a tecla
                    BringToTop(ProcessQueue[i]);
                    await Task.Delay(5);
                    SendKeyPress(ProcessQueue[i], KEY);

                    // Dorme x segundos
                    // Valor do numericupdown / pela quantidade de contas abertas
                    await Task.Delay((Count <= 4 ? Convert.ToInt32(numericUpDown1.Value) : Convert.ToInt32(numericUpDown2.Value)) / ProcessQueue.Count);
                }

                // Caso o onlyf4 seja true, checa se é em loop
                if (OnlyF4)
                {
                    // Se NÃO for um loop, já seta o valor necessário pra parar na próxima iteração
                    if (!checkBox1.Checked)
                        Stop();
                }
                else
                {
                    // Só mantem o track se o Onlyf4 for false
                    Count++;
                }
                ComboLoop(OnlyF4);
            }
            catch(Exception x)
            {
                Stop();
                WarningMessages wm = new WarningMessages();
                wm.ShowMessage(x.ToString(), 3);
            }
        }

        private void Combo_Load_1(object sender, EventArgs e)
        {
            // Hooka as teclas
            HookKeys();

            UpdateListBox1();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (IndexQueue.Count <= 0 || listBox2.Items.Count <= 0)
                return;

            // Cria a string com as contas abertas
            string Contas = "";

            // A primeira linha da string será os timers
            Contas += "[" + numericUpDown1.Value.ToString() + "]{" + numericUpDown2.Value.ToString() + "}";

            // Depois o resto das contas é adicionado
            foreach (int i in IndexQueue)
                if (Settings.Default.User[i] != null)
                    Contas += "\n" + Settings.Default.User[i];

            // Salva o arquivo
            SaveFileDialog sd = new SaveFileDialog();
            sd.Filter = "Lista de Combo | *.plcombo";
            sd.InitialDirectory = Application.StartupPath + "\\Perfect Launcher\\Lista de Combos";
            sd.FileName = "Lista";
            sd.ValidateNames = true;

            if (sd.ShowDialog() == DialogResult.OK)
                File.WriteAllText(sd.FileName, Contas);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            DialogResult dr = DialogResult.Yes;

            // Se já existir contas na fila, pergunta se realmente deseja carregar
            if (IndexQueue.Count > 0)
                dr = w.ShowMessage("Carregar a lista irá sobrepor as contas já adicionadas.\nDeseja continuar mesmo assim?", 1, true);

            // Caso a resposta NÃO SEJA mais yes, cancela
            if (dr != DialogResult.Yes)
                return;

            OpenFileDialog od = new OpenFileDialog();
            od.Filter = "Lista de Combo | *.plcombo";
            od.InitialDirectory = Application.StartupPath + "\\Perfect Launcher\\Lista de Combos";
            od.FileName = "";
            od.ValidateNames = true;

            if (od.ShowDialog() == DialogResult.OK)
            {
                // Só limpa o index caso o dialog seja OK
                IndexQueue.Clear();

                string[] Combo = File.ReadAllLines(od.FileName);

                // Pega a primeira linha do text, que é referente aos timers
                string Timers = Combo[0];

                // Começa do index 1, pois o 0 são os timers
                for (int i = 1; i < Combo.Length; i++)
                    if (Settings.Default.User.Contains(Combo[i]))
                        IndexQueue.Add(Settings.Default.User.IndexOf(Combo[i]));

                // Seta os timings [numeric1]{numeric2}
                var start = Timers.IndexOf("[") + 1;
                var numeric1 = Timers.Substring(start, Timers.IndexOf("]") - start);

                var start2 = Timers.IndexOf("{") + 1;
                var numeric2 = Timers.Substring(start2, Timers.IndexOf("}") - start2);

                try
                {
                    numericUpDown1.Value = Convert.ToInt32(numeric1);
                    numericUpDown2.Value = Convert.ToInt32(numeric2);
                }
                catch (Exception x)
                {
                    w.ShowMessage(x.ToString(), 3);
                }

                // Atualiza a listbox2 baseada na listbox1
                UpdateListBox2();
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null || !bChangeBlocked)
            {
                if (ProcessQueue.Count > listBox2.SelectedIndex)
                {
                    // Pega o processo direto do runningGames
                    foreach (RunningGames rg in RGames)
                    {
                        if (rg.User == listBox1.SelectedItem.ToString())
                        {
                            BringToTop(Process.GetProcessById(rg.ProcessId));

                            break;
                        }
                    }
                }
                else
                {
                    w.ShowMessage("Lista de processos vazia.");
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            UpdateListBox1();
            UpdateListBox2();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (bForceStop || ProcessQueue.Count <= 0)
            {
                Stop();
                return;
            }

            Start(true);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void listBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (listBox3.SelectedIndex == listBox2.SelectedIndex || listBox2.SelectedItem == null)
                return;

            if (listBox3.Items.Count > listBox2.SelectedIndex)
                listBox3.SelectedIndex = listBox2.SelectedIndex;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
            Button btnSender = (Button)sender;
            Point ptLowerLeft = new Point(0, btnSender.Height);
            ptLowerLeft = btnSender.PointToScreen(ptLowerLeft);
            contextMenuStrip1.Show(ptLowerLeft);
        }

        private void trocaEntreDuasContasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bBlockBandF = !trocaEntreDuasContasToolStripMenuItem.Checked;
        }

        private void próximaContaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bBlockNext = !próximaContaToolStripMenuItem.Checked;
        }

        private void tecladoNuméricoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bBlockNumpad = !tecladoNuméricoToolStripMenuItem.Checked;
        }

        private void Combo_Resize(object sender, EventArgs e)
        {
            bWasMinimized = WindowState == FormWindowState.Minimized;
        }

        private void listBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                button2_Click_1(this, e);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            // Tamanho do form full 535; 465
            // Tamanho do form encolhido 190; 310
            Size Pequeno = new Size(190, 310);
            Size = Pequeno;
            tabControl1.SelectedIndex = 1;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            // Tamanho do form full 535; 465
            // Tamanho do form encolhido 190; 310
            Size Grande = new Size(535, 465);
            Size = Grande;
            tabControl1.SelectedIndex = 0;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            button5_Click(this, e);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            button6_Click(this, e);
        }

        private void listBox3_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (listBox3.SelectedItem != null || !bChangeBlocked)
                {
                    UpdatePrevAndNewSelectedIndex(listBox3.SelectedIndex);

                    if (ProcessQueue.Count > listBox3.SelectedIndex)
                        BringToTop(ProcessQueue[listBox3.SelectedIndex]);
                }
            }
            catch
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }

        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void pularParaContaDeCimaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bBlockBack = !pularParaContaDeCimaToolStripMenuItem.Checked;
        }

        private void listBox3_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex == listBox3.SelectedIndex || listBox3.SelectedItem == null)
                return;

            if (listBox2.Items.Count > listBox3.SelectedIndex)
                listBox2.SelectedIndex = listBox3.SelectedIndex;
        }
    }


    class GlobalKeyboardHookEventArgs : HandledEventArgs
    {
        public GlobalKeyboardHook.KeyboardState KeyboardState { get; private set; }
        public GlobalKeyboardHook.LowLevelKeyboardInputEvent KeyboardData { get; private set; }

        public GlobalKeyboardHookEventArgs(
            GlobalKeyboardHook.LowLevelKeyboardInputEvent keyboardData,
            GlobalKeyboardHook.KeyboardState keyboardState)
        {
            KeyboardData = keyboardData;
            KeyboardState = keyboardState;
        }
    }

    //Based on https://gist.github.com/Stasonix
    class GlobalKeyboardHook : IDisposable
    {
        public event EventHandler<GlobalKeyboardHookEventArgs> KeyboardPressed;

        // EDT: Added an optional parameter (registeredKeys) that accepts keys to restict
        // the logging mechanism.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="registeredKeys">Keys that should trigger logging. Pass null for full logging.</param>
        public GlobalKeyboardHook(Keys[] registeredKeys = null)
        {
            RegisteredKeys = registeredKeys;
            _windowsHookHandle = IntPtr.Zero;
            _user32LibraryHandle = IntPtr.Zero;
            _hookProc = LowLevelKeyboardProc; // we must keep alive _hookProc, because GC is not aware about SetWindowsHookEx behaviour.

            _user32LibraryHandle = LoadLibrary("User32");
            if (_user32LibraryHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to load library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }



            _windowsHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, _user32LibraryHandle, 0);
            if (_windowsHookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to adjust keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // because we can unhook only in the same thread, not in garbage collector thread
                if (_windowsHookHandle != IntPtr.Zero)
                {
                    if (!UnhookWindowsHookEx(_windowsHookHandle))
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode, $"Failed to remove keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                    }
                    _windowsHookHandle = IntPtr.Zero;

                    // ReSharper disable once DelegateSubtraction
                    _hookProc -= LowLevelKeyboardProc;
                }
            }

            if (_user32LibraryHandle != IntPtr.Zero)
            {
                if (!FreeLibrary(_user32LibraryHandle)) // reduces reference to library by 1.
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to unload library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }
                _user32LibraryHandle = IntPtr.Zero;
            }
        }

        ~GlobalKeyboardHook()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private IntPtr _windowsHookHandle;
        private IntPtr _user32LibraryHandle;
        private HookProc _hookProc;

        delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain.
        /// You would install a hook procedure to monitor the system for certain types of events. These events are
        /// associated either with a specific thread or with all threads in the same desktop as the calling thread.
        /// </summary>
        /// <param name="idHook">hook type</param>
        /// <param name="lpfn">hook procedure</param>
        /// <param name="hMod">handle to application instance</param>
        /// <param name="dwThreadId">thread identifier</param>
        /// <returns>If the function succeeds, the return value is the handle to the hook procedure.</returns>
        [DllImport("USER32", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        /// <summary>
        /// The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.
        /// </summary>
        /// <param name="hhk">handle to hook procedure</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("USER32", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hHook);

        /// <summary>
        /// The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain.
        /// A hook procedure can call this function either before or after processing the hook information.
        /// </summary>
        /// <param name="hHook">handle to current hook</param>
        /// <param name="code">hook code passed to hook procedure</param>
        /// <param name="wParam">value passed to hook procedure</param>
        /// <param name="lParam">value passed to hook procedure</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("USER32", SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hHook, int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct LowLevelKeyboardInputEvent
        {
            /// <summary>
            /// A virtual-key code. The code must be a value in the range 1 to 254.
            /// </summary>
            public int VirtualCode;

            // EDT: added a conversion from VirtualCode to Keys.
            /// <summary>
            /// The VirtualCode converted to typeof(Keys) for higher usability.
            /// </summary>
            public Keys Key { get { return (Keys)VirtualCode; } }

            /// <summary>
            /// A hardware scan code for the key. 
            /// </summary>
            public int HardwareScanCode;

            /// <summary>
            /// The extended-key flag, event-injected Flags, context code, and transition-state flag. This member is specified as follows. An application can use the following values to test the keystroke Flags. Testing LLKHF_INJECTED (bit 4) will tell you whether the event was injected. If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event was injected from a process running at lower integrity level.
            /// </summary>
            public int Flags;

            /// <summary>
            /// The time stamp stamp for this message, equivalent to what GetMessageTime would return for this message.
            /// </summary>
            public int TimeStamp;

            /// <summary>
            /// Additional information associated with the message. 
            /// </summary>
            public IntPtr AdditionalInformation;
        }

        public const int WH_KEYBOARD_LL = 13;
        //const int HC_ACTION = 0;

        public enum KeyboardState
        {
            KeyDown = 0x0100,
            KeyUp = 0x0101,
            SysKeyDown = 0x0104,
            SysKeyUp = 0x0105
        }

        // EDT: Replaced VkSnapshot(int) with RegisteredKeys(Keys[])
        public static Keys[] RegisteredKeys;
        const int KfAltdown = 0x2000;
        public const int LlkhfAltdown = (KfAltdown >> 8);

        public IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool fEatKeyStroke = false;

            var wparamTyped = wParam.ToInt32();
            if (Enum.IsDefined(typeof(KeyboardState), wparamTyped))
            {
                object o = Marshal.PtrToStructure(lParam, typeof(LowLevelKeyboardInputEvent));
                LowLevelKeyboardInputEvent p = (LowLevelKeyboardInputEvent)o;

                var eventArguments = new GlobalKeyboardHookEventArgs(p, (KeyboardState)wparamTyped);

                // EDT: Removed the comparison-logic from the usage-area so the user does not need to mess around with it.
                // Either the incoming key has to be part of RegisteredKeys (see constructor on top) or RegisterdKeys
                // has to be null for the event to get fired.
                var key = (Keys)p.VirtualCode;
                if (RegisteredKeys == null || RegisteredKeys.Contains(key))
                {
                    EventHandler<GlobalKeyboardHookEventArgs> handler = KeyboardPressed;
                    handler?.Invoke(this, eventArguments);

                    fEatKeyStroke = eventArguments.Handled;
                }
            }

            return fEatKeyStroke ? (IntPtr)1 : CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
    }
}
