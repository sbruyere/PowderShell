using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowderShell
{
    public class HierarchicalNamespaceNode
    {
        public string Value { get; set; }
        public SortedDictionary<string, HierarchicalNamespaceNode> Children { get; } = new SortedDictionary<string, HierarchicalNamespaceNode>();

        public void Add(string[] parts, string value)
        {
            if (parts.Length == 0)
                return;

            var current = parts[0];
            if (!Children.ContainsKey(current))
            {
                Children[current] = new HierarchicalNamespaceNode();
            }

            if (parts.Length == 1)
            {
                Children[current].Value = value;
            }
            else
            {
                Children[current].Add(parts[1..], value);
            }
        }
    }
}
