using DriverInterface.Configuration;
using DriverInterface.Type;
using System;
using System.Collections.Generic;

namespace DriverInterface.Driver
{
    public interface IDriverBase
    {
        event DriverStatusChangedEventHandler StatusChanged;
        event RequestConfigEventHandler RequestConfig;
        event RequestDataEventHandler RequestData;
        
        string DriverID { get; set; }

        DriverType GetDriverType { get; }
        
        DriverStatus GetDriverStatus();

        bool StartDriver();
             
        bool StopDriver();
    }
}
