using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Variable
{
    public class FileName
    {
        public static readonly string JsonConfig = "DeviceInfo.json";
        public static readonly string DriverInfoJsonToWeb = "InitDriverInfoToWeb.json";
        public static readonly string DriverDllNameXml = "DriverNameInfo.xml";
        public static readonly string DriverStatusJson = "DriverStatus.json";
        public static readonly string OPCAEConfig = "OPCAEConfig.csv";
        public static readonly string ProgramConfig = "ProgramConfig.xml";
    }
    public class FolderName
    {
        static string configPath = "";
        public static readonly string WebFolder = "View";
        public static readonly string GeneratedName = "Generated";
        public static readonly string DriverFoler = "Driver";        
        public static string ConfigFolder {
            set
            {
                configPath = value;
            }
            get
            {
                if (configPath != "")
                {
                    return configPath;
                }
                
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DriverAutomation", "DriverGateway");

            }
        }
    }
}
