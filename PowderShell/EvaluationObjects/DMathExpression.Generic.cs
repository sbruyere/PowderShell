
using MathNet.Symbolics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Expr = MathNet.Symbolics.SymbolicExpression;

namespace PowderShell
{
    internal class DMathExpression<T> : DMathExpression
        where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>
    {
        public override bool HasSideEffet { get; set; }

        public DMathExpression(SymbolicExpression value)
        {
            MathObject = value;
        }

        public DMathExpression(T value)
        {
            if (IsNumericType(value))
            {
                if (value is Char)
                {
                    MathObject = Convert.ToDouble((byte)Convert.ToChar(value));
                }
                else
                {
                    MathObject = Convert.ToDouble(value);
                }
                return;
            }
        }

        public T GetRealValue()
        {
            return (T)Convert.ChangeType(MathObject.RealNumberValue, typeof(T));
        }


        public override object GetValueObject()
        {
            return MathObject.RealNumberValue;
        }

        public override bool IsValuable
        {
            get
            {
                return MathObject.Expression.IsNumber || MathObject.Expression.IsApproximation;
            }
            set => throw new NotImplementedException();
        }

        public override string ToExpressionString()
        {
            if (IsValuable)
            { 
                return GetRealValue().ToString();
            }

            return MathObject.ToString();
        }

        internal override Expr GetSymExp()
        {
            return this.MathObject;
        }

        public override string ToValueString()
        {
            if (IsValuable)
                return ToExpressionString();
            else
                throw new Exception("Not Valuable");
        }

    }
}