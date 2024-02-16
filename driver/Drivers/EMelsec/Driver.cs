using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronInterface;
using IronInterface.Commnuication;
using IronInterface.Configuration;
using IronInterface.Driver;
using IronInterface.Type;
using IronUtility;

namespace EMelsec
{
    public class Driver : TCPIPClient, IMemoryDriver
    {
        public delegate void DriverEndEventHandler();

        public event DriverEndEventHandler endEvent = null;
        public event ReadDataChangdEventHandler ReadBitsChanged;
        public event ReadDataChangdEventHandler ReadBytesChanged;
        public event DriverStatusChangedEventHandler StatusChanged;
        public event RequestConfigEventHandler RequestConfig;
        public event RequestDataEventHandler RequestData;
        public event ReadObjectChangdEventHandler ReadObjectChanged;

        BufferClass<byte> BUFFER;
        BufferClass<byte> BUFFER_BIT;
        BufferClass<byte> BUFFER_BYTE;

        Thread loopThread;
        MelsecEthernet packet;
        Queue<string> requestQueue;
        QnA_3E frameConfig;
        Queue<byte[]> WriteBuffer;

        public int readinterval = 1000;
        public int loopinterval = 10;
        bool threadKill = false;
        bool isInit = false;
        bool reset = false;
        DriverStep state = DriverStep.Init;
        DriverStep recentState = DriverStep.Init;
        int MAXCount = 950;
        const int MTUCount = 1400;
        private int MSSCount = 640;
        private DateTime lastReadTime = DateTime.Now;
        public string ErrorCode = "";
        public int bufferMaxRange = 0;
        string driverID = "";
        int readDataUpdateTime = 500;
        private List<byte[]> bytes;
        int readCount = 0;
        
        public bool Started
        {
            get { return !threadKill; }
        }

        public int ReadDataUpdateTime { get => readDataUpdateTime; }

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
            BUFFER = new BufferClass<byte>();
            BUFFER_BIT = new BufferClass<byte>();
            BUFFER_BYTE = new BufferClass<byte>();
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

            BUFFER = null;
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

        public byte[] ReadBits()
        {
            if (BUFFER == null)
            {
                return null;
            }
            else
            {
                return BUFFER_BIT.BufferData;
            }
        }

        public byte[] ReadBytes()
        {
            if (BUFFER == null)
            {
                return null;
            }
            else
            {
                return BUFFER_BYTE.BufferData;
            }
        }

