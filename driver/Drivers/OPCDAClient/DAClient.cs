using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronInterface;
using IronInterface.Driver;

namespace OPCDAClient
{
    public class DAClient : ITagDriver
    {
        string driverID = "OPCDAClient";
        Opc.Da.Server opcServer = null;
        OpcCom.Factory factory = new OpcCom.Factory();
        Opc.Da.Subscription subScription = null;
        Opc.Da.SubscriptionState subScriptionState = new Opc.Da.SubscriptionState();
        Opc.Da.Item[] subscribedItems;
        int readDataUpdateTime = 0;
        string server = "opcda://localhost/IronOPC.OpcDaServer";
        string group = "MyGroup";
        string topic = "";
        int ID = 0;
        bool simulationMode = false;
        //Dictionary<string, OPCVariable> variableDic = new Dictionary<string, OPCVariable>();
        Dictionary<int, DriverTagDataInfo> TagManager = new Dictionary<int, DriverTagDataInfo>();

        public string Server
        {
            get { return server; }
            set { server = value; }
        }

        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        public string Topic
        {
            get { return topic; }
            set { topic = value; }
        }

        public string DriverID { get => driverID; set => driverID = value; }

        public DriverType GetDriverType => DriverType.OPCDAClient;

        public bool Started
        {
            get
            {
                if (opcServer != null)
                {
                    return opcServer.IsConnected;
                }
                return false;
            }
        }

        public bool SimulationMode { get => simulationMode; set => simulationMode = value; }

        public int ReadDataUpdateTime => readDataUpdateTime;

        public event ReadObjectChangdEventHandler ReadObjectChanged;
        public event DriverStatusChangedEventHandler StatusChanged;
        public event RequestConfigEventHandler RequestConfig;
        public event RequestDataEventHandler RequestData;
        public event DriverUpdateTagInfoEventHandler UpdateTagInformation;

        public List<DriverTagDataInfo> GetObjectTagInfo()
        {
            List<DriverTagDataInfo> result = new List<DriverTagDataInfo>();

            foreach (int key in TagManager.Keys)
            {
                result.Add(TagManager[key]);
            }
            return result;
        }

        public DriverStatus GetDriverStatus()
        {
            if (opcServer != null)
            {
                Opc.Da.ServerStatus status = opcServer.GetStatus();

                if (status.ServerState == Opc.Da.serverState.running)
                {
                    return DriverStatus.Run;
                }

                return DriverStatus.Bad;
            }

            return DriverStatus.Stop;
        }

        public DateTime GetLastReadTime()
        {
            throw new NotImplementedException();
        }

        public object ReadAny(string tag)
        {
            try
            {
                Opc.Da.Item[] item = new Opc.Da.Item[1];

                item[0] = new Opc.Da.Item();
                item[0].ItemName = tag;

                Opc.Da.ItemValue[] values = opcServer.Read(item);

                return values[0].Value;
            }
            catch (Exception except)
            {
                IronLog4Net.IronLogger.LOG.WriteLog("Exception", except.ToString());
            }
            return null;
        }

        public object ReadAny(string[] tags)
        {
            try
            {
                Opc.Da.Item[] item = new Opc.Da.Item[tags.Length];

                for (int i = 0; i < tags.Length; i++)
                {
                    item[i] = new Opc.Da.Item();
                    item[i].ItemName = tags[i];
                }
                Opc.Da.ItemValue[] values = opcServer.Read(item);

                return values[0].Value;
            }
            catch (Exception except)
            {
                IronLog4Net.IronLogger.LOG.WriteLog("Exception", except.ToString());
            }
            return null;
        }

        public int WriteData(string tag, object value)
        {

            try
            {
                Opc.Da.Item[] item = new Opc.Da.Item[1];


                item[0] = new Opc.Da.Item();
                item[0].ItemName = tag;

                Opc.Da.ItemValue[] itemValue = new Opc.Da.ItemValue[1];

                itemValue[0] = new Opc.Da.ItemValue(item[0]);
                itemValue[0].Value = value;

                Opc.IdentifiedResult[] result = opcServer.Write(itemValue);

                return result[0].ResultID.Code;
            }
            catch (Exception except)
            {
                IronLog4Net.IronLogger.LOG.WriteLog("Exception", except.ToString());
            }

            return 1;

        }

