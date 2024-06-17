using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Symbolics;

namespace PowderShell.EvaluationObjects
{
    internal class DComplexStringExpression
        : DExpression, IStringExpression
    {

        public List<DExpression> ConcatExpressions { get; set; } = new List<DExpression>();

        public override bool HasSideEffet { get; set; } = false;
        public override bool IsValuable { get; set; } = true;

        public DComplexStringExpression()
        {
        }

        public DComplexStringExpression(DExpression leftExp)
        {
            Concat(leftExp);
        }

        public override string ToExpressionString()
        {

            //if (IsValuable)
            //{
            //    return ToValueString();
            //}
            //else
            //{
                List<string> stringResult = new List<string>();
                foreach (var v in ConcatExpressions)
                {
                    //if (v is DSimpleStringExpression)
                    //{
                    stringResult.Add(v.ToExpressionString());
                    //}
                }
                return string.Join(" & ", stringResult);
            //}
        }

        internal override SymbolicExpression GetSymExp()
        {
            return SymbolicExpression.Variable(ToExpressionString());
        }

        public override string ToString()
        {
            return ToExpressionString();
        }

        public void Concat(DExpression expression)
        {
            if (expression is DComplexStringExpression)
            {
                foreach (var v in ((DComplexStringExpression)expression).ConcatExpressions)
                {
                    Concat(v);
                }
                return;
            }

            if (!expression.IsValuable)
                IsValuable = false;

            if (expression.HasSideEffet || !expression.IsValuable)
            {
                HasSideEffet = true;
                ConcatExpressions.Add(expression);
                return;
            }

            var curRightExp = ConcatExpressions.LastOrDefault();

            string rightExpStr = GetStringFromStringExpression(curRightExp);
            string expStr = GetStringFromStringExpression(expression);

            if ((curRightExp is DSimpleStringExpression || curRightExp is DCharExpression) && expression.IsValuable)
            {
                //var options = (curRightExp as DSimpleStringExpression).Options;

                ConcatExpressions[ConcatExpressions.Count - 1] = new DSimpleStringExpression(rightExpStr + expStr, Encoding.Unicode);
                //((DSimpleStringExpression)curRightExp).SetValue(curRightExp.ToValueString() + expression.ToValueString()); <= this was causing side effect
                return;
            }

            ConcatExpressions.Add(expression);
        }

        private static string GetStringFromStringExpression(DExpression? curRightExp)
        {
            string rightExpStr = null;

            if (curRightExp is DCharExpression)
                rightExpStr = ((DCharExpression)curRightExp).ToCharValue().ToString();

            if (curRightExp is DSimpleStringExpression)
                rightExpStr = ((DSimpleStringExpression)curRightExp).ToValueString();

            return rightExpStr;
        }

        public override string ToValueString()
        {
            if (!this.IsValuable)
                throw new System.Exception("Not valuable");

            DSimpleStringExpression exp;

            if (ConcatExpressions.Count() == 1 && (exp = ConcatExpressions.FirstOrDefault() as DSimpleStringExpression) != null)
            {
                return exp.ToValueString();
            }
            else
            {
                StringBuilder stringResult = new StringBuilder();

                foreach (var v in ConcatExpressions)
                {
                    //if (v is DSimpleStringExpression)
                    //{
                    stringResult.Append(v.ToValueString());
                    //}
                }

                return stringResult.ToString();
            }
        }
    }
}
