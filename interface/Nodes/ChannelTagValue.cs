using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Nodes
{
    public class ChannelTagValue
    {
        System.Type dataType;
        byte[] values;
        string[] tags;

        public System.Type DataType
        {
            get
            {
                return dataType;
            }
        }

        public byte[] Data
        {
            get
            {
                return Values;
            }
            set
            {
                Values = value;
            }
        }

        public string[] Tags
        {
            get
            {
                return tags;
            }

            set
            {
                tags = value;
            }
        }

        public byte[] Values
        {
            get
            {
                return values;
            }

            set
            {
                values = value;
            }
        }
        


        public object GetData()
        {
            if (dataType == typeof(byte))
            {
                return Values;
            }
            else if (dataType == typeof(short))
            {
                int typeSize = sizeof(short);
                short[] buf = new short[Values.Length % typeSize == 0 ? Values.Length / typeSize : Values.Length / typeSize + 1];
                Buffer.BlockCopy(Values, 0, buf, 0, Values.Length);
                return buf;
            }
            else if (dataType == typeof(ushort))
            {
                int typeSize = sizeof(ushort);
                ushort[] buf = new ushort[Values.Length % typeSize == 0 ? Values.Length / typeSize : Values.Length / typeSize + 1];
                Buffer.BlockCopy(Values, 0, buf, 0, Values.Length);
                return buf;
            }
            else if (dataType == typeof(int))
            {
                int typeSize = sizeof(int);
                int[] buf = new int[Values.Length % typeSize == 0 ? Values.Length / typeSize : Values.Length / typeSize + 1];
                Buffer.BlockCopy(Values, 0, buf, 0, Values.Length);
                return buf;
            }
            else if (dataType == typeof(uint))
            {
                int typeSize = sizeof(uint);
                uint[] buf = new uint[Values.Length % typeSize == 0 ? Values.Length / typeSize : Values.Length / typeSize + 1];
                Buffer.BlockCopy(Values, 0, buf, 0, Values.Length);
                return buf;
            }
            else if (dataType == typeof(float))
            {
                int typeSize = sizeof(float);
                float[] buf = new float[Values.Length % typeSize == 0 ? Values.Length / typeSize : Values.Length / typeSize + 1];
                Buffer.BlockCopy(Values, 0, buf, 0, Values.Length);
                return buf;
            }
            else if (dataType == typeof(double))
            {
                int typeSize = sizeof(double);
                double[] buf = new double[Values.Length % typeSize == 0 ? Values.Length / typeSize : Values.Length / typeSize + 1];
                Buffer.BlockCopy(Values, 0, buf, 0, Values.Length);
                return buf;
            }
            return null;

        }

    }
}
