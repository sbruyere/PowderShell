using System.Text;

namespace PowderShell
{
    public class NestedNamespaceAtom : SortedDictionary<string, NestedNamespaceAtom>
    {
        public NestedNamespaceAtom() : base(StringComparer.OrdinalIgnoreCase) { }

        public void AddFullName(string fullname)
        {
            string[] atomicName = fullname.Split(".");

            NestedNamespaceAtom currentAtom = this;

            foreach (string? atom in atomicName)
            {
                if (currentAtom.ContainsKey(atom))
                    currentAtom = currentAtom[atom];
                else
                    currentAtom.Add(atom, []);
            }
        }

        public string GetFixedCaseFullname(string fullname)
        {
            List<string> fixedAtoms = new List<string>();
            string[] atomicName = fullname.Split(".");

            NestedNamespaceAtom currentAtom = this;

            bool broken = false;
            foreach (string? atom in atomicName)
            {
                if (!broken && currentAtom.ContainsKey(atom))
                {
                    var keys = currentAtom.Keys.ToList();
                    var fixedKey = keys.FirstOrDefault(v => v.Equals(atom, StringComparison.OrdinalIgnoreCase));

                    fixedAtoms.Add(fixedKey);
                    currentAtom = currentAtom[atom];
                    
                }
                else
                {
                    broken = true;
                    fixedAtoms.Add(atom);
                }
            }

            return string.Join(".", fixedAtoms);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            
            if (this.Count == 0)
            {
                return "[]";
            } 
            else
            {
                stringBuilder.AppendLine("[");
                foreach (var atom in this)
                {
                    stringBuilder.AppendLine(Helpers.IndentLines(4, $"[\"{atom.Key}\"] = " + atom.Value.ToString() + ","));
                }
                stringBuilder.Append("]");
            }

            return stringBuilder.ToString();
        }

    }
}