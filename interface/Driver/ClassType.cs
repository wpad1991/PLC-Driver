using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Driver
{
    public class DeviceDllNameInfo
    {
        public string Mitsubishi_Melsec_Ethernet = "Mitsubishi_Ethernet.dll";
        public string Robostar_RS232C = "Robostar_RS232C.dll";
        public string OPCDAClient = "OPCDAClient.dll";
        public string Modbus_TCP = "ModbusTCP.dll";
        public string LS_FastEthernet = "Enet.dll";

        public string Mitsubishi_Melsec_Ethernet_Namespace = "Mitsubishi_Ethernet.Driver";
        public string Robostar_RS232C_Namespace = "Robostar_RS232C.Driver";
        public string OPCDAClient_Namespace = "OPCDAClient.DAClient";
        public string Modbus_TCP_Namespace = "ModbusTCP.Driver";
        public string LS_FastEthernet_Namespace = "Enet.Driver";
    }


    public class DriverStatus_Http
    {
        DriverStatus status;
        int sendCount;
        int receiveCount;
        int totalTagCount;
        string version;

        public DriverStatus Status
        {
            get
            {
                return status;
            }

            set
            {
                status = value;
            }
        }

        public int SendCount
        {
            get
            {
                return sendCount;
            }

            set
            {
                sendCount = value;
            }
        }

        public int ReceiveCount
        {
            get
            {
                return receiveCount;
            }

            set
            {
                receiveCount = value;
            }
        }

        public int TotalTagCount
        {
            get
            {
                return totalTagCount;
            }

            set
            {
                totalTagCount = value;
            }
        }

        public string Version
        {
            get
            {
                return version;
            }

            set
            {
                version = value;
            }
        }

        public DriverStatus_Http(DriverStatus status, int sendCount, int receiveCount, int totalTagCount, string version)
        {
            this.status = status;
            this.sendCount = sendCount;
            this.receiveCount = receiveCount;
            this.totalTagCount = totalTagCount;
            this.version = version;
        }
    }

    public class DriverTagDataInfo : Nodes.INodeBase
    {
        string tagName = "";
        string description = "";
        object data = null;
        System.Type dataType;
        int dataSize;
        double dataMultiple;
        int itemAddr = -1;
        DateTime updateTime;
        Func<bool> actionFunc = null;


        public DriverTagDataInfo(
            string tagName,
            string description,
            object data,
            Func<bool> actionFunc,
            System.Type dataType,
            int dataSize,
            double dataMultiple)
        {
            this.tagName = tagName;
            this.description = description;
            this.data = data;
            this.actionFunc = actionFunc;
            this.UpdateTime = DateTime.Now;
            this.dataType = dataType;
            this.DataSize = dataSize;
            this.dataMultiple = dataMultiple;
        }


        public Nodes.ObjectType ObjectType
        {
            get
            {
                return Nodes.ObjectType.Value;
            }
        }

        public string TagName
        {
            get
            {
                return tagName;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public object Data
        {
            get
            {
                return data;
            }
            set
            {
                if (dataType == null && data != null)
                {
                    dataType = data.GetType();
                }
                data = value;
            }
        }

        public Func<bool> ActionFunc
        {
            get
            {
                return actionFunc;
            }
        }

        public int ItemAddr
        {
            get
            {
                return itemAddr;
            }

            set
            {
                itemAddr = value;
            }
        }

        public DateTime UpdateTime
        {
            get
            {
                return updateTime;
            }

            set
            {
                updateTime = value;
            }
        }

        public System.Type DataType
        {
            get
            {
                return dataType;
            }
        }

        public int DataSize
        {
            get
            {
                return dataSize;
            }

            set
            {
                dataSize = value;
            }
        }

        public double DataMultiple
        {
            get
            {
                return dataMultiple;
            }

            set
            {
                dataMultiple = value;
            }
        }
    }
}
