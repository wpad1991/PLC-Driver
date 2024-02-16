using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronInterface;
using IronInterface.Driver;
using System.Threading;
using System.IO.Ports;

namespace Robostar_RS232C
{
    public class Driver : ITagDriver
    {
        /// <summary>
        /// 2019년 6월 3일부로 Serial 통신은 IronSAN을 이용하는 방법으로 사용되기 때문에
        /// 개발 중지되었습니다.
        /// </summary>


        SerialPort serialPort;
        IronInterface.Configuration.SerialConfig serialConfig;
        DateTime lastReadTime;
        ManualResetEvent loopStopEvent = new ManualResetEvent(false);
        List<string> TotalTagList = new List<string>();
        List<DriverTagDataInfo> TagManager_Update = new List<DriverTagDataInfo>();
        List<DriverTagDataInfo> TagManager_Digital = new List<DriverTagDataInfo>();
        List<DriverTagDataInfo> TagManager_Analog = new List<DriverTagDataInfo>();
        DriverStep state = DriverStep.Init;
        Thread loopThread;
        int loopInterval = 50;
        string driverID = "Robostar_RS232C";
        bool simulationMode = false;
        public Driver()
        {
            if (!Initailize())
            {
                throw new InvalidProgramException();
            }
        }

        public string DriverID { get => driverID; set { driverID = value; } }

        public DriverType GetDriverType => DriverType.Robostar_RS232C;

        public bool SimulationMode { get => simulationMode; set => simulationMode = value; }

        public bool Started
        {
            get { return !loopStopEvent.WaitOne(0); }
        }

        public int ReadDataUpdateTime => throw new NotImplementedException();

        public event ReadDataChangdEventHandler ReadBitsChanged;
        public event ReadDataChangdEventHandler ReadBytesChanged;
        public event ReadObjectChangdEventHandler ReadObjectChanged;
        public event DriverStatusChangedEventHandler StatusChanged;
        public event RequestConfigEventHandler RequestConfig;
        public event RequestDataEventHandler RequestData;

        public DriverStatus GetDriverStatus()
        {
            if (state == DriverStep.Error)
            {
                return DriverStatus.Error;
            }
            else if (state == DriverStep.Init || state == DriverStep.None)
            {
                return DriverStatus.Normal;
            }

            return DriverStatus.Run;
        }

        public DateTime GetLastReadTime()
        {
            return lastReadTime;
        }

        public byte[] ReadBits()
        {
            List<byte> result = new List<byte>();

            foreach (DriverTagDataInfo dataInfo in TagManager_Digital)
            {
                if (dataInfo.Data == null)
                {
                    result.Add(0);
                }
                else
                {
                    result.Add(Convert.ToByte(dataInfo.Data));
                }
            }

            return result.ToArray();
        }

        public byte[] ReadBytes()
        {
            List<byte> result = new List<byte>();

            foreach (DriverTagDataInfo dataInfo in TagManager_Analog)
            {
                if (dataInfo.Data == null)
                {
                    result.Add(0);
                }
                else
                {
                    result.Add(Convert.ToByte(dataInfo.Data));
                }
            }
            return result.ToArray();
        }

        public bool RestartDriver()
        {
            StopDriver();
            StartDriver();
            return true;
        }

        public bool StartDriver()
        {
            if (simulationMode)
            {
                return true;
            }

            if (loopThread == null)
            {
                loopStopEvent.Reset();

                loopThread = new Thread(execute);
                loopThread.Start();
            }
            else if (loopThread.ThreadState == ThreadState.Stopped)
            {
                loopStopEvent.Reset();

                loopThread = new Thread(execute);
                loopThread.Start();
            }
            else if (loopThread.ThreadState == ThreadState.Unstarted)
            {
                loopStopEvent.Reset();

                loopThread.Start();
            }
            else
            {
                return false;
            }

            return true;
        }

        public bool StopDriver()
        {
            if (simulationMode)
            {
                return true;
            }

            StatusChanged?.BeginInvoke(this, DriverStatus.Stop, DateTime.Now, null, null);
            loopStopEvent.Set();
            state = DriverStep.Init;

            loopThread.Join();

            return Started;
        }

