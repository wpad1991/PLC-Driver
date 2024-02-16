using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DriverInterface;
using DriverInterface.Commnuication;
using DriverInterface.Configuration;
using DriverInterface.Driver;
using DriverInterface.Type;
using DriverLog;
using DriverUtility;

namespace Mitsubishi_Ethernet
{
    public class Driver : TCPIPClient, IMemoryDriver
    {
        public delegate void DriverEndEventHandler();

        public event DriverEndEventHandler endEvent = null;
        public event ReadDataChangdEventHandler ReadBytesChanged;
        public event DriverStatusChangedEventHandler StatusChanged;
        public event RequestConfigEventHandler RequestConfig;
        public event RequestDataEventHandler RequestData;
        public event ReadObjectChangdEventHandler ReadObjectChanged;
        
        Thread loopThread;
        MelsecEthernet packet;
        Queue<string> requestQueue;
        QnA_3E frameConfig;
        Queue<byte[]> WriteBuffer;

        public int readinterval = 1;
        bool threadKill = false;
        bool isInit = false;
        bool reset = false;
        DriverStep state = DriverStep.Init;
        int MAXCount = 950;
        const int MTUCount = 1400;
        private DateTime lastReadTime = DateTime.Now;
        public string ErrorCode = "";
        public int bufferMaxRange = 0;
        string driverID = "";
        private List<byte[]> bytes;
        int readCount = 0;
        object Lock = new object();
        public bool Started
        {
            get { return !threadKill; }
        }

        public string DriverID
        {
            get { return driverID; }
            set { driverID = value; }
        }

        public DriverType GetDriverType => DriverType.MELSEC_Ethernet;

        public bool SimulationMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<byte[]> Bytes { get => bytes; set => bytes = value; }

        public QnA_3E GetConfig()
        {
            return frameConfig;
        }

        #region Interface

        public Driver()
        {
            //Listen = new Thread(listenSocket);
            requestQueue = new Queue<string>();
            frameConfig = new QnA_3E();
            packet = new MelsecEthernet();
            loopThread = new Thread(execute);
            WriteBuffer = new Queue<byte[]>();

            StatusChanged?.Invoke(this, DriverStatus.Normal, DateTime.Now);
        }

        ~Driver()
        {

            if (loopThread != null)
            {
                if (loopThread.IsAlive)
                {
                    loopThread.Interrupt();
                    loopThread.Abort();
                }
            }

            if (ConnectedComm)
            {
                DisconnectComm();
            }

            loopThread = null;
            packet = null;
            requestQueue = null;
            frameConfig = null;


        }

        public bool StartDriver()
        {
            threadKill = false;

            if (loopThread.ThreadState == ThreadState.Stopped)
            {
                loopThread = new Thread(execute);
                loopThread.Start();

            }
            else if (loopThread.ThreadState == ThreadState.Unstarted)
            {
                loopThread.Start();
            }
            
            return Started;
        }

        public bool StopDriver()
        {

            StatusChanged?.Invoke(this, DriverStatus.Stop, DateTime.Now);
            threadKill = true;
            isInit = false;
            state = DriverStep.Init;
            if (ConnectedComm)
            {
                DisconnectComm();
            }

            loopThread.Join();

            return Started;
        }

        public bool RestartDriver()
        {
            StopDriver();

            DisconnectComm();

            StartDriver();

            return Started;
        }
     
        public int WriteBytes(int channelIndex, int addr, byte[] data)
        {
            if (data != null)
            {

                ushort[] udata;
                byte[] databyte;
                byte[] totaldata;
                if (Utility.CheckIsBit(DriverType.MELSEC_Ethernet, frameConfig.DeviceCode[channelIndex]))
                {
                    udata = new ushort[] { ((ushort)data[0]) };// Utility.InvertingByteToUshort(data);
                    databyte = packet.GetDataByteArray(addr, udata, frameConfig.DeviceCode[channelIndex], false);
                    totaldata = packet.SetBlockProtocol(databyte, frameConfig.DeviceCode[channelIndex], 2, false);
                }
                else
                {
                    udata = Utility.InvertingByteToUshort(data);
                    databyte = packet.GetDataByteArray(addr, udata, frameConfig.DeviceCode[channelIndex], false);
                    totaldata = packet.SetBlockProtocol(databyte, frameConfig.DeviceCode[channelIndex], 2, false);
                }

                lock (Lock)
                {

                    WriteComm(totaldata);

                    byte[] receiveByte = listenSocket();

                    if (packet.CheckBlockErrorProtocol(receiveByte) != "OK")
                    {
                        ErrorCode += packet.CheckBlockErrorProtocol(receiveByte);

                        DriverManager.Manager.WriteLog("ErrorDriver", "ReadSend Faill index is < 0");

                        state = DriverStep.Error;

                        return DriverInterface.Protocol.ErrorCodes.WriteDriverFault;
                    }
                }

                return DriverInterface.Protocol.ErrorCodes.Good;
            }

            return DriverInterface.Protocol.ErrorCodes.InvalidArgument;
        }

