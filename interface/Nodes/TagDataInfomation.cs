using DriverInterface.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Nodes
{
    public class TagDataInformation
    {
        public byte[] Data = null;
        DriverTagDataInfo[] tagValue;

        public DriverTagDataInfo[] TagValue
        {
            get
            {
                return tagValue;
            }

            set
            {
                tagValue = value;
            }
        }

        public TagDataInformation()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data">
        /// 물리적으로 사용할 Data 영역
        /// </param>
        /// <param name="dataType">
        /// 실제 물리적 공간에서 data를 활용할 dataType
        /// </param>
        /// <param name="tagValue">
        /// 실제 물리적인 영역이 존재하지 않는 드라이버에서 사용
        /// 물리적인 영역이 존재하여도 추후 활용 가능
        /// </param>
        /// <param name="isReal">
        /// 실제적인 물리적 드라이버가 있는지 확인
        /// </param>
        public TagDataInformation(byte[] data, DriverTagDataInfo[] tagValue)
        {
            this.Data = data;
            this.tagValue = tagValue;
        }

        public DriverTagDataInfo GetTagValue(string tag)
        {
            foreach (DriverTagDataInfo tagInfo in tagValue)
            {
                if (tag == tagInfo.TagName)
                {
                    return tagInfo;
                }
            }
            return null;
        }
    }
}