        public void WriteBits(int addr, byte[] array)
        {
            IronUtility.IronLogger.LOG.WriteLog("Console", "[Robostar_RS232C.Driver] : WriteBits Addr : " + addr + ", Data : " + Encoding.Default.GetString(array));

            if (array != null)
            {
                if (TagManager_Digital.Count > addr)
                {
                    if (Encoding.ASCII.GetString(array) == "1")
                    {
                        TagManager_Digital[addr].Data = 1;
                        if (TagManager_Digital[addr].ActionFunc != null)
                        {
                            lock (serialPort)
                            {
                                TagManager_Digital[addr].ActionFunc();
                            }
                        }
                    }
                    else
                    {
                        TagManager_Digital[addr].Data = 0;
                    }
                }
                else
                {
                    IronUtility.IronLogger.LOG.WriteLog("Console", "[Robostar_RS232C.Driver] : WriteBits Invalid Addr : " + addr + ", Data : " + Encoding.Default.GetString(array));
                }
            }
            else
            {
                IronUtility.IronLogger.LOG.WriteLog("Console", "[Robostar_RS232C.Driver] : WriteBits Array Is Null, Addr : " + addr);
            }
        }

        public void WriteBytes(int addr, byte[] array)
        {
            IronUtility.IronLogger.LOG.WriteLog("Console", "[Robostar_RS232C.Driver] : WriteBytes Addr : " + addr + ", Data : " + Encoding.Default.GetString(array));

            if (array != null)
            {
                if (TagManager_Analog.Count > addr)
                {
                    if (double.TryParse(Encoding.ASCII.GetString(array), out double d_val))
                    {
                        TagManager_Analog[addr].Data = d_val;
                        if (TagManager_Analog[addr].ActionFunc != null)
                        {
                            lock (serialPort)
                            {
                                TagManager_Analog[addr].ActionFunc();
                            }
                        }
                    }
                    else
                    {
                        IronUtility.IronLogger.LOG.WriteLog("Console", "[Robostar_RS232C.Driver] : WriteBytes Invalid Value,  Addr : " + addr + ", Data : " + Encoding.Default.GetString(array));
                    }
                }
                else
                {
                    IronUtility.IronLogger.LOG.WriteLog("Console", "[Robostar_RS232C.Driver] : WriteBytes Invalid Addr : " + addr + ", Data : " + Encoding.Default.GetString(array));
                }
            }
            else
            {
                IronUtility.IronLogger.LOG.WriteLog("Console", "[Robostar_RS232C.Driver] : WriteBytes Array Is Null, Addr : " + addr);
            }
        }

        public List<DriverTagDataInfo> GetBitsTagInfo()
        {
            return TagManager_Digital;
        }

        public List<DriverTagDataInfo> GetBytesTagInfo()
        {
            return TagManager_Analog;
        }

