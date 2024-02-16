using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Type
{
    public class TypeValue
    {
        object data;
        System.Type dataType;

        public object Data
        {
            get
            {
                return data;
            }

            set
            {
                data = value;
            }
        }

        public System.Type DataType
        {
            get
            {
                return dataType;
            }

            set
            {
                dataType = value;
            }
        }

        public TypeValue(object data, System.Type dataType)
        {
            this.data = data;
            this.dataType = dataType;
        }
    }
}
