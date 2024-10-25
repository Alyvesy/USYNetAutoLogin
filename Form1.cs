using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USYNetAutoLogin
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;
        private Timer timer;
        private HttpClient httpClient;
        private Icon icon_connected = new Icon("icon_connected.ico");
        private Icon icon_disconnected = new Icon("icon_disconnected.ico");

        public Form1()
        {
            InitializeComponent(); // 调用控件初始化
            SetupTrayIcon(); // 调用托盘图标设置方法

            httpClient = new HttpClient();

            // 初始化定时器
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 6000; // 设置6秒的间隔
            timer.Tick += new EventHandler(CheckInternetConnection); // 绑定Tick事件
            timer.Start(); // 启动定时器
        }

        private void SetupTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = icon_connected; // 你可以使用自定义图标
            notifyIcon.Visible = true;
            notifyIcon.Text = "USY校园网自动登录插件";

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => { ExitApp(); }; // 点击退出时执行退出方法
            contextMenu.Items.Add(exitItem);

            notifyIcon.ContextMenuStrip = contextMenu; // 关联右键菜单
        }

        private void ExitApp()
        {
            notifyIcon.Visible = false; // 隐藏托盘图标
            Application.Exit(); // 退出应用
        }

        private bool isWebOpened = false; // 标记网页是否已打开

        private async void CheckInternetConnection(object sender, EventArgs e)
        {
            try
            {
                // 确保 httpClient 被正确初始化
                if (httpClient == null)
                {
                    httpClient = new HttpClient();
                }

                var websites = new List<string>
                {
                    "http://www.baidu.com",
                    "https://ping.chinaz.com",
                    "http://www.google.com" 
                };

                bool allDisconnected = true; // 标记所有网站都无法连接

                foreach (var website in websites)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(website);
                        if (response.IsSuccessStatusCode)
                        {
                            allDisconnected = false; // 如果有一个网站能连接，标记为 false
                            break; // 跳出循环
                        }
                    }
                    catch
                    {
                        // 捕获异常，继续检查下一个网站
                    }
                }

                if (allDisconnected)
                {
                    notifyIcon.Icon = icon_disconnected; // 无连接时的图标
                }
                else
                {
                    notifyIcon.Icon = icon_connected; // 有连接时的图标
                }

                // 只有当所有网站都无法连接时才触发
                if (allDisconnected && !isWebOpened)
                {
                    AutoLogin();
                    isWebOpened = true; // 设置网页已打开状态
                    await Task.Delay(TimeSpan.FromHours(0.5)); // 等待1小时
                    isWebOpened = false; // 重置状态，重新开始检测
                } 
            }
            catch (HttpRequestException)
            {
                // 处理异常
                if (!isWebOpened)
                {
                    AutoLogin();
                    isWebOpened = true; // 设置网页已打开状态
                    await Task.Delay(TimeSpan.FromHours(1)); // 等待1小时
                    isWebOpened = false; // 重置状态，重新开始检测
                }
            }

        }

        private void AutoLogin()
        {
            try
            {
                // 打开校园网登录链接，后台运行
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "http://123.123.123.123", // 替换为实际的登录URL
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized // 最小化窗口
                };

                var process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during login: {ex.Message}"); // 记录错误日志
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false; // 不显示在任务栏
            this.WindowState = FormWindowState.Minimized; // 最小化窗口
            this.Hide(); // 隐藏窗体
        }


    }
}
