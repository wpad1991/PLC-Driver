using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Nodes
{
    public struct TagConfig
    {
        public string Tag;
        public string Description;
        public System.Type DataType;
        public int DataSize;
        public double DataMultiple;
        public int DataAddr;
        public bool WriteAsync;
        public DateTime UpdateTime;
    }
}
