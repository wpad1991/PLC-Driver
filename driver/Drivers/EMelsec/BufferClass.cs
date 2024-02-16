using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronUtility;

namespace EMelsec
{
    class BufferClass<T>
    {
        bool initialized = false;
        public int addrCount = 0;
        public int[] addrArray { get; set; }
        public int[] addrSize { get; set; }
        public int[] byteSize { get; set; }
        public string[] deviceCode { get; set; }
        public QnA_3E CONFIG = new QnA_3E();

        private List<T[]> buffer = new List<T[]>();
        private DateTime[] readDate;
        private int maxArrayRange;
        private int currentRangeIndex = 0;

        
        public T[] BufferData
        {
            get
            {
                if (buffer != null && buffer.Count > 0)
                {

                    return buffer[0];
                }

                return null;
            }

        }

        public BufferClass()
        {

        }

        public bool Initialize(QnA_3E config, int maxArray)
        {

            try
            {

                int count = config.DeviceCode.Length;

                int totalSize = GetBufferSize(config);
                if (totalSize == 0)
                {
                    return false;
                }

                if (config.DeviceCode.Length == 0)
                {
                    return false;
                }

                if (maxArray == 0)
                {
                    return false;
                }

                maxArrayRange = maxArray;
                addrCount = count;
                addrArray = new int[count];
                addrSize = new int[count];
                byteSize = new int[count];
                deviceCode = new string[count];

                readDate = new DateTime[maxArray];

                for (int i = 0; i < maxArray; i++)
                {
                    buffer.Add(new T[totalSize]);
                }
                for (int i = 0; i < config.DeviceCode.Length; i++)
                {
                    addrSize[i] = config.Size[i];
                    deviceCode[i] = config.DeviceCode[i];
                    addrArray[i] = config.Address[i];
                    if (Utility.CheckIsBit(config.DeviceCode[i]))
                    {
                        byteSize[i] = config.Size[i];
                    }
                    else
                    {
                        byteSize[i] = config.Size[i] * 2;
                    }
                }
            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : " + except.ToString());
                return false;
            }

            initialized = true;

            return initialized;
        }

        public bool Initialize(QnA_3E _config, int maxArray, bool isBit)
        {

            try
            {


                int count = 0;

                int totalSize = GetBufferSize(_config, isBit);

                if (totalSize == 0)
                {
                    return false;
                }

                if (_config.DeviceCode.Length == 0)
                {
                    return false;
                }

                if (maxArray == 0)
                {
                    return false;
                }
                if (isBit)
                {
                    foreach (string s in _config.DeviceCode)
                    {
                        if (Utility.CheckIsBit(s) == isBit)
                        {
                            count++;
                        }
                    }
                }
                else
                {
                    foreach (string s in _config.DeviceCode)
                    {
                        if (Utility.CheckIsBit(s) == isBit)
                        {
                            count++;
                        }
                    }
                }

                if (count == 0)
                {
                    return false;
                }

                maxArrayRange = maxArray;
                addrCount = count;
                addrArray = new int[count];
                addrSize = new int[count];
                byteSize = new int[count];
                deviceCode = new string[count];

                readDate = new DateTime[maxArray];

                for (int i = 0; i < maxArray; i++)
                {
                    buffer.Add(new T[totalSize]);
                }

                int count_buf = 0;

                for (int i = 0; i < _config.DeviceCode.Length; i++)
                {

                    if (Utility.CheckIsBit(_config.DeviceCode[i]) == isBit)
                    {
                        if (_config.Size.Length > i)
                        {
                            if (count > count_buf)
                            {
                                addrSize[count_buf] = _config.Size[i];
                                deviceCode[count_buf] = _config.DeviceCode[i];

                                if (isBit)
                                {
                                    byteSize[count_buf] = _config.Size[i];
                                }
                                else
                                {
                                    byteSize[count_buf] = _config.Size[i] * 2;
                                }

                                addrArray[count_buf] = _config.Address[i];

                                count_buf++;
                            }
                            else
                            {
                                IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : " + "count_buf Error!!");
                            }
                        }
                        else
                        {
                            IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : " + "Config 항목의 갯수가 서로 맞지 않습니다.");
                        }
                    }
                }

                CONFIG.Address = addrArray;
                CONFIG.DeviceCode = deviceCode;
                CONFIG.Size = addrSize;


            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : " + except.ToString());
                return false;
            }

            initialized = true;

            return initialized;
        }

        private int GetBufferSize(QnA_3E config)
        {
            int size = 0;

            for (int i = 0; i < config.DeviceCode.Length; i++)
            {
                if (config.Size.Length > i)
                {
                    if (Utility.CheckIsBit(config.DeviceCode[i]))
                    {
                        size += config.Size[i];
                    }
                    else
                    {
                        size += config.Size[i] * 2;
                    }
                }
                else
                {
                    IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : " + "Config 항목의 갯수가 서로 맞지 않습니다.");
                }
            }
            return size;
        }


