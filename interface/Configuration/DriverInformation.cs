using DriverInterface.Driver;
using System;
using System.Collections.Generic;


namespace DriverInterface.Configuration
{
    public class DriverInformation
    {
        public string DriverName = "";
        public string Description = "";
        public readonly string Version = "1.0.24";
        public DriverType deviceType = DriverType.NONE;
        public DriverStatus Status = DriverStatus.None;
        public bool Readonly = false;
        public string[] Channel;
        public int[] Address;
        public int[] Size;
        public int[] DataSize;
        public List<string[]> TagList;
        public List<System.Type[]> TypeList;
        public List<int[]> SizeList;
        public List<double[]> MultiList;
        public List<int[]> IndexerList;
        //public List<string[]> groupList;

        //public List<Dictionary<string, string>> TagInfo;
        public List<Dictionary<string, string>> TagInfo;
        /* TagInfo 
         * 
         * Dictionary<Tag의 속성 이름, Tag의 속성 값> -> 하나의 Tag
         * List<Dictionary> -> 각 Tag(Dic)가 List로 구성되어 있음
         */

    }
}
