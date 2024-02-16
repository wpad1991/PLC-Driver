
namespace DriverInterface.Configuration
{
    public class ModbusTCPInformation : DriverInformation
    {
        public string IPAddr;
        public int Port;
        public string MS_Type;
        public int Period;

    }

    public enum ModbusBlock
    {
        Coils = 1,
        DiscreteInputs = 2,
        HoldingRegisters = 3,
        InputRegisters = 4,
    }

    public enum ModbusType
    {
        BOOLEAN,
        BYTE,
        SHORT,
        USHORT,
        INT,
        UINT,
        FLOAT,
        DOUBLE,
    }
}

