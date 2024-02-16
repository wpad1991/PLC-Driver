using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Nodes
{
    public class ObjectBase : INodeBase
    {
        ObjectType objectType = ObjectType.None;

        public ObjectBase(ObjectType objectType)
        {
            this.objectType = objectType;
        }

        public ObjectType ObjectType
        {
            get
            {
                return objectType;
            }
        }
    }
}
