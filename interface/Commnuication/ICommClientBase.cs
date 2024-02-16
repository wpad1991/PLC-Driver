using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DriverInterface.Commnuication
{
    public interface ICommClientBase
    {
        bool ConnectedComm { get;}

        bool ConfigurationComm(params object[] values);

        bool ConnectComm(params object[] values);

        bool DisconnectComm();

        void ReadComm(byte[] data);

        void WriteComm(byte[] data);

        void Flush();
    }
    
}
