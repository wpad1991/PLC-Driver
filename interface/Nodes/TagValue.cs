using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverInterface.Nodes
{
    public class TagList
    {
        string channelId;
        string[] tags;
        System.Type[] dataType;
        //DateTime[] updateTime;

        public string[] Tags
        {
            get
            {
                return tags;
            }
        }

        public string ChannelId
        {
            get
            {
                return channelId;
            }
        }

        public System.Type[] DataType
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

        //public DateTime[] UpdateTime
        //{
        //    get
        //    {
        //        return updateTime;
        //    }
        //    set
        //    {
        //        updateTime = value;
        //    }
        //}

        public TagList(string id, int length)
        {
            if (length > 0)
            {
                tags = new string[length];
                channelId = id;
                DataType = new System.Type[length];

                //updateTime = new DateTime[tags.Length];

                //for (int i = 0; i < updateTime.Length; i++)
                //{
                //    updateTime[i] = DateTime.Now;
                //}
            }
        }

        public TagList(string id, string[] tags, System.Type[] dataType)
        {
            this.tags = tags;
            channelId = id;
            this.DataType = dataType;

            //if (tags?.Length > 0)
            //{
            //    updateTime = new DateTime[tags.Length];

            //    for (int i = 0; i < updateTime.Length; i++)
            //    {
            //        updateTime[i] = DateTime.Now;
            //    }
            //}
        }
    }
}
