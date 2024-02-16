using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverInterface.Commnuication
{
    public interface ICommServerBase
    {
        bool IsBindComm { get; }

        bool ConfigurationComm(params object[] values);

        bool OpenComm();

        bool CloseComm();

        bool ReadComm(ref byte[] data);

        bool WriteComm(byte[] data);
    }
    
}
