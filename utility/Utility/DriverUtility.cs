using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using DriverInterface.Configuration;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Net.NetworkInformation;
using DriverLog;

namespace DriverUtility
{
    public class Utility
    {
        public static string ChangeE(string sVal)
        {
            if (sVal == null)
                return sVal;

            string sRet = double.Parse(sVal).ToString("E2");

            int first = sRet.IndexOf("E") + 2;
            int last = -1;

            for (int index = first; index < sRet.Length; ++index)
            {
                if (int.Parse(sRet.Substring(index, 1)) != 0)
                {
                    last = index;
                    break;
                }
                else
                {
                    last = sRet.Length - 1;
                }
            }

            return sRet.Substring(0, first) + sRet.Substring(last);
        }

        public static List<string> GetFileList(string path)
        {
            List<string> groups = new List<string>();

            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);

                if (dir.Exists == false)
                {
                    dir.Create();
                }

                FileInfo[] files = dir.GetFiles("*.xml");

                foreach (FileInfo group in files)
                {
                    groups.Add(group.Name.ToString());
                }
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
            }

            return groups;
        }

        public static bool DeleteFile(string path)
        {
            try
            {
                FileInfo file = new FileInfo(path);

                if (file.Exists)
                {
                    file.Delete();
                }
                else
                {
                    DriverManager.Manager.WriteLog("Utility", "DeleteFile Func not Exist File => " + path);
                    return false;
                }
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
            }
            return true;
        }

        public static bool RenameFile(string oldNamePath, string NewNamePath)
        {
            try
            {
                if (File.Exists(oldNamePath))
                {
                    FileInfo file = new FileInfo(oldNamePath);
                    file.MoveTo(NewNamePath);
                }
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
            }
            return false;
        }

        public static bool IsJson(string input)
        {
            try
            {
                input = input.Trim();
                return input.StartsWith("{") && input.EndsWith("}")
                       || input.StartsWith("[") && input.EndsWith("]");
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", "[Utility] : " + except.ToString());
                return false;
            }
        }

        public static string Trim(string sVal, int nSize)
        {
            if (sVal == null)
                return sVal;

            int first = sVal.IndexOf(".");

            if (nSize == 0)
                nSize = -1;

            if (first != -1 && first + nSize < sVal.Length)
            {
                return sVal.Substring(0, first + nSize + 1);
            }
            else if (first == -1 && nSize > 0)
            {
                string sRet = sVal + ".";

                for (int i = 0; i < nSize; i++)
                {
                    sRet += "0";
                }

                return sRet;
            }
            else if (nSize > 0)
            {
                string sRet = sVal;

                for (int i = 0; i < nSize - (sVal.Length - first) + 1; i++)
                {
                    sRet += "0";
                }
                return sRet;
            }

            return sVal;
        }

        public static string TrimMilliSecond(TimeSpan t)
        {
            string timeStr = t.ToString();

            int index = timeStr.IndexOf(':');
            index = timeStr.IndexOf('.', index);
            if (index > 0)
                timeStr = timeStr.Substring(0, index);

            return timeStr;
        }

        public static TimeSpan SumErrorTime(List<DateTime> d1, List<DateTime> d2)
        {
            if (d1.Count == 0 || d1.Count != d2.Count)
                return new TimeSpan();

            List<DateTime> dd1 = new List<DateTime>();
            List<DateTime> dd2 = new List<DateTime>();

            DateTime startTime = d1[0];
            DateTime endTime = d2[0];
            TimeSpan t = endTime - startTime;

            int index = 1;

            while (endTime - startTime > new TimeSpan() && d1.Count > index)
            {
                if (d1[index] < startTime && d2[index] >= startTime && d2[index] <= endTime)
                {
                    t += startTime - d1[index];
                    startTime = d1[index];
                }
                else if (d1[index] >= startTime && d1[index] <= endTime && d2[index] > endTime)
                {
                    t += d2[index] - endTime;
                    endTime = d2[index];
                }
                else if (d1[index] < startTime && d2[index] > endTime)
                {
                    t += startTime - d1[index];
                    t += d2[index] - endTime;
                    startTime = d1[index];
                    endTime = d2[index];
                }
                else
                {
                    dd1.Add(d1[index]);
                    dd2.Add(d2[index]);
                }

                ++index;
            }

            if (dd1.Count > 0)
            {
                if (d1.Count == dd1.Count + 1)
                {
                    t += SumErrorTime(dd1, dd2);
                }
                else
                {
                    dd1.Insert(0, startTime);
                    dd2.Insert(0, endTime);
                    t += SumErrorTime(dd1, dd2) - t;
                }
            }

            return t;
        }

        public static int ASCIItoBinary(char ch)
        {
            int num = 0;

            switch (ch)
            {
                case 'A':
                case 'a':
                    num = 0xA;
                    break;
                case 'B':
                case 'b':
                    num = 0xB;
                    break;
                case 'C':
                case 'c':
                    num = 0xC;
                    break;
                case 'D':
                case 'd':
                    num = 0xD;
                    break;
                case 'E':
                case 'e':
                    num = 0xE;
                    break;
                case 'F':
                case 'f':
                    num = 0xF;
                    break;
                default:
                    num = ch - '0';
                    break;
            }

            return num;
        }

        public static bool IsNum(string str)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("^(\\d*)$");

            return regex.IsMatch(str);
        }
        
        public static bool PingTest(string url, int timeout)
        {
            if (timeout == 0)
            {
                timeout = 1;
            }

            Ping ping = new Ping();
            PingReply reply = ping.Send(url, timeout);

            if (reply.Status == IPStatus.Success) //핑이 제대로 들어가고 있을 경우
            {
                return true;
            }

            return false;
        }


        #region Json & Xml Use

        public static string GetIPAddress()
        {
            try
            {
                string strHostName = "";
                strHostName = System.Net.Dns.GetHostName();
                IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);
                IPAddress[] addr = ipEntry.AddressList;
                return addr[addr.Length - 1].ToString();
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Console", except.ToString());
            }

            return "";
        }

        public static XmlNode FindXmlNodeKey(XmlNode parent, string key)
        {
            if (parent[key] == null)
            {
                foreach (XmlNode node in parent.ChildNodes)
                {
                    XmlNode _node = FindXmlNodeKey(node, key);

                    if (_node != null)
                    {
                        return _node;
                    }

                    if (node.NextSibling != null)
                    {
                        _node = FindXmlNodeKey(node.NextSibling, key);
                    }

                    if (_node != null)
                    {
                        return _node;
                    }
                }
            }
            else
            {
                return parent[key];
            }
            return null;
        }

        public static XmlNode FindXmlNodeValue(XmlNode parent, string key, string value)
        {
            if (parent[key] == null)
            {
                foreach (XmlNode node in parent.ChildNodes)
                {
                    XmlNode _node = FindXmlNodeValue(node, key, value);

                    if (_node != null)
                    {
                        return _node;
                    }
                }
            }
            else
            {
                if (parent[key].InnerText == value)
                {
                    return parent[key];
                }
                else
                {
                    foreach (XmlNode node in parent.ChildNodes)
                    {
                        XmlNode _node = FindXmlNodeValue(node, key, value);

                        if (_node != null)
                        {
                            return _node;
                        }
                    }
                }
            }

            return null;
        }

        public static void GetSameLevelBubblingDictionary(XmlNode parent, ref List<DriverInterface.DataType.SerializableDictionary<string, string>> keyValues)
        {
            if (parent != null)
            {
                DriverInterface.DataType.SerializableDictionary<string, string> dic = new DriverInterface.DataType.SerializableDictionary<string, string>();

                foreach (XmlNode node in parent.ChildNodes)
                {
                    dic[node.Name] = node.InnerText;
                }


                keyValues.Add(dic);
                GetSameLevelBubblingDictionary(parent.NextSibling, ref keyValues);

            }
        }

        public static void GetCountXmlSibling(XmlNode node, ref int count)
        {
            if (node != null)
            {
                count++;
                GetCountXmlSibling(node.NextSibling, ref count);
            }
        }

        #endregion

        public static byte[] GetObjectToByteArray(object value)
        {
            if (value.GetType() == typeof(byte))
            {
                return BitConverter.GetBytes((byte)value);
            }
            else if (value.GetType() == typeof(short))
            {
                return BitConverter.GetBytes((short)value);
            }
            else if (value.GetType() == typeof(ushort))
            {
                return BitConverter.GetBytes((ushort)value);
            }
            else if (value.GetType() == typeof(int))
            {
                return BitConverter.GetBytes((int)value);
            }
            else if (value.GetType() == typeof(uint))
            {
                return BitConverter.GetBytes((uint)value);
            }
            else if (value.GetType() == typeof(float))
            {
                return BitConverter.GetBytes((float)value);
            }
            else if (value.GetType() == typeof(double))
            {
                return BitConverter.GetBytes((double)value);
            }
            else if (value.GetType() == typeof(bool))
            {
                return BitConverter.GetBytes((bool)value);
            }
            else if (value.GetType() == typeof(long))
            {
                return BitConverter.GetBytes((long)value);
            }
            else if (value.GetType() == typeof(ulong))
            {
                return BitConverter.GetBytes((ulong)value);
            }
            else if (value.GetType() == typeof(char))
            {
                return BitConverter.GetBytes((char)value);
            }
            else if (value.GetType() == typeof(Int16))
            {
                return BitConverter.GetBytes((Int16)value);
            }
            else if (value.GetType() == typeof(UInt16))
            {
                return BitConverter.GetBytes((UInt16)value);
            }
            else if (value.GetType() == typeof(Int32))
            {
                return BitConverter.GetBytes((Int32)value);
            }
            else if (value.GetType() == typeof(UInt32))
            {
                return BitConverter.GetBytes((UInt32)value);
            }
            else if (value.GetType() == typeof(Int64))
            {
                return BitConverter.GetBytes((Int64)value);
            }
            else if (value.GetType() == typeof(UInt64))
            {
                return BitConverter.GetBytes((UInt64)value);
            }

            return null;
        }

		public static XmlDocument GetJsonToXmlString(string json)
        {
            try
            {
                if (IsJson(json.Trim().Trim(':')))
                {
                    return JsonConvert.DeserializeXmlNode(json);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", "[Utility] : " + except.ToString());
                return null;
            }
        }

        public static string StringRemoveString(string str)
        {
            return Regex.Replace(str, @"\d", string.Empty);
        }

        public static string StringRemoveNumber(string str)
        {
            return Regex.Replace(str, @"\D", "");
        }

        public static string StringRemoveCustom(string str, string remove)
        {
            return Regex.Replace(str, @remove, "");
        }

        public static byte[] InvertingUshortToByte(ushort[] obj)
        {
            byte[] x = new byte[obj.Length * 2];

            int i = 0;
            foreach (short xx in obj)
            {
                x[i++] = (byte)(xx & 0xff);
                x[i++] = (byte)(xx >> 8 & 0xff);
            }
            return x;
        }
        
        public static ushort[] InvertingByteToUshort(byte[] obj)
        {
            ushort[] rData = new ushort[obj.Length / 2];

            byte[] temp = new byte[2];

            for (int i = 0; i < obj.Length; i = i + 2)
            {
                temp[1] = obj[i + 1];
                temp[0] = obj[i];

                rData[i / 2] = (ushort)BitConverter.ToInt16(temp, 0);
            }
            return rData;
        }

        public static List<string> TagChangeList(List<string> list, string oldName, string newName)
        {
            List<string> result = new List<string>();

            foreach (string s in list)
            {
                result.Add(s.Replace(oldName, newName));
            }

            return result;
        }

        public static int FindArrayIndexItem(string[] array, string str)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == str)
                {
                    return i;
                }
            }
            return -1;

        }

        public static int FindArrayIndexItem(List<object> array, object obj)
        {
            for (int i = 0; i < array.Count; i++)
            {
                if (object.ReferenceEquals(obj, array[i]))
                {
                    return i;
                }
            }
            return -1;

        }

        public static int FindArrayIndexItem(object[] array, object obj)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (object.ReferenceEquals(obj, array[i]))
                {
                    return i;
                }
            }
            return -1;

        }

        public static bool CheckOtherLetter(string str)
        {

            char[] charList = str.ToCharArray();

            foreach (char c in charList)
            {
                if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.OtherLetter)

                {
                    return true;
                }
            }

            return false;
        }

        public static bool CheckOtherLetter(byte[] array)
        {
            foreach (byte b in array)
            {
                if (b > 127)
                {
                    return true;
                }
            }
            return false;
        }

        public static byte[] RemoveByteArrayNumber(byte[] array, int rem)
        {
            List<byte> result = new List<byte>();

            foreach (byte b in array)
            {
                if (b != rem)
                {
                    result.Add(b);
                }
            }

            return result.ToArray();
        }

        public static string ConvertAnalogToBitString(int value, int length)
        {

            string base2 = Convert.ToString(value, 2);

            int len = base2.Length;
            if (len < length)
            {
                base2 = base2.PadLeft(length, '0');

            }
            return base2;
        }

        public static int GetAnalogInBitValue(int value, int length, int count)
        {
            if (length == count)
            {
                throw new Exception("GetAnalogInBitValue length and count Value equal");
            }

            string base2 = Convert.ToString(value, 2);

            int len = base2.Length;
            if (len < length)
            {
                base2 = base2.PadLeft(length, '0');

            }

            return base2[base2.Length - count - 1] == '0' ? 0 : 1;
        }

        public static List<int> FindTrueValueIndex(int value)
        {
            List<int> indexList = new List<int>();

            string base2 = Convert.ToString(value, 2);

            for (int i = 0; i < base2.Length; i++)
            {
                if (base2[base2.Length - i - 1] == '1')
                {
                    indexList.Add(i);
                }
            }

            return indexList;
        }
        
        public static System.Type GetTypeOfString(string str)
        {
            if (str.ToUpper() == "BOOLEAN")
            {
                return typeof(bool);
            }
            else if (str.ToUpper() == "BYTE")
            {
                return typeof(byte);
            }
            else if (str.ToUpper() == "SHORT")
            {
                return typeof(short);
            }
            else if (str.ToUpper() == "USHORT")
            {
                return typeof(ushort);
            }
            else if (str.ToUpper() == "INT")
            {
                return typeof(int);
            }
            else if (str.ToUpper() == "UINT")
            {
                return typeof(uint);
            }
            else if (str.ToUpper() == "FLOAT")
            {
                return typeof(float);
            }
            else if (str.ToUpper() == "DOUBLE")
            {
                return typeof(double);
            }
            else if (str.ToUpper() == "STRING")
            {
                return typeof(string);
            }

            return null;
        }

        public static int GetChannelDataSize(DriverInterface.Driver.DriverType driverType, string channel)
        {
            switch (driverType)
            {
                case DriverInterface.Driver.DriverType.MELSEC_Ethernet:
                    return CheckIsBit(driverType, channel) ? 1 : 2;
                default:
                    break;
            }
            return 1;
        }

        public static bool CheckIsBit(DriverInterface.Driver.DriverType driverType, string channel)
        {

            string str = channel.Replace("*", "");

            switch (driverType)
            {
                case DriverInterface.Driver.DriverType.MELSEC_Ethernet:
                    switch (str)
                    {
                        case "X":
                            return true;
                        case "Y":
                            return true;
                        case "M":
                            return true;
                        case "L":
                            return true;
                        case "F":
                            return true;
                        case "V":
                            return true;
                        case "B":
                            return true;
                        case "D":
                            return false;
                        case "W":
                            return false;
                        case "SM":
                            return true;
                        case "SD":
                            return false;
                        case "TS":
                            return true;
                        case "TC":
                            return true;
                        case "TN":
                            return false;
                        case "SS":
                            return true;
                        case "SC":
                            return true;
                        case "SN":
                            return false;
                        case "CS":
                            return true;
                        case "CC":
                            return true;
                        case "CN":
                            return false;
                        case "SB":
                            return true;
                        case "SW":
                            return false;
                        case "S":
                            return true;
                        case "DX":
                            return true;
                        case "DY":
                            return true;
                        case "Z":
                            return false;
                        case "R":
                            return false;
                        case "ZR":
                            return false;
                        default:
                            DriverManager.Manager.WriteLog("Utility", "[EMelsec : Melsec] : GetDevicecode Code Error return null >>>>> " + channel);
                            return false;

                    }

                default:
                    break;
            }

            return false;
        }
        public static string ConvertXmlnodeToJson(XmlNode node)
        {
            try
            {
                return JsonConvert.SerializeXmlNode(node);
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Console", except.ToString());
            }
            return "";
        }

        public static int GetSizeOfType(System.Type dataType)
        {
            if (dataType == typeof(byte))
            {
                return sizeof(byte);
            }
            else if (dataType == typeof(short))
            {
                return sizeof(short);
            }
            else if (dataType == typeof(ushort))
            {
                return sizeof(ushort);
            }
            else if (dataType == typeof(int))
            {
                return sizeof(int);
            }
            else if (dataType == typeof(uint))
            {
                return sizeof(uint);
            }
            else if (dataType == typeof(float))
            {
                return sizeof(float);
            }
            else if (dataType == typeof(double))
            {
                return sizeof(double);
            }
            else if (dataType == typeof(bool))
            {
                return sizeof(bool);
            }
            else if (dataType == typeof(long))
            {
                return sizeof(long);
            }
            else if (dataType == typeof(ulong))
            {
                return sizeof(ulong);
            }
            else if (dataType == typeof(char))
            {
                return sizeof(char);
            }
            else if (dataType == typeof(Int16))
            {
                return sizeof(Int16);
            }
            else if (dataType == typeof(UInt16))
            {
                return sizeof(UInt16);
            }
            else if (dataType == typeof(Int32))
            {
                return sizeof(Int32);
            }
            else if (dataType == typeof(UInt32))
            {
                return sizeof(UInt32);
            }
            else if (dataType == typeof(Int64))
            {
                return sizeof(Int64);
            }
            else if (dataType == typeof(UInt64))
            {
                return sizeof(UInt64);
            }

            return 1;
        }

        public static object CastingArrayToObject(byte[] data, Type dataType, int index, int size, double multi)
        {
            if (dataType == typeof(byte))
            {
                return (byte)(data[index] * multi);
            }
            else if (dataType == typeof(short))
            {
                return (short)(BitConverter.ToInt16(data, index) * multi);
            }
            else if (dataType == typeof(ushort))
            {
                return (ushort)(BitConverter.ToUInt16(data, index) * multi);
            }
            else if (dataType == typeof(int))
            {
                return (int)(BitConverter.ToInt32(data, index) * multi);
            }
            else if (dataType == typeof(uint))
            {
                return (uint)(BitConverter.ToUInt32(data, index) * multi);
            }
            else if (dataType == typeof(float))
            {
                return (float)(BitConverter.ToSingle(data, index) * multi);
            }
            else if (dataType == typeof(double))
            {
                return (double)(BitConverter.ToDouble(data, index) * multi);
            }
            else if (dataType == typeof(bool))
            {
                return BitConverter.ToBoolean(data, index);
            }
            else if (dataType == typeof(long))
            {
                return (long)(BitConverter.ToInt64(data, index) * multi);
            }
            else if (dataType == typeof(ulong))
            {
                return (ulong)(BitConverter.ToUInt64(data, index) * multi);
            }
            else if (dataType == typeof(char))
            {
                return (char)(BitConverter.ToChar(data, index) * multi);
            }
            else if (dataType == typeof(string))
            {
                byte[] dest = new byte[size];
                Array.Copy(data, index, dest, 0, size);
                return Encoding.Default.GetString(dest);
            }
            else if (dataType == typeof(Int16))
            {
                return (short)(BitConverter.ToInt16(data, index) * multi);
            }
            else if (dataType == typeof(UInt16))
            {
                return (ushort)(BitConverter.ToUInt16(data, index) * multi);
            }
            else if (dataType == typeof(Int32))
            {
                return (int)(BitConverter.ToInt32(data, index) * multi);
            }
            else if (dataType == typeof(UInt32))
            {
                return (uint)(BitConverter.ToUInt32(data, index) * multi);
            }
            else if (dataType == typeof(Int64))
            {
                return (long)(BitConverter.ToInt64(data, index) * multi);
            }
            else if (dataType == typeof(UInt64))
            {
                return (ulong)(BitConverter.ToUInt64(data, index) * multi);
            }

            return null;
        }

        #region File IO

        public static List<string> GetFileGroupList(string path)
        {
            List<string> groups = new List<string>();

            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);

                if (dir.Exists == true)
                {
                    foreach (DirectoryInfo group in dir.GetDirectories())
                    {
                        groups.Add(group.Name.ToString());
                    }
                }
                else
                {
                    DriverManager.Manager.WriteLog("Utility", "GetGroupList Func not Exist dir => " + path);
                }
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
            }
            return groups;
        }

        public static List<string[]> OpenCSVArray(string path)
        {
            try
            {
                StreamReader sr = new StreamReader(path);

                // 스트림의 끝까지 읽기
                List<string[]> array = new List<string[]>();


                while (!sr.EndOfStream)
                {
                    array.Add(sr.ReadLine().Split(','));
                }


                return array;
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", "[Utility] : " + except.ToString());
                return null;
            }
        }

        public static List<string[]> OpenTxtArray(string path, char split)
        {
            try
            {
                StreamReader sr = new StreamReader(path);

                // 스트림의 끝까지 읽기
                List<string[]> array = new List<string[]>();


                while (!sr.EndOfStream)
                {
                    array.Add(sr.ReadLine().Split(split));
                }


                return array;
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", "[Utility] : " + except.ToString());
                return null;
            }
        }

        public static string GetFilePathExecute()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }

        public static string GetFilePathExecuteFolder()
        {
            return Directory.GetCurrentDirectory();
        }

        public static void SerializeXML(string dirName, string fileName, Type type, object obj)
        {
            if (dirName.Length == 0)
            {
                dirName = ".\\";
            }

            if (dirName.LastIndexOf("\\") != dirName.Length - 1)
            {
                dirName += Path.DirectorySeparatorChar;
            }

            DirectoryInfo dir = new DirectoryInfo(dirName);

            if (dir.Exists == false)
            {
                dir.Create();
            }

            using (Stream stream = new FileStream(dirName + fileName, FileMode.Create, FileAccess.Write))
            {
                XmlSerializer ser = new XmlSerializer(type);
                ser.Serialize(stream, obj);
            }
        }

        public static object DeserializeXML(string fileName, Type type)
        {
            FileInfo file = new FileInfo(fileName);

            if (file.Exists)
            {
                try
                {

                    using (Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    {
                        XmlSerializer ser = new XmlSerializer(type);
                        return ser.Deserialize(stream);
                    }
                }
                catch (Exception except)
                {
                    DriverManager.Manager.WriteLog("Exception", except.ToString());
                }
            }

            return null;
        }

        public static void SaveJsonSerialize(string path, string content)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(DriverInterface.Variable.FolderName.ConfigFolder);

                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
                var obj = Newtonsoft.Json.Linq.JObject.Parse(content);

                File.WriteAllText(path, obj.ToString());
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
            }
        }

        public static string ReadFileString(string path)
        {
            if (File.Exists(path))
            {
                using (StreamReader r = new StreamReader(path, Encoding.UTF8))
                {
                    return r.ReadToEnd();
                }
            }

            return "";
        }

        public static string ReadDriverInfoJsonToWeb()
        {
            string fileName = DriverInterface.Variable.FolderName.GeneratedName + Path.DirectorySeparatorChar + DriverInterface.Variable.FileName.DriverInfoJsonToWeb;
            if (File.Exists(fileName))
            {
                using (StreamReader r = new StreamReader(fileName))
                {
                    return r.ReadToEnd();
                }
            }
            return "";
        }

        public static XmlDocument GetJsonToXmlDocument(string path)
        {
            string json = ReadFileString(path);

            if (json == "")
            {
                return null;
            }
            if (IsJson(json))
            {
                return GetJsonToXmlString(json);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region GatewayUtil
        public static List<string> GetGroupList(Dictionary<string, string> tagInfo)
        {
            List<string> list_group = new List<string>();

            if (tagInfo.ContainsKey("Group"))
            {
                list_group.Add(tagInfo["Group"]);

                if (tagInfo.ContainsKey("Group2"))
                {
                    list_group.Add(tagInfo["Group2"]);

                    if (tagInfo.ContainsKey("Group3"))
                    {
                        list_group.Add(tagInfo["Group3"]);

                        if (tagInfo.ContainsKey("Group4"))
                        {
                            list_group.Add(tagInfo["Group4"]);

                            if (tagInfo.ContainsKey("Group5"))
                            {
                                list_group.Add(tagInfo["Group5"]);
                            }
                        }
                    }
                }
            }
            return list_group;
        }

        public static string GetTagString(string deviceName, List<string> list_group, string tagName)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(deviceName);
            sb.Append(".");
            for (int i = 0; i < list_group.Count; i++)
            {
                if (list_group[i] != "")
                {
                    sb.Append(list_group[i]);
                    sb.Append(".");
                }
                else
                {
                    break;
                }
            }

            sb.Append(tagName);

            return sb.ToString();
        }

        #endregion
    }
}
