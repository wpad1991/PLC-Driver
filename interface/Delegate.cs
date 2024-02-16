using DriverInterface.Driver;
using System.Collections.Generic;
using DriverInterface.Configuration;
using DriverInterface.Nodes;
using System;
using System.Xml;

namespace DriverInterface
{
    /// <summary>
    /// Driver Event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="dateTime"></param>
    /// <param name="dataInfo"></param>
    public delegate void ReadDataChangdEventHandler(object sender, string channelName, byte[] newData, System.Type dataType, DateTime dateTime);
    public delegate void ReadObjectChangdEventHandler(object sender, string[] tagName, object[] newData, DateTime[] dateTime);
    public delegate void DriverStatusChangedEventHandler(object sender, DriverStatus status, DateTime dateTime);
    public delegate void DriverUpdateTagInfoEventHandler(object sender, List<DriverTagDataInfo> tagInfo);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public delegate object RequestDataEventHandler(object sender, object request);
    public delegate List<DriverInformation> RequestConfigsEventHandler(object sender);
    public delegate DriverInformation RequestConfigEventHandler(object sender);

    /// <summary>
    /// Web Server Event
    /// </summary>
    /// <param name="configList"></param>
    /// <returns></returns>
    public delegate bool ConfigDataEventHandler(XmlDocument configList);
    public delegate void ActionEventHandler(string action);
    public delegate DriverStatus RequestStatusEventHandler(object sender);

    /// <summary>
    /// Module Write Data to Gateway
    /// </summary>
    /// <param name="tagName"></param>
    /// <param name="value"></param>
    /// <param name="dateTime"></param>
    public delegate int DataWriteValue(object sender, string tagName, object value, DateTime dateTime);

    public delegate object d();
}
