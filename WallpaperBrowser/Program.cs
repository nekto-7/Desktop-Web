using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace WallpaperBrowser
{
    static class Program
    {
        // === Важно! Функции DllImport должны быть определены здесь ===
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        public const int GWL_STYLE = -16;
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;


        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private WebView2 webView;

        public MainForm()
        {
            Load += MainForm_Load;
        }
        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)3;  // MA_NOACTIVATE
                return;
            }
            base.WndProc(ref m);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            IntPtr progman = Program.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", null);
            IntPtr desktop = Program.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (desktop == IntPtr.Zero)
            {
                // Ищем WorkerW, если SHELLDLL_DefView не найден
                IntPtr workerw = IntPtr.Zero;
                IntPtr hwnd = IntPtr.Zero;

                do
                {
                    workerw = Program.FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null);
                    hwnd = Program.FindWindowEx(workerw, IntPtr.Zero, "SHELLDLL_DefView", null);
                }
                while (hwnd == IntPtr.Zero && workerw != IntPtr.Zero);

                desktop = hwnd;  // Используем найденный WorkerW
            }
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(webView);
            webView.CoreWebView2InitializationCompleted += (s, e) =>
            {
                webView.Source = new Uri("https://www.youtube.com/");
            };
            webView.EnsureCoreWebView2Async();

            if (desktop == IntPtr.Zero)
            {
                MessageBox.Show("Не удалось найти окно рабочего стола!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Встраиваем WebView2 в рабочий стол
            IntPtr hwndForm = Handle;
            Program.SetParent(hwndForm, desktop);
            int style = Program.GetWindowLong(hwndForm, Program.GWL_STYLE);
            Program.SetWindowLong(hwndForm, Program.GWL_STYLE, style | Program.WS_CHILD | Program.WS_VISIBLE);

            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = false;

        }
    }
}
