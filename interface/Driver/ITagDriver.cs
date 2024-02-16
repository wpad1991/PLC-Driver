using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Driver
{
    public interface ITagDriver : IDriverBase
    {
        event ReadObjectChangdEventHandler ReadObjectChanged;
        event DriverUpdateTagInfoEventHandler UpdateTagInformation;

        object ReadAny(string tag);

        object ReadAny(string[] tags);

        int WriteData(string tag, object value);

        int[] WriteData(string[] tags, object[] values);
    }
}
