using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.DataBase
{
    public interface IDataBase
    {
        bool SetDB(string database, string channel, string key, object value, DateTime localTime);
        bool SetDB(string database, string channel, string[] keys, object[] values, DateTime localTime);
    }
}
