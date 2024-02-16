using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Mitsubishi
{
    public class Ethenet
    {
        public string DriverName = "";
        public string Description = "";
        public readonly string Version = "1.0.158";
        public int ReadDataInvokeInteval = 500;
        public string[] Channel;
        public int[] Address;
        public int[] Size;
        public List<string[]> tagList;

        //public List<Dictionary<string, string>> TagInfo;
        public List<DriverInterface.DataType.SerializableDictionary<string, string>> TagInfo;


        public string IPAddr;
        public int Port;
        public bool Binary;
        public int Network;
        public int PLC;
        public int IOModule;
        public int Local;
        public int CPUTimer;
        public int LoopInterval;
        public int ReadInterval;
        public int MaxBufferSize;
    }
}
