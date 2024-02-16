using System;
using System.IO;


using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DriverLog;

namespace DriverUtility.Converter
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

    }
}
