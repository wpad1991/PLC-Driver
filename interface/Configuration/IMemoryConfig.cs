using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Configuration
{
    public class IMemoryConfig
    {
        public string[] DeviceCode;
        public int[] Address;
        public int[] Size;
        public List<DevisionBuffer> ReadInformation;
    }

    public class DevisionBuffer
    {
        public int ChannelIndex;
        public int StartAddr;
        public int Size;
    }
}
