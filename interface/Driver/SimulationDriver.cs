using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Driver
{
    public class SimulationDriver : ITagDriver
    {

        string driverID;

        public string DriverID
        {
            get
            {
                return driverID;
            }

            set
            {
                driverID = value;
            }
        }

        public DriverType GetDriverType
        {
            get
            {
                return DriverType.Simulator;
            }
        }

        public bool SimulationMode
        {
            get
            {
                return true;
            }

            set
            {
                
            }
        }

        public bool Started
        {
            get
            {
                return true;
            }
        }

        public int ReadDataUpdateTime
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event ReadDataChangdEventHandler ReadBitsChanged;
        public event ReadDataChangdEventHandler ReadBytesChanged;
        public event DriverStatusChangedEventHandler StatusChanged;
        public event RequestConfigEventHandler RequestConfig;
        public event RequestDataEventHandler RequestData;
        public event ReadObjectChangdEventHandler ReadObjectChanged;
        public event DriverUpdateTagInfoEventHandler UpdateTagInformation;

        public List<DriverTagDataInfo> GetBitsTagInfo()
        {
            throw new NotImplementedException();
        }

        public List<DriverTagDataInfo> GetBytesTagInfo()
        {
            throw new NotImplementedException();
        }

        public DriverStatus GetDriverStatus()
        {
            //DriverLog.LogManager.Manager.WriteLog("Console", "SimulationDriver : GetDriverStatus");
            return DriverStatus.Run;
        }

        public DateTime GetLastReadTime()
        {
            //DriverLog.LogManager.Manager.WriteLog("Console", "SimulationDriver : GetLastReadTime");
            return DateTime.Now;
        }

        public List<DriverTagDataInfo> GetObjectTagInfo()
        {
            throw new NotImplementedException();
        }

        public object ReadAny(string tag)
        {
            return null;
        }

        public object ReadAny(string[] tags)
        {
            return null;
        }

        public byte[] ReadBits()
        {
            //DriverLog.LogManager.Manager.WriteLog("Console", "SimulationDriver : ReadBits");
            return null;
        }

        public byte[] ReadBytes()
        {
            //DriverLog.LogManager.Manager.WriteLog("Console", "SimulationDriver : ReadBytes");
            return null;
        }

        public bool RestartDriver()
        {
            //DriverLog.LogManager.Manager.WriteLog("Console", "SimulationDriver : RestartDriver");
            return true;
        }

        public bool StartDriver()
        {
            //DriverLog.LogManager.Manager.WriteLog("Console", "SimulationDriver : StartDriver");
            return true;
        }

        public bool StopDriver()
        {
            //DriverLog.LogManager.Manager.WriteLog("Console", "SimulationDriver : StopDriver");
            return true;
        }

        public void WriteBits(int addr, byte[] array)
        {
            //DriverLog.LogManager.Manager.WriteLog("Console", "SimulationDriver : WriteBits");
        }

        public void WriteBytes(int addr, byte[] array)
        {
            //DriverLog.LogManager.Manager.WriteLog("Console", "SimulationDriver : WriteBytes");
        }

        public int WriteData(string tag, string value)
        {
            //throw new NotImplementedException();
            return 0;
        }

        public int[] WriteData(string[] tags, string[] values)
        {
            //throw new NotImplementedException();

            return null;
        }

        public int WriteData(string tag, object value)
        {

            return 0;
            //throw new NotImplementedException();
        }

        public int[] WriteData(string[] tags, object[] values)
        {
            return null;//throw new NotImplementedException();
        }
    }
}
