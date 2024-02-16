using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Configuration
{
    public class DriverTagInfoConfig : DriverInformation
    {
        public List<Driver.DriverTagDataInfo> TagInformation = new List<Driver.DriverTagDataInfo>();
    }
}
