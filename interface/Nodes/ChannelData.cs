using DriverInterface.Type;
using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Nodes
{
    public class ChannelData
    {
        //public ChannelData()
        //{
        //    channelId = "Default";
        //}

        //public ChannelData(ChannelData dataObject)
        //{
        //    this.channelId = dataObject.channelId;
        //    this.dataType = dataObject.dataType;
        //    this.data = (byte[])dataObject.data.Clone();
        //}

        //public ChannelData(string channelId, System.Type dataType)
        //{
        //    this.channelId = channelId;
        //    this.dataType = dataType;
        //}

        //public ChannelData(string channelId, System.Type dataType, byte[] data)
        //{
        //    this.channelId = channelId;
        //    this.dataType = dataType;
        //    this.data = (byte[])data.Clone();
        //}

        //public object GetData()
        //{
        //    if (dataType == typeof(byte))
        //    {
        //        return data;
        //    }
        //    else if (dataType == typeof(short))
        //    {
        //        int typeSize = sizeof(short);
        //        short[] buf = new short[data.Length % typeSize == 0 ? data.Length / typeSize : data.Length / typeSize + 1];
        //        Buffer.BlockCopy(data, 0, buf, 0, data.Length);
        //        return buf;
        //    }
        //    else if (dataType == typeof(ushort))
        //    {
        //        int typeSize = sizeof(ushort);
        //        ushort[] buf = new ushort[data.Length % typeSize == 0 ? data.Length / typeSize : data.Length / typeSize + 1];
        //        Buffer.BlockCopy(data, 0, buf, 0, data.Length);
        //        return buf;
        //    }
        //    else if (dataType == typeof(int))
        //    {
        //        int typeSize = sizeof(int);
        //        int[] buf = new int[data.Length % typeSize == 0 ? data.Length / typeSize : data.Length / typeSize + 1];
        //        Buffer.BlockCopy(data, 0, buf, 0, data.Length);
        //        return buf;
        //    }
        //    else if (dataType == typeof(uint))
        //    {
        //        int typeSize = sizeof(uint);
        //        uint[] buf = new uint[data.Length % typeSize == 0 ? data.Length / typeSize : data.Length / typeSize + 1];
        //        Buffer.BlockCopy(data, 0, buf, 0, data.Length);
        //        return buf;
        //    }
        //    else if (dataType == typeof(float))
        //    {
        //        int typeSize = sizeof(float);
        //        float[] buf = new float[data.Length % typeSize == 0 ? data.Length / typeSize : data.Length / typeSize + 1];
        //        Buffer.BlockCopy(data, 0, buf, 0, data.Length);
        //        return buf;
        //    }
        //    else if (dataType == typeof(double))
        //    {
        //        int typeSize = sizeof(double);
        //        double[] buf = new double[data.Length % typeSize == 0 ? data.Length / typeSize : data.Length / typeSize + 1];
        //        Buffer.BlockCopy(data, 0, buf, 0, data.Length);
        //        return buf;
        //    }
        //    return null;

        //}

    }
}
