using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Nodes
{
    public class LogicTag
    {
        string _id;
        string _desc;
        string _group;
        string _sourceType;
        string _rankType;
        List<LogicNode> _lNode;

        public string Id { get => _id; set => _id = value; }
        public string Desc { get => _desc; set => _desc = value; }
        public string Group { get => _group; set => _group = value; }
        public string SourceType { get => _sourceType; set => _sourceType = value; }
        public string RankType { get => _rankType; set => _rankType = value; }
        public List<LogicNode> LNode { get => _lNode; set => _lNode = value; }
    }

    public struct LogicNode
    {
        public bool Condition;
        public string Formula;
        public List<LogicNode> LNode;
    }
}
