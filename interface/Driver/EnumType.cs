using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Driver
{
    public enum DriverType
    {
        NONE,
        MELSEC_Ethernet,
        OPCUAClient,
        Custom,
        Simulator,
        Robostar_RS232C,
        Robostar_RLS4700,
        OPCDAClient,
        ModbusTCP,
        LS_FEnet,
        SPU,
        Logic
    }

    public enum DriverStatus
    {
        None,
        Normal,
        Run,
        Stop,
        Bad,
        Error
    }

    public enum DriverTagType
    {
        Dynamic,
        Static
    }

    public enum DriverStep
    {
        None,
        Init,
        Start,
        Idle,
        Run,
        RequestRead,
        RequestWrite,
        ReadSend,
        WriteSend,
        ReceiveBlock,
        Confirm,
        Reset,
        Error
    }

    public enum IOType
    {
        NONE,
        UPDATE,
        DYNAMIC,
        DI,
        DO,
        AI,
        AO
    }
}