        public void ThreadKill()
        {
            loopThread.Interrupt();
            loopThread.Abort();
        }

        public DriverStatus GetDriverStatus()
        {

            //string result = "";
            //if (ErrorCode == "")
            //{
            //    result = "[Driver] : State : " + state;
            //}
            //else
            //{
            //    result = "[Driver] : State : " + state + ", ErrorMessage : " + ErrorCode ;
            //    ErrorCode = "";
            //}



            if (ErrorCode != "")
            {
                return DriverStatus.Error;
            }
            if (state == DriverStep.Init || state == DriverStep.Start)
            {
                return DriverStatus.Run;
            }


            return DriverStatus.Normal;
        }

        public DriverStep GetDetailStatusDriver()
        {
            return state;
        }

        public DateTime GetLastReadTime()
        {
            return lastReadTime;
        }

        #endregion

        #region Utility
        
        private byte[] listenSocket()
        {
            byte[] bufferData = new byte[MTUCount];

            try
            {
                ReadComm(bufferData);
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
                return null;
            }

            return bufferData;
        }
       
        #endregion

        private void execute()
        {
            StatusChanged?.Invoke(this, DriverStatus.Run, DateTime.Now);

            try
            {
                while (!threadKill)
                {
                    switch (state)
                    {
                        case DriverStep.Init:
                            if (isInit)
                            {
                                state = DriverStep.Start;
                            }
                            else
                            {
                                Thread.Sleep(1000);
                            }
                            break;
                        case DriverStep.Start:

                            if (ConnectComm(frameConfig.IPAddress, frameConfig.Port))
                            {
                                packet.InitializeProtocol(frameConfig);
                                
                                reset = false;

                                state = DriverStep.Idle;
                            }
                            else
                            {
                                state = DriverStep.Init;
                            }
                            break;
                        case DriverStep.Idle:
                            state = DriverStep.ReadSend;
                            Thread.Sleep(readinterval);
                            break;
                        case DriverStep.ReadSend:
                            if (readCount < frameConfig.ReadInformation.Count)
                            {
                                byte[] databyte;
                                byte[] totaldata;

                                databyte = packet.GetDataByteArray(frameConfig.ReadInformation[readCount].StartAddr, new ushort[] { (ushort)frameConfig.ReadInformation[readCount].Size }, frameConfig.DeviceCode[frameConfig.ReadInformation[readCount].ChannelIndex], true);

                                totaldata = packet.SetBlockProtocol(databyte, frameConfig.DeviceCode[frameConfig.ReadInformation[readCount].ChannelIndex], 1, true);

                                lock (Lock)
                                {

                                    WriteComm(totaldata);

                                    byte[] receiveByte = listenSocket();

                                    if (receiveByte == null)
                                    {
                                        DriverManager.Manager.WriteLog("ErrorDriver", "[EMELSEC] ReceiveByte is Null");
                                        DisconnectComm();
                                        state = DriverStep.Init;
                                        break;
                                    }

                                    if (packet.CheckBlockErrorProtocol(receiveByte) != "OK")
                                    {
                                        ErrorCode += packet.CheckBlockErrorProtocol(receiveByte);

                                        DriverManager.Manager.WriteLog("ErrorDriver", "ReadSend Faill index is < 0");

                                        state = DriverStep.Error;
                                    }
                                    else
                                    {

                                        bool isBit = DriverUtility.Utility.CheckIsBit(DriverType.MELSEC_Ethernet, frameConfig.DeviceCode[frameConfig.ReadInformation[readCount].ChannelIndex]);

                                        int arrayIndex = isBit ? frameConfig.ReadInformation[readCount].StartAddr : frameConfig.ReadInformation[readCount].StartAddr * 2;

                                        byte[] receiveushort = (byte[])packet.GetDataProtocol(receiveByte, frameConfig.DeviceCode[frameConfig.ReadInformation[readCount].ChannelIndex], frameConfig.ReadInformation[readCount].Size);
                                        
                                        Buffer.BlockCopy(receiveushort, 0, bytes[frameConfig.ReadInformation[readCount].ChannelIndex], arrayIndex, receiveushort.Length);

                                        readCount++;

                                        if (readCount < frameConfig.ReadInformation.Count)
                                        {
                                            state = DriverStep.ReadSend;
                                        }
                                        else
                                        {
                                            readCount = 0;
                                            lastReadTime = DateTime.Now;
                                            state = DriverStep.Confirm;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                DriverManager.Manager.WriteLog("EMelsec", "State WriteSend Error WriteBuffer Count 0");
                                readCount = 0;
                                state = DriverStep.Idle;
                            }
                            break;
                        case DriverStep.Confirm:
                            state = DriverStep.Idle;
                            Thread.Sleep(readinterval);
                            break;
                        case DriverStep.Reset:

                            DisconnectComm();
                            state = DriverStep.Init;
                            Thread.Sleep(readinterval);
                            break;
                        case DriverStep.Error:
                            StatusChanged?.Invoke(this, DriverStatus.Error, DateTime.Now);
                            Flush();
                            if (requestQueue.Count > 0)
                            {
                                DriverManager.Manager.WriteLog("ErrorDriver", "[EMELSEC] State = Error, " + requestQueue.Peek() + "가 실패하였습니다. Q에서 제거하고 Idle로 돌아갑니다.");
                                requestQueue.Dequeue();
                            }
                            readCount = 0;

                            state = DriverStep.Idle;

                            Thread.Sleep(readinterval);
                            break;
                        default:
                            break;
                    }
                }
                
                if (ConnectedComm)
                {
                    DisconnectComm();
                }

                endEvent?.Invoke();
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
                StatusChanged?.Invoke(this, DriverStatus.Error, DateTime.Now);
                Thread.Sleep(3000);
                state = DriverStep.Idle;
                loopThread = new Thread(execute);
                loopThread.Start();
            }
        }

        public bool SetConfig(DriverInformation driverInfo)
        {
            try
            {
                if (driverInfo == null)
                {
                    return false;
                }

                driverID = driverInfo.DriverName;

                QnA_3E config = new QnA_3E();

                if (driverInfo as MelsecEhternetInformation != null)
                {
                    config.IPAddress = ((MelsecEhternetInformation)driverInfo).IPAddr;

                    config.Port = ((MelsecEhternetInformation)driverInfo).Port;

                    config.Binary = ((MelsecEhternetInformation)driverInfo).Binary;

                    config.Network = ((MelsecEhternetInformation)driverInfo).Network;

                    config.PLC = ((MelsecEhternetInformation)driverInfo).PLC;

                    config.IOModule = ((MelsecEhternetInformation)driverInfo).IOModule;

                    config.Local = ((MelsecEhternetInformation)driverInfo).Local;

                    config.CPUCheckTimer = ((MelsecEhternetInformation)driverInfo).CPUTimer;

                    config.DeviceCode = ((MelsecEhternetInformation)driverInfo).Channel;

                    config.Address = ((MelsecEhternetInformation)driverInfo).Address;

                    config.Size = ((MelsecEhternetInformation)driverInfo).Size;

                    readinterval = ((MelsecEhternetInformation)driverInfo).ReadInterval;
                    
                    bytes = new List<byte[]>();

                    config.ReadInformation = new List<DevisionBuffer>();
                    
                    for (int i = 0; i < config.DeviceCode.Length; i++)
                    {
                        int dataSize = Utility.CheckIsBit(DriverType.MELSEC_Ethernet, config.DeviceCode[i]) ? 1 : 2;

                        bytes.Add(new byte[config.Size[i] * dataSize]);

                        int bufferMaxSize = MAXCount / dataSize;

                        for (int d = 0; d <= config.Size[i] / bufferMaxSize; d++)
                        {
                            DevisionBuffer d_buffer = new DevisionBuffer();
                            d_buffer.ChannelIndex = i;
                            d_buffer.StartAddr = config.Address[i] + (d * bufferMaxSize);

                            if (d == config.Size[i] / bufferMaxSize)
                            {
                                d_buffer.Size = config.Size[i] - (d * bufferMaxSize);
                            }
                            else
                            {
                                d_buffer.Size = bufferMaxSize;
                            }

                            config.ReadInformation.Add(d_buffer);
                        }
                    }

                    frameConfig = config;

                    if (isInit)
                    {
                        reset = true;
                    }
                    
                    if (config.DeviceCode.Length != config.Address.Length || config.Address.Length != config.Size.Length)
                    {
                        DriverManager.Manager.WriteLog("Exception_Driver", "[EMelsec : Driver] : Config DeviceCode, Address, Size index fail");
                        isInit = false;
                    }

                    isInit = true;
                }
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
                return false;
            }
            return isInit;
        }
       
    }
}
