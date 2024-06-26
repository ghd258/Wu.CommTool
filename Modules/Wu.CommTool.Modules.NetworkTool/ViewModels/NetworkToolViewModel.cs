﻿namespace Wu.CommTool.Modules.NetworkTool.ViewModels;

public partial class NetworkToolViewModel : NavigationViewModel
{
    public NetworkToolViewModel()
    {

    }
    public NetworkToolViewModel(IDialogHostService dialogHost)
    {
        ExecuteCommand = new DelegateCommand<string>(Execute);
        EnableDhcpCommand = new DelegateCommand<NetworkCard>(EnableDhcp);

        获取物理网卡信息();
        this.dialogHost = dialogHost;
    }

    private void Execute(string obj)
    {
        switch (obj)
        {
            case "获取物理网卡信息": 获取物理网卡信息(); break;
            case "获取所有网卡信息": 获取所有网卡信息(); break;
            case "打开网络连接": 打开网络连接(); break;
        }
    }

    #region 属性
    public ObservableCollection<NetworkCard> NetworkCards { get => _NetworkCards; set => SetProperty(ref _NetworkCards, value); }
    private ObservableCollection<NetworkCard> _NetworkCards = [];
    private readonly IDialogHostService dialogHost;
    #endregion

    /// <summary>
    /// 获取所有网卡信息
    /// </summary>
    /// <returns></returns>
    private static List<NetworkInterface> 获取所有网卡信息()
    {
        List<NetworkInterface> result = [.. NetworkInterface.GetAllNetworkInterfaces()];
        return result;
    }

    /// <summary>
    /// 指定网卡启用DHCP
    /// </summary>
    /// <param name="nwc"></param>
    public async void EnableDhcp(NetworkCard nwc)
    {
        try
        {
            //判断管理员权限 非管理员权限请求提权
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                //提示获取管理员权限
                var result = await dialogHost.Question("警告", "该操作需要管理员权限,点击确认以管理员权限重启该软件，重启后再使用该功能。", "Root");
                // 如果不是管理员，则重新启动具有管理员权限的应用程序
                if (result.Result != ButtonResult.OK)
                {
                    return;
                }

                // 获取当前运行的可执行文件的完整路径
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                var processInfo = new ProcessStartInfo(currentExe)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };

                try
                {
                    Process.Start(processInfo);
                }
                catch (Exception ex)
                {
                    // 用户取消了UAC提示或其他错误处理
                    HcGrowlExtensions.Warning(ex.Message);
                    return;
                }
                Application.Current.Shutdown();
                return;
            }

            //以下需要管理员权限
            //设置网卡为DHCP
            ProcessStartInfo psi = new()
            {
                FileName = "netsh",
                Arguments = $"interface ip set address \"{nwc.ConnectionId}\" source=dhcp",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using Process process = new();
            process.StartInfo = psi;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            //TODO 若已经启用DHCP则会报错, 需要设置已启用DHCP时按钮不可用
            if (process.ExitCode != 0)
            {
                HcGrowlExtensions.Warning("设置失败,需要管理员权限...");
            }
            else
            {
                HcGrowlExtensions.Success("启用成功");
            }
        }
        catch (Exception ex)
        {
        }
    }


    #region 命令
    public DelegateCommand<string> ExecuteCommand { get; private set; }

    public DelegateCommand<NetworkCard> EnableDhcpCommand { get; private set; }
    #endregion


    void 获取物理网卡信息()
    {
        try
        {
            string query = @"SELECT * FROM Win32_NetworkAdapter WHERE Manufacturer!='Microsoft' AND NOT PNPDeviceID LIKE 'ROOT\\%'";
            ManagementObjectSearcher mos = new(query);
            ManagementObjectCollection moc = mos.Get();
            NetworkCards = [];
            NetworkCards.AddRange(moc.OfType<ManagementObject>().Select(mo => new NetworkCard(mo)));
        }
        catch (Exception ex)
        {
            HcGrowlExtensions.Warning(ex.Message);
        }
    }

    void 打开网络连接()
    {
        ProcessStartInfo startInfo = new("NCPA.cpl")
        {
            UseShellExecute = true
        };
        Process.Start(startInfo);
    }
}
