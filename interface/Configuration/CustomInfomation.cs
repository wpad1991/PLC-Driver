using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Configuration
{
    public class CustomInformation : DriverInformation
    {
        public string DllName;

        public Dictionary<string, string> Info = new Dictionary<string, string>();
    }
}
