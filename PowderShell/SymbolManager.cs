using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowderShell
{
    internal class SymbolObject
    {
        public string Name { get; set; }
        public string FullName { get; set; }
    }

    internal class SymbolScopeContext
    {
        bool isRootContext = false;
        public Guid ScopeId { get; set; }
        public SymbolScopeContext? Parent { get; }
        public string Name { get; set; } 
        public string ContextType { get; set; }

        public SymbolScopeContext(SymbolScopeContext parent, string name, string contextType)
        {
            if (parent == null)
                isRootContext = true;

            Parent = parent;

            Name = name;
            ContextType = contextType;
            ScopeId = Guid.NewGuid();
        }
    }

    internal static class SymbolManager
    {
        public static Dictionary<string, SymbolScopeContext> ContextDictionary { get; set; } = new Dictionary<string, SymbolScopeContext>();

        public static SymbolScopeContext CreateContext(SymbolScopeContext parent, string name, string contextType)
        {
            ContextDictionary[name] = new SymbolScopeContext(parent, name, contextType);

            return ContextDictionary[name];
        }
    }
}
