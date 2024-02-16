using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DriverLog;

namespace DriverUtility.IO
{
    public class FileConverter
    {
        public static byte[] ConvertFileToStream(string filePath)
        {

            try
            {

                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Exists)
                {
                    byte[] data = new byte[fileInfo.Length];
                    FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
                    fileStream.Read(data, 0, data.Length);
                    fileStream.Close();

                    return data;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
            }
            return null;
        }

        public static bool SaveStringToFile(string path, string content)
        {
            try
            {
                FileInfo fi = new FileInfo(path);

                DirectoryInfo di = new DirectoryInfo(fi.DirectoryName);

                if (di.Exists == false)
                {
                    di.Create();
                }
                
                FileStream fs = null;

                if (fi.Exists == false)
                {
                    fs = new FileStream(fi.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                }
                else
                {
                    fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Write);
                }

                StreamWriter sw = new StreamWriter(fs, Encoding.Default);

                sw.WriteLine(content);
                sw.Close();
                fs.Close();
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception", except.ToString());
                return false;
            }
            return true;
        }
    }
}
