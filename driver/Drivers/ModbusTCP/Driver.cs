using DriverInterface;
using DriverInterface.Configuration;
using DriverInterface.Driver;
using DriverLog;
using DriverUtility;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ModbusTCP
{
    enum LoopStep
    {
        Stop,
        Init,
        Run
    }

    class ModbusTag
    {
        public string Name;
        public List<string> GroupName;
        public ushort Addr;
        public object Value;
        public string Type;

        public string Format;
    }

    class MemoryBlock
    {
        public string format;
        public List<ModbusTag> tags = new List<ModbusTag>();
    }
    

    public class Driver : ITagDriver
    {
        // DriverBase Event
        public event DriverStatusChangedEventHandler StatusChanged;
        public event RequestConfigEventHandler RequestConfig;
        public event RequestDataEventHandler RequestData;

        // TagDriver Event
        public event ReadObjectChangdEventHandler ReadObjectChanged;
        public event DriverUpdateTagInfoEventHandler UpdateTagInformation;

        // MemoryDriver Event
        //public event ReadDataChangdEventHandler ReadBitsChanged;
        //public event ReadDataChangdEventHandler ReadBytesChanged;

        // Thread
        private bool bRun = false;
        private Thread threadLoop = null;
        private LoopStep loopStep = LoopStep.Stop;

        // Driver
        private string driverID = "";
        private int readDataUpdateTime = 500;
        private DriverStatus currentStatus = DriverStatus.None;
        private DateTime lastReadTime = DateTime.Now;

        // Modbus Master
        private Master master = null;
        private Exception lastException = null;
        private string ipAddr = "127.0.0.1";
        private ushort port = 502;

        // Communication status
        //private bool CommStatus = false;
        private bool socketError = false;
        public bool IsNetworkConnected
        {
            get
            {
                if (master == null)
                    return false;

                if (master.connected == false)
                    return false;

                if (master._currentSocket == null)
                    return false;
                else
                {
                    if (master._currentSocket.Connected == false)
                        return false;

                    if (master._currentSocket.Poll(1000, SelectMode.SelectRead) && (master._currentSocket.Available == 0))
                        return false;
                }

                if (socketError)
                    return false;
                else
                    return true;
            }
        }

        // <Addr, Data>
        /*
        private Dictionary<int, TagValue> tag_Coils = new Dictionary<int, TagValue>();
        private Dictionary<int, TagValue> tag_DiscreteInputs = new Dictionary<int, TagValue>();
        private Dictionary<int, TagValue> tag_HoldingRegister = new Dictionary<int, TagValue>();
        private Dictionary<int, TagValue> tag_InputRegister = new Dictionary<int, TagValue>();
        */


        /*
        private List<TagValue> tag_Coils = new List<TagValue>();
        private List<TagValue> tag_DiscreteInputs = new List<TagValue>();
        private List<TagValue> tag_HoldingRegister = new List<TagValue>();
        private List<TagValue> tag_InputRegister = new List<TagValue>();
        */

        /*
        // Value / Dic<Address, Value>
        private Dictionary<int, Int16> valueAnalog = new Dictionary<int, short>();
        private Dictionary<int, bool> valueDigital = new Dictionary<int, bool>();
        */

        private List<MemoryBlock> listMemoryBlock = new List<MemoryBlock>();

        // Write용 
        private Dictionary<string, ModbusTag> dicTag = new Dictionary<string, ModbusTag>();
        //private Dictionary<string, ushort> dicAddr = new Dictionary<string, ushort>();
        //private Dictionary<string, int> dicMemID = new Dictionary<string, int>();
        

        public string DriverID { get => driverID; set => driverID = value; }

        public DriverType GetDriverType => DriverType.ModbusTCP;

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
                    StatusChanged?.Invoke(this, currentStatus, DateTime.Now);
                }
            }
        }

        public Exception LastException
        {
            get => lastException;

            set
            {
                lastException = value;

                DriverManager.Manager.WriteLog("Exception", lastException.ToString());
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

            return Started;
        }

        public bool RestartDriver()
        {
            StopDriver();
            StartDriver();

            return true;
        }

        /*
        public byte[] ReadBits()
        {
            throw new NotImplementedException();
        }

        public byte[] ReadBytes()
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
        */

        /// <summary>
        /// TagGroup에 해당 format을 가진 Group을 찾아서 return
        /// </summary>
        /// <param name="format"></param>
        /// <returns>찾지 못할 경우 null 반환</returns>
        private MemoryBlock CheckFormat(string format)
        {
            MemoryBlock result = null;
            
            for (int i = 0; i < listMemoryBlock.Count; i++)
            {
                if (listMemoryBlock[i].format == format)
                {
                    result = listMemoryBlock[i];
                    break;
                }
            }

            return result;
        }

        private void Loop()
        {
            try
            {
                LoopStep recentStep = loopStep;

                while (bRun)
                {
                    DateTime tickStart = DateTime.Now;

                    if (recentStep != loopStep)
                    {
                        DriverManager.Manager.WriteLog("Driver", "[Modbus Driver] Current Step : " + loopStep.ToString());
                        recentStep = loopStep;
                    }
                    
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
                        // Modbus TCP Comm Connection Check
                        if (CheckCommStatus() == false)
                        {
                            DriverManager.Manager.WriteLog("Driver","[Modbus Driver] Comm Check False");
                            Thread.Sleep(1000);
                            continue;
                        }

                        // Read Tag
                        ReadSlave();
                    }

                    // Check Period
                    DateTime tickEnd = DateTime.Now;
                    TimeSpan tickProc = tickEnd - tickStart;
                    int tickGap = readDataUpdateTime - (int)tickProc.TotalMilliseconds;

                    if (tickGap > 0)
                        Thread.Sleep(tickGap);
                    
                    

                    /*
                    for (int i = 0; i < 10; i++)
                    {
                        System.Diagnostics.Debug.WriteLine("[Modbus Driver] ");
                        Thread.Sleep(1000);
                    }
                    */
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
                    if (config is ModbusTCPInformation modbusConfig)
                    {
                        DriverID = modbusConfig.DriverName;

                        readDataUpdateTime = modbusConfig.Period;
                        ipAddr = modbusConfig.IPAddr;
                        port = (ushort)modbusConfig.Port;
                        
                        listMemoryBlock.Clear();
                        dicTag.Clear();

                        for (int i = 0; i < modbusConfig.TagInfo.Count; i++)
                        {
                            // Check
                            if (!modbusConfig.TagInfo[i].ContainsKey("Id"))
                            {
                                continue;
                            }
                            if (!modbusConfig.TagInfo[i].ContainsKey("Address"))
                            {
                                continue;
                            }
                            if (!modbusConfig.TagInfo[i].ContainsKey("Format"))
                            {
                                continue;
                            }

                            string strAddr = modbusConfig.TagInfo[i]["Address"];
                            if (ushort.TryParse(strAddr, out ushort addr))
                            {
                                ModbusTag tag = new ModbusTag();

                                // ID
                                tag.Name = modbusConfig.TagInfo[i]["Id"];

                                // Addr
                                tag.Addr = addr;

                                // Value = None
                                tag.Value = null;

                                // Type
                                if (!modbusConfig.TagInfo[i].TryGetValue("SourceType", out tag.Type))
                                {
                                    tag.Type = "SHORT";
                                }

                                // Format
                                string format = modbusConfig.TagInfo[i]["Format"];
                                tag.Format = format;

                                // Group
                                string groupName = "";
                                tag.GroupName = new List<string>();

                                for (int g_index = 0; g_index < 5; g_index++)
                                {
                                    if (g_index == 0)
                                    {
                                        if (modbusConfig.TagInfo[i].TryGetValue("Group", out groupName))
                                        {
                                            if (groupName.Length > 0)
                                            {
                                                tag.GroupName.Add(groupName);
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (modbusConfig.TagInfo[i].TryGetValue("Group" + (g_index + 1), out groupName))
                                        {
                                            tag.GroupName.Add(groupName);
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }

                                // MemoryBlock 생성 (최대 4개)
                                MemoryBlock memory = CheckFormat(format);
                                if (memory == null)
                                {
                                    memory = new MemoryBlock();
                                    memory.format = format;
                                    listMemoryBlock.Add(memory);
                                }

                                memory.tags.Add(tag);


                                // WriteData()에서 사용할 Dictionary Key
                                string TagFullName = GetFullTagName(DriverID, tag.GroupName, tag.Name);
                                
                                dicTag[TagFullName] = tag;
                            }
                            
                        }

                        bResult = true;
                    }
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.WriteLine("LoopStart Exception : " + exception.ToString());
                LastException = exception;
                bResult = false;
            }
            return bResult;
        }

        private string GetFullTagName(string driverID, List<string> group, string tagName)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(driverID);
            sb.Append(".");

            for (int i = 0; i < group.Count; i++)
            {
                sb.Append(group[i]);
                sb.Append(".");
            }

            sb.Append(tagName);
            return sb.ToString();
        }
        
        private bool CheckCommStatus()
        {
            bool bResult = false;

            try
            {
                if (master == null)
                {
                    master = new Master();
                    master.OnResponseData += Modbus_OnResponseData;
                    master.OnException += Modbus_OnException;
                }

                if (IsNetworkConnected == false)
                {
                    try
                    {
                        master.connect(ipAddr, port, false);
                        socketError = false;
                    }
                    catch (Exception except)
                    {
                        DriverManager.Manager.WriteLog("Exception_Driver", except.ToString());
                        master.Dispose();
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

        #region Modbus Function

        public void ReadSlave()
        {
            try
            {
                List<ModbusTag> changeTag = new List<ModbusTag>();

                for (int i = 0; i < listMemoryBlock.Count; i++)
                {
                    ushort ID = 0;
                    
                    if (listMemoryBlock[i].format == ModbusBlock.Coils.ToString())
                        ID = 1;
                    else if (listMemoryBlock[i].format == ModbusBlock.DiscreteInputs.ToString())
                        ID = 2;
                    else if (listMemoryBlock[i].format == ModbusBlock.HoldingRegisters.ToString())
                        ID = 3;
                    else if (listMemoryBlock[i].format == ModbusBlock.InputRegisters.ToString())
                        ID = 4;

                    
                    for (int idx = 0; idx < listMemoryBlock[i].tags.Count; idx++)
                    { 
                        try
                        {

                            string type = listMemoryBlock[i].tags[idx].Type;

                            byte unit = 1;
                            ushort Length = 1;
                            ushort Addr = listMemoryBlock[i].tags[idx].Addr;
                            byte[] data = null;// new byte[Length * 2];

                            if (type == ModbusType.BOOLEAN.ToString())
                                Length = 1;
                            else if (type == ModbusType.SHORT.ToString())
                                Length = 1;
                            else if (type == ModbusType.USHORT.ToString())
                                Length = 1;
                            else if (type == ModbusType.INT.ToString())
                                Length = 2;
                            else if (type == ModbusType.UINT.ToString())
                                Length = 2;
                            else if (type == ModbusType.FLOAT.ToString())
                                Length = 2;
                            else if (type == ModbusType.DOUBLE.ToString())
                                Length = 4;

                            if (ID == 1)
                                master.ReadCoils(ID, unit, Addr, Length, ref data);
                            else if (ID == 2)
                                master.ReadDiscreteInputs(ID, unit, Addr, Length, ref data);
                            else if (ID == 3)
                                master.ReadHoldingRegister(ID, unit, Addr, Length, ref data);
                            else if (ID == 4)
                                master.ReadInputRegister(ID, unit, Addr, Length, ref data);

                            if (data == null)
                            {
                                DriverManager.Manager.WriteLog("Driver", "[ModbusTCP : Driver] read error Addr " + Addr);
                                continue;
                            }

                            object registerValue = null;
                            if (ID == 1 || ID == 2)
                            {
                                registerValue = (data[0] & 0x01) == 0x01 ? true : false;
                            }
                            if (ID == 3 || ID == 4)
                            {
                                Array.Reverse(data);

                                if (type == ModbusType.BOOLEAN.ToString())
                                    registerValue = BitConverter.ToBoolean(data, 0);
                                if (type == ModbusType.SHORT.ToString())
                                    registerValue = BitConverter.ToInt16(data, 0);
                                if (type == ModbusType.USHORT.ToString())
                                    registerValue = BitConverter.ToUInt16(data, 0);
                                if (type == ModbusType.INT.ToString())
                                    registerValue = BitConverter.ToInt32(data, 0);
                                if (type == ModbusType.UINT.ToString())
                                    registerValue = BitConverter.ToUInt32(data, 0);
                                if (type == ModbusType.FLOAT.ToString())
                                    registerValue = BitConverter.ToSingle(data, 0);
                                if (type == ModbusType.DOUBLE.ToString())
                                    registerValue = BitConverter.ToDouble(data, 0);
                            }

                            bool bChange = false;
                            if (listMemoryBlock[i].tags[idx].Value == null)
                            {
                                bChange = true;
                            }
                            else
                            {
                                object original = listMemoryBlock[i].tags[idx].Value;

                                if (original != registerValue)
                                {
                                    bChange = true;
                                }
                            }

                            if (bChange)
                            {
                                listMemoryBlock[i].tags[idx].Value = registerValue;

                                changeTag.Add(listMemoryBlock[i].tags[idx]);
                            }
                        }
                        catch (Exception exception)
                        {
                            //socketError = true;
                            DriverManager.Manager.WriteLog("Exception_Driver", "[Modbus Driver] exception -> " + exception.ToString());
                            continue;
                        }
                    }
                    
                }
                
                /////////////////////////////////////////////////
                /// 
                int arrLength = changeTag.Count;

                if (arrLength > 0)
                {
                    string[] tagName = new string[arrLength];
                    object[] newData = new object[arrLength];
                    DateTime[] dateTime = new DateTime[arrLength];

                    for (int idx = 0; idx < arrLength; idx++)
                    {
                        tagName[idx] = GetFullTagName(DriverID, changeTag[idx].GroupName, changeTag[idx].Name);
                        newData[idx] = changeTag[idx].Value;
                        dateTime[idx] = DateTime.Now;

                    }
                    
                    ReadObjectChanged?.Invoke(this, tagName, newData, dateTime);
                }
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception_Driver", except.ToString());
                LastException = except;
            }
        }
        
        private void Modbus_OnResponseData(ushort id, byte unit, byte function, byte[] data)
        {
            if (data.Length > 0)
            {
                switch (id)
                {
                    case 1: // Read coils

                        break;

                    case 2: // Read discrete inputs

                        break;

                    case 3: // Read holding register

                        break;

                    case 4: // Read input register

                        break;
                }
            }

            for (int i = 0; i < data.Length; i++)
            {
                string temp = "[ModbusTCP : Driver] : Modbus_OnResponseData data[" + i.ToString() + "] : " + data[i];
                DriverManager.Manager.WriteLog("Driver", temp);
                System.Diagnostics.Trace.WriteLine(temp);
            }
            
            /*
            if (id == 3) Console.WriteLine("ReadHoldingRegister");
            if (id == 4) Console.WriteLine("ReadInputRegister");
            Console.Write(DateTime.Now.ToString());
            Console.WriteLine("  id:{0} / unit:{1} / function:{2} / dataLength:{3}", id, unit, function, data.Length);
            for (int i = 0; i < data.Length; i++)
                Console.Write("{0} ", data[i]);
            Console.WriteLine();
            Console.WriteLine();
            */
        }

        private void Modbus_OnException(ushort id, byte unit, byte function, byte exception)
        {
            string exc = "Modbus says error: ";
            switch (exception)
            {
                case Master.excIllegalFunction: exc += "Illegal function!"; break;
                case Master.excIllegalDataAdr: exc += "Illegal data adress!"; break;
                case Master.excIllegalDataVal: exc += "Illegal data value!"; break;
                case Master.excSlaveDeviceFailure: exc += "Slave device failure!"; break;
                case Master.excAck: exc += "Acknoledge!"; break;
                case Master.excGatePathUnavailable: exc += "Gateway path unavailbale!"; break;
                case Master.excExceptionTimeout: exc += "Slave timed out!"; break;
                case Master.excExceptionConnectionLost: exc += "Connection is lost!"; break;
                case Master.excExceptionNotConnected: exc += "Not connected!"; break;
            }

            DriverManager.Manager.WriteLog("Exception_Driver","[ModbusTCP : Driver] OnException -> " + exc);
        }
        #endregion

        #region TagDriver Function
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
            //if (dicAddr.ContainsKey(tag) && dicMemID.ContainsKey(tag))
            if (dicTag.ContainsKey(tag))
            {
                if (dicTag[tag].Format == ModbusBlock.Coils.ToString())
                {
                    bool bValue;
                    if (value is bool bTemp)
                    {
                        bValue = bTemp;
                    }
                    else if (int.TryParse(value.ToString(), out int nBuff))
                    {
                        if (nBuff != 0) bValue = true;
                        else bValue = false;
                    }
                    else //Error InValid Value
                    {
                        return 1;
                    }

                    ushort ID = 5;
                    byte unit = 1;
                    ushort Addr = dicTag[tag].Addr;

                    byte[] result = null;
                    master.WriteSingleCoils(ID, unit, Addr, bValue, ref result);
                    return 0;
                }
                else if (dicTag[tag].Format == ModbusBlock.HoldingRegisters.ToString())
                {


                    ushort ID = 7;
                    byte unit = 1;
                    ushort Addr = dicTag[tag].Addr;

                    byte[] data = null;

                    if (value is bool bTemp)
                    {
                        UInt16 word = (UInt16)(bTemp ? 1 : 0);
                        data = BitConverter.GetBytes(word);
                        Array.Reverse(data);
                    }
                    else if (int.TryParse(value.ToString(), out int nBuff))
                    {
                        UInt16 word = (UInt16)nBuff;
                        data = BitConverter.GetBytes(word);
                        Array.Reverse(data);
                    }
                    else //Error InValid Value
                    {
                        return 1;
                    }

                    byte[] result = null;
                    master.WriteSingleRegister(ID, unit, Addr, data, ref result);
                    return 0;
                    // 자료형 별 추가해야함...
                }
            }
            return 2;
        }

        public int[] WriteData(string[] tags, object[] values)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
