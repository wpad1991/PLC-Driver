using DriverLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DriverLog
{
    public delegate void MessageChangedHandler(string log);

    public class DriverManager
    {
        public event MessageChangedHandler MessageChanged = null;

        public static DriverManager Manager;

        public List<string> List_ConsoleFilter = new List<string>();

        ConcurrentDictionary<string, Queue<string>> currentLoggingQ = null;

        ConcurrentDictionary<string, Queue<string>>[] array_loggingQ = new ConcurrentDictionary<string, Queue<string>>[10];

        Dictionary<string, string> dic_logfile = new Dictionary<string, string>();

        int loggingIndex = 0;

        int maximumFileSizeM = 10;

        DriverThread loggingThread;

        DriverThread removeThread;

        public int LogSaveDay;

        public string LogFolder = "Log_DriverAuto";

        string logPath = "";

        int logSaveDay = 7;

        public string LogPath
        {
            get => logPath;
            set
            {
                if (logPath != value)
                {
                    logPath = value;
                    Console.WriteLine("Log Path : " + logPath);
                }
            }
        }

        static DriverManager()
        {
            Manager = new DriverManager();
        }

        DriverManager()
        {
            PathConfiguration();

            for (int i = 0; i < array_loggingQ.Length; i++)
            {
                array_loggingQ[i] = new ConcurrentDictionary<string, Queue<string>>();
            }

            loggingThread = new DriverThread(QueueSaveLog, null);
            loggingThread.Interval = 1000;
            loggingThread.Start();

            removeThread = new DriverThread(RemoveLog, null);
            removeThread.Interval = 1000 * 60 * 60;
            removeThread.Start();
        }

        private void PathConfiguration()
        {
            LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), LogFolder);
        }

        private int MoveNextLogIndex(int index, int maxCount)
        {
            return (index + 1) % maxCount;
        }

        private int PriviousLogIndex(int index, int maxCount)
        {
            if (index - 1 < 0)
            {
                return maxCount - 1;
            }

            return index - 1;
        }

        public void WriteLog(string logName, string msg)
        {
            if (List_ConsoleFilter.Contains(logName) || logName == "Exception" || logName == "Error"
            || logName == "Console" || logName == "Warning"
            || logName == "Else" || logName.Contains("Exception"))
            {
                Console.WriteLine(msg);

                if (MessageChanged != null)
                {
                    MessageChanged(msg);
                }
            }
            lock (array_loggingQ)
            {
                if (!array_loggingQ[loggingIndex].ContainsKey(logName))
                {
                    array_loggingQ[loggingIndex][logName] = new Queue<string>();
                    array_loggingQ[loggingIndex][logName].Enqueue(DateTime.Now.ToString("HH:mm:ss:fff,") + msg);
                }
                else
                {
                    array_loggingQ[loggingIndex][logName].Enqueue(DateTime.Now.ToString("HH:mm:ss:fff,") + msg);
                }
            }
        }

        public bool QueueSaveLog(string[] args)
        {
            try
            {
                loggingIndex = MoveNextLogIndex(loggingIndex, array_loggingQ.Length);

                currentLoggingQ = array_loggingQ[loggingIndex];

                ConcurrentDictionary<string, Queue<string>> dic_logging = array_loggingQ[PriviousLogIndex(loggingIndex, array_loggingQ.Length)];

                StringBuilder sb = new StringBuilder();

                foreach (string key in dic_logging.Keys)
                {
                    sb.Clear();

                    int qCount = dic_logging[key].Count;

                    for (int i = 0; i < qCount; i++)
                    {
                        sb.Append(dic_logging[key].Dequeue());
                        sb.Append("\n");
                    }

                    if (dic_logfile.TryGetValue(key, out string filename))
                    {
                        FileInfo fi = new FileInfo(filename);

                        if (!Directory.Exists(Path.GetDirectoryName(fi.FullName)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(filename));
                        }

                        if (fi.Exists)
                        {
                            if (fi.Length > maximumFileSizeM * 1024 * 1024)
                            {
                                dic_logfile[key] = Path.Combine(Path.Combine(logPath, key), DateTime.Now.ToString("yyyyMMdd_HHmmss_") + key + ".log");
                            }
                        }
                    }
                    else
                    {
                        dic_logfile[key] = Path.Combine(Path.Combine(logPath, key), DateTime.Now.ToString("yyyyMMdd_HHmmss_") + key + ".log");

                        Directory.CreateDirectory(Path.GetDirectoryName(dic_logfile[key]));
                    }

                    if (dic_logging[key].Count > 0)
                    {
                        Console.WriteLine("Error Log Q : " + key + ", " + dic_logging[key].Count);
                    }

                    if (sb.Length > 0)
                    {
                        WriteFileLog(dic_logfile[key], sb.ToString().Trim());
                    }
                }
            }
            catch (Exception except)
            {
                WriteLog("Exception", except.ToString());
            }

            return true;
        }

        private void WriteFileLog(string path, string msg)
        {
            if (File.Exists(path))
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(msg);
                }
            }
            else
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(msg);
                }
            }

        }

        public bool RemoveLog(string[] args)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(logPath);

                if (di != null)
                {
                    if (di.Exists)
                    {
                        foreach (DirectoryInfo dInfo in di.GetDirectories())
                        {
                            List<FileInfo> fileInfos = dInfo.EnumerateFiles().ToList();
                            for (int i = 0; i < fileInfos.Count; i++)
                            {
                                if ((DateTime.Now - fileInfos[i].CreationTime).TotalDays > logSaveDay)
                                {
                                    if (fileInfos[i].Name.ToUpper().Contains(".LOG"))
                                    {
                                        WriteLog("DeleteLog", fileInfos[i].Name);
                                        fileInfos[i].Delete();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception except)
            {
                WriteLog("Exception", except.ToString());
            }
            return true;
        }
    }
}
