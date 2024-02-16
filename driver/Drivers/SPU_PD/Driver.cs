using IronInterface;
using IronInterface.Configuration;
using IronInterface.Driver;
using IronUtility;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace SPU_PD
{
    enum LoopStep
    {
        Stop,
        Init,
        Run
    }

    public class Driver : ICustomDriver
    {
        // TCP Socket
        Socket socket = null;
        private bool socketError = false;

        public bool IsNetworkConnected
        {
            get
            {
                if (socket == null)
                    return false;
                else
                {
                    if (socket.Connected == false)
                        return false;

                    if (socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0))
                        return false;
                }

                if (socketError)
                    return false;
                else
                    return true;
            }
        }

        // DriverBase Event
        public event DriverStatusChangedEventHandler StatusChanged;
        public event RequestConfigEventHandler RequestConfig;
        public event RequestDataEventHandler RequestData;

        // Custom Event
        public event ReadObjectChangdEventHandler ReadObjectChanged;

        // Thread
        private bool bRun = false;
        private Thread threadLoop = null;
        private LoopStep loopStep = LoopStep.Stop;
        private Thread threadRead = null;

        // Driver
        private string driverID = "";
        private int readDataUpdateTime = 1000;
        private DriverStatus currentStatus = DriverStatus.None;
        private DateTime lastReadTime = DateTime.Now;

        // SPU FTP
        private Exception lastException = null;
        private string ipAddr = "192.168.0.1";
        private int port = 11000;
        private string EquipmentId = "EQP000000";
        private List<string> TagName = new List<string>();

        private bool _3S_Ch1 = false;
        private bool _3S_Ch2 = false;
        private bool _4S_Ch1 = false;
        private bool _4S_Ch2 = false;
        public bool _6S_Ch1 = false;
        public bool _6S_Ch2 = false;

        private bool _4S_Request_Ch1 = false;
        private bool _4S_Request_Ch2 = false;

        private bool _11R_SendFlag = false;
        private byte[] _11R_Data = null;

        // Network receive queue
        List<byte> receive = new List<byte>();
        private int _2R_SendCount = 0;

        public string DriverID { get => driverID; set => driverID = value; }

        public DriverType GetDriverType => DriverType.Custom;

        public bool Started { get => bRun; }

        public bool SimulationMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int ReadDataUpdateTime { get => readDataUpdateTime; }

        public DriverStatus CurrentStatus
        {
            get => currentStatus;

            set
            {
                if (currentStatus != value)
                {
                    currentStatus = value;
                    StatusChanged?.BeginInvoke(this, currentStatus, DateTime.Now, null, null);
                }
            }
        }

        public Exception LastException
        {
            get => lastException;

            set
            {
                lastException = value;

                //IronLog4Net.IronLogger.LOG.WriteLog("Exception", lastException.ToString());
            }
        }



        public DriverStatus GetDriverStatus() { return CurrentStatus; }

        public DateTime GetLastReadTime() { return lastReadTime; }

        public Driver()
        {

        }

        ~Driver()
        {
            bRun = false;

            if (threadLoop != null)
            {
                if (threadLoop.IsAlive)
                {
                    threadLoop.Interrupt();
                    threadLoop.Abort();
                }
            }

            if (threadRead != null)
            {
                if (threadRead.IsAlive)
                {
                    threadRead.Interrupt();
                    threadRead.Abort();
                }
            }
        }

        public bool StartDriver()
        {
            bRun = true;

            // next step
            loopStep = LoopStep.Init;

            if (threadLoop == null)
            {
                threadLoop = new Thread(Loop);
            }

            if (threadLoop.ThreadState == ThreadState.Unstarted)
            {
                threadLoop.Start();
            }

            if (threadLoop.ThreadState == ThreadState.Stopped)
            {
                threadLoop = new Thread(Loop);
                threadLoop.Start();
            }


            if (threadRead == null)
            {
                threadRead = new Thread(ReadLoop);
            }

            if (threadRead.ThreadState == ThreadState.Unstarted)
            {
                threadRead.Start();
            }

            if (threadRead.ThreadState == ThreadState.Stopped)
            {
                threadRead = new Thread(ReadLoop);
                threadRead.Start();
            }

            return Started;
        }

        public bool StopDriver()
        {
            // next step
            loopStep = LoopStep.Stop;
            CurrentStatus = DriverStatus.Stop;

            bRun = false;
            threadLoop.Join();
            threadLoop = null;
            threadRead.Join();
            threadRead = null;

            return Started;
        }

        public bool RestartDriver()
        {
            StopDriver();
            StartDriver();

            return true;
        }

        private bool CheckCommStatus()
        {
            bool bResult = false;

            try
            {
                if (socket == null)
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }

                if (IsNetworkConnected == false || _2R_SendCount >= 10)
                {
                    if (_2R_SendCount >= 10)
                        API.OutputDebugViewString("[PD Driver] 2S not received, init socket");

                    try
                    {
                        socket.Connect(ipAddr, port);
                        receive.Clear();
                        socketError = false;
                    }
                    catch
                    {
                        socket.Dispose();
                        socket = null;
                    }
                }

                bResult = IsNetworkConnected;
            }
            catch (Exception exception)
            {
                LastException = exception;
            }

            // Comm status update
            string[] tagName = new string[1];
            object[] newData = new object[1];
            DateTime[] dateTime = new DateTime[1];

            tagName[0] = DriverID + ".CommunicationStatus";
            newData[0] = bResult ? 1 : 0;
            dateTime[0] = DateTime.Now;

            ReadObjectChanged?.Invoke(this, tagName, newData, dateTime);

            return bResult;
        }

        private void RemoveZeroData()
        {
            do
            {
                if (receive.Count == 0)
                    break;

                if (receive[0] == 0)
                    receive.RemoveAt(0);
                else
                    break;

            } while (true);
        }

        private void ReadLoop()
        {
            try
            {
                while (bRun)
                {
                    if (loopStep == LoopStep.Run)
                    {
                        ReadData();
                        RemoveZeroData();

                        int ParseCnt;

                        do
                        {
                            ParseCnt = 0;
                            if (Parse_2S()) ParseCnt++;
                            if (Parse_3S()) ParseCnt++;
                            if (Parse_4S()) ParseCnt++;
                            if (Parse_6S()) ParseCnt++;
                            if (Parse_11S()) ParseCnt++;
                            if (Parse_12S()) ParseCnt++;
                        } while (ParseCnt > 0);

                        if (ParseCnt == 0)
                        {
                            if (receive.Count > 1294) //패킷 최대 길이
                            {
                                RemoveZeroData();

                                if (receive[0] != 2 && receive[0] != 3 && receive[0] != 4 && receive[0] != 6)
                                { 
                                    // 테스트용 큐 클리어 (임시)
                                    System.Diagnostics.Trace.WriteLine("");
                                    System.Diagnostics.Trace.WriteLine("");
                                    System.Diagnostics.Trace.WriteLine("큐 클리어 : " + receive.Count);
                                    System.Diagnostics.Trace.Write("[0] " + receive[0] + ", ");
                                    System.Diagnostics.Trace.Write("[1] " + receive[1] + ", ");
                                    System.Diagnostics.Trace.Write("[2] " + receive[2] + ", ");
                                    System.Diagnostics.Trace.Write("[3] " + receive[3] + ", ");
                                    System.Diagnostics.Trace.Write("[4] " + receive[4] + ", ");
                                    System.Diagnostics.Trace.Write("[5] " + receive[5] + ", ");
                                    System.Diagnostics.Trace.WriteLine("");
                                    System.Diagnostics.Trace.WriteLine("");
                                    System.Diagnostics.Trace.WriteLine("");
                                    receive.Clear();
                                }
                            }
                        }

                        Thread.Sleep(1);
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }
            }
            catch (Exception exception)
            {
                LastException = exception;

                CurrentStatus = DriverStatus.Error;

                threadRead = new Thread(ReadLoop);
                threadRead.Start();
            }
        }

        private void Loop()
        {
            try
            {
                while (bRun)
                {
                    DateTime tickStart = DateTime.Now;


                    // Driver Information Init
                    if (loopStep == LoopStep.Init)
                    {
                        if (InitDriver())
                        {
                            // next step
                            loopStep = LoopStep.Run;
                            CurrentStatus = DriverStatus.Run;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                            continue;
                        }
                    }


                    if (loopStep == LoopStep.Run)
                    {

                        // TCP Comm Connection Check
                        if (CheckCommStatus() == false)
                        {
                            API.OutputDebugViewString("[PD Driver Comm Check False!!!!!");
                            Thread.Sleep(1000);
                            continue;
                        }

                        // Data Request
                        API.OutputDebugViewString("[PD Driver Send 2R");
                        Send_2R();
                        Send_11R();

                        /*
                        // Check event data receive flag
                        for (int i = 0; i < 2; i++)
                        {
                            TimeSpan span = DateTime.Now - dtEventReceive[i];

                            string[] tagName = new string[1];
                            object[] newData = new object[1];
                            DateTime[] time = new DateTime[1];

                            tagName[0] = DriverID + ".Ch" + (i + 1) + ".EventFlag";
                            
                            time[0] = DateTime.Now;


                            if (span.Seconds <= 10)
                                newData[0] = 1;
                            else
                                newData[0] = 0;

                            ReadObjectChanged(this, tagName, newData, time);
                        }*/
                        /*
                        ReadStream();

                        Parse_2S();
                        Parse_3S();
                        Parse_4S();
                        Parse_6S();
                        */
                    }

                    // Check Period
                    DateTime tickEnd = DateTime.Now;
                    TimeSpan tickProc = tickEnd - tickStart;
                    int tickGap = readDataUpdateTime - (int)tickProc.TotalMilliseconds;

                    if (tickGap > 0)
                        Thread.Sleep(tickGap);
                }
            }
            catch (Exception exception)
            {
                LastException = exception;

                CurrentStatus = DriverStatus.Error;

                //state = DriverStep.Idle;
                threadLoop = new Thread(Loop);
                threadLoop.Start();
            }
        }

        private bool InitDriver()
        {
            bool bResult = false;

            try
            {
                DriverInformation config = null;

                if (RequestConfig != null)
                    config = RequestConfig(this);

                if (config != null)
                {
                    if (config is CustomInformation customConfig)
                    {
                        DriverID = customConfig.DriverName;

                        Dictionary<string, string> dicInfo = customConfig.Info;

                        if (dicInfo.ContainsKey("IPaddr"))
                        {
                            ipAddr = dicInfo["IPaddr"];
                        }
                        
                        if (dicInfo.ContainsKey("EquipmentId"))
                        {
                            EquipmentId = dicInfo["EquipmentId"];
                        }
                        
                        bResult = true;
                    }
                }
            }
            catch (Exception exception)
            {
                LastException = exception;
                bResult = false;
            }

            return bResult;
        }

        private void SendData_2R(object obj)
        {
            try
            {
                byte[] data = PacketManager.StructureToByte(obj);
                PacketManager.ReverseByte_2R(ref data);
                
                int sendbyte = socket.Send(data);
            }
            catch
            {
                socketError = true;
            }
        }
        
        private void SendData(byte[] data)
        {
            try
            {
                int sendbyte = socket.Send(data);
            }
            catch
            {
                socketError = true;
            }
        }

        private void ReadData()
        {
            try
            {
                if (socket != null)
                {
                    byte[] buff = new byte[102400];

                    int readByte = socket.Receive(buff, 102400, SocketFlags.None);
                    
                    for (int i = 0; i < readByte; i++)
                        receive.Add(buff[i]);
                }
            }
            catch
            {
                socketError = false;
            }
        }

        private byte[] ReceiveDequeue(int count)
        {
            byte[] data = new byte[count];
            if (receive != null && receive.Count >= count)
            {
                for (int i = 0; i < count; i++)
                {
                    data[i] = receive[0];
                    receive.RemoveAt(0);
                }
            }

            return data;
        }

        private byte GetChannelUse(bool ch1, bool ch2)
        {
            byte rtn = 0;

            if (ch1)
                rtn = (byte)(rtn | 0x01);

            if (ch2)
                rtn = (byte)(rtn | 0x02);

            return rtn;
        }

        private bool[] GetChannelUse(byte use)
        {
            bool[] rtn = new bool[2];

            if ((use & 0x01) == 0x01)
                rtn[0] = true;
            else
                rtn[0] = false;

            if ((use & 0x02) == 0x02)
                rtn[1] = true;
            else
                rtn[1] = false;

            return rtn;
        }

        private void Send_2R()
        {
            try
            {
                if (_2R_SendCount < 10)
                    _2R_SendCount++;

                byte DASAddress = 0;
                string[] arrIp = ipAddr.Split('.');
                if (arrIp.Length >= 4)
                    byte.TryParse(arrIp[3], out DASAddress);

                // 3S : 그래프 정보 // 항상 요청
                _3S_Ch1 = true;
                _3S_Ch2 = true;

                // 4S : Event // 정보 있을때 한번 요청
                _4S_Ch1 = _4S_Request_Ch1;
                _4S_Ch2 = _4S_Request_Ch2;
                _4S_Request_Ch1 = false;
                _4S_Request_Ch2 = false;

                /*
                // 이벤트 테스트용 코드
                if (DateTime.Now.Second % 15 == 0)
                {
                    _4S_Request_Ch2 = true;
                }
                if (DateTime.Now.Second % 15 == 5)
                {
                    _4S_Request_Ch1 = true;
                }*/

                // 6S : Trend, History // 15분 마다 요청
                int minute = DateTime.Now.Minute;
                int second = DateTime.Now.Second;
                if (minute % 15 == 0 && second <= 5)
                {
                    _6S_Ch1 = true;
                    _6S_Ch2 = true;
                }
                else
                {
                    _6S_Ch1 = false;
                    _6S_Ch2 = false;
                }

                Packet_PD_2R _2r;

                _2r.FrameIndex = 0x02;
                _2r.FrameLength = 14;
                _2r.DASAddress = DASAddress;
                _2r.Year = (short)DateTime.Now.Year;
                _2r.Month = (byte)DateTime.Now.Month;
                _2r.Day = (byte)DateTime.Now.Day;
                _2r.Hour = (byte)DateTime.Now.Hour;
                _2r.Minite = (byte)DateTime.Now.Minute;
                _2r.Second = (byte)DateTime.Now.Second;
                _2r._3SInfo = GetChannelUse(_3S_Ch1, _3S_Ch2);
                _2r._4SInfo = GetChannelUse(_4S_Ch1, _4S_Ch2);
                _2r._6SInfo = GetChannelUse(_6S_Ch1, _6S_Ch2);

                SendData_2R(_2r);
            }
            catch (Exception exception)
            {
                int a = 0;
            }
        }

        private void Send_11R()
        {
            try
            {
                if (_11R_SendFlag)
                {
                    SendData(_11R_Data);

                    _11R_SendFlag = false;
                    _11R_Data = null;
                    
                    API.OutputDebugViewString("[PD Driver [PD 11R Send]");
                }
            }
            catch (Exception exception)
            {
                int a = 0;
            }
        }

        private bool Parse_2S()
        {
            bool bParse = false;

            try
            {
                if (receive.Count >= 46)
                {
                    if (receive[0] == 2)
                    {
                        // 2S
                        byte[] data = ReceiveDequeue(46);
                        PacketManager.ReverseByte_2S(ref data);
                        Packet_PD_2S _2s = PacketManager.ByteToStruct<Packet_PD_2S>(data);
                        
                        bParse = true;
                        API.OutputDebugViewString("[PD Driver [PD 2S Receive]");
                        _2R_SendCount = 0;

                        bool [] b4s = GetChannelUse(_2s._4S_Request);
                        if (_4S_Request_Ch1 == false)
                            _4S_Request_Ch1 = b4s[0];

                        if (_4S_Request_Ch2 == false)
                            _4S_Request_Ch2 = b4s[1];

                        if (_4S_Request_Ch1 || _4S_Request_Ch2)
                        {
                            System.Diagnostics.Trace.WriteLine("");
                            System.Diagnostics.Trace.WriteLine("");
                            System.Diagnostics.Trace.WriteLine("####################################");
                            System.Diagnostics.Trace.WriteLine("_4S_Request_Ch1 : "  + _4S_Request_Ch1);
                            System.Diagnostics.Trace.WriteLine("_4S_Request_Ch2 : "  + _4S_Request_Ch2);
                            System.Diagnostics.Trace.WriteLine("####################################");
                            System.Diagnostics.Trace.WriteLine("");
                            System.Diagnostics.Trace.WriteLine("");
                        }

                        const int dataCount = 9;

                        // Ch1, Ch2
                        string[] tagName = new string[dataCount * 2];
                        object[] newData = new object[dataCount * 2];
                        DateTime[] time = new DateTime[dataCount * 2];

                        string[] tagHeader = {
                            DriverID + ".Ch1.",
                            DriverID + ".Ch2.",
                        };
                        
                        int idx = 0;
                        int header = 0;

                        bool[] EventAlarm = GetChannelUse(_2s._4S_Request);
                        bool[] MaxCountAlarm = GetChannelUse(_2s.EventPer15Min);
                        bool[] HIQCAlarm = GetChannelUse(_2s.HIQC_Alarm);
                        bool[] HINCAlarm = GetChannelUse(_2s.HINC_Alarm);
                        
                        // Ch 1
                        header = 0;
                        tagName[idx] = tagHeader[header] + "Max";
                        newData[idx] = _2s.MAX1;
                        idx++;
                        tagName[idx] = tagHeader[header] + "Avg";
                        newData[idx] = _2s.AVG1;
                        idx++;
                        tagName[idx] = tagHeader[header] + "PPS";
                        newData[idx] = _2s.PPS1;
                        idx++;
                        tagName[idx] = tagHeader[header] + "HIQ";
                        newData[idx] = _2s.HIQ1;
                        idx++;
                        tagName[idx] = tagHeader[header] + "HIN";
                        newData[idx] = _2s.HIN1;
                        idx++;
                        tagName[idx] = tagHeader[header] + "EventAlarm";
                        newData[idx] = EventAlarm[header] ? 1 : 0;
                        idx++;
                        tagName[idx] = tagHeader[header] + "MaxCountAlarm";
                        newData[idx] = MaxCountAlarm[header] ? 1 : 0;
                        idx++;
                        tagName[idx] = tagHeader[header] + "HIQCAlarm";
                        newData[idx] = HIQCAlarm[header] ? 1 : 0;
                        idx++;
                        tagName[idx] = tagHeader[header] + "HINCAlarm";
                        newData[idx] = HINCAlarm[header] ? 1 : 0;
                        idx++;

                        // Ch 2
                        header = 1;
                        tagName[idx] = tagHeader[header] + "Max";
                        newData[idx] = _2s.MAX2; //System.Diagnostics.Trace.Write(", " + _2s.MAX2);
                        idx++;
                        tagName[idx] = tagHeader[header] + "Avg";
                        newData[idx] = _2s.AVG2; //System.Diagnostics.Trace.Write(", " + _2s.AVG2);
                        idx++;
                        tagName[idx] = tagHeader[header] + "PPS";
                        newData[idx] = _2s.PPS2; //System.Diagnostics.Trace.Write(", " + _2s.PPS2);
                        idx++;
                        tagName[idx] = tagHeader[header] + "HIQ";
                        newData[idx] = _2s.HIQ2; //System.Diagnostics.Trace.Write(", " + _2s.HIQ2);
                        idx++;
                        tagName[idx] = tagHeader[header] + "HIN";
                        newData[idx] = _2s.HIN2; //System.Diagnostics.Trace.WriteLine(", " + _2s.HIN2);
                        idx++;
                        tagName[idx] = tagHeader[header] + "EventAlarm";
                        newData[idx] = EventAlarm[header] ? 1 : 0;
                        idx++;
                        tagName[idx] = tagHeader[header] + "MaxCountAlarm";
                        newData[idx] = MaxCountAlarm[header] ? 1 : 0;
                        idx++;
                        tagName[idx] = tagHeader[header] + "HIQCAlarm";
                        newData[idx] = HIQCAlarm[header] ? 1 : 0;
                        idx++;
                        tagName[idx] = tagHeader[header] + "HINCAlarm";
                        newData[idx] = HINCAlarm[header] ? 1 : 0;
                        idx++;

                        // Time
                        for (int i = 0; i < time.Length; i++)
                            //time[i] = new DateTime(_2s.Year, _2s.Month, _2s.Day, _2s.Hour, _2s.Minite, _2s.Second);
                            time[i] = DateTime.Now;

                        ReadObjectChanged(this, tagName, newData, time);
                    }
                    else
                    {

                    }
                }



            }
            catch (Exception exception)
            {
                int a = 0;
            }

            return bParse;
        }

        private bool Parse_3S()
        {
            bool bParse = false;

            try
            {
                if (receive.Count >= 1294)
                {
                    if (receive[0] == 3)
                    {
                        // 3S
                        bParse = true;
                        
                        int SensorID = receive[4];
                        int FrameNum = receive[5];
                        
                        if (SensorID >= 1 && SensorID <= 2 && FrameNum >= 1 && FrameNum <= 6)
                        {
                            API.OutputDebugViewString("[PD Driver [PD 3S, " + SensorID  + ", " + FrameNum + " Receive]");

                            byte[] header = ReceiveDequeue(14);
                            byte[] data = ReceiveDequeue(1280);


                            string[] tagName = new string[1];
                            object[] newData = new object[1];
                            DateTime[] time = new DateTime[1];

                            tagName[0] = DriverID + ".Ch" + SensorID + ".PRPS" + FrameNum;
                            newData[0] = data;
                            time[0] = DateTime.Now;

                            ReadObjectChanged(this, tagName, newData, time);

                        }
                        else
                        {
                            // Ch 1,2가 아니거나 FrameNo 1~6 아닌것을 수신했을 때 예외처리
                            byte[] data = ReceiveDequeue(1294);
                        }
                    }
                }

            }
            catch (Exception exception)
            {
                int a = 0;
            }

            return bParse;
        }

        DateTime[] dtEventReceive = new DateTime[2];
        int[] lastEventFlagValue = new int[2];

        private bool Parse_4S()
        {
            bool bParse = false;

            try
            {
                // 길이 가변이니 두번 체크해야함
                if (receive.Count >= 40)
                {
                    if (receive[0] == 4)
                    {
                        // 4S
                        int SensorID = receive[4];
                        int FrameNum = receive[5];

                        if (FrameNum == 1)
                        {
                            byte[] data = ReceiveDequeue(60);
                            API.OutputDebugViewString("[PD Driver] [PD 4S FrameNo." + FrameNum + " Receive], ch : " + SensorID);
                            bParse = true;
                        }

                        if (receive.Count >= 1294)
                        {
                            int dataSensorID = receive[4];
                            int dataFrameNum = receive[5];
                            if (dataSensorID >= 1 && dataSensorID <= 2 && dataFrameNum >= 2 && dataFrameNum <= 7)
                            {
                                API.OutputDebugViewString("[PD Driver] [PD 4S FrameNo." + dataFrameNum + " Receive], ch" + dataSensorID);
                                
                                byte[] header = ReceiveDequeue(14);
                                byte[] data = ReceiveDequeue(1280);
                                
                                string[] tagName = new string[1];
                                object[] newData = new object[1];
                                DateTime[] time = new DateTime[1];

                                tagName[0] = DriverID + ".Ch" + dataSensorID + ".Event" + (dataFrameNum - 1);
                                newData[0] = data;
                                time[0] = DateTime.Now;

                                ReadObjectChanged(this, tagName, newData, time);

                                bParse = true;
                            }
                            else
                            {
                                // Ch 1,2가 아니거나 FrameNo 1~6 아닌것을 수신했을 때 예외처리
                                byte[] data = ReceiveDequeue(1294);
                            }

                            
                            
                            
                        }
                    }
                }

            }
            catch (Exception exception)
            {
                int a = 0;
            }

            return bParse;
        }

        private bool Parse_6S()
        {
            bool bParse = false;

            try
            {
                if (receive.Count >= 528)
                {
                    int SensorID = receive[4];
                    
                    if (receive[0] == 6 && (SensorID == 1 || SensorID == 2))
                    {
                        // 6S
                        //byte[] data = ReceiveDequeue(528);
                        //bParse = true;
                        API.OutputDebugViewString("[PD Driver] [PD 6S Receive, ch : " + SensorID);


                        byte[] header = ReceiveDequeue(9);
                        byte[] trend = ReceiveDequeue(7);
                        byte[] history = ReceiveDequeue(512);

                        string[] tagName = new string[2];
                        object[] newData = new object[2];
                        DateTime[] time = new DateTime[2];

                        tagName[0] = DriverID + ".Ch" + SensorID + ".History";
                        newData[0] = history;
                        time[0] = DateTime.Now;

                        tagName[1] = DriverID + ".Ch" + SensorID + ".Trend";
                        newData[1] = trend;
                        time[1] = DateTime.Now;

                        ReadObjectChanged(this, tagName, newData, time);

                        bParse = true;
                    }
                }

            }
            catch (Exception exception)
            {
                int a = 0;
            }

            return bParse;
        }

        private bool Parse_11S()
        {
            bool bParse = false;

            try
            {
                if (receive.Count >= 32)
                {
                    if (receive[0] == 11)
                    {
                        // 11S
                        byte[] data = ReceiveDequeue(32);
                        API.OutputDebugViewString("[PD Driver] [PD 11S Receive (Setting)]");
                        System.Diagnostics.Trace.WriteLine("[PD Driver] [PD 11S Receive (Setting)]");

                        string[] tagName = new string[1];
                        object[] newData = new object[1];
                        DateTime[] time = new DateTime[1];
                        
                        tagName[0] = DriverID + ".ChAll.Setting";
                        newData[0] = data;
                        time[0] = DateTime.Now;

                        ReadObjectChanged(this, tagName, newData, time);

                        
                        bParse = true;
                    }
                }

            }
            catch (Exception exception)
            {
                int a = 0;
            }

            return bParse;
        }

        private bool Parse_12S()
        {
            bool bParse = false;

            try
            {
                if (receive.Count >= 12)
                {
                    if (receive[0] == 12)
                    {
                        // 12S
                        ReceiveDequeue(12);
                        bParse = true;

                        API.OutputDebugViewString("[PD Driver] [PD 12S Receive]");
                    }
                }

            }
            catch (Exception exception)
            {
                int a = 0;
            }

            return bParse;
        }

        public object ReadAny(string tag)
        {
            throw new NotImplementedException();
        }

        public object ReadAny(string[] tags)
        {
            throw new NotImplementedException();
        }

        public int WriteData(string tag, object value)
        {
            API.OutputDebugViewString("[PD Driver] WriteData tag : " + tag);
            API.OutputDebugViewString("[PD Driver] WriteData value : " + value);

            string tag_Setting = DriverID + ".ChAll.Setting";

            if (tag == tag_Setting)
            {
                if (value is byte[] data)
                {
                    if (data.Length == 32)
                    {
                        //API.OutputDebugViewString("[PD Driver] 11R SendFlag is true");
                        _11R_Data = data;
                        _11R_SendFlag = true;
                    }
                    else
                    {
                        API.OutputDebugViewString("[PD Driver] Length != 32");
                    }
                }
                else
                {
                    API.OutputDebugViewString("[PD Driver] Not byte array");
                }
            }

            return 0;
        }

        public int[] WriteData(string[] tags, object[] values)
        {
            //throw new NotImplementedException();

            System.Diagnostics.Trace.WriteLine("WriteData tags.Length : " + tags.Length);
            System.Diagnostics.Trace.WriteLine("WriteData values.Length : " + values.Length);

            return null;
        }
    }
}
