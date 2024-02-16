using IronInterface;
using IronInterface.Configuration;
using IronInterface.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace SPU_FTP
{
    enum LoopStep
    {
        Stop,
        Init,
        Run
    }

    public class Driver : ICustomDriver
    {
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

        // Driver
        private string driverID = "";
        private int readDataUpdateTime = 1000;
        private DriverStatus currentStatus = DriverStatus.None;
        private DateTime lastReadTime = DateTime.Now;

        // SPU FTP
        private Exception lastException = null;
        private string ipAddr = "127.0.0.1";
        private string ftpName = "";
        private string ftpPassword = "";
        private string EquipmentId = "EQP000000";
        private int Offset = 0;
        private List<string> TagName = new List<string>();

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

                IronLog4Net.IronLogger.LOG.WriteLog("Exception", lastException.ToString());
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
                        /*
                        // Modbus TCP Comm Connection Check
                        if (CheckCommStatus() == false)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }*/

                        // Read Tag
                        GetFTP();
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

                        if (dicInfo.ContainsKey("Period"))
                        {
                            int.TryParse(dicInfo["Period"], out int period);
                            readDataUpdateTime = period;
                        }

                        if (dicInfo.ContainsKey("Name"))
                        {
                            ftpName = dicInfo["Name"];
                        }

                        if (dicInfo.ContainsKey("Password"))
                        {
                            ftpPassword = dicInfo["Password"];
                        }

                        if (dicInfo.ContainsKey("Offset"))
                        {
                            int.TryParse(dicInfo["Offset"], out int offset);
                            Offset = offset;
                        }

                        if (dicInfo.ContainsKey("EquipmentId"))
                        {
                            EquipmentId = dicInfo["EquipmentId"];
                        }
                        
                        for (int i = 0; i < customConfig.TagInfo.Count; i++)
                        {
                            string id = "";
                            if (customConfig.TagInfo[i].ContainsKey("Id"))
                            {
                                id = customConfig.TagInfo[i]["Id"];
                            }

                            string group = "";
                            if (customConfig.TagInfo[i].ContainsKey("Group"))
                            {
                                group = customConfig.TagInfo[i]["Group"];
                            }

                            TagName.Add(DriverID + "." + group + "." + id);
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

        private string GetSPUCamUri(string folderName, int CamIndex)
        {
            StringBuilder uri = new StringBuilder();
            uri.Append("ftp://");
            uri.Append(ipAddr);
            uri.Append("/JPEG/");
            uri.Append(folderName);
            uri.Append(CamIndex.ToString());

            return uri.ToString();
        }

        private string GetFilePath(string folderName, int CamIndex)
        {
            string uri = GetSPUCamUri(folderName, CamIndex);

            FtpWebRequest ftp = (FtpWebRequest)WebRequest.Create(new Uri(uri));
            ftp.Method = WebRequestMethods.Ftp.ListDirectory;
            ftp.UsePassive = true;
            ftp.Credentials = new NetworkCredential(ftpName, ftpPassword);

            // Connect
            FtpWebResponse response = ftp.GetResponse() as FtpWebResponse;

            // Get FTP File Name List
            List<string> listFtpFile = new List<string>();

            Stream ftpStream = response.GetResponseStream();
            using (StreamReader ftpReader = new StreamReader(ftpStream))
            {

                while (ftpReader.EndOfStream == false)
                    listFtpFile.Add(ftpReader.ReadLine());

                ftpStream.Close();
                ftpReader.Close();
                response.Close();
            }

            StringBuilder rtnValue = new StringBuilder();

            if (listFtpFile.Count > (0 + Offset))
            {
                rtnValue.Append("ftp://");
                rtnValue.Append(ipAddr);
                rtnValue.Append("/JPEG/");
                rtnValue.Append(listFtpFile[listFtpFile.Count - 1 - Offset]);
            }

            return rtnValue.ToString();
        }

        private byte[] GetFtpImage(string folderName, int CamIndex)
        {
            string filePath = GetFilePath(folderName, CamIndex);

            FtpWebRequest ftp = (FtpWebRequest)WebRequest.Create(filePath);
            ftp.Method = WebRequestMethods.Ftp.DownloadFile;
            ftp.Credentials = new NetworkCredential(ftpName, ftpPassword);

            // Connect
            FtpWebResponse response = ftp.GetResponse() as FtpWebResponse;

            byte[] data = null;
            using (Stream ftpStream = response.GetResponseStream())
            {
                MemoryStream ms = new MemoryStream();
                byte[] buff = new byte[4096]; // 전통적인 FTP 버퍼 크기임
                int bytesRead;
                while ((bytesRead = ftpStream.Read(buff, 0, buff.Length)) > 0)
                    ms.Write(buff, 0, bytesRead);

                data = ms.ToArray();
            }

            response.Close();

            return data;
        }

        private void SaveImage(byte[] data, string folderName, int CamIndex)
        {
            if (data != null)
            {
                string path = @"C:\SPU\webservice\public\img\HEAT\" + EquipmentId + "\\";

                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Exists == false)
                    di.Create();

                string fileType = "";
                if (folderName == "Visual")
                    fileType = "Visual";
                if (folderName == "Edge")
                    fileType = "thermal";

                if (CamIndex == 0)
                    path += "busbar-" + fileType + "-01.jpg";
                if (CamIndex == 1)
                    path += "busbar-" + fileType + "-02.jpg";
                if (CamIndex == 2)
                    path += "cable-" + fileType + "-01.jpg";
                if (CamIndex == 3)
                    path += "cable-" + fileType + "-02.jpg";

                FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                fs.Write(data, 0, data.Length);
                fs.Close();
            }
        }
        
        private void GetFTP()
        {
            try
            {
                string[] tagName = new string[8];
                object[] newData = new object[8];
                DateTime[] time = new DateTime[8];
                int idx = 0;

                for (int i = 0; i < 4; i++)
                {
                    string folderName;

                    // Thermal
                    folderName = "Edge";
                    byte[] dataThermal = GetFtpImage(folderName, i);
                    SaveImage(dataThermal, folderName, i);

                    tagName[idx] = driverID + ".Thermal.Image" + (i + 1).ToString();
                    newData[idx] = dataThermal;
                    time[idx] = DateTime.Now;
                    idx++;

                    
                    // Visual
                    folderName = "Visual";
                    byte[] dataVisual = GetFtpImage(folderName, i);
                    SaveImage(dataVisual, folderName, i);

                    tagName[idx] = driverID + ".Visual.Image" + (i + 1).ToString();
                    newData[idx] = dataVisual;
                    time[idx] = DateTime.Now;
                    idx++;

                    int a = 0;
                }

                ReadObjectChanged(this, tagName, newData, time);
            }
            catch(Exception exception)
            {
                int a = 0;
            }
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
            throw new NotImplementedException();
        }

        public int[] WriteData(string[] tags, object[] values)
        {
            throw new NotImplementedException();
        }
    }
}
