using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace DriverInterface.Nodes
{
    public class TreeNode<T>
    {
        private readonly string name;
        private object value;
        private readonly List<TreeNode<T>> children = new List<TreeNode<T>>();


        public string Name
        {
            get
            {
                return name;
            }
        }

        public TreeNode(string name)
        {
            this.name = name;
        }

        public TreeNode(string name, object value)
        {
            this.name = name;
            this.value = value;
        }

        public TreeNode<T> this[string key]
        {
            get
            {
                foreach (TreeNode<T> item in children)
                {
                    if (item.name == key)
                    {
                        return item;
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        public TreeNode<T> Parent { get; private set; }

        public object Value {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
            }
        }

        public ReadOnlyCollection<TreeNode<T>> Children
        {
            get { return children.AsReadOnly(); }
        }

        public TreeNode<T> AddChild(string name)
        {
            var node = new TreeNode<T>(name) { Parent = this };
            children.Add(node);
            return node;
        }

        public TreeNode<T> AddChild(string name, object value)
        {
            var node = new TreeNode<T>(name, value) { Parent = this };
            children.Add(node);
            return node;
        }

        public TreeNode<T> AddChild(TreeNode<T> node)
        {
            node.Parent = this;
            children.Add(node);
            return node;
        }
        
        //public TreeNode<T>[] AddChildren(params T[] values)
        //{
        //    return values.Select(AddChild).ToArray();
        //}

        public bool RemoveChild(TreeNode<T> node)
        {
            return children.Remove(node);
        }

        //public void Traverse(Action<T> action)
        //{
        //    action(Value);
        //    foreach (var child in children)
        //        child.Traverse(action);
        //}

        //public IEnumerable<T> Flatten()
        //{
        //    return new[] { Value }.Concat(children.SelectMany(x => x.Flatten()));
        //}
    }
}
