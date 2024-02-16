using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Configuration
{
    public class AlarmAreaConfiguration
    {
        public string Id = "";
        public string Desc = "";
        public List<AlarmEventConfiguration> Events = new List<AlarmEventConfiguration>();
    }

    public class AlarmEventConfiguration
    {
        public string Id = "";
        public string Desc = "";
        public string SourceDriver = "";
        public string SourceTag = "";
        public string Condition = "";
        public string Active = "";
        public string DeadbandType = "";
        public string DeadbandValue = "";
        public string DeadbandRangeMin = "";
        public string DeadbandRangeMax = "";
        public string Severity = "";
        public string Message = "";
        public string ThresholdType = "";
        public string Threshold = "";
        public string ThresholdDriver = "";
        public string ThresholdTag = "";
        public string Comparison = "";
        public string AckRequired = "";
        public string EventType = "";
        public string EventCategory = "";
    }
}
