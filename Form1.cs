using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USYNetAutoLogin
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;
        private Timer timer;
        private HttpClient httpClient;
        private readonly Icon icon_connected = new Icon("icon_connected.ico");
        private readonly Icon icon_disconnected = new Icon("icon_disconnected.ico");
        private DateTime lastLoginAttempt = DateTime.MinValue;
        private bool wasDisconnected = false; // 状态-是否处于断网

        public Form1()
        {
            InitializeComponent();
            SetupTrayIcon();

            httpClient = new HttpClient();

            timer = new Timer();
            timer.Interval = 2000; // 2秒检测一次
            timer.Tick += CheckInternetConnection;
            timer.Start();
        }

        private void SetupTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = icon_connected;
            notifyIcon.Visible = true;
            notifyIcon.Text = "USY校园网自动登录插件";

            var contextMenu = new ContextMenuStrip();
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => { ExitApp(); };
            contextMenu.Items.Add(exitItem);

            var statusItem = new ToolStripMenuItem("检测连接状态");
            contextMenu.Items.Add(statusItem);

            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ExitApp()
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private async void CheckInternetConnection(object sender, EventArgs e)
        {
            bool allDisconnected = true;
            var websites = new List<string> { "http://www.baidu.com", "https://ping.chinaz.com", "http://www.google.com" };

            foreach (var website in websites)
            {
                try
                {
                    var response = await httpClient.GetAsync(website);
                    if (response.IsSuccessStatusCode)
                    {
                        allDisconnected = false;
                        break;
                    }
                }
                catch
                {
                    // 忽略异常，继续检测其他网站
                }
            }

            // 更新托盘图标
            notifyIcon.Icon = allDisconnected ? icon_disconnected : icon_connected;

            // 断网后重新连接，重置等待时间
            if (!allDisconnected && wasDisconnected)
            {
                lastLoginAttempt = DateTime.MinValue; // 重置为最小值，立即检测
            }

            // 如果所有网站都断开连接且距离上次尝试超过30分钟，执行自动登录
            if (allDisconnected && (DateTime.Now - lastLoginAttempt).TotalMinutes >= 30)
            {
                AutoLogin();
                lastLoginAttempt = DateTime.Now;
            }

            // 更新断网状态
            wasDisconnected = allDisconnected;
        }

        private void AutoLogin()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "http://123.123.123.123",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during login: {ex.Message}");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }
    }
}
