using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Text;
using MathNet.Symbolics;
using PowderShell.EvaluationObjects;

namespace PowderShell
{
    public abstract class DMathExpression : DExpression
    {
        protected DMathExpression()
        {
        }

        public SymbolicExpression MathObject { get; set; } = 0;
        public abstract object GetValueObject();


        public static bool IsNumericType(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValuableConstant(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.DateTime:
                case TypeCode.Char:
                    return true;
                default:
                    return false;
            }
        }

        public static DMathExpression Get(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte: return new DMathExpression<Byte>((Byte)o);
                case TypeCode.SByte: return new DMathExpression<SByte>((SByte)o);
                case TypeCode.UInt16: return new DMathExpression<UInt16>((UInt16)o);
                case TypeCode.UInt32: return new DMathExpression<UInt32>((UInt32)o);
                case TypeCode.UInt64: return new DMathExpression<UInt64>((UInt64)o);
                case TypeCode.Int16: return new DMathExpression<Int16>((Int16)o);
                case TypeCode.Int32: return new DMathExpression<Int32>((Int32)o);
                case TypeCode.Int64: return new DMathExpression<Int64>((Int64)o);
                case TypeCode.Decimal: return new DMathExpression<Decimal>((Decimal)o);
                case TypeCode.Double: return new DMathExpression<Double>((Double)o);
                case TypeCode.Single: return new DMathExpression<Single>((Single)o);
                case TypeCode.Boolean: return new DBoolExpression((Boolean)o);
                case TypeCode.DateTime: return new DDateTimeExpression((DateTime)o);
                case TypeCode.Char: return new DCharExpression((Char)o);
                default:
                    return null;
            }
        }

    }
}