using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronInterface;
using IronInterface.Commnuication;
using IronInterface.Driver;
namespace Robostar_RLS4700
{
    public class Driver : TCPIPClient, IDriverBase
    {
        /// <summary>
        /// 2019년 6월 3일부로 Serial 통신은 IronSAN을 이용하는 방법으로 사용되기 때문에
        /// 개발 중지되었습니다.
        /// </summary>
        public string DriverID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public DriverType GetDriverType => throw new NotImplementedException();

        public bool SimulationMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Started => throw new NotImplementedException();

        public int ReadDataUpdateTime => throw new NotImplementedException();

        public event ReadDataChangdEventHandler ReadBitsChanged;
        public event ReadDataChangdEventHandler ReadBytesChanged;
        public event DriverStatusChangedEventHandler StatusChanged;
        public event RequestConfigEventHandler RequestConfig;
        public event RequestDataEventHandler RequestData;
        public event ReadObjectChangdEventHandler ReadObjectChanged;

        public Driver()
        {
        }

        public DriverStatus GetDriverStatus()
        {
            throw new NotImplementedException();
        }

        public DateTime GetLastReadTime()
        {
            throw new NotImplementedException();
        }

        public byte[] ReadBits()
        {
            throw new NotImplementedException();
        }

        public byte[] ReadBytes()
        {
            throw new NotImplementedException();
        }

        public bool RestartDriver()
        {
            throw new NotImplementedException();
        }

        public bool StartDriver()
        {
            throw new NotImplementedException();
        }

        public bool StopDriver()
        {
            throw new NotImplementedException();
        }

        public void WriteBits(int addr, byte[] array)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(int addr, byte[] array)
        {
            throw new NotImplementedException();
        }

        public List<DriverTagDataInfo> GetBitsTagInfo()
        {
            throw new NotImplementedException();
        }

        public List<DriverTagDataInfo> GetBytesTagInfo()
        {
            throw new NotImplementedException();
        }

        public object ReadAny(string tag)
        {
            throw new NotImplementedException();
        }

        public object ReadAny(string[] tags)
        {
            throw new NotImplementedException();
        }

        public int WriteData(string tag, string value)
        {
            throw new NotImplementedException();
        }

        public int[] WriteData(string[] tags, string[] values)
        {
            throw new NotImplementedException();
        }

        public List<DriverTagDataInfo> GetObjectTagInfo()
        {
            throw new NotImplementedException();
        }
    }
}
