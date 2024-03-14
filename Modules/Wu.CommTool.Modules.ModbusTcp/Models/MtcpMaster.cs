﻿namespace Wu.CommTool.Modules.ModbusTcp.Models;

/// <summary>
/// Modbus Tcp 从站=Master
/// </summary>
public partial class MtcpMaster : ObservableObject
{
    public MtcpMaster()
    {
        ShowMessage("开发中...");
        ShowMessage("开发中...");
        ShowMessage("开发中...");
    }
    IModbusMaster master;



    /// <summary>
    /// 页面消息
    /// </summary>
    [ObservableProperty]
    ObservableCollection<MessageData> messages = [];


    #region ModbusTcp服务器参数
    [ObservableProperty]
    string serverIp = "127.0.0.1";

    [ObservableProperty]
    int serverPort = 502;

    [ObservableProperty]
    int connectTimeout = 3000;

    [ObservableProperty]
    int requestTimeout = 1000;
    #endregion

    #region 属性
    [ObservableProperty]
    ObservableCollection<MtcpCustomFrame> mtcpCustomFrames = [
        new ("01 03 0000 0001 "),
        new ("01 04 0000 0001 "),
        new (""),
    ];
    #endregion



    #region 方法
    [RelayCommand]
    async Task Execute(string cmd)
    {
        switch (cmd)
        {
            case "新增行":
                MtcpCustomFrames.Add(new MtcpCustomFrame());
                break;

        }
    }





    [RelayCommand]
    [property: JsonIgnore]
    async Task TestMaster()
    {
        try
        {
            #region modbus tcp 读取保持寄存器测试
            //验证当前TcpClient是否有效并连接成功;
            //var client2 = new TcpClient();

            using TcpClient client2 = new TcpClient("127.0.0.1", 502);
            client2.ReceiveTimeout = 1000;
            client2.SendTimeout = 1000;
            //client.ConnectAsync(serverIp, serverPort);
            var factory = new ModbusFactory(logger: new DebugModbusLogger());
            master = factory.CreateMaster(client2);

            byte slaveId = 1;
            byte startAddress = 0;
            byte numberOfPoints = 5;

            //请求
            var request = new ReadHoldingInputRegistersRequest(
                    ModbusFunctionCodes.ReadHoldingRegisters,
                    slaveId,
                    startAddress,
                    numberOfPoints);

            var ccc = master.Transport.BuildMessageFrame(request);//生成 读取保持寄存器帧


            ShowMessage(ccc.ToHexString(), MessageType.Send);//输入16进制的帧

            var aa = await master.ReadHoldingRegistersAsync(slaveId, startAddress, numberOfPoints);

            ShowMessage(string.Join(" ", aa), MessageType.Receive);
            #endregion
        }
        catch (Exception ex)
        {

        }
    }



    MbusTcpClient mbusTcpClient;

    [ObservableProperty]
    bool isOnline;


    /// <summary>
    /// 建立Tcp/Ip连接
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    async Task Connect()
    {
        try
        {
            //建立TcpIp连接
            mbusTcpClient?.Dispose();
            mbusTcpClient = new MbusTcpClient();
            mbusTcpClient.ClientConnecting += () =>
            {
                ShowMessage($"连接中...");
            };
            mbusTcpClient.ClientConnected += (e) =>
                {
                    IsOnline = true;
                    ShowMessage($"连接服务器成功... {ServerIp}:{ServerPort}");
                };
            mbusTcpClient.ClientDisconnected += (e) =>
                {
                    IsOnline = false;
                    ShowMessage("断开连接...");
                };
            mbusTcpClient.MessageSending += (s) =>
            {
                //ShowSendMessage(new MtcpFrame(s));
                ShowMessage(s);
            };
            mbusTcpClient.MessageReceived += (s) =>
            {
                ShowMessage(s);
            };
            mbusTcpClient.ErrorOccurred += (s) =>
            {
                ShowErrorMessage(s);
            };
            await mbusTcpClient.ConnectAsync(ServerIp, ServerPort);
        }
        catch (Exception ex)
        {
            IsOnline = mbusTcpClient.Connected;
            ShowErrorMessage($"连接失败...{ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SendMessage(MtcpCustomFrame mtcpCustomFrame)
    {
        //若未初始化客户端或未连接,则先连接
        if (mbusTcpClient == null || !mbusTcpClient.Connected)
        {
            await Connect();
        }
        string message = mtcpCustomFrame.Frame.Replace(" ", "");
        if (message.Length %2 == 1)
        {
            ShowErrorMessage("消息少个字符");
            return;
        }
        mbusTcpClient.SendMessage(mtcpCustomFrame.Frame);
    }

    /// <summary>
    /// 断开Tcp连接
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    async Task DisConnect()
    {
        try
        {
            mbusTcpClient.Close();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
    }

    #endregion

    #region******************************  页面消息  ******************************
    /// <summary>
    /// 页面显示消息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="type"></param>
    public void ShowMessage(string message, MessageType type = MessageType.Info)
    {
        try
        {
            void action()
            {
                Messages.Add(new MessageData($"{message}", DateTime.Now, type));
                while (Messages.Count > 260)
                {
                    Messages.RemoveAt(0);
                }
            }
            Wu.Wpf.Utils.ExecuteFunBeginInvoke(action);
        }
        catch (Exception) { }
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    /// <param name="message"></param>
    public void ShowErrorMessage(string message) => ShowMessage(message, MessageType.Error);

    ///// <summary>
    ///// 页面展示接收数据消息
    ///// </summary>
    ///// <param name="frame"></param>
    //public void ShowReceiveMessage(MtcpFrame frame)
    //{
    //    try
    //    {
    //        void action()
    //        {
    //            var msg = new MtcpMessageData("", DateTime.Now, MessageType.Receive, frame)
    //            {
    //                MessageSubContents = new ObservableCollection<MessageSubContent>(frame.GetmessageWithErrMsg())
    //            };
    //            Messages.Add(msg);
    //            while (Messages.Count > 200)
    //            {
    //                Messages.RemoveAt(0);
    //            }
    //        }
    //        Wu.Wpf.Utils.ExecuteFunBeginInvoke(action);
    //    }
    //    catch (Exception) { }
    //}

    /// <summary>
    /// 页面展示发送数据消息
    /// </summary>
    /// <param name="frame"></param>
    public void ShowSendMessage(MtcpFrame frame)
    {
        try
        {
            void action()
            {
                var msg = new MtcpMessageData("", DateTime.Now, MessageType.Send, frame);
                Messages.Add(msg);
                while (Messages.Count > 200)
                {
                    Messages.RemoveAt(0);
                }
            }
            Wu.Wpf.Utils.ExecuteFunBeginInvoke(action);
        }
        catch (System.Exception) { }
    }


    /// <summary>
    /// 清空消息
    /// </summary>
    [RelayCommand]
    [property: JsonIgnore]
    public void MessageClear()
    {
        Messages.Clear();
    }

    /// <summary>
    /// 暂停更新接收的数据
    /// </summary>
    [RelayCommand]
    public void Pause()
    {
        //IsPause = !IsPause;
        //if (IsPause)
        //{
        //    ShowMessage("暂停更新接收的数据");
        //}
        //else
        //{
        //    ShowMessage("恢复更新接收的数据");
        //}
    }
    #endregion
}
