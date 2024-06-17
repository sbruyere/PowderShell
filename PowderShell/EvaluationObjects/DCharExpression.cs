using MathNet.Symbolics;

namespace PowderShell.EvaluationObjects
{
    internal class DCharExpression : DMathExpression<char>, IStringExpression
    {

        public DCharExpression(SymbolicExpression exp)
            : base(exp)
        {
            MathObject = exp;
        }

        public DCharExpression(char value)
            : base(value)
        {
        }


        public override string ToString()
        {
            return base.ToString();
        }


        public override string ToExpressionString()
        {
            return "[char] " + (int)this.GetSymExp().RealNumberValue;
        }

        internal char ToCharValue()
        {
            return (char)(int)this.GetSymExp().RealNumberValue;
        }
    }
}
