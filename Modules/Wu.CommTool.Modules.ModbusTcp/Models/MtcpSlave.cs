﻿namespace Wu.CommTool.Modules.ModbusTcp.Models;

/// <summary>
/// Modbus Tcp 主站Slave
/// </summary>
public partial class MtcpSlave : ObservableObject
{

    [ObservableProperty]
    bool isOnline;

}