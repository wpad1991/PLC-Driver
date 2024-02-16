using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Protocol
{
    public static class ErrorCodes
    {
        public static readonly int Good = 0;
        public static readonly int InvalidValue = 1;
        public static readonly int InvalidTag = 2;
        public static readonly int NotExistDevice = 3;
        public static readonly int NotExistConfig = 4;
        public static readonly int InvalidArgument = 5;
        public static readonly int ValueTypeMisMatch = 6;
        public static readonly int WriteDriverFault = 7;
        public static readonly int Exception = 10;
    }
}