        public void WriteBits(int addr, byte[] data)
        {
            try
            {

                QnA_3E dev = BUFFER_BIT.CONFIG;
                int dataSize_1 = data.Length;
                int arrayindex_0 = 0;
                int endarrayindex_0 = 0;
                int sizeSum_1 = 0;


                for (int i = 0; i < dev.Size.Length; i++)
                {
                    sizeSum_1 += dev.Size[i];
                    if (sizeSum_1 > addr)
                    {
                        break;
                    }
                    arrayindex_0++;
                }

                sizeSum_1 = 0;

                for (int i = 0; i < dev.Size.Length; i++)
                {
                    sizeSum_1 += dev.Size[i];
                    if (sizeSum_1 > addr + dataSize_1 - 1)
                    {
                        break;
                    }
                    endarrayindex_0++;
                }

                List<ushort> list = new List<ushort>();


                int checkcount = 0;
                for (int i = arrayindex_0; i <= endarrayindex_0; i++)
                {
                    if (i == arrayindex_0)
                    {
                        int defaultindex = 0;

                        for (int j = 0; j < arrayindex_0; j++)
                        {
                            defaultindex += dev.Size[j];
                        }

                        int addrbuf = addr - defaultindex;

                        int diff = dev.Size[arrayindex_0] - addrbuf;

                        if (diff > data.Length)
                        {
                            diff = data.Length;
                        }

                        for (int k = 0; k <= diff / MSSCount; k++)
                        {

                            ushort[] data_buf;

                            if (k < (diff / MSSCount))
                            {
                                data_buf = new ushort[MSSCount];
                            }
                            else
                            {
                                data_buf = new ushort[diff % MSSCount];
                            }

                            for (int j = 0; j < data_buf.Length; j++)
                            {
                                data_buf[j] = (ushort)(data[checkcount++] == 0 ? 0 : 1);
                            }

                            byte[] databyte = packet.GetDataByteArray(dev.Address[i] + addrbuf + (k * MSSCount), data_buf, dev.DeviceCode[i], false);
                            byte[] totaldata = packet.SetBlockProtocol(databyte, dev.DeviceCode[i], 2, false);

                            if (totaldata.Length > 0)
                            {
                                WriteBuffer.Enqueue(totaldata);

                                requestQueue.Enqueue("Write");
                            }
                        }

                    }
                    else if (i == endarrayindex_0)
                    {
                        int defaultindex = 0;

                        for (int j = 0; j < endarrayindex_0; j++)
                        {
                            defaultindex += dev.Size[j];
                        }

                        int diff = dataSize_1 - checkcount;


                        for (int k = 0; k <= diff / MSSCount; k++)
                        {

                            ushort[] data_buf;

                            if (k < (diff / MSSCount))
                            {
                                data_buf = new ushort[MSSCount];
                            }
                            else
                            {
                                data_buf = new ushort[diff % MSSCount];
                            }

                            for (int j = 0; j < diff; j++)
                            {
                                data_buf[j] = data[checkcount++];
                            }

                            byte[] databyte = packet.GetDataByteArray(dev.Address[i] + (k * MSSCount), data_buf, dev.DeviceCode[i], false);
                            byte[] totaldata = packet.SetBlockProtocol(databyte, dev.DeviceCode[i], 2, false);

                            if (totaldata.Length > 0)
                            {
                                WriteBuffer.Enqueue(totaldata);

                                requestQueue.Enqueue("Write");
                            }
                        }
                    }
                    else
                    {


                        for (int k = 0; k <= dev.Size[i] / MSSCount; k++)
                        {


                            ushort[] data_buf;

                            if (k < (dev.Size[i] / MSSCount))
                            {
                                data_buf = new ushort[MSSCount];
                            }
                            else
                            {
                                data_buf = new ushort[dev.Size[i] % MSSCount];
                            }

                            for (int j = 0; j < data_buf.Length; j++)
                            {
                                data_buf[j] = data[checkcount++];
                            }

                            byte[] databyte = packet.GetDataByteArray(dev.Address[i] + (k * MSSCount), data_buf, dev.DeviceCode[i], false);
                            byte[] totaldata = packet.SetBlockProtocol(databyte, dev.DeviceCode[i], 2, false);

                            if (totaldata.Length > 0)
                            {
                                WriteBuffer.Enqueue(totaldata);

                                requestQueue.Enqueue("Write");
                            }

                        }
                    }

                }

            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception", except.ToString());
            }

        }