        #region Private Field
        private bool Initailize()
        {
            try
            {
                TotalTagList.Clear();

                TagManager_Update.Clear();

                TagManager_Digital.Clear();

                TagManager_Analog.Clear();

                #region Update Tag Setting

                TagManager_Update.Add(new DriverTagDataInfo(
                    "R_STATE",
                    "로봇 상태 정보를 외부 장치로 반환 합니다.",
                    0,
                    R_STATE,
                    IOType.UPDATE,
                    typeof(bool)));
                /* 추후 작업 현재 필요 없음
                TagManager_Update.Add(new DriverTagDataInfo(
                    "GETCPOS",
                    "로봇 Current Postion 값을 반환 합니다.",
                    0,
                    GETCPOS,
                    IOType.UPDATE));

                TagManager_Update.Add(new DriverTagDataInfo(
                    "GETFOLD",
                    "확인 하고자 하는 ARM FOLD 값을 반환 합니다.",
                    0,
                    GETFOLD,
                    IOType.UPDATE));
                    */
                #endregion

                #region Digital Tag Setting
                TagManager_Digital.Add(new DriverTagDataInfo(
                    "INITIAL",
                    "Master Job running, 서보전원 認可(인가) , 리니어 TYPE 로봇 사용시 ORIGN 수행, 전축 HOME 이동 동작수행",
                    0,
                    INITIAL,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "SERVOON",
                    "서보전원 認可(인가)",
                    0,
                    SERVOON,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "SERVOOF",
                    "서보전원 未認可(미인가)",
                    0,
                    SERVOOF,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "VACUMON",
                    "지정핸드 진공 흡착동작 및 GRIP동작 수행",
                    0,
                    VACUMON,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "VACUMOF",
                    "지정핸드 진공 파기동작 및 UNGRIP동작 수행",
                    0,
                    VACUMOF,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "R_RHOME",
                    "워크 [有,無] 상태확인후 진공 초기화 , 전축 HOME이동 동작수행",
                    0,
                    R_RHOME,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "FGREADY",
                    "배출전 대기위치",
                    0,
                    FGREADY,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "FPREADY",
                    "투입전 대기위치",
                    0,
                    FPREADY,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETFROM",
                    "워크 배출하는 동작",
                    0,
                    GETFROM,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "PUTINTO",
                    "워크 투입하는 동작",
                    0,
                    PUTINTO,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETRTAL",
                    "RT-alignment 배출 동작 [핸드 alignment 와 주행 alignment를 모두 진행]",
                    0,
                    GETRTAL,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "R_PAUSE",
                    "동작 중인 로봇 HOLD (일시정지)",
                    0,
                    R_PAUSE,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "R_RESUM",
                    "HOLD중인 로봇 REHOLD (일시정지 해제)",
                    0,
                    R_RESUM,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "R_RESET",
                    "제어기 ALARM 해제 및 ERROR RESET",
                    0,
                    R_RESET,
                    IOType.DO,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "R_RSTOP",
                    "동작중인 로봇을 정지동작 및 구동 프로그램 초기화",
                    0,
                    R_RSTOP,
                    IOType.DO,
                    typeof(bool)));


                TagManager_Digital.Add(new DriverTagDataInfo(
                    "Robot_Error",
                    "Robot Error = Error - Normal",
                    0,
                    null,
                    IOType.DI,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "Servo_ON",
                    "Servo ON = OFF - ON",
                    0,
                    null,
                    IOType.DI,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "Remote_Mode",
                    "Remote Mode = Servo OFF - Servo ON",
                    0,
                    null,
                    IOType.DI,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "Master_Job_Running",
                    "Master Job Running = Manual - Auto",
                    0,
                    null,
                    IOType.DI,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "Arm_Folding",
                    "Arm Folding = Forward - Backward",
                    0,
                    null,
                    IOType.DI,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "Vacuum_ON",
                    "Vacuum ON = OFF - ON",
                    0,
                    null,
                    IOType.DI,
                    typeof(bool)));

                TagManager_Digital.Add(new DriverTagDataInfo(
                   "Solenoid_Valve",
                   "Solenoid Valve = Open - Close",
                   0,
                   null,
                   IOType.DI,
                    typeof(bool)));

                /*
                TagManager_Digital.Add(new DriverTagDataInfo(
                    "MOVEABS",
                    "지정축을 절대위치로 이동합니다. [로봇 O점 위치에서 이동거리 만큼]",
                    0,
                    MOVEABS,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "MOVEREL",
                    "지정축을 상대위치로 이동합니다. [현 위치에서 이동거리 만큼]",
                    0,
                    MOVEREL,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETTPOS",
                    "배출 동작으로 Teaching위치로 이동합니다.",
                    0,
                    GETTPOS,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "PUTTPOS",
                    "투입 동작으로 Teaching위치로 이동합니다.",
                    0,
                    PUTTPOS,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GEXTEND",
                    "배출 위치 핸드삽입 동작 동작 순서 : G1 -> G2",
                    0,
                    GEXTEND,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "PEXTEND",
                    "투입 위치 핸드삽입 동작 동작 순서 : P1 -> P2",
                    0,
                    PEXTEND,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETEXUP",
                    "배출 위치 핸드삽입 후 상승 동작 동작 순서 : G1 -> G2 -> G3",
                    0,
                    GETEXUP,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "PUTEXDN",
                    "투입 위치 핸드삽입 후 하강 동작 동작 순서 : P1 -> P2 -> P3",
                    0,
                    PUTEXDN,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GRETRAC",
                    "배출 위치 핸드삽입 동작완료 후 상승치 만큼 상승후 핸드 복귀동작 동작 순서 : G2 -> G3 -> G4",
                    0,
                    GRETRAC,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "PRETRAC",
                    "투입 위치 핸드삽입 동작완료 후 하강치 만큼 하강후 핸드 복귀동작 동작 순서 : P2 -> P3 -> P4",
                    0,
                    PRETRAC,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETAFLD",
                    "워크 有 확인 후 현 위치에서 핸드 복귀동작",
                    0,
                    GETAFLD,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "PUTAFLD",
                    "워크 無 확인 후 현 위치에서 핸드 복귀동작",
                    0,
                    PUTAFLD,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "TRANSGP",
                    "지정된 핸드로 배출동작 완료후 연속해서 투입동작을 수행 완료함",
                    0,
                    TRANSGP,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "TRANSPG",
                    "지정된 핸드로 투입동작 완료후 연속해서 배출동작을 수행 완료함",
                    0,
                    TRANSPG,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "TRANSGF",
                    "지정된 핸드로 배출동작 완료후 연속해서 투입대기동작을 수행 완료함",
                    0,
                    TRANSGF,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "TRANSPF",
                    "지정된 핸드로 투입동작 완료후 연속해서 배출대기동작을 수행 완료함",
                    0,
                    TRANSPF,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "SETLPOS",
                    "현 위치를 티칭 Position 위치로 지정 합니다. [티칭값 저장]",
                    0,
                    SETLPOS,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "SETFOLD",
                    "로봇 핸드 안전 복귀 위치값을 지정 합니다. [ARM FOLD 거리값 저장]",
                    0,
                    SETFOLD,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "SETLPAR",
                    "워크 반송에 필요한 설정값을 지정 합니다. [PARA 값 저장]",
                    0,
                    SETLPAR,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "SETTSPD",
                    "워크 반송에 필요한 스피드 값을 지정합니다.",
                    0,
                    SETTSPD,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "SETCORD",
                    "HOST로부터 수신된 좌표값을 티칭 값으로 저장 합니다.",
                    0,
                    SETCORD,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETLPAR",
                    "워크 반송에 필요한 설정 값을 반환 합니다.",
                    0,
                    GETLPAR,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETTSPD",
                    "워크 반송에 필요한 스피드 값을 반환 합니다.",
                    0,
                    GETTSPD,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETLPOS",
                    "확인 하고자 하는 Port 티칭 Position 값을 반환 합니다.",
                    0,
                    GETLPOS,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETFOLD",
                    "확인 하고자 하는 ARM FOLD 값을 반환 합니다",
                    0,
                    GETFOLD,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETLPAR",
                    "워크 반송에 필요한 설정 값을 반환 합니다.",
                    0,
                    GETLPAR,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETTSPD",
                    "워크 반송에 필요한 스피드 값을 반환 합니다.",
                    0,
                    GETTSPD,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETALRM",
                    "로봇 제어기의 직전 Sys alarm 및 Command alarm Code를 반환 합니다.",
                    0,
                    GETALRM,
                    IOType.DO));

                TagManager_Digital.Add(new DriverTagDataInfo(
                    "GETATXT",
                    "로봇 제어기의 직전 Sys alarm 및 Command alarm 내용을 반환 합니다.",
                    0,
                    GETATXT,
                    IOType.DO));
                    */
                #endregion

                #region Analog Tag Setting

                TagManager_Analog.Add(new DriverTagDataInfo(
                    "Station_No1",
                    "워크 반송에 필요한 스피드 값을 반환 합니다.",
                    0,
                    null,
                    IOType.AO,
                    typeof(double)));

                TagManager_Analog.Add(new DriverTagDataInfo(
                    "Slot_No1",
                    "워크 반송에 필요한 스피드 값을 반환 합니다.",
                    0,
                    null,
                    IOType.AO,
                    typeof(double)));

                TagManager_Analog.Add(new DriverTagDataInfo(
                    "Arm_No1",
                    "워크 반송에 필요한 스피드 값을 반환 합니다.",
                    0,
                    null,
                    IOType.AO,
                    typeof(double)));

                TagManager_Analog.Add(new DriverTagDataInfo(
                    "Size_No1",
                    "워크 반송에 필요한 스피드 값을 반환 합니다.",
                    0,
                    null,
                    IOType.AO,
                    typeof(double)));

                TagManager_Analog.Add(new DriverTagDataInfo(
                    "Taget_No1",
                    "워크 반송에 필요한 스피드 값을 반환 합니다.",
                    0,
                    null,
                    IOType.AO,
                    typeof(double)));


                #endregion

                for (int i = 0; i < TagManager_Digital.Count; i++)
                {

                    TagManager_Digital[i].ItemAddr = i;
                    if (TotalTagList.Contains(TagManager_Digital[i].TagName))
                    {
                        IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver Initailize] : Already Exist Tag... Check TagList");
                        return false;
                    }

                    TotalTagList.Add(TagManager_Digital[i].TagName);
                }

                for (int i = 0; i < TagManager_Analog.Count; i++)
                {
                    TagManager_Analog[i].ItemAddr = i;
                    if (TotalTagList.Contains(TagManager_Analog[i].TagName))
                    {
                        IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver Initailize] : Already Exist Tag... Check TagList");
                        return false;
                    }

                    TotalTagList.Add(TagManager_Analog[i].TagName);
                }
            }
            catch (Exception except)
            {
                IronUtility.IronLogger.LOG.WriteLog("Exception", except.ToString());
                return false;
            }
            return true;
        }

        private string GetDigitalTagValue(string tag)
        {
            foreach (DriverTagDataInfo tagInfo in TagManager_Digital)
            {
                if (tagInfo.TagName == tag)
                {
                    return tagInfo.Data == null ? throw new ArgumentNullException() : tagInfo.Data.ToString();
                }
            }

            throw new InvalidOperationException();
        }

        private string GetAnalogTagValue(string tag)
        {
            foreach (DriverTagDataInfo tagInfo in TagManager_Analog)
            {
                if (tagInfo.TagName == tag)
                {
                    return tagInfo.Data == null ? throw new ArgumentNullException() : tagInfo.Data.ToString();
                }
            }

            throw new InvalidOperationException();
        }

        private bool SetDigitalTagValue(string tag, object value)
        {
            foreach (DriverTagDataInfo tagInfo in TagManager_Digital)
            {
                if (tagInfo.TagName == tag)
                {
                    tagInfo.Data = value;

                    if (tagInfo.ActionFunc != null)
                    {
                        lock (serialPort)
                        {
                            tagInfo.ActionFunc();
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private bool SetAnalogTagValue(string tag, object value)
        {
            IronUtility.IronLogger.LOG.WriteLog("Console", "[Robostar_RS232C.Driver] : Write Tag : " + tag + ", Data : " + value);

            foreach (DriverTagDataInfo tagInfo in TagManager_Analog)
            {
                if (tagInfo.TagName == tag)
                {
                    tagInfo.Data = value;
                    if (tagInfo.ActionFunc != null)
                    {
                        lock (serialPort)
                        {
                            tagInfo.ActionFunc();
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private string PortReadLine()
        {
            if (!simulationMode)
                return serialPort.ReadLine();
            return "";
        }

        private void PortWriteLine(string content)
        {
            if(!simulationMode)
                serialPort.WriteLine(content);
        }

        private void execute()
        {

            StatusChanged?.BeginInvoke(this, DriverStatus.Run, DateTime.Now, null, null);

            try
            {

                while (!loopStopEvent.WaitOne(loopInterval))
                {

                    switch (state)
                    {
                        case DriverStep.Init:

                            if (RequestConfig != null)
                            {
                                serialConfig = (IronInterface.Configuration.SerialConfig)RequestConfig(this);

                                if (serialConfig != null)
                                {
                                    state = DriverStep.Start;
                                }
                                else
                                {
                                    Thread.Sleep(3000);
                                }
                            }
                            else
                            {
                                Thread.Sleep(3000);
                            }
                            break;
                        case DriverStep.Start:

                            Parity parity = (Parity)serialConfig.parity;
                            StopBits stopBits = (StopBits)serialConfig.stopBits;

                            serialPort = new SerialPort(serialConfig.portName, serialConfig.baudRate, parity, serialConfig.dataBits, stopBits);

                            if (serialPort != null)
                            {
                                serialPort.Open();

                                if (serialPort.IsOpen)
                                {
                                    serialPort.NewLine = "\r\n";
                                    state = DriverStep.Idle;
                                }
                                else
                                {
                                    Thread.Sleep(3000);
                                }
                            }
                            else
                            {
                                Thread.Sleep(3000);
                            }
                            break;
                        case DriverStep.Idle:

                            Thread.Sleep(1);

                            state = DriverStep.ReadSend;

                            break;
                        case DriverStep.ReadSend:

                            foreach (DriverTagDataInfo taginfo in TagManager_Update)
                            {
                                lock (serialPort)
                                {
                                    taginfo.ActionFunc();
                                }
                            }

                            lastReadTime = DateTime.Now;
                            
                            List<byte> digitalChange = new List<byte>();

                            foreach (DriverTagDataInfo tagInfo in TagManager_Digital)
                            {
                                digitalChange.Add((byte)(tagInfo.Data ?? 0));
                            }

                            List<byte> analogChange = new List<byte>();
                            foreach (DriverTagDataInfo tagInfo in TagManager_Analog)
                            {
                                analogChange.Add((byte)(tagInfo.Data ?? 0));
                            }

                            if (digitalChange.Count > 0)
                            {
                                ReadBitsChanged?.BeginInvoke(this, driverID, digitalChange.ToArray(), typeof(bool), lastReadTime, null, null);
                            }

                            if (analogChange.Count > 0)
                            {
                                ReadBytesChanged?.BeginInvoke(this, driverID, analogChange.ToArray(), typeof(byte), lastReadTime, null, null);
                            }


                            state = DriverStep.Idle;
                            break;
                        case DriverStep.WriteSend:


                            break;
                        case DriverStep.ReceiveBlock:

                            break;
                        case DriverStep.Confirm:

                            break;
                        case DriverStep.Reset:

                            break;
                        case DriverStep.Error:

                            break;
                        default:
                            break;
                    }


                }
            }
            catch (Exception except)
            {
                IronUtility.IronLogger.LOG.WriteLog("Exception", except.ToString());

                StatusChanged?.BeginInvoke(this, DriverStatus.Error, DateTime.Now, null, null);

            }
            finally
            {
                if (serialPort != null)
                {
                    if (serialPort.IsOpen)
                    {
                        serialPort.Close();
                    }
                }
                state = DriverStep.Init;

                loopThread = new Thread(execute);
                loopThread.Start();
            }
            StatusChanged?.BeginInvoke(this, DriverStatus.Stop, DateTime.Now, null, null);


        }
        #endregion


        #region Unit Command
        private bool INITIAL()
        {
            string command = "INITIAL";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool SERVOON()
        {
            string command = "SERVOON";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool SERVOOF()
        {
            string command = "SERVOOF";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool VACUMON()
        {
            string command = "VACUMON";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool VACUMOF()
        {
            string command = "VACUMOF";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool R_RHOME()
        {
            string command = "R_RHOME";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool FGREADY()
        {
            string command = "FGREADY\0"
                + GetAnalogTagValue("Station_No1")
                + "," + GetAnalogTagValue("Slot_No1")
                + "," + GetAnalogTagValue("Arm_No1")
                + "," + GetAnalogTagValue("Size_No1")
                + "," + GetAnalogTagValue("Taget_No1");

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool FPREADY()
        {
            string command = "FPREADY\0"
                + GetAnalogTagValue("Station_No1")
                + "," + GetAnalogTagValue("Slot_No1")
                + "," + GetAnalogTagValue("Arm_No1")
                + "," + GetAnalogTagValue("Size_No1")
                + "," + GetAnalogTagValue("Taget_No1");


            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETFROM()
        {
            string command = "GETFROM\0"
                + GetAnalogTagValue("Station_No1")
                + "," + GetAnalogTagValue("Slot_No1")
                + "," + GetAnalogTagValue("Arm_No1")
                + "," + GetAnalogTagValue("Size_No1")
                + "," + GetAnalogTagValue("Taget_No1");


            PortWriteLine(command);

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool PUTINTO()
        {
            string command = "PUTINTO\0"
                + GetAnalogTagValue("Station_No1")
                + "," + GetAnalogTagValue("Slot_No1")
                + "," + GetAnalogTagValue("Arm_No1")
                + "," + GetAnalogTagValue("Size_No1")
                + "," + GetAnalogTagValue("Taget_No1");


            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETRTAL()
        {
            string command = "GETRTAL\0"
                + GetAnalogTagValue("Station_No1")
                + "," + GetAnalogTagValue("Slot_No1")
                + "," + GetAnalogTagValue("Arm_No1")
                + "," + GetAnalogTagValue("Size_No1")
                + "," + GetAnalogTagValue("Taget_No1");


            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool R_PAUSE()
        {
            string command = "R_PAUSE";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool R_RESUM()
        {
            string command = "R_RESUM";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool R_RESET()
        {
            string command = "R_RESET";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool R_RSTOP()
        {
            string command = "R_RSTOP";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool R_STATE()
        {
            string command = "R_STATE";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                string responData = PortReadLine();

                if (responData.Length == 7)
                {

                    SetDigitalTagValue("Robot_Error", responData[0] == '0' ? 0 : 1);
                    SetDigitalTagValue("Servo_ON", responData[1] == '0' ? 0 : 1);
                    SetDigitalTagValue("Remote_Mode", responData[2] == '0' ? 0 : 1);
                    SetDigitalTagValue("Master_Job_Running", responData[3] == '0' ? 0 : 1);
                    SetDigitalTagValue("Arm_Folding", responData[4] == '0' ? 0 : 1);
                    SetDigitalTagValue("Vacuum_ON", responData[5] == '0' ? 0 : 1);
                    SetDigitalTagValue("Solenoid_Valve", responData[6] == '0' ? 0 : 1);

                    //responeData *S1S2S3S4S5S6
                    //                     0   -   1
                    //S1 : Robot Error = Error - Normal
                    //S2 : Servo ON = OFF - ON
                    //S3 : Remote Mode = Servo OFF - Servo ON
                    //S4 : Master Job running = Manual - Auto
                    //S5 : Arm folding = Forward - Backward
                    //S6 : Vacuum ON = OFF - ON
                    //S7 : Solenoid valve = Open - Close
                }
                else
                {
                    IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : responData.Length != 7 ==> Command : " + command + ", Response : " + response);
                }

                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        public object ReadAny(string tag)
        {
            throw new NotImplementedException();
        }

        public object ReadAny(string[] tags)
        {
            throw new NotImplementedException();
        }

        public List<DriverTagDataInfo> GetObjectTagInfo()
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

        public int WriteData(string tag, object value)
        {
            throw new NotImplementedException();
        }

        public int[] WriteData(string[] tags, object[] values)
        {
            throw new NotImplementedException();
        }

        /* 추후 작업 예정
        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis">
        /// 1 : 회전축 Degree (°)
        /// 2 : 승강축 millimeter (mm)
        /// 3 : LOWER 핸드축 millimeter (mm)
        /// 4 : UPPER 핸드축 millimeter (mm)
        /// 5 : 주행축 millimeter (mm)
        /// 6 : FLIP 축 Degree (°)</param>
        /// <param name="distance">
        /// -Limit range ~ +Limit range</param>
        /// <param name="speed">
        /// Cx7 Contorller 1~100
        /// T1 1~1000
        /// </param>
        /// <returns></returns>
        private bool MOVEABS(string axis, string distance, string speed)
        {
            string command = "MOVEABS\0" + axis + "," + distance + "," + speed;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis">
        /// 1 : 회전축 Degree (°)
        /// 2 : 승강축 millimeter (mm)
        /// 3 : LOWER 핸드축 millimeter (mm)
        /// 4 : UPPER 핸드축 millimeter (mm)
        /// 5 : 주행축 millimeter (mm)
        /// 6 : FLIP 축 Degree (°)</param>
        /// <param name="distance">
        /// -Limit range ~ +Limit range</param>
        /// <param name="speed">
        /// Cx7 Contorller 1~100
        /// T1 1~1000
        /// </param>
        /// <returns></returns>
        private bool MOVEREL(string axis, string distance, string speed)
        {
            string command = "MOVEREL\0" + axis + "," + distance + "," + speed;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETTPOS(string stage, string slot, string arm, string size, string multi)
        {
            string command = "GETTPOS\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool PUTTPOS(string stage, string slot, string arm, string size, string multi)
        {
            string command = "PUTTPOS\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GEXTEND(string stage, string slot, string arm, string size, string multi)
        {
            string command = "GEXTEND\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool PEXTEND(string stage, string slot, string arm, string size, string multi)
        {
            string command = "PEXTEND\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETEXUP(string stage, string slot, string arm, string size, string multi)
        {
            string command = "GETEXUP\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool PUTEXDN(string stage, string slot, string arm, string size, string multi)
        {
            string command = "PUTEXDN\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GRETRAC(string stage, string slot, string arm, string size, string multi)
        {
            string command = "GRETRAC\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool PRETRAC(string stage, string slot, string arm, string size, string multi)
        {
            string command = "PRETRAC\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETAFLD(string stage, string slot, string arm, string size, string multi)
        {
            string command = "GETAFLD\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool PUTAFLD(string stage, string slot, string arm, string size, string multi)
        {
            string command = "PUTAFLD\0" + stage + "," + slot + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool TRANSGP(string stage1, string slot1, string arm1, string size1, string multi1, 
            string stage2, string slot2, string arm2, string size2, string multi2)
        {
            string command = "TRANSGP\0" + stage1 + "," + slot1 + "," + arm1 + "," + size1 + "," + multi1
                + "," + stage2 + "," + slot2 + "," + arm2 + "," + size2 + "," + multi2;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool TRANSPG(string stage1, string slot1, string arm1, string size1, string multi1,
            string stage2, string slot2, string arm2, string size2, string multi2)
        {
            string command = "TRANSPG\0" + stage1 + "," + slot1 + "," + arm1 + "," + size1 + "," + multi1
                + "," + stage2 + "," + slot2 + "," + arm2 + "," + size2 + "," + multi2;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool TRANSGF(string stage1, string slot1, string arm1, string size1, string multi1,
            string stage2, string slot2, string arm2, string size2, string multi2)
        {
            string command = "TRANSGF\0" + stage1 + "," + slot1 + "," + arm1 + "," + size1 + "," + multi1
                + "," + stage2 + "," + slot2 + "," + arm2 + "," + size2 + "," + multi2;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool TRANSPF(string stage1, string slot1, string arm1, string size1, string multi1,
            string stage2, string slot2, string arm2, string size2, string multi2)
        {
            string command = "TRANSPF\0" + stage1 + "," + slot1 + "," + arm1 + "," + size1 + "," + multi1
                + "," + stage2 + "," + slot2 + "," + arm2 + "," + size2 + "," + multi2;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="arm">
        /// 1 : 상 핸드
        /// 2 : 하 핸드
        /// </param>
        /// <param name="size"></param>
        /// <param name="multi"></param>
        /// <returns></returns>
        private bool SETLPOS(string stage, string arm, string size, string multi)
        {
            string command = "SETLPOS\0" + stage + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool SETFOLD(string distance)
        {
            string command = "SETFOLD\0" + distance;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool SETLPAR(string stage, string arm, string size, string multi, 
            string pitch, string upSt, string downSt, string maxslot)
        {
            string command = "SETLPOS\0" + stage + "," + arm + "," + size + "," + multi
                + "," + pitch + "," + upSt + "," + downSt + "," + maxslot;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool SETTSPD(string speedmode, string speedset)
        {
            string command = "SETLPOS\0" + speedmode + "," + speedset;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool SETCORD(string stage, string arm, string size, string multi, 
            string value1, string value2, string value3, string value4, string value5, string value6)
        {
            string command = "SETCORD\0" + stage + "," + arm + "," + size + "," + multi
                + "," + value1 + "," + value2 + "," + value3 + "," + value4 + "," + value5 + "," + value6;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETCPOS()
        {
            string command = "GETCPOS";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                string responData = PortReadLine();

                //responeData *value1,value2,value3,value4,value5,value6
                //                     0   -   1
                //value1 : 회전축 Motor
                //value2 : 승강축 Motor
                //value3 : 하핸드축 Motor
                //value4 : 상핸드축 Motor
                //value5 : 주행축 Motor
                //value6 : 0


                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="arm">
        /// 1 : 상 핸드
        /// 2 : 하 핸드
        /// </param>
        /// <param name="size"></param>
        /// <param name="multi"></param>
        /// <returns></returns>
        private bool GETLPOS(string stage, string arm, string size, string multi)
        {
            string command = "GETLPOS\0" + stage + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                string responData = PortReadLine();

                //responeData *value1,value2,value3,value4,value5,value6
                //                     0   -   1
                //value1 : 회전축 Motor
                //value2 : 승강축 Motor
                //value3 : 하핸드축 Motor
                //value4 : 상핸드축 Motor
                //value5 : 주행축 Motor
                //value6 : 0


                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETFOLD()
        {
            string command = "GETFOLD";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                string responData = PortReadLine();

                //responeData *value1
                //value1 : Arm Fold 값 로보트 기구 메뉴얼 LAY OUT 참조
 
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETLPAR(string stage, string arm, string size, string multi)
        {
            string command = "GETLPAR\0" + stage + "," + arm + "," + size + "," + multi;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                string responData = PortReadLine();

                //responeData *value1,value2,value3,value4
                //                     0   -   1
                //value1 : Pitch, cassette Pitch
                //value2 : Up Pitch, 상부 Offset [G3 / P2]
                //value3 : Down Pitch, 하부 Offser [G2 / P3]
                //value4 : Maxslot, Cassette 최대 단수
                
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETTSPD(string speedmode)
        {
            string command = "GETTSPD\0" + speedmode;

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                string responData = PortReadLine();

                //responeData *value
                //
                //value : T1, 1~1000

                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETALRM()
        {
            string command = "GETALRM";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                string responData = PortReadLine();

                //responeData *Alarm Code
                //
                //Alarm Code : Sys Alarm Code / command Alarm Code
                
                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }

        private bool GETATXT()
        {
            string command = "GETATXT";

            PortWriteLine(command);

            string response = PortReadLine();

            if (response == "-" + command)
            {
                string responData = PortReadLine();

                //responeData *Alarm Text
                //
                //Alarm Text : 알람 내용

                return true;
            }

            IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Command Fail ==> Command : " + command);

            if (response.Substring(0, 4) == "*ERR")
            {
                IronUtility.IronLogger.LOG.WriteLog("Error", "[Robostar_RS232C.Driver] : Error occur ==> Error Code : " + response);
            }

            return false;
        }
        */
        #endregion





    }
}
