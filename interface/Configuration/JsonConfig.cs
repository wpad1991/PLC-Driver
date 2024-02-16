using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DriverInterface.Configuration
{
    public class JsonConfig
    {
        public JsonProject Project { get; set; } = new JsonProject();
    }
    public class JsonProject
    {
        public string Name { get; set; }
        public List<DriverConfig> Driver { get; set; } = new List<DriverConfig>();
        public List<string> Scheduler { get; set; } = new List<string>();
        public List<string> Simulator { get; set; } = new List<string>();
        public List<string> Logger { get; set; } = new List<string>();
        public List<string> Agent { get; set; } = new List<string>();
        public Dictionary<string, Dictionary<string, string>> Config { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, bool> Settings { get; set; } = new Dictionary<string, bool>();
    }

    public class DriverConfig
    { 
        public string Id { get; set; }
        public string Type { get; set; }
        public string Desc { get; set; }
        public string Dll { get; set; }
        public string IPaddr { get; set; }
        public string Port { get; set; }
        public string Binary { get; set; }
        public string Network { get; set; }
        public string NetworkType { get; set; }
        public string PLC { get; set; }
        public string IOModule { get; set; }
        public string Local { get; set; }
        public string CPUCheckTimer { get; set; }
        public string LoopInterval { get; set; }
        public string ReadInterval { get; set; }
        public string BufferMaxRange { get; set; }
        public bool Readonly { get; set; }
        public int BaudRate { get; set; }
        public int Parity { get; set; }
        public int DataBit { get; set; }
        public int StopBit { get; set; }
        public string Period { get; set; }
        public int ScanTime { get; set; }
        public string DeviceType { get; set; }
        public string Protocol { get; set; }
        


        public EthernetConfig Ethernet { get; set; } = new EthernetConfig();

        public SerialPortConfig SerialPort { get; set; } = new SerialPortConfig();

        public Dictionary<string, string> Extra { get; set; } = new Dictionary<string, string>();

        public List<Dictionary<string, string>> Tag { get; set; } = new List<Dictionary<string, string>>();

    }

    public class EthernetConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _IPaddr;
        private int _port;
        private int _network;

        public string IPaddr
        {
            get
            {
                return _IPaddr;
            }

            set
            {
                _IPaddr = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Reflection.MethodBase.GetCurrentMethod().Name.Substring(4)));
            }
        }
        public int Port
        {
            get
            {
                return _port;
            }

            set
            {
                _port = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Reflection.MethodBase.GetCurrentMethod().Name.Substring(4)));
            }
        }

        public int Network
        {
            get
            {
                return _network;
            }

            set
            {
                _network = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Reflection.MethodBase.GetCurrentMethod().Name.Substring(4)));
            }
        }
    }

    public class SerialPortConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string _COMPort;
        private string _baudrate;
        private string _dataBits;
        private string _stopBits;
        private string _parity;

        public string COMPort
        {
            get
            {
                return _COMPort;
            }

            set
            {
                _COMPort = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Reflection.MethodBase.GetCurrentMethod().Name.Substring(4)));

            }
        }

        public string Baudrate
        {
            get
            {
                return _baudrate;
            }

            set
            {
                _baudrate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Reflection.MethodBase.GetCurrentMethod().Name.Substring(4)));
            }
        }

        public string DataBits
        {
            get
            {
                return _dataBits;
            }

            set
            {
                _dataBits = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Reflection.MethodBase.GetCurrentMethod().Name.Substring(4)));
            }
        }

        public string StopBits
        {
            get
            {
                return _stopBits;
            }

            set
            {
                _stopBits = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Reflection.MethodBase.GetCurrentMethod().Name.Substring(4)));
            }
        }

        public string Parity
        {
            get
            {
                return _parity;
            }

            set
            {
                _parity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Reflection.MethodBase.GetCurrentMethod().Name.Substring(4)));
            }
        }
    }
}

