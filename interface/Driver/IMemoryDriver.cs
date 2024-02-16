using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Driver
{
    public interface IMemoryDriver : IDriverBase
    {
        List<byte[]> Bytes { get; set; }

        bool SetConfig(Configuration.DriverInformation driverInfo);
        
        int WriteBytes(int channelIndex, int addr, byte[] data);
    }
}