        public int[] WriteData(string[] tags, object[] values)
        {
            try
            {
                Opc.Da.Item[] item = new Opc.Da.Item[tags.Length];
                Opc.Da.ItemValue[] itemValue = new Opc.Da.ItemValue[tags.Length];

                for (int i = 0; i < tags.Length; i++)
                {
                    item[i] = new Opc.Da.Item();
                    item[i].ItemName = tags[i];

                    itemValue[i] = new Opc.Da.ItemValue(item[i]);
                    itemValue[i].Value = values[i];
                }


                Opc.IdentifiedResult[] result = opcServer.Write(itemValue);

                int[] resultCode = new int[result.Length];

                for (int i = 0; i < result.Length; i++)
                {
                    resultCode[i] = result[i].ResultID.Code;
                }

                return resultCode;
            }
            catch (Exception except)
            {
                IronLog4Net.IronLogger.LOG.WriteLog("Exception", except.ToString());
            }

            return null;
        }

        public bool RestartDriver()
        {
            StopDriver();
            StartDriver();
            return true;
        }

        public bool StartDriver()
        {
            try
            {
                IronLog4Net.IronLogger.LOG.WriteLog("Console", "[OPCDAClient] : StartDriver()");

                if (opcServer == null)
                {
                    opcServer = new Opc.Da.Server(factory, null);
                    opcServer.Url = new Opc.URL(server);
                    opcServer.Connect();
                }
                else
                {
                    return false;
                }

                if (subScription == null)
                {
                    subScriptionState.Name = group;
                    subScription = (Opc.Da.Subscription)opcServer.CreateSubscription(subScriptionState);
                    subScription.DataChanged += new Opc.Da.DataChangedEventHandler(SubScription_DataChanged);

                    if (SubscribeItemSetting())
                    {
                        return true;
                    }
                    else
                    {
                        StopDriver();
                        return false;
                    }
                }
                else
                {
                    StopDriver();
                    return false;
                }
            }
            catch (Exception except)
            {
                IronLog4Net.IronLogger.LOG.WriteLog("Exception", except.ToString());
                new Thread(() => 
                {
                    Thread.Sleep(3000);
                    StartDriver();
                }).Start();
                return false;
            }
        }

        public bool StopDriver()
        {
            try
            {
                if (opcServer != null)
                {
                    opcServer.Disconnect();
                    opcServer.Dispose();
                }
            }
            catch (Exception except)
            {
                IronLog4Net.IronLogger.LOG.WriteLog("Exception", except.ToString());
                return false;
            }

            return true;
        }

        public int Subscribe(string variable)
        {
            if (opcServer == null || opcServer.IsConnected == false || variable.Length == 0)
                return -1;

            if (topic != "")
            {
                variable = "[" + topic + "]" + variable;
            }

            if (!GetTagManagerTagExist(variable))
            {
                DriverTagDataInfo info = new DriverTagDataInfo(variable, "", null, null, IOType.DYNAMIC, null);

                info.ItemAddr = ++ID;

                TagManager.Add(ID, info);
            }

            if (subscribedItems != null && subScription.Items.Length > 0)
            {
                subScription.RemoveItems(subscribedItems);
            }

            subscribedItems = new Opc.Da.Item[TagManager.Count];
            int i = 0;

            foreach (DriverTagDataInfo var in TagManager.Values)
            {
                subscribedItems[i] = new Opc.Da.Item();
                subscribedItems[i].ItemName = var.TagName;
                subscribedItems[i].SamplingRate = 100;
                subscribedItems[i].ClientHandle = var.ItemAddr;
                i++;
            }

            Opc.Da.ItemResult[] itemResult = subScription.AddItems(subscribedItems);

            i = 0;

            foreach (Opc.Da.ItemResult ret in itemResult)
            {
                subscribedItems[i++].ServerHandle = ret.ServerHandle;
            }

            Opc.IRequest req;
            subScription.Read(subscribedItems, TagManager.Count, ReadComplete, out req);

            return ID;
        }

        public int Subscribe(string[] variables)
        {
            if (opcServer == null || opcServer.IsConnected == false || variables.Length == 0)
                return -1;


            for (int idx = 0; idx < variables.Length; idx++)
            {
                if (topic != "")
                {
                    variables[idx] = "[" + topic + "]" + variables[idx];
                }

                if (!GetTagManagerTagExist(variables[idx]))
                {
                    DriverTagDataInfo info = new DriverTagDataInfo(variables[idx], "", null, null, IOType.DYNAMIC, null);

                    info.ItemAddr = ++ID;

                    TagManager.Add(ID, info);
                }
            }
            if (subscribedItems != null && subScription.Items.Length > 0)
            {
                subScription.RemoveItems(subscribedItems);
            }

            subscribedItems = new Opc.Da.Item[TagManager.Count];
            int i = 0;

            foreach (DriverTagDataInfo var in TagManager.Values)
            {
                subscribedItems[i] = new Opc.Da.Item();
                subscribedItems[i].ItemName = var.TagName;
                subscribedItems[i].SamplingRate = 100;
                subscribedItems[i].ClientHandle = var.ItemAddr;
                i++;
            }

            Opc.Da.ItemResult[] itemResult = subScription.AddItems(subscribedItems);

            i = 0;

            foreach (Opc.Da.ItemResult ret in itemResult)
            {
                subscribedItems[i++].ServerHandle = ret.ServerHandle;
            }

            Opc.IRequest req;
            subScription.Read(subscribedItems, TagManager.Count, ReadComplete, out req);

            return ID;
        }