        public void WriteBytes(int addr, byte[] data)
        {
            try
            {

                QnA_3E dev = BUFFER_BYTE.CONFIG;
                int dataSize_1 = data.Length / 2;
                int arrayindex_0 = 0;
                int endarrayindex_0 = 0;
                int sizeSum_1 = 0;

                for (int i = 0; i < dev.Size.Length; i++)
                {
                    sizeSum_1 += dev.Size[i];
                    if (sizeSum_1 > addr)
                    {
                        break;
                    }
                    arrayindex_0++;
                }

                sizeSum_1 = 0;

                for (int i = 0; i < dev.Size.Length; i++)
                {
                    sizeSum_1 += dev.Size[i];
                    if (sizeSum_1 > addr + dataSize_1 - 1)
                    {
                        break;
                    }
                    endarrayindex_0++;
                }

                List<ushort> list = new List<ushort>();


                int checkcount = 0;
                for (int i = arrayindex_0; i <= endarrayindex_0; i++)
                {
                    if (i == arrayindex_0)
                    {
                        int defaultindex = 0;

                        for (int j = 0; j < arrayindex_0; j++)
                        {
                            defaultindex += dev.Size[j];
                        }

                        int addrbuf = addr - defaultindex;

                        int diff = dev.Size[arrayindex_0] - addrbuf;

                        if (diff > data.Length / 2)
                        {
                            diff = data.Length / 2;
                        }

                        for (int k = 0; k <= diff / MSSCount; k++)
                        {

                            ushort[] data_buf;

                            if (k < (diff / MSSCount))
                            {
                                data_buf = new ushort[MSSCount];
                            }
                            else
                            {
                                data_buf = new ushort[diff % MSSCount];
                            }

                            for (int j = 0; j < data_buf.Length; j++)
                            {
                                data_buf[j] += data[0 + (checkcount * 2)];
                                data_buf[j] += (ushort)(data[1 + (checkcount * 2)] << 8);

                                checkcount++;
                            }

                            byte[] databyte = packet.GetDataByteArray(dev.Address[i] + addrbuf + (k * MSSCount), data_buf, dev.DeviceCode[i], false);
                            byte[] totaldata = packet.SetBlockProtocol(databyte, dev.DeviceCode[i], 2, false);

                            if (totaldata.Length > 0)
                            {
                                WriteBuffer.Enqueue(totaldata);

                                requestQueue.Enqueue("Write");
                            }
                        }

                    }
                    else if (i == endarrayindex_0)
                    {
                        int defaultindex = 0;

                        for (int j = 0; j < endarrayindex_0; j++)
                        {
                            defaultindex += dev.Size[j];
                        }

                        int diff = dataSize_1 - checkcount;


                        for (int k = 0; k <= diff / MSSCount; k++)
                        {

                            ushort[] data_buf;

                            if (k < (diff / MSSCount))
                            {
                                data_buf = new ushort[MSSCount];
                            }
                            else
                            {
                                data_buf = new ushort[diff % MSSCount];
                            }

                            for (int j = 0; j < diff; j++)
                            {
                                //data_buf[j] = data[checkcount++];
                                data_buf[j] += data[0 + (checkcount * 2)];
                                data_buf[j] += (ushort)(data[1 + (checkcount * 2)] << 8);

                                checkcount++;
                            }

                            byte[] databyte = packet.GetDataByteArray(dev.Address[i] + (k * MSSCount), data_buf, dev.DeviceCode[i], false);
                            byte[] totaldata = packet.SetBlockProtocol(databyte, dev.DeviceCode[i], 2, false);

                            if (totaldata.Length > 0)
                            {
                                WriteBuffer.Enqueue(totaldata);

                                requestQueue.Enqueue("Write");
                            }
                        }
                    }
                    else
                    {


                        for (int k = 0; k <= dev.Size[i] / MSSCount; k++)
                        {


                            ushort[] data_buf;

                            if (k < (dev.Size[i] / MSSCount))
                            {
                                data_buf = new ushort[MSSCount];
                            }
                            else
                            {
                                data_buf = new ushort[dev.Size[i] % MSSCount];
                            }

                            for (int j = 0; j < data_buf.Length; j++)
                            {
                                //data_buf[j] = data[checkcount++];

                                data_buf[j] += data[0 + (checkcount * 2)];
                                data_buf[j] += (ushort)(data[1 + (checkcount * 2)] << 8);

                                checkcount++;
                            }

                            byte[] databyte = packet.GetDataByteArray(dev.Address[i] + (k * MSSCount), data_buf, dev.DeviceCode[i], false);
                            byte[] totaldata = packet.SetBlockProtocol(databyte, dev.DeviceCode[i], 2, false);

                            if (totaldata.Length > 0)
                            {
                                WriteBuffer.Enqueue(totaldata);

                                requestQueue.Enqueue("Write");
                            }

                        }
                    }

                }

            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception", except.ToString());
            }

        }
        