        private int GetBufferSize(QnA_3E config, bool isBit)
        {
            int size = 0;

            for (int i = 0; i < config.DeviceCode.Length; i++)
            {
                if (Utility.CheckIsBit(config.DeviceCode[i]) == isBit)
                {
                    if (config.Size.Length > i)
                    {
                        if (isBit)
                        {
                            size += config.Size[i];
                        }
                        else
                        {
                            size += config.Size[i] * 2;
                        }
                    }
                    else
                    {
                        IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : " + "Config 항목의 갯수가 서로 맞지 않습니다.");
                    }
                }
            }
            return size;
        }

        public bool SetBufferData(int addr, T[] data)
        {
            if (buffer == null)
            {
                IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : buffer is null");
                return false;
            }

            try
            {
                if (data.Length <= 0)
                {
                    IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : data.Length : " + data.Length);
                    return false;
                }


                if (addr == 0)
                {
                    if (data[0].GetType() == typeof(Int16))
                    {
                        Buffer.BlockCopy(buffer[currentRangeIndex], 0, buffer[NextArrayIndex()], 0, buffer[currentRangeIndex].Length * sizeof(short));
                    }
                    else if (data[0].GetType() == typeof(byte))
                    {
                        Buffer.BlockCopy(buffer[currentRangeIndex], 0, buffer[NextArrayIndex()], 0, buffer[currentRangeIndex].Length * sizeof(byte));
                    }
                }
                if (data[0].GetType() == typeof(Int16))
                {
                    Buffer.BlockCopy(data, 0, buffer[currentRangeIndex], addr * sizeof(short), data.Length * sizeof(short));
                }
                else if (data[0].GetType() == typeof(byte))
                {
                    Buffer.BlockCopy(data, 0, buffer[currentRangeIndex], addr * sizeof(byte), data.Length * sizeof(byte));
                }
                readDate[currentRangeIndex] = DateTime.Now;

            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : " + except.ToString());
                return false;
            }
            return true;
        }

        public T[] GetBufferData()
        {

            if (buffer == null)
            {
                IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : buffer is null");
                return null;
            }


            return buffer[currentRangeIndex];
        }

        public object[] GetAllBufferData()
        {

            object[] obj = new object[2];

            if (buffer == null)
            {
                IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : buffer is null");
                return null;
            }


            List<T[]> list = new List<T[]>();
            List<DateTime> datelist = new List<DateTime>();


            for (int i = 0; i < maxArrayRange; i++)
            {
                list.Add(buffer[PreviousArrayIndex(i)]);
                datelist.Add(readDate[PreviousArrayIndex(i)]);
            }

            obj[0] = (object)datelist.ToArray();
            obj[1] = (object)list;

            return obj;

        }

        public int GatMaxArrayRnage()
        {
            return maxArrayRange;
        }

        public int GetByteBufferSize(int index)
        {

            int count = 0;
            for (int i = 0; i < index; i++)
            {
                if (byteSize.Length > i)
                {
                    count += byteSize[i];
                }
                else
                {
                    IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : GetByteBufferSize Not Match i != byeSize.length");
                }
            }

            return count;
        }


        private int NextArrayIndex()
        {
            currentRangeIndex++;


            if (currentRangeIndex == maxArrayRange)
            {
                currentRangeIndex = 0;
            }

            return currentRangeIndex;

        }


        private int PreviousArrayIndex(int pre)
        {
            int index = currentRangeIndex;

            for (int i = 0; i < pre; i++)
            {
                index--;

                if (index < 0)
                {
                    index = maxArrayRange - 1;
                }

            }

            return index;
        }
        //잘못된 부분
        public int GetBufferDivision(int MSS)
        {
            if (buffer == null)
            {
                return -1;
            }

            int result = 0;

            foreach (int s in addrSize)
            {
                if (s == 0)
                {
                    return -1;
                }
                result += (s / MSS) + 1;
            }

            return result;
        }

        public int GetBufferOrder(int divisionCount, int MSS)
        {

            int count = 0;
            int result = 0;

            foreach (int s in addrSize)
            {
                if (s == 0)
                {
                    return -1;
                }

                count += (s / MSS) + 1;

                if (divisionCount < count)
                {
                    return result;
                }

                result++;
            }

            return -1;
        }

        public int GetBufferStartAddr(int arrayNum, int curCount, int MSS)
        {
            try
            {
                int count = 0;
                int lastlegnth = 0;
                int result = 0;

                for (int i = 0; i < arrayNum; i++)
                {
                    lastlegnth += (addrSize[i] / MSS) + 1;
                }
                count = curCount - lastlegnth;

                result = addrArray[arrayNum] + (MSS * count);

                return result;
            }
            catch (Exception except)
            {
                IronLog.LogManager.Manager.WriteLog("Exception_Driver","[EMelsec : BufferClass] : " + except.ToString());
                return -1;
            }
        }

    }
}
