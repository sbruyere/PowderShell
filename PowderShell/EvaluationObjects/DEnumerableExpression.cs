using MathNet.Symbolics;
using System;
using System.Collections.Generic;
using System.Text;
using PowderShell;
using System.Dynamic;

namespace PowderShell.EvaluationObjects
{
    internal class DEnumerableExpression : DExpression
    {
        public IEnumerable<DExpression> Elements { get; set; } = new List<DExpression>();

        public override bool IsValuable { get => Elements.All(v => v.IsValuable); set => throw new NotImplementedException(); }
        public override bool HasSideEffet { get => Elements.Any(v => v.HasSideEffet); set => throw new NotImplementedException(); }

        public DEnumerableExpression()
        {

        }

        public DEnumerableExpression(IEnumerable<DExpression> elements)
        {
            Elements = elements;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override string ToExpressionString()
        {
            string[] expressionstr = Elements.Select(v => v.ToExpressionString()).ToArray();
            return $"@({string.Join(", ", expressionstr)})";
        }

        public override string ToValueString()
        {
            return ToExpressionString();
        }

        internal override SymbolicExpression GetSymExp()
        {
            
            throw new NotImplementedException();
        }
    }
}