        public void Unsubscribe(int id)
        {
            if (TagManager.ContainsKey(id))
            {
                TagManager.Remove(id);
            }

            if (subscribedItems != null && subScription.Items.Length > 0)
            {
                subScription.RemoveItems(subscribedItems);
            }

            if (opcServer != null && opcServer.IsConnected)
            {
                subscribedItems = new Opc.Da.Item[TagManager.Count];
                int i = 0;

                foreach (int var in TagManager.Keys)
                {
                    subscribedItems[i] = new Opc.Da.Item();
                    subscribedItems[i].ItemName = TagManager[var].TagName;
                    subscribedItems[i].SamplingRate = 100;
                    subscribedItems[i].ClientHandle = TagManager[var].ItemAddr;
                    i++;
                }

                Opc.Da.ItemResult[] itemResult = subScription.AddItems(subscribedItems);

                i = 0;
                foreach (Opc.Da.ItemResult ret in itemResult)
                {
                    subscribedItems[i++].ServerHandle = ret.ServerHandle;
                }
            }
        }

        bool GetTagManagerTagExist(string tagName)
        {
            foreach (int index in TagManager.Keys)
            {
                if (TagManager[index].TagName == tagName)
                {
                    return true;
                }
            }
            return false;
        }

        int GetTagManagerID(string tagName)
        {
            foreach (int index in TagManager.Keys)
            {
                if (TagManager[index].TagName == tagName)
                {
                    return index;
                }
            }
            return 0;
        }

        bool SubscribeItemSetting()
        {
            try
            {
                if (RequestConfig != null)
                {
                    IronInterface.Configuration.DriverTagInfoConfig config = (IronInterface.Configuration.DriverTagInfoConfig)RequestConfig(this);

                    if (config == null)
                    {
                        IronLog4Net.IronLogger.LOG.WriteLog("Error", "[DAClient.SubscribeItemSetting] : Config is Null");
                        return false;
                    }
                    List<string> tagList = new List<string>();

                    foreach (DriverTagDataInfo tagInfo in config.TagInformation)
                    {
                        tagList.Add(tagInfo.TagName);
                    }

                    Subscribe(tagList.ToArray());
                }
                else
                {
                    IronLog4Net.IronLogger.LOG.WriteLog("Error", "[DAClient.SubscribeItemSetting] : Request Config Event is Null");
                    return false;
                }
            }
            catch (Exception except)
            {
                IronLog4Net.IronLogger.LOG.WriteLog("Exception", except.ToString());
                return false;
            }
            return true;
        }

        void ReadComplete(object requestHandle, Opc.Da.ItemValueResult[] results)
        {
        }

        void SubScription_DataChanged(object subscriptionHandle, object requestHandle, Opc.Da.ItemValueResult[] values)
        {
            try
            {

                List<string> tagList = new List<string>();
                List<object> valueList = new List<object>();
                List<DateTime> timeList = new List<DateTime>();

                foreach (Opc.Da.ItemValueResult value in values)
                {

                    int handle = (int)value.ClientHandle;
                    if (TagManager.ContainsKey(handle))
                    {
                        tagList.Add(TagManager[handle].TagName);
                        valueList.Add(value.Value);
                        timeList.Add(value.Timestamp);
                    }
                    else
                    {
                        IronLog4Net.IronLogger.LOG.WriteLog("Error", "[OPCDAClient.DAClient] : SubScription_DataChanged, Client Handle Not Exist TagManager, Handle : " + handle);
                    }

                }
                ReadObjectChanged?.BeginInvoke(this, "", tagList.ToArray(), valueList.ToArray(), timeList.ToArray(), null, null);
            }
            catch (Exception except)
            {
                IronLog4Net.IronLogger.LOG.WriteLog("Exception", except.ToString());
            }
        }

    }
}
