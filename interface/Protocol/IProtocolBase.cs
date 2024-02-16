using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DriverInterface.Protocol
{
    public interface IProtocolBase
    {
        bool InitializeProtocol(params object[] values);
        
        byte[] SetBlockProtocol(byte[] data, params object[] values);
        
        object GetDataProtocol(params object[] values);
        
        string CheckBlockErrorProtocol(byte[] block);
    }
}
