using DriverInterface.Configuration;
using System;
using System.Collections.Generic;

namespace DriverInterface
{

    public interface IModuleBase
    {
        event RequestDataEventHandler RequestData;
        event RequestConfigsEventHandler RequestConfigs;
        event DataWriteValue DataWriteEvent;

        bool StartModule();

        bool Configuration(params object[] values);

        bool StopModule();

        void UpdateTagData(Dictionary<string, object> changedData, DateTime dateTime);
    }
}
