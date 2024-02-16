using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Driver
{
    public interface ICustomDriver : IDriverBase
    {
        event ReadObjectChangdEventHandler ReadObjectChanged;

        int WriteData(string tag, object value);
    }
}
