using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        private int checkInterval = 2000;  // 默认设置检测间隔 2 秒

        public Form1()
        {
            InitializeComponent();
            LoadConfig();
            SetupTrayIcon();

            httpClient = new HttpClient();

            timer = new Timer();
            timer.Interval = checkInterval;
            timer.Tick += CheckInternetConnection;
            timer.Start();

        }
         
        private void LoadConfig()
        {
            try
            {
                var configLines = File.ReadAllLines("config.txt");
                foreach (var line in configLines)
                {
                    if (line.StartsWith("时间间隔="))
                    {
                        var value = line.Split('=')[1];
                        checkInterval = int.Parse(value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading config: {ex.Message}");
            }
        }

        private void SetupTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = icon_connected;
            notifyIcon.Visible = true;
            notifyIcon.Text = "USY校园网自动登录插件";

            var contextMenu = new ContextMenuStrip();

            var intervalDisplay = new ToolStripMenuItem("当前检测间隔：" + checkInterval.ToString() + "ms");
            contextMenu.Items.Add(intervalDisplay);

            var aboutItem = new ToolStripMenuItem("关于更多");
            aboutItem.Click += (s, e) => { OpenAbout(); };
            contextMenu.Items.Add(aboutItem);

            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => { ExitApp(); };
            contextMenu.Items.Add(exitItem);

            

            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ExitApp()
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void OpenAbout()
        {
            ProcessStartInfo psiAbout = new ProcessStartInfo
            {
                FileName = "https://github.com/Alyvesy/USYNetAutoLogin",
                UseShellExecute = true,
            };

            Process.Start(psiAbout);
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
                await Task.Delay(300);  // 防止网络波动，所以网站通讯检测间隔0.3秒
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
                    FileName = "http://10.10.200.102/",  // 校园网登录网址
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