        public void WriteBytes(int channelIndex, int addr, byte[] data)
        {
            WriteBytes(addr, data);
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

        private void WirteBytes(ushort[] data)
        {
            try
            {
                QnA_3E config = frameConfig;
                if (config == null)
                {
                    IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : Driver] : freamConfig is null");
                    return;
                }


                byte[] databyte = packet.GetDataByteArray(config.Address[0], data, config.DeviceCode[0], false);
                byte[] totaldata = packet.SetBlockProtocol(databyte, config.DeviceCode[0], 2, false);

                if (totaldata.Length > 0)
                {
                    WriteBuffer.Enqueue(totaldata);

                    requestQueue.Enqueue("Write");
                }

            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception", except.ToString());
            }


        }

        private int GetRealAddress(int index)
        {
            return 0;
        }

        private byte[] listenSocket()
        {
            byte[] bufferData = new byte[MTUCount];

            try
            {
                ReadComm(bufferData);
            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception", except.ToString());
                return null;
            }

            return bufferData;
        }

        private void DivideTotalBuffer()
        {
            int bitCount = 0;
            int byteCount = 0;

            try
            {
                for (int i = 0; i < BUFFER.deviceCode.Length; i++)
                {
                    if (Utility.CheckIsBit(BUFFER.deviceCode[i]))
                    {
                        if (BUFFER_BIT.deviceCode.Length > bitCount)
                        {

                            Buffer.BlockCopy(BUFFER.BufferData, BUFFER.GetByteBufferSize(i), BUFFER_BIT.BufferData, BUFFER_BIT.GetByteBufferSize(bitCount), BUFFER.byteSize[i]);

                            bitCount++;
                        }
                        else
                        {
                            IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : Driver] : DivideTotalBuffer BIT : Un Match BUFFER Count & Device Count");
                        }
                    }
                    else
                    {
                        if (BUFFER_BYTE.deviceCode.Length > byteCount)
                        {

                            Buffer.BlockCopy(BUFFER.BufferData, BUFFER.GetByteBufferSize(i), BUFFER_BYTE.BufferData, BUFFER_BYTE.GetByteBufferSize(byteCount), BUFFER.byteSize[i]);

                            byteCount++;
                        }
                        else
                        {
                            IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : Driver] : DivideTotalBuffer BYTE : Un Match BUFFER Count & Device Count");
                        }
                    }
                }
            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception", except.ToString());
            }
        }

        #endregion

        private void execute()
        {

            DateTime checkTime = DateTime.Now;
            DateTime checkinitTime = DateTime.Now;
            DateTime invokeinitTime = DateTime.Now;
            TimeSpan readCheckTime = new TimeSpan(0);
            TimeSpan loopCheckTime = new TimeSpan(0);
            TimeSpan invokeCheckTime = new TimeSpan(0);

            StatusChanged?.Invoke(this, DriverStatus.Run, DateTime.Now);

            try
            {

                while (!threadKill)
                {

                    readCheckTime = DateTime.Now - checkTime;
                    if (readCheckTime.TotalMilliseconds > readinterval)
                    {
                        checkTime = DateTime.Now;

                        if (requestQueue.Count > 0)
                        {
                            if (requestQueue.ToArray()[requestQueue.Count - 1] != "Read")
                            {
                                requestQueue.Enqueue("Read");
                            }
                        }
                        else if (requestQueue.Count == 0)
                        {
                            requestQueue.Enqueue("Read");
                        }
                    }

                    if (recentState != state)
                    {
                        recentState = state;
                    }

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
                            if (requestQueue.Count > 0)
                            {
                                string peek = requestQueue.Peek();

                                if (peek == "Write")
                                {
                                    state = DriverStep.RequestWrite;
                                }
                                else if (peek == "Read")
                                {
                                    state = DriverStep.RequestRead;
                                }
                            }
                            break;
                        case DriverStep.RequestRead:
                            
                            state = DriverStep.ReadSend;

                            break;

                        case DriverStep.RequestWrite:
                            
                            state = DriverStep.WriteSend;

                            break;

                        case DriverStep.ReadSend:
                            if (readCount < frameConfig.ReadInformation.Count)
                            {
                                byte[] databyte;
                                byte[] totaldata;

                                databyte = packet.GetDataByteArray(frameConfig.ReadInformation[readCount].StartAddr, new ushort[] { (ushort)frameConfig.ReadInformation[readCount].Size }, frameConfig.DeviceCode[frameConfig.ReadInformation[readCount].ChannelIndex], true);

                                totaldata = packet.SetBlockProtocol(databyte, frameConfig.DeviceCode[frameConfig.ReadInformation[readCount].ChannelIndex], 1, true);
                                
                                WriteComm(totaldata);

                                state = DriverStep.ReceiveBlock;
                            }
                            else
                            {
                                IronLog.LogManager.Manager.WriteLog("EMelsec", "State WriteSend Error WriteBuffer Count 0");
                                readCount = 0;
                                state = DriverStep.Idle;
                            }

                            break;
                        case DriverStep.WriteSend:

                            if (true)//currentCount < continueCount)
                            {
                                if (WriteBuffer.Count > 0)
                                {
                                    WriteComm(WriteBuffer.Dequeue());
                                    state = DriverStep.ReceiveBlock;
                                }
                                else
                                {
                                    IronLog.LogManager.Manager.WriteLog("ErrorDriver", "[EMELSEC] readCount < frameConfig.ReadInformation.Count else");
                                    state = DriverStep.Error;
                                }
                            }
                            else
                            {
                                state = DriverStep.Idle;
                            }

                            break;
                        case DriverStep.ReceiveBlock:

                            byte[] receiveByte = listenSocket();

                            if (receiveByte == null)
                            {
                                IronLog.LogManager.Manager.WriteLog("ErrorDriver", "[EMELSEC] ReceiveByte is Null");
                                DisconnectComm();
                                state = DriverStep.Init;
                                break;
                            }

                            if (packet.CheckBlockErrorProtocol(receiveByte) != "OK")
                            {
                                ErrorCode += packet.CheckBlockErrorProtocol(receiveByte);

                                IronLog.LogManager.Manager.WriteLog("ErrorDriver", "ReadSend Faill index is < 0");

                                state = DriverStep.Error;
                            }
                            else if (requestQueue.Peek() == "Write")
                            {
                                state = DriverStep.Confirm;
                            }
                            else
                            {
                                byte[] receiveushort = (byte[])packet.GetDataProtocol(receiveByte, frameConfig.DeviceCode[frameConfig.ReadInformation[readCount].ChannelIndex], frameConfig.ReadInformation[readCount].Size);
                                Buffer.BlockCopy(receiveushort, 0, bytes[frameConfig.ReadInformation[readCount].ChannelIndex], frameConfig.ReadInformation[readCount].StartAddr, receiveushort.Length);
                                

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

                            break;
                        case DriverStep.Confirm:
                            if (requestQueue.Count > 0)
                            {
                                if (requestQueue.Peek() == "Read")
                                {
                                    //DivideTotalBuffer();
                                }
                                requestQueue.Dequeue();
                            }
                            if (reset)
                            {
                                state = DriverStep.Reset;
                                break;
                            }

                            state = DriverStep.Idle;

                            break;
                        case DriverStep.Reset:

                            DisconnectComm();
                            state = DriverStep.Init;

                            break;
                        case DriverStep.Error:


                            StatusChanged?.Invoke(this, DriverStatus.Error, DateTime.Now);
                            Flush();
                            if (requestQueue.Count > 0)
                            {
                                IronLog.LogManager.Manager.WriteLog("ErrorDriver", "[EMELSEC] State = Error, " + requestQueue.Peek() + "가 실패하였습니다. Q에서 제거하고 Idle로 돌아갑니다.");
                                requestQueue.Dequeue();
                            }
                            readCount = 0;

                            state = DriverStep.Idle;

                            break;
                        default:
                            break;
                    }

                    loopCheckTime = DateTime.Now - checkinitTime;

                    if (loopCheckTime.TotalMilliseconds > loopinterval)
                    {
                        checkinitTime = DateTime.Now;
                    }
                    else
                    {
                        Thread.Sleep(loopinterval - (int)loopCheckTime.TotalMilliseconds);
                    }
                    
                    invokeCheckTime = DateTime.Now - invokeinitTime;
                    if (invokeCheckTime.TotalMilliseconds > readDataUpdateTime)
                    {
                        invokeinitTime = DateTime.Now;

                        //if (BUFFER_BIT.deviceCode != null)
                        //{
                        //    ReadBitsChanged?.BeginInvoke(this, BUFFER_BIT.deviceCode[0], BUFFER_BIT.BufferData, typeof(bool), DateTime.Now, null, null);
                        //}
                        //if (BUFFER_BYTE.deviceCode != null)
                        //{
                        //    ReadBytesChanged?.BeginInvoke(this, BUFFER_BYTE.deviceCode[0], BUFFER_BYTE.BufferData, typeof(ushort), DateTime.Now, null, null);
                        //}
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
                IronLog.LogManager.Manager.WriteLog("Exception", except.ToString());

                StatusChanged?.Invoke(this, DriverStatus.Error, DateTime.Now);
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

                    loopinterval = ((MelsecEhternetInformation)driverInfo).LoopInterval;

                    readinterval = ((MelsecEhternetInformation)driverInfo).ReadInterval;

                    bufferMaxRange = ((MelsecEhternetInformation)driverInfo).MaxBufferSize;

                    readDataUpdateTime = ((MelsecEhternetInformation)driverInfo).ReadDataInvokeInteval;
                    
                    if (bufferMaxRange <= 0)
                    {
                        IronLog.LogManager.Manager.WriteLog("Exception_Driver", "[EMelsec : Driver] : BufferMaxRange Error >>>>> " + bufferMaxRange);
                        return false;
                    }

                    bytes = new List<byte[]>();
                    config.ReadInformation = new List<DevisionBuffer>();

                    for (int i = 0; i < config.DeviceCode.Length; i++)
                    {
                        int dataSize = IronUtility.Utility.CheckIsBit(config.DeviceCode[i]) ? 1 : 2;

                        bytes.Add(new byte[config.Size[i] * dataSize]);

                        int bufferMaxSize = MAXCount / dataSize;

                        for (int d = 0; d <= config.Size[i] / MAXCount; d++)
                        {
                            DevisionBuffer d_buffer = new DevisionBuffer();
                            d_buffer.ChannelIndex = i;
                            d_buffer.StartAddr = config.Address[i] + (d * MAXCount);

                            if (d == config.Size[i] / MAXCount)
                            {
                                d_buffer.Size = (config.Size[i] / dataSize) - (d * bufferMaxSize);
                            }
                            else
                            {
                                d_buffer.Size = bufferMaxSize;
                            }

                            Console.WriteLine("Buffer : " + d_buffer.ChannelIndex + ", " + d_buffer.StartAddr + ", " + d_buffer.Size);
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
                        IronLog.LogManager.Manager.WriteLog("Exception_Driver", "[EMelsec : Driver] : Config DeviceCode, Address, Size index fail");
                        isInit = false;
                    }

                    isInit = true;
                }
            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception", except.ToString());
                return false;
            }
            return isInit;
        }

    }
}
