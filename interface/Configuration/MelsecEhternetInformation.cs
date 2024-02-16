
namespace DriverInterface.Configuration
{
    public class MelsecEhternetInformation : DriverInformation
    {
        public string IPAddr;
        public int Port;
        public bool Binary;
        public int Network;
        public int PLC;
        public int IOModule;
        public int Local;
        public int CPUTimer;
        public int ReadInterval;
        public int MaxBufferSize;
    }
}
