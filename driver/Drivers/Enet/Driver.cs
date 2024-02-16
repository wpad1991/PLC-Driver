using IronInterface;
using IronInterface.Configuration;
using IronInterface.Driver;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Enet
{
    enum LoopStep
    {
        Stop,
        Init,
        Run
    }

    public class Driver : IMemoryDriver
    {
        // DriverBase Event
        public event DriverStatusChangedEventHandler StatusChanged;
        public event RequestConfigEventHandler RequestConfig;
        public event RequestDataEventHandler RequestData;

        // IMemory Driver Event
        public event ReadDataChangdEventHandler ReadBitsChanged;
        public event ReadDataChangdEventHandler ReadBytesChanged;

        // Thread
        private bool bRun = false;
        private Thread threadLoop = null;
        private LoopStep loopStep = LoopStep.Stop;

        // Driver
        string driverID = "";
        private int readDataUpdateTime = 500;
        private DriverStatus currentStatus = DriverStatus.None;
        private DateTime lastReadTime = DateTime.Now;


        EnetClient.Enet enet = new EnetClient.Enet();


        public string DriverID { get => driverID; set => driverID = value; }

        public DriverType GetDriverType => DriverType.LS_FEnet;

        public bool Started { get => bRun; }

        public bool SimulationMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int ReadDataUpdateTime { get => readDataUpdateTime; set => readDataUpdateTime = value; }

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

        public List<byte[]> Bytes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public DriverStatus GetDriverStatus() { return CurrentStatus; }

        public DateTime GetLastReadTime() { return lastReadTime; }

        public bool RestartDriver()
        {
            StartDriver();
            StopDriver();

            return true;
        }

        int _M_Offset = 0;

        public bool StartDriver()
        {
            bRun = true;

            LS_FEhternetInformation config = null;

            if (RequestConfig != null)
                config = (LS_FEhternetInformation)RequestConfig(this);

            if (config != null)
            {
                enet.SET_IP = config.IPAddr;
                enet.SET_PORT = config.Port;

                int index = 0;
                int frameNum = 0;

                foreach (string c in config.Channel)
                {
                    if (c == "D")
                        enet.SET_NetBlock(c, config.Address[index], config.Address[index], config.Size[index] * 2, config.ScanTime, frameNum++);
                    else if (c == "M")
                    {
                        int size = config.Size[index];

                        if (size == 0)
                            continue;

                        _M_Offset = 0;
                        if (size % 8 != 0)
                        {
                            _M_Offset = 8 - (size % 8);
                            size += _M_Offset;
                        }

                        size /= 8;

                        enet.SET_NetBlock(c, config.Address[index], config.Address[index], size, config.ScanTime, frameNum++);
                    }
                    else
                        enet.SET_NetBlock(c, config.Address[index], config.Address[index], config.Size[index], config.ScanTime, frameNum++);

                    index++;
                }
            }

            enet.NetRun();
            
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
            enet.NetEnd();

            // next step
            loopStep = LoopStep.Stop;
            CurrentStatus = DriverStatus.Stop;

            bRun = false;
            threadLoop.Join();
            threadLoop = null;

            return Started;
        }


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
                        if (ReadBitsChanged != null)
                        {
                            foreach (EnetClient.Enet.NET_BLOCK block in enet.Blocks)
                            {
                                if (block.DATA_COUNT <= 0)
                                {
                                    continue;
                                }
                                
                                IntPtr addr = enet.ReadMemory(block.DEVICE, block.START_ADD);

                                byte[] managedArray = new byte[block.DATA_COUNT];

                                System.Runtime.InteropServices.Marshal.Copy(addr, managedArray, 0, block.DATA_COUNT);

                                Type type = null;
                                if (block.DEVICE == "P")
                                    type = typeof(bool);
                                else if (block.DEVICE == "M")
                                {
                                    type = typeof(byte);
                                    
                                    byte [] tempArray = new byte[block.DATA_COUNT * 8 - _M_Offset];

                                    for (int i = 0; i < block.DATA_COUNT; i++)
                                    {
                                        for (int idx = 0; idx < 8; idx++)
                                        {
                                            int temp = 0x01;
                                            temp = temp << idx;

                                            int arrIndex = (i * 8) + idx;

                                            if (arrIndex < tempArray.Length)
                                            {
                                                if ((byte)(managedArray[i] & temp) != 0)
                                                    tempArray[(i * 8) + idx] = 1;
                                                else
                                                    tempArray[(i * 8) + idx] = 0;
                                            }
                                        }
                                    }

                                    managedArray = tempArray;
                                }
                                else if (block.DEVICE == "L")
                                    type = typeof(bool);
                                else if (block.DEVICE == "F")
                                    type = typeof(bool);
                                else if (block.DEVICE == "K")
                                    type = typeof(bool);
                                else if (block.DEVICE == "C")
                                    type = typeof(bool);
                                else if (block.DEVICE == "D")
                                    type = typeof(ushort);
                                else if (block.DEVICE == "T")
                                    type = typeof(bool);
                                else if (block.DEVICE == "N")
                                    type = typeof(bool);
                                else if (block.DEVICE == "R")
                                    type = typeof(bool);

                                IronUtility.API.OutputDebugViewString("[ENet : Driver] : block.Device/length >>>>> " + block.DEVICE + "/" + block.DATA_COUNT.ToString());

                                ReadBitsChanged(this, block.DEVICE, managedArray, type, DateTime.Now);
                            }
                        }
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
                LS_FEhternetInformation config = null;

                if (RequestConfig != null)
                    config = (LS_FEhternetInformation)RequestConfig(this);

                if (config != null)
                {
                    
                    bResult = true;
                }
            }
            catch (Exception exception)
            {
                //LastException = exception;
                bResult = false;
            }

            return bResult;
        }

        public bool SetConfig(DriverInformation driverInfo)
        {
            return true;
        }

        public void WriteBytes(int channelIndex, int addr, byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
