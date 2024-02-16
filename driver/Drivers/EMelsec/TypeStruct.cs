using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace EMelsec
{
   

    public class QnA_3E : IronInterface.Configuration.IMemoryConfig
    {
        public string IPAddress;
        public int Port;
        public bool Binary;
        public int Network;
        public int PLC;
        public int IOModule;
        public int Local;
        public int CPUCheckTimer;

        public void Clear()
        {
            IPAddress = "";
            Port = 0;
            Binary = true;
            Network = 0;
            PLC = 0;
            IOModule = 0;
            Local = 0;
            CPUCheckTimer = 0;

            DeviceCode = null;
            Address = null;
            Size = null;
        }
    }
}
