using FParsec;
using Markdig.Helpers;
using PowderShell.EvaluationObjects;
using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;

namespace PowderShell
{
    public class PipelineContext
    {
        public VariableExpressionAstWrapper ContextVariable { get; set; }
        public DExpression CurrentExpression { get; set; }
    }

    public static class PowderShellOptions
    {
        public static bool TagUnmanagedAst { get; set; } = true;
    }

    [Obsolete]
    public abstract class AstWrapper
    {
        private PipelineContext pipelineContext;

        public PipelineContext PipelineContext
        {
            get
            {
                if (pipelineContext != null)
                    return pipelineContext;

                if (Parent == null)
                    return null;
                else
                    return Parent.PipelineContext;
            }

            set
            {
                pipelineContext = value;
            }
        }


        public AstWrapper Parent { get; set; }



        public Ast BaseAst { get; set; }

        [Obsolete]
        public AstWrapper(AstWrapper Parent, Ast ast)
        {
            this.Parent = Parent;
        }

        public override string ToString()
        {
            if (CanEvaluate())
                return Evaluate().ToExpressionString();

            return Prettify();
        }

        public virtual bool CanEvaluate()
        {
            return false;
        }

        public abstract string Prettify();

        public virtual DExpression Evaluate()
        {
            return new DCodeBlock(ToString());
        }

        public static string GetOperatorStrFrom(TokenKind tokenKind)
        {
            var index = GlobalCollections.s_operatorTokenKind.IndexOf(tokenKind);
            if (index == -1)
            {
                return tokenKind.ToString();
            }
            else
            {

                return GlobalCollections._operatorSymbols[index];
            }
        }

        [Obsolete]
        internal string NotImplementedPrettify()
        {
            if (PowderShellOptions.TagUnmanagedAst)
                return $"[Unmanaged({BaseAst.GetType().Name})] {BaseAst}";
            else
                return $"{BaseAst}";
        }
    }

    [Obsolete]
    public abstract class AstWrapper<T> : AstWrapper
        where T : Ast
    {

        public T TypedAst { get { return (T)BaseAst; } }

        [Obsolete]
        public AstWrapper(AstWrapper Parent, T ast)
            : base(Parent, ast)
        {
            BaseAst = ast ?? throw new ArgumentNullException(nameof(ast));
        }

        public override abstract string Prettify();

    }

    public class AttributedExpressionAstWrapper : ExpressionAstWrapper
    {
        public ExpressionAstWrapper Child { get; }
        public AttributeBaseAstWrapper Attribute { get; }

        public AttributedExpressionAstWrapper(AstWrapper Parent, AttributedExpressionAst ast)
            : base(Parent, ast)
        {
            Child = Get(this, ast.Child);
            Attribute = AttributeBaseAstWrapper.Get(this, ast.Attribute);
        }

        public static AttributedExpressionAstWrapper Get(AstWrapper Parent, AttributedExpressionAst ast)
        {
            if (ast is ConvertExpressionAst)
                return new ConvertExpressionAstWrapper(Parent, ast as ConvertExpressionAst);

            return new AttributedExpressionAstWrapper(Parent, ast);
        }

        public override string Prettify()
        {
            return $"{Attribute} {Child}";
        }
    }

    public class TypeExpressionAstWrapper : ExpressionAstWrapper
    {
        public ITypeName TypeName { get; }
        public Type? ReflectionType { get; }

        public TypeExpressionAstWrapper(AstWrapper Parent, TypeExpressionAst ast) 
            : base(Parent, ast)
        {
            TypeName = ast.TypeName;

            ReflectionType = TypeName.GetReflectionType();
        }

        public override bool CanEvaluate()
        {
            if (ReflectionType == null)
                return false;

            if (ReflectionType.Name.Equals("Convert", StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public override DExpression Evaluate()
        {
            return new DCodeBlock(Prettify());
        }

        public override string Prettify()
        {
            return $"[{ReflectionType?.Name ?? TypeName.Name}]";
        }
    }

    public class MemberExpressionAstWrapper : ExpressionAstWrapper
    {
        public string MemberName { get; private set; }

        private MemberInfo[] reflectedMembers;

        public ExpressionAstWrapper Expression { get; }
        public bool NullConditional { get; private set; }
        public CommandElementAstWrapper Member { get; }
        public bool Static { get; }

        public void LoadMemberInfo()
        {
            if (reflectedMembers == null)
                if (Expression is TypeExpressionAstWrapper)
                {
                    var expTypeAst = (Expression as TypeExpressionAstWrapper);
                    ReflectedType = expTypeAst.ReflectionType;

                    if (ReflectedType != null)
                    {
                        if (Member is ConstantExpressionAstWrapper)
                        {
                            var expMember = Member as ConstantExpressionAstWrapper;
                            MemberName = expMember.Evaluate().ToValueString();
                            reflectedMembers = ReflectedType.GetMember(MemberName, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
                            //(0).ToString();
                        }
                    }
                }

        }

        public Type? ReflectedType { get; private set; }

        public MemberExpressionAstWrapper(AstWrapper Parent, MemberExpressionAst ast) : base(Parent, ast)
        {
            if (ast.Expression != null)
                Expression = ExpressionAstWrapper.Get(this, ast.Expression);


            if (ast.Member != null)
                Member = CommandElementAstWrapper.Get(this, ast.Member);



            NullConditional = ast.NullConditional;
            Static = ast.Static;
        }

        public static MemberExpressionAstWrapper Get(AstWrapper Parent, MemberExpressionAst ast)
        {
            if (ast is InvokeMemberExpressionAst)
            {
                return new InvokeMemberExpressionAstWrapper(Parent, ast as InvokeMemberExpressionAst);
            }

            return new MemberExpressionAstWrapper(Parent, ast);
        }


        public override string Prettify()
        {
            string nullConditionalOperator = NullConditional ? "?." : ".";
            string staticOperator = Static ? "::" : nullConditionalOperator;
            return $"{Expression}{staticOperator}{Member}";
        }

    }

    public class InvokeMemberExpressionAstWrapper : MemberExpressionAstWrapper
    {
        public InvokeMemberExpressionAstWrapper(AstWrapper parent, InvokeMemberExpressionAst ast)
            : base(parent, ast)
        {
            if (ast.Arguments != null)
                Arguments = ast.Arguments.Select(v => ExpressionAstWrapper.Get(this, v)).ToList();
            if (ast.GenericTypeArguments != null)
                GenericTypeArguments = ast.GenericTypeArguments;
        }

        public List<ExpressionAstWrapper> Arguments { get; }
        public ReadOnlyCollection<ITypeName> GenericTypeArguments { get; }


        public override bool CanEvaluate()
        {
            LoadMemberInfo();
            if (this.GenericTypeArguments != null && this.GenericTypeArguments.Count > 0)
                return false;

            if (!this.Member.CanEvaluate())
                return false;

            if (!this.Expression.CanEvaluate())
                return false;

            bool argumentsEvaluable = Arguments.All(a => a.CanEvaluate());
            if (!argumentsEvaluable)
                return false;


            string memberName = Member.Evaluate().ToValueString().ToLower();

            if (this.Static)
            {
                //LoadMemberInfo

                //return false;
                
                return true;
            }


            switch (memberName)
            {
                case "replace":
                    return true;

                    break;
                case "join":
                    return true;
                default:
                    return false;
            }

        }

        public override DExpression Evaluate()
        {
            LoadMemberInfo();

            if (!CanEvaluate())
                return new DCodeBlock(Prettify());

            if (this.Static)
            {
                try
                {
                    switch (this.ReflectedType?.Name)
                    {
                        // We only authorize "Convert" class to be called there
                        case "Convert":
                            break;
                        default:
                            return new DCodeBlock(Prettify());
                    }

                    List<Object?> values = new List<Object?>();

                    DExpression[] arguments = Arguments.Select(v => v.Evaluate()).ToArray();

                    foreach (var arg in arguments)
                    {
                        switch (arg)
                        {
                            case DBoolExpression boolExpr:
                                values.Add(boolExpr.GetRealValue());
                                break;

                            case DCharExpression charExpr:
                                values.Add(charExpr.GetRealValue());
                                break;

                            case DCodeBlock codeBlock:
                                return new DCodeBlock(Prettify());
                                break;

                            case DComplexStringExpression complexStrExpr:
                                return new DCodeBlock(Prettify());
                                break;

                            case DDateTimeExpression dateTimeExpr:
                                values.Add(dateTimeExpr.GetRealValue());
                                break;

                            case DEmptyVariable emptyVar:
                                return new DCodeBlock(Prettify());
                                break;

                            case DEnumerableExpression enumerableExpr:
                                return new DCodeBlock(Prettify());
                                break;


                            case DMathExpression<byte> mathExprByte:
                                values.Add(mathExprByte.GetRealValue());
                                break;

                            case DMathExpression<sbyte> mathExprSByte:
                                values.Add(mathExprSByte.GetRealValue());
                                break;

                            case DMathExpression<ushort> mathExprUInt16:
                                values.Add(mathExprUInt16.GetRealValue());
                                break;

                            case DMathExpression<uint> mathExprUInt32:
                                values.Add(mathExprUInt32.GetRealValue());
                                break;

                            case DMathExpression<ulong> mathExprUInt64:
                                values.Add(mathExprUInt64.GetRealValue());
                                break;

                            case DMathExpression<short> mathExprInt16:
                                values.Add(mathExprInt16.GetRealValue());
                                break;

                            case DMathExpression<int> mathExprInt32:
                                values.Add(mathExprInt32.GetRealValue());
                                break;

                            case DMathExpression<long> mathExprInt64:
                                values.Add(mathExprInt64.GetRealValue());
                                break;

                            case DMathExpression<decimal> mathExprDecimal:
                                values.Add(mathExprDecimal.GetRealValue());
                                break;

                            case DMathExpression<double> mathExprDouble:
                                values.Add(mathExprDouble.GetRealValue());
                                break;

                            case DMathExpression<float> mathExprSingle:
                                values.Add(mathExprSingle.GetRealValue());
                                break;

                            case DSimpleStringExpression stringExpr:
                                values.Add(stringExpr.ToValueString());
                                break;


                            case DUndefinedVariable undefinedVar:
                                return new DCodeBlock(Prettify());
                                break;

                            default:
                                return new DCodeBlock(Prettify());
                                break;
                        }
                    }

                    var agrTypes = values.Select(v => v.GetType()).ToArray();
                    var methodRef = ReflectedType.GetMethod(
                        MemberName,
                        BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public,
                        agrTypes);

                    if (methodRef == null)
                        return new DCodeBlock(Prettify());

                    var result = methodRef.Invoke(null, values.ToArray());

                    if (DMathExpression.IsNumericType(result))
                    {
                        return DMathExpression.Get(result);
                    }

                    if (result is string)
                    {
                        return new DSimpleStringExpression((string)result, Encoding.Unicode);
                    }

                    switch (result)
                    {

                    }
                }
                catch
                {
                    return new DCodeBlock(Prettify());
                }
                (0).ToString();
                //arguments[0].

                //LoadMemberInfo

                //return false;
            }

            string memberName = Member.Evaluate().ToValueString().ToLower();

            switch (memberName)
            {
                case "join":
                    //(0);
                    break;
                case "replace":
                    DExpression dExpression = Expression.Evaluate();
                    DExpression[] arguments = Arguments.Select(v => v.Evaluate()).ToArray();

                    if (!(dExpression is DSimpleStringExpression))
                        return new DCodeBlock(Prettify());

                    string sResult = dExpression.ToValueString();
                    string p1 = arguments[0].ToValueString();
                    string p2 = arguments[1].ToValueString();

                    sResult = sResult.Replace(p1, p2);

                    return new DSimpleStringExpression(sResult, Encoding.Unicode);
            }

            return new DCodeBlock(Prettify());
        }

        public override string Prettify()
        {
            string args =
                Arguments == null ?
                "" :
                string.Join(", ", Arguments.Select(arg => arg.ToString()));

            string generics = "";
            if (GenericTypeArguments != null && GenericTypeArguments.Count > 0)
            {
                generics = $"<{string.Join(", ", GenericTypeArguments.Select(g => g.FullName))}>";
            }

            return $"{base.Prettify()}{generics}({args})";
        }

    }

    public class ConvertExpressionAstWrapper : AttributedExpressionAstWrapper
    {
        public TypeConstraintAstWrapper Type { get; }

        public ConvertExpressionAstWrapper(AstWrapper parent, ConvertExpressionAst ast) : base(parent, ast)
        {
            Type = new TypeConstraintAstWrapper(this, ast.Type);
        }

        public override bool CanEvaluate()
        {
            string typeName = Type.TypeName.Name.ToLower();

            if (!this.Child.CanEvaluate())
                return false;

            DExpression valueExp = this.Child.Evaluate();

            if (!valueExp.IsValuable)
                return false;

            if (typeName == "string")
            {
                return true;
            }

            if (valueExp is DMathExpression mathExp)
            {
                double realNumber = mathExp.GetSymExp().RealNumberValue;

                switch (typeName)
                {
                    case "char":
                    case "int":
                    case "uint":
                    case "long":
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }

        public override DExpression Evaluate()
        {
            string typeName = Type.TypeName.Name.ToLower();

            if (!this.Child.CanEvaluate())
                return new DCodeBlock(Prettify());

            DExpression valueExp = this.Child.Evaluate();

            if (!valueExp.IsValuable)
                return new DCodeBlock(Prettify());

            if (typeName == "string")
            {
                return new DSimpleStringExpression(valueExp.ToValueString(), Encoding.Unicode);
            }

            if (valueExp is DMathExpression mathExp)
            {
                double realNumber = mathExp.GetSymExp().RealNumberValue;

                switch (typeName)
                {
                    case "char":
                        return DMathExpression.Get((char)(int)realNumber);
                    case "int":
                        return DMathExpression.Get((int)realNumber);
                    case "uint":
                        return DMathExpression.Get((uint)realNumber);
                    case "long":
                        return DMathExpression.Get((long)realNumber);
                    default:
                        return new DCodeBlock(Prettify());
                }
            }
            return new DCodeBlock(Prettify());
        }


        public override string Prettify()
        {
            return $"{Type} {Child}";
        }

    }

    public class ParamBlockAstWrapper : AstWrapper<ParamBlockAst>
    {
        public List<AttributeAstWrapper>? Attributes { get; }
        public List<ParameterAstWrapper>? Parameters { get; }

        public ParamBlockAstWrapper(AstWrapper Parent, ParamBlockAst ast)
            : base(Parent, ast)
        {

            Parameters = ast.Parameters?.Select(v => new ParameterAstWrapper(this, v))?.ToList();
            Attributes = ast.Attributes?.Select(v => new AttributeAstWrapper(this, v))?.ToList();
            (0).ToString();
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            if (Attributes != null && Attributes.Count > 0)
            {
                foreach (var attribute in Attributes)
                {
                    result.AppendLine(attribute.ToString());
                }
            }

            result.Append("param(");

            if (Parameters != null && Parameters.Count > 0)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    result.Append(Parameters[i].ToString());
                    if (i < Parameters.Count - 1)
                    {
                        result.Append(", ");
                    }
                }
            }

            result.Append(")");

            return result.ToString();
        }

    }

    public class StringConstantExpressionAstWrapper : ConstantExpressionAstWrapper
    {
        public string Value { get; set; }
        public StringConstantType StringConstantType { get; }

        public StringConstantExpressionAstWrapper(AstWrapper parent, StringConstantExpressionAst ast)
            : base(parent, ast)
        {
            Value = ast.Value;
            StringConstantType = ast.StringConstantType;
        }

        public override DExpression Evaluate()
        {
            return new DSimpleStringExpression(Value, Encoding.Unicode);
        }

        public override string Prettify()
        {
            switch (StringConstantType)
            {
                case StringConstantType.DoubleQuoted:
                    return $"\"{Value}\""; // Wrap the mathExp in double quotes
                case StringConstantType.SingleQuoted:
                    return $"'{Value}'"; // Wrap the mathExp in single quotes
                case StringConstantType.BareWord:
                    return Value; // No quotes
                case StringConstantType.SingleQuotedHereString:
                    return $"@'\n{Value}\n'@"; // Single-quoted here-string
                case StringConstantType.DoubleQuotedHereString:
                    return $"@\"\n{Value}\n\"@"; // Double-quoted here-string
                default:
                    return base.ToString();
            }
        }
    }

    public class ConstantExpressionAstWrapper : ExpressionAstWrapper
    {
        public object Value { get; }

        internal ConstantExpressionAstWrapper(AstWrapper parent, ConstantExpressionAst ast)
            : base(parent, ast)
        {
            Value = ast.Value;
        }

        public static ConstantExpressionAstWrapper Get(AstWrapper parent, ConstantExpressionAst ast)
        {
            if (ast is StringConstantExpressionAst)
                return new StringConstantExpressionAstWrapper(parent, ast as StringConstantExpressionAst);

            return new ConstantExpressionAstWrapper(parent, ast);
        }


        public override string Prettify()
        {
            return Value.ToString();
        }

        public override bool CanEvaluate()
        {
            return true;
        }

        public override DExpression Evaluate()
        {
            DMathExpression dMathExpression = DMathExpression.Get(Value);

            if (dMathExpression == null)
                return base.Evaluate();

            return dMathExpression;
        }

    }

    public class ContinueStatementAstWrapper : StatementAstWrapper
    {

        public ContinueStatementAstWrapper(AstWrapper parent, ContinueStatementAst ast)
            : base(parent, ast)
        {
            if (ast.Label != null)
                Label = ExpressionAstWrapper.Get(parent, ast.Label);
        }

        public ExpressionAstWrapper Label { get; }

        public override string Prettify()
        {
            if (Label != null)
            {
                return $"continue {Label}";
            }
            else
            {
                return "continue";
            }
        }
    }

    public class ThrowStatementAstWrapper : StatementAstWrapper
    {
        public bool IsRethrow { get; }
        public PipelineBaseAstWrapper Pipeline { get; }

        public ThrowStatementAstWrapper(AstWrapper parent, ThrowStatementAst ast)
            : base(parent, ast)
        {
            IsRethrow = ast.IsRethrow;
            Pipeline = PipelineBaseAstWrapper.Get(this, ast.Pipeline);
        }

        public override string Prettify()
        {
            if (IsRethrow)
            {
                return "throw";
            }
            else
            {
                return $"throw {Pipeline}";
            }
        }
    }

    public class ForStatementAstWrapper : LoopStatementAstWrapper
    {
        public PipelineBaseAstWrapper Initializer { get; }
        public PipelineBaseAstWrapper Iterator { get; }

        public ForStatementAstWrapper(AstWrapper parent, ForStatementAst ast)
            : base(parent, ast)
        {
            Initializer = PipelineBaseAstWrapper.Get(this, ast.Initializer);
            Iterator = PipelineBaseAstWrapper.Get(this, ast.Iterator);
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine();
            result.Append("for (");

            if (Initializer != null)
            {
                result.Append(Initializer.ToString());
            }

            result.Append("; ");

            if (Condition != null)
            {
                result.Append(Condition.ToString());
            }

            result.Append("; ");

            if (Iterator != null)
            {
                result.Append(Iterator.ToString());
            }

            result.Append(") ").Append(Body.ToString());
            result.AppendLine();

            return result.ToString();
        }
    }

    public class ForEachStatementAstWrapper : LoopStatementAstWrapper
    {
        public ExpressionAstWrapper ThrottleLimit { get; }
        public VariableExpressionAstWrapper Variable { get; }
        public ForEachFlags Flags { get; }

        public ForEachStatementAstWrapper(AstWrapper parent, ForEachStatementAst ast)
            : base(parent, ast)
        {
            if (ast.ThrottleLimit != null)
                ThrottleLimit = ExpressionAstWrapper.Get(this, ast.ThrottleLimit);

            if (ast.Variable != null)
                Variable = new VariableExpressionAstWrapper(this, ast.Variable);

            Flags = ast.Flags;
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine();
            result.Append("foreach ");

            if (Flags.HasFlag(ForEachFlags.Parallel))
            {
                result.Append("-Parallel ");
            }

            result.Append("(");

            if (ThrottleLimit != null)
            {
                result.Append("ThrottleLimit = ").Append(ThrottleLimit).Append("; ");
            }

            result.Append(Variable).Append(" in ").Append(Condition)
                  .Append(") ")
                  .Append(Body.ToString());

            result.AppendLine();

            return result.ToString();
        }
    }

    public class DoWhileStatementAstWrapper : LoopStatementAstWrapper
    {
        public PipelineBaseAstWrapper Condition { get; }

        public DoWhileStatementAstWrapper(AstWrapper parent, DoWhileStatementAst ast)
            : base(parent, ast)
        {
            Condition = PipelineBaseAstWrapper.Get(this, ast.Condition);
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();


            result
                .AppendLine()
                .Append("do ")
                .Append(Body.ToString())
                .Append(" while (").Append(Condition.ToString()).Append(")")
                .AppendLine();

            return result.ToString();
        }
    }


    public class DoUntilStatementAstWrapper : LoopStatementAstWrapper
    {
        public PipelineBaseAstWrapper Condition { get; }

        public DoUntilStatementAstWrapper(AstWrapper parent, DoUntilStatementAst ast)
            : base(parent,ast)
        {
            Condition = PipelineBaseAstWrapper.Get(this, ast.Condition);
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            result
                .AppendLine()
                .Append("do ")
                .Append(Body.ToString())
                .Append("until (").Append(Condition.ToString()).Append(")")
                .AppendLine();

            return result.ToString();
        }
    }

    public class WhileStatementAstWrapper : LoopStatementAstWrapper
    {
        public PipelineBaseAstWrapper Condition { get; }

        public WhileStatementAstWrapper(AstWrapper parent, WhileStatementAst ast)
            : base(parent, ast)
        {
            Condition = PipelineBaseAstWrapper.Get(this, ast.Condition);
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            result
                .AppendLine()
                .Append("while (").Append(Condition.ToString()).Append(") ")
                .Append(Body.ToString())
                .AppendLine();

            return result.ToString();
        }
    }

    public abstract class LoopStatementAstWrapper : LabeledStatementAstWrapper
    {
        public LoopStatementAstWrapper(AstWrapper parent, LoopStatementAst ast)
            : base(parent, ast)
        {
            Body = new StatementBlockAstWrapper(this, ast.Body);
        }

        public StatementBlockAstWrapper Body { get; }

        public static LoopStatementAstWrapper Get(AstWrapper parent, LoopStatementAst ast)
        {
            if (ast is ForEachStatementAst v1)
                return new ForEachStatementAstWrapper(parent, v1);

            if (ast is ForStatementAst v2)
                return new ForStatementAstWrapper(parent, v2);

            if (ast is DoWhileStatementAst v3)
                return new DoWhileStatementAstWrapper(parent, v3);

            if (ast is DoUntilStatementAst v4)
                return new DoUntilStatementAstWrapper(parent, v4);

            if (ast is WhileStatementAst v5)
                return new WhileStatementAstWrapper(parent, v5);

            //return new LoopStatementAstWrapper(parent, ast);
            throw new NotImplementedException();
        }
    }

    //TODO: Improve Prettify function
    public class SwitchStatementAstWrapper : LabeledStatementAstWrapper
    {
        public Tuple<ExpressionAstWrapper, StatementBlockAstWrapper>[] Clauses { get; }
        public StatementBlockAstWrapper Default { get; }

        public SwitchStatementAstWrapper(AstWrapper parent, SwitchStatementAst ast)
            : base(parent, ast)
        {
            Clauses = ast.Clauses.Select(v => new Tuple<ExpressionAstWrapper, StatementBlockAstWrapper>(ExpressionAstWrapper.Get(this, v.Item1), new StatementBlockAstWrapper(this, v.Item2))).ToArray();
            Default = new StatementBlockAstWrapper(this, ast.Default);
        }

        public override string Prettify()
        {
            return NotImplementedPrettify();
        }

    }



    public abstract class LabeledStatementAstWrapper : StatementAstWrapper
    {
        public string Label { get; }
        public PipelineBaseAstWrapper Condition { get; }

        public LabeledStatementAstWrapper(AstWrapper parent, LabeledStatementAst ast)
            : base(parent,ast)
        {
            Label = ast.Label;
            Condition = PipelineBaseAstWrapper.Get(this, ast.Condition);
        }

        public static LabeledStatementAstWrapper Get(AstWrapper parent, LabeledStatementAst ast)
        {
            if (ast is LoopStatementAst ast2)
                return LoopStatementAstWrapper.Get(parent, ast2);

            if (ast is SwitchStatementAst ast3)
                return new SwitchStatementAstWrapper(parent, ast3);

            throw new NotImplementedException(ast.GetType().Name + " has no implemented PowderShell wrapper.");
        }
    }

    public class TrapStatementAstWrapper : StatementAstWrapper
    {
        public TypeConstraintAstWrapper TrapType { get; }
        public StatementBlockAstWrapper Body { get; }

        public TrapStatementAstWrapper(AstWrapper parent, TrapStatementAst ast)
            : base(parent, ast)
        {
            TrapType = new TypeConstraintAstWrapper(this, ast.TrapType);
            Body = new StatementBlockAstWrapper(this, ast.Body);
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();
            result
                .AppendLine()
                .AppendLine($"trap {TrapType}")
                .AppendLine(Body.ToString());

            return result.ToString();
        }
    }

    public class ScriptBlockExpressionAstWrapper : ExpressionAstWrapper
    {
        public ScriptBlockAstWrapper ScriptBlock { get; }

        public ScriptBlockExpressionAstWrapper(AstWrapper parent, ScriptBlockExpressionAst ast)
            : base(parent, ast)
        {
            ScriptBlock = new ScriptBlockAstWrapper(this, ast.ScriptBlock);
        }

        public override bool CanEvaluate()
        {
            return ScriptBlock.CanEvaluate();
        }

        public override DExpression Evaluate()
        {
            return ScriptBlock.Evaluate();
        }

        public override string Prettify()
        {
            if (CanEvaluate())
                return Evaluate().ToExpressionString();

            return ScriptBlock.ToString();
        }

    }


    public class VariableExpressionAstWrapper : ExpressionAstWrapper
    {
        public bool Splatted { get; }
        public VariablePath VariablePath { get; }

        public VariableExpressionAstWrapper(AstWrapper parent, VariableExpressionAst ast)
            : base(parent, ast)
        {
            Splatted = ast.Splatted;
            VariablePath = ast.VariablePath;

            if (VariablePath.ToString() == "_")
            {
                PipelineContext.ContextVariable = this;
            }
        }

        public override bool CanEvaluate()
        {
            if (PipelineContext?.CurrentExpression == null)
                return false;

            return PipelineContext.CurrentExpression.IsValuable;
        }

        public override DExpression Evaluate()
        {
            if (CanEvaluate())
                return PipelineContext.CurrentExpression;

            return new DCodeBlock(PipelineContext.ContextVariable.ToString());
        }


        public override string Prettify()
        {
            if (CanEvaluate())
                return Evaluate().ToExpressionString();

            if (Splatted)
            {
                return $"@{VariablePath}";
            }
            else
            {
                return $"${VariablePath}";
            }
        }

    }

    public class ParenExpressionAstWrapper : ExpressionAstWrapper
    {
        public PipelineBaseAstWrapper Pipeline { get; }

        public ParenExpressionAstWrapper(AstWrapper parent, ParenExpressionAst ast)
            : base(parent, ast)
        {
            Pipeline = PipelineBaseAstWrapper.Get(this, ast.Pipeline);
        }

        public override bool CanEvaluate()
        {
            return Pipeline.CanEvaluate();
        }

        public override DExpression Evaluate()
        {
            if (CanEvaluate())
                return Pipeline.Evaluate();

            return new DCodeBlock($"({Pipeline})");
        }

        public override string Prettify()
        {
            if (CanEvaluate())
                return Evaluate().ToExpressionString();

            return $"({Pipeline})";
        }
    }

    public class ArrayLiteralAstWrapper : ExpressionAstWrapper
    {
        public ArrayLiteralAstWrapper(AstWrapper parent, ArrayLiteralAst ast) : base(parent, ast)
        {
            Elements = ast.Elements.Select(v => Get(this, v)).ToList();

        }

        public override bool CanEvaluate()
        {
            return Elements.All(e => e.CanEvaluate());
        }

        public override DExpression Evaluate()
        {
            return new DEnumerableExpression(Elements.Select(v => v.Evaluate()));
        }

        public List<ExpressionAstWrapper> Elements { get; }

        public override string Prettify()
        {
            return $"@({string.Join(", ", Elements)})";
        }

    }

    public class SubExpressionAstWrapper : ExpressionAstWrapper
    {
        public SubExpressionAstWrapper(AstWrapper parent, SubExpressionAst ast) : base(parent,ast)
        {
            SubExpression = new StatementBlockAstWrapper(this, ast.SubExpression);
        }

        public StatementBlockAstWrapper SubExpression { get; }

        public override bool CanEvaluate()
        {
            return SubExpression.CanEvaluate();
        }

        public override DExpression Evaluate()
        {
            return SubExpression.Evaluate();
        }

        public override string Prettify()
        {
            if (CanEvaluate())
                return SubExpression.Evaluate().ToExpressionString();

            return $"$({SubExpression})";
        }
    }

    public class IndexExpressionAstWrapper : ExpressionAstWrapper
    {
        public bool NullConditional { get; }
        public ExpressionAstWrapper Index { get; }
        public ExpressionAstWrapper Target { get; }

        public IndexExpressionAstWrapper(AstWrapper parent, IndexExpressionAst ast) : base(parent, ast)
        {
            NullConditional = ast.NullConditional;
            Index = ExpressionAstWrapper.Get(this, ast.Index);
            Target = ExpressionAstWrapper.Get(this, ast.Target);
            (0).ToString();
        }

        public override bool CanEvaluate()
        {
            return Index.CanEvaluate() && Target.CanEvaluate();
        }

        public override DExpression Evaluate()
        {
            //return new DCodeBlock(GetStringExp());
            try
            {
                DExpression indexExpression = Index.Evaluate();
                DExpression targetExpression = Target.Evaluate();
                List<DExpression> result = new();

                if (targetExpression is IStringExpression)
                {
                    var charArray = targetExpression.ToValueString().ToCharArray();

                    if (indexExpression is DEnumerableExpression)
                    {
                        DEnumerableExpression dIndexes = (DEnumerableExpression)indexExpression;

                        foreach (var x in dIndexes.Elements)
                        {
                            var idx = (int)x.GetSymExp().RealNumberValue;

                            char element = charArray[idx];

                            result.Add(new DMathExpression<char>(element));
                        }
                        return new DEnumerableExpression(result);
                    }
                    else
                    {
                        var idx = (int)indexExpression.GetSymExp().RealNumberValue;

                        char element = charArray[idx];

                        return new DMathExpression<char>(element);
                    }

                }

                if (targetExpression is DEnumerableExpression)
                {
                    var targetEnum = targetExpression as DEnumerableExpression;

                    if (indexExpression is DEnumerableExpression)
                    {
                        DEnumerableExpression dIndexes = (DEnumerableExpression)indexExpression;

                        foreach (var x in dIndexes.Elements)
                        {
                            var idx = (int)x.GetSymExp().RealNumberValue;

                            var expElement = targetEnum.Elements.ElementAt(idx);

                            result.Add(expElement);
                        }

                        return new DEnumerableExpression(result);
                    }
                    else
                    {
                        var idx = (int)indexExpression.GetSymExp().RealNumberValue;

                        var expElement = targetEnum.Elements.ElementAt(idx);

                        return expElement;
                    }
                }
            }
            catch { }

            return new DCodeBlock(GetStringExp());
        }

        public override string Prettify()
        {
            return GetStringExp();
        }

        private string GetStringExp()
        {
            string nullConditionalOperator = NullConditional ? "?" : "";
            return $"{Target}{nullConditionalOperator}[{Index}]";
        }
    }

    public class UnaryExpressionAstWrapper : ExpressionAstWrapper
    {
        public TokenKind TokenKind { get; }
        public ExpressionAstWrapper Child { get; }

        public UnaryExpressionAstWrapper(AstWrapper parent, UnaryExpressionAst ast) : base(parent, ast)
        {
            TokenKind = ast.TokenKind;
            Child = ExpressionAstWrapper.Get(this, ast.Child);

        }

        public override bool CanEvaluate()
        {
            if (TokenKind == TokenKind.Join)
            {
                if (Child.CanEvaluate())
                {
                    return true;
                }
            }
            return false;
        }

        public override DExpression Evaluate()
        {
            if (!CanEvaluate())
                return new DCodeBlock(Prettify());

            switch (TokenKind)
            {
                case TokenKind.Join:
                    DExpression childEv = Child.Evaluate();

                    if (childEv is DEnumerableExpression)
                    {
                        var enumExp = (DEnumerableExpression)childEv;
                        if (enumExp.Elements.All(e => e.IsValuable))
                        {
                            char[] resultEnum = enumExp.Elements.Select(v => (char)(byte)(v.GetSymExp().RealNumberValue)).ToArray();
                            return new DSimpleStringExpression(new string(resultEnum), System.Text.Encoding.Unicode);
                            (0).ToString();
                        }
                    }
                    break;
            }
            return new DCodeBlock(Prettify());
        }

        public override string Prettify()
        {
            return Child.Prettify();
        }
    }

    public class BinaryExpressionAstWrapper : ExpressionAstWrapper
    {
        public BinaryExpressionAstWrapper(AstWrapper parent, BinaryExpressionAst ast) : base(parent, ast)
        {
            Operator = ast.Operator;
            Right = ExpressionAstWrapper.Get(this, ast.Right);
            Left = ExpressionAstWrapper.Get(this, ast.Left);

        }

        public TokenKind Operator { get; }
        public ExpressionAstWrapper Right { get; }
        public ExpressionAstWrapper Left { get; }

        public override bool CanEvaluate()
        {
            return Right.CanEvaluate() & Left.CanEvaluate();
        }

        public override DExpression Evaluate()
        {
                //if (!CanEvaluate())
                //    return base.Evaluate();

                var LeftExp = Left.Evaluate();
                var RightExp = Right.Evaluate();
                string op = GetStringOperatorFromToken();
            //return new DCodeBlock($"{Left} {Operator.ToString().ToLower()} {Right}");


            try
            {
                return Operation.DoOperation(op, LeftExp, RightExp);
            }
            catch
            {
                return new DCodeBlock(Prettify());
            }
        }

        private string GetStringOperatorFromToken()
        {
            string op = null;
            switch (Operator)
            {
                case TokenKind.Plus:
                    op = "+";
                    break;
                case TokenKind.Minus:
                    op = "-";
                    break;
                case TokenKind.Multiply:
                    op = "*";
                    break;
                case TokenKind.Divide:
                    op = "/";
                    break;
                case TokenKind.Xor:
                    op = "-";
                    break;
                case TokenKind.Equals:
                    op = "=";
                    break;
                case TokenKind.DotDot:
                    op = "..";
                    break;
                default:
                    string textOp = null;
                    int idx = -1;
                    if ((idx = GlobalCollections.s_operatorTokenKind.IndexOf(Operator)) >= 0)
                        textOp = GlobalCollections._operatorText[idx];
                    else
                        textOp = Operator.ToString();

                    op = "-" + textOp;
                    break;
            }

            return op;
        }

        public override string Prettify()
        {
            string left;
            string right;

            if (Left.CanEvaluate())
                left = Left.Evaluate().ToExpressionString();
            else
                left = Left.Prettify();


            if (Right.CanEvaluate())
                right = Right.Evaluate().ToExpressionString();
            else
                right = Right.Prettify();

            return $"{left} {GetStringOperatorFromToken()} {right}";
        }
    }

    [Obsolete]
    public class UnknownExpressionAstWrapper : ExpressionAstWrapper
    {
        public UnknownExpressionAstWrapper(AstWrapper parent, ExpressionAst ast) : base(parent,ast)
        {

        }

        public override string Prettify()
        {
            return NotImplementedPrettify();
        }
    }

    public abstract class ExpressionAstWrapper : CommandElementAstWrapper
    {
        public Type StaticType { get; }

        internal ExpressionAstWrapper(AstWrapper parent, ExpressionAst ast)
            : base(parent, ast)
        {
            StaticType = ast.StaticType;
        }


        internal static ExpressionAstWrapper Get(AstWrapper parent, ExpressionAst v)
        {
            if (v is VariableExpressionAst) return new VariableExpressionAstWrapper(parent, v as VariableExpressionAst);
            if (v is ErrorExpressionAst) return new UnknownExpressionAstWrapper(parent, v);

            if (v is UnaryExpressionAst uea) return new UnaryExpressionAstWrapper(parent, uea);
            if (v is BinaryExpressionAst) return new BinaryExpressionAstWrapper(parent, v as BinaryExpressionAst);
            if (v is TernaryExpressionAst) return new UnknownExpressionAstWrapper(parent, v);

            if (v is AttributedExpressionAst) return AttributedExpressionAstWrapper.Get(parent, v as AttributedExpressionAst);
            if (v is MemberExpressionAst) return MemberExpressionAstWrapper.Get(parent, v as MemberExpressionAst);
            if (v is TypeExpressionAst) return new TypeExpressionAstWrapper(parent, v as TypeExpressionAst);
            if (v is VariableExpressionAst) return new UnknownExpressionAstWrapper(parent, v);
            if (v is ConstantExpressionAst) return ConstantExpressionAstWrapper.Get(parent, v as ConstantExpressionAst);
            // if (v is StringConstantExpressionAst) return new ExpressionAstWrapper(v);

            if (v is ExpandableStringExpressionAst) return new ExpandableStringExpressionAstWrapper(parent, v as ExpandableStringExpressionAst);
            if (v is ScriptBlockExpressionAst) return new ScriptBlockExpressionAstWrapper(parent, v as ScriptBlockExpressionAst);
            if (v is ArrayLiteralAst) return new ArrayLiteralAstWrapper(parent, v as ArrayLiteralAst);
            if (v is HashtableAst) return new UnknownExpressionAstWrapper(parent, v);
            if (v is ArrayExpressionAst) return new UnknownExpressionAstWrapper(parent, v);
            if (v is ParenExpressionAst) return new ParenExpressionAstWrapper(parent, v as ParenExpressionAst);
            if (v is SubExpressionAst) return new SubExpressionAstWrapper(parent, v as SubExpressionAst);
            if (v is UsingExpressionAst) return new UnknownExpressionAstWrapper(parent, v);
            if (v is IndexExpressionAst) return new IndexExpressionAstWrapper(parent, v as IndexExpressionAst);

            string typeName = v.GetType().Name;
            return new UnknownExpressionAstWrapper(parent, v);
        }

    }

    public class CommandParameterAstWrapper : CommandElementAstWrapper
    {
        public ExpressionAstWrapper Argument { get; }
        public string ParameterName { get; }

        public CommandParameterAstWrapper(AstWrapper parent, CommandParameterAst ast)
            : base(parent, ast)
        {
            if (ast.Argument != null)
                Argument = ExpressionAstWrapper.Get(this, ast.Argument);
            ParameterName = ast.ParameterName;
        }

        public override string Prettify()
        {
            if (Argument != null)
            {
                return $"-{ParameterName}:{Argument}";
            }
            else
            {
                return $"-{ParameterName}";
            }
        }
    }

    public abstract class CommandElementAstWrapper : AstWrapper<CommandElementAst>
    {
        public CommandElementAstWrapper(AstWrapper Parent, CommandElementAst ast)
            : base(Parent, ast)
        {
        }

        internal static CommandElementAstWrapper Get(AstWrapper parent, CommandElementAst v)
        {
            if (v is CommandParameterAst)
            {
                return new CommandParameterAstWrapper(parent, v as CommandParameterAst);
            }
            else if (v is ExpressionAst)
            {
                return ExpressionAstWrapper.Get(parent, v as ExpressionAst);
            }
            //else
            //{
            //    return new CommandElementAstWrapper(parent, v);
            //}
            throw new NotImplementedException();
        }

    }


    public class TypeConstraintAstWrapper : AttributeBaseAstWrapper
    {
        public TypeConstraintAstWrapper(AstWrapper Parent, TypeConstraintAst ast)
        : base(Parent,ast)
        {

        }

        public override string Prettify()
        {
            return $"[{TypeName.FullName}]";
        }
    }



    public class AttributeBaseAstWrapper : AstWrapper<AttributeBaseAst>
    {
        public ITypeName TypeName { get; }

        public AttributeBaseAstWrapper(AstWrapper Parent, AttributeBaseAst ast)
        : base(Parent, ast)
        {
            TypeName = ast.TypeName;
        }

        public override string Prettify()
        {
            return $"[{TypeName.FullName}]";
        }

        internal static AttributeBaseAstWrapper Get(AstWrapper Parent, AttributeBaseAst v)
        {
            if (v is AttributeAst v1)
            {
                return new AttributeAstWrapper(Parent, v1);
            }
            else if (v is TypeConstraintAst v2)
            {
                return new TypeConstraintAstWrapper(Parent, v2);
            }

            throw new NotImplementedException();
        }
    }

    public class ExpandableStringExpressionAstWrapper : ExpressionAstWrapper
    {
        public string Value { get; }
        public StringConstantType StringConstantType { get; }
        public List<ExpressionAstWrapper> NestedExpressions { get; }

        public ExpandableStringExpressionAstWrapper(AstWrapper parent, ExpandableStringExpressionAst ast)
            : base(parent,ast)
        {
            Value = ast.Value;
            StringConstantType = ast.StringConstantType;
            NestedExpressions = ast.NestedExpressions.Select(v => Get(this, v)).ToList();
        }

        public override bool CanEvaluate()
        {
            return NestedExpressions.All(v => v.CanEvaluate());
        }

        public override DExpression Evaluate()
        {
            return new DSimpleStringExpression(GetEvalString(), Encoding.Unicode);
        }

        private string GetEvalString()
        {
            string valResult = Value;

            foreach (var nestedExpression in NestedExpressions)
            {
                if (nestedExpression.CanEvaluate())
                    valResult = valResult.Replace(nestedExpression.BaseAst.ToString(), nestedExpression.Evaluate().ToValueString());
            }

            return valResult;
        }

        public override string Prettify()
        {
            var result = new StringBuilder();
            string valResult = GetEvalString();

            switch (StringConstantType)
            {
                case StringConstantType.SingleQuoted:
                    result.Append('\'').Append(valResult).Append('\'');
                    break;
                case StringConstantType.DoubleQuoted:
                    result.Append('"').Append(valResult).Append('"');
                    break;
                case StringConstantType.BareWord:
                    result.Append(valResult);
                    break;
                case StringConstantType.SingleQuotedHereString:
                    result.Append("@'").Append(valResult).Append("'@");
                    break;
                case StringConstantType.DoubleQuotedHereString:
                    result.Append("@\"").Append(valResult).Append("\"@");
                    break;
                default:
                    throw new NotSupportedException($"Unsupported string constant type: {StringConstantType}");
            }


            return result.ToString();
        }

    }

    public class StatementBlockAstWrapper : AstWrapper<StatementBlockAst>
    {

        public List<StatementAstWrapper>? Statements { get; set; } = new List<StatementAstWrapper>();

        public StatementBlockAstWrapper(AstWrapper parent, StatementBlockAst ast)
            : base(parent, ast)
        {
            var traps = ast.Traps?.ToArray() ?? Array.Empty<TrapStatementAst>();
            var statements = ast.Statements?.ToArray() ?? Array.Empty<TrapStatementAst>();

            var allStmt = traps.Union(statements)
                .OrderBy(v => v.Extent.StartOffset).ToArray();


            foreach (var stmt in allStmt)
            {
                Statements.Add(StatementAstWrapper.Get(this, stmt));
            }

        }

        public override bool CanEvaluate()
        {
            if (Statements.Count == 1)
            {
                return Statements[0].CanEvaluate();
            }
            return false;
        }

        public override DExpression Evaluate()
        {
            if (CanEvaluate())
            {
                return Statements[0].Evaluate();
            }

            return base.Evaluate();
        }

        public override string Prettify()
        {
            if (CanEvaluate())
            {
                return Evaluate().ToExpressionString();
            }

            return PrettifyBlock();
        }

        private string PrettifyBlock()
        {
            StringBuilder result = new StringBuilder();

            if (Statements != null)
            {
                result.AppendLine($"{{");
                foreach (var statement in Statements)
                {
                    result.AppendLine(Helpers.IndentLines(4, statement.ToString()));
                }
                result.AppendLine("}");
            }
            else
            {
                result.AppendLine($"{{ }}");
            }

            return result.ToString();
        }
    }


    public class CatchClauseAstWrapper : AstWrapper<CatchClauseAst>
    {
        public bool IsCatchAll { get; }
        public List<TypeConstraintAstWrapper> CatchTypes { get; }
        public StatementBlockAstWrapper Body { get; }

        public CatchClauseAstWrapper(AstWrapper parent, CatchClauseAst ast) : base(parent, ast)
        {
            IsCatchAll = ast.IsCatchAll;
            CatchTypes = ast.CatchTypes.Select(t => new TypeConstraintAstWrapper(this, t)).ToList();
            Body = new StatementBlockAstWrapper(this, ast.Body);
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            if (IsCatchAll)
            {
                result.Append("catch ");
            }
            else
            {
                result.Append("catch [");

                for (int i = 0; i < CatchTypes.Count; i++)
                {
                    result.Append(CatchTypes[i].ToString());
                    if (i < CatchTypes.Count - 1)
                    {
                        result.Append(", ");
                    }
                }

                result.Append("] ");
            }

            result.Append(Body.ToString());

            return result.ToString();
        }
    }

    public class TryStatementAstWrapper : StatementAstWrapper<TryStatementAst>
    {
        public TryStatementAstWrapper(AstWrapper parent, TryStatementAst ast)
            : base(parent,ast)
        {
            if (ast.Finally != null)
                Finally = new StatementBlockAstWrapper(this, ast.Finally);

            Body = new StatementBlockAstWrapper(this, ast.Body);
            CatchClauses = ast.CatchClauses.Select(v => new CatchClauseAstWrapper(this, v)).ToList();
        }

        public StatementBlockAstWrapper Finally { get; }
        public StatementBlockAstWrapper Body { get; }
        public List<CatchClauseAstWrapper> CatchClauses { get; }


        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine();
            result.Append("try ").Append(Body.ToString());

            foreach (var catchClause in CatchClauses)
            {
                result.Append(catchClause.ToString());
            }

            if (Finally != null)
            {
                result.Append(" finally ").Append(Finally.ToString());
            }
            result.AppendLine();

            return result.ToString();
        }
    }

    public class IfStatementAstWrapper : StatementAstWrapper<IfStatementAst>
    {
        public List<Tuple<PipelineBaseAstWrapper, StatementBlockAstWrapper>>? Clauses { get; }
        public StatementBlockAstWrapper ElseClause { get; }

        public IfStatementAstWrapper(AstWrapper parent, IfStatementAst ast)
            : base(parent, ast)
        {
            Clauses = ast.Clauses?.Select(v => new Tuple<PipelineBaseAstWrapper, StatementBlockAstWrapper>(PipelineBaseAstWrapper.Get(this, v.Item1), new StatementBlockAstWrapper(this, v.Item2))).ToList();

            if (ast.ElseClause != null)
                ElseClause = new StatementBlockAstWrapper(this, ast.ElseClause);
        }


        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            if (Clauses != null)
            {
                for (int i = 0; i < Clauses.Count; i++)
                {
                    if (i == 0)
                    {
                        result.AppendLine();
                        result.Append("if (").Append(Clauses[i].Item1).Append(") ").Append(Clauses[i].Item2);
                    }
                    else
                    {
                        result.Append("elseif (").Append(Clauses[i].Item1).Append(") ").Append(Clauses[i].Item2);
                    }
                    result.AppendLine();
                }
            }

            if (ElseClause != null && ElseClause.Statements.Count > 0)
            {
                result.Append("else ").Append(ElseClause);
            }

            return result.ToString();
        }
    }

    public class RedirectionAstWrapper : AstWrapper<RedirectionAst>
    {
        public RedirectionAstWrapper(AstWrapper Parent, RedirectionAst ast) 
            : base(Parent, ast)
        {
        }

        public override string Prettify()
        {
            return NotImplementedPrettify();
        }
    }

    public class CommandBaseAstWrapper : StatementAstWrapper<CommandBaseAst>
    {
        public List<RedirectionAstWrapper>? Redirections { get; }

        public CommandBaseAstWrapper(AstWrapper parent, CommandBaseAst ast)
            : base(parent, ast)
        {
            Redirections = ast.Redirections?.Select(v => new RedirectionAstWrapper(this, v)).ToList();
        }

        public static CommandBaseAstWrapper Get(AstWrapper parent, CommandBaseAst v)
        {
            if (v is CommandAst commandAst)
            {
                return new CommandAstWrapper(parent, commandAst);
            }
            else if (v is CommandExpressionAst commandExpressionAst)
            {
                return new CommandExpressionAstWrapper(parent, commandExpressionAst);
            }
            else
            {
                return new CommandBaseAstWrapper(parent, v);
            }
        }

        public virtual bool CanEvaluateFromPipeline(CommandBaseAstWrapper arg)
        {
            DExpression test = arg.Evaluate();



            return base.CanEvaluate();
        }

        public virtual DExpression EvaluateFromPipeline(CommandBaseAstWrapper arg)
        {


            return base.Evaluate();
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();
            if (Redirections != null && Redirections.Count > 0)
            {
                foreach (var redirection in Redirections)
                {
                    result.Append(" ").Append(redirection.ToString()).Append(" ");
                }
            }
            return result.ToString();
        }
    }

    public class CommandAstWrapper : CommandBaseAstWrapper
    {
        public List<CommandElementAstWrapper> CommandElements { get; }
        public string CommandOperator { get; }
        public CommandElementAstWrapper Command { get; }
        public TokenKind InvocationOperator { get; }

        public CommandAstWrapper(AstWrapper parent, CommandAst ast)
            : base(parent, ast)
        {
            CommandElements = ast.CommandElements.Select(v => CommandElementAstWrapper.Get(this, v)).ToList();

            CommandOperator = CommandElements.FirstOrDefault()?.ToString();
            Command = CommandElements.Skip(1).FirstOrDefault();

            if (CommandOperator.StartsWith("\""))
                CommandOperator = CommandOperator.Substring(1, CommandOperator.Length - 2);

            if (Helpers.aliasToCommand.ContainsKey(CommandOperator))
            {
                CommandOperator = Helpers.aliasToCommand[CommandOperator];
            }

            InvocationOperator = ast.InvocationOperator;
        }

        public override bool CanEvaluateFromPipeline(CommandBaseAstWrapper arg)
        {
            DExpression dExpression = arg.Evaluate();

            switch (CommandOperator)
            {
                case "ForEach-Object":
                    if (dExpression is DEnumerableExpression)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        public override DExpression EvaluateFromPipeline(CommandBaseAstWrapper arg)
        {
            DExpression dExpression = arg.Evaluate();

            if (!Command.Prettify().Contains("$_"))
                return new DCodeBlock(Prettify());

            switch (CommandOperator)
            {
                case "ForEach-Object":
                    if (dExpression is DEnumerableExpression enumerableExpression)
                    {
                        List<DExpression> newExp = new();
                        foreach (var v in enumerableExpression.Elements)
                        {
                            Command.PipelineContext.CurrentExpression = v;
                            DExpression evaluated = Command.Evaluate();
                            newExp.Add(evaluated);
                            //string newCommand = Command.Replace("$_", v.ToExpressionString());
                            //CommandElementAstWrapper instance = CommandElements[1].GetContextInstance(v);
                            (0).ToString();
                        }
                        Command.PipelineContext.CurrentExpression = null;
                        return new DEnumerableExpression(newExp);
                    }
                    break;
            }

            return new DCodeBlock(Prettify());
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();
            bool simplify = InvocationOperator == TokenKind.Dot
                && CommandElements.Count > 0
                && CommandElements[0].BaseAst is StringConstantExpressionAst;


            // Handle invocation operator
            if (!simplify)
            {
                switch (InvocationOperator)
                {
                    case TokenKind.Dot:
                        result.Append(". ");
                        break;
                    case TokenKind.Ampersand:
                        result.Append("& ");
                        break;
                }
            }

            // Append command elements
            for (int i = 0; i < CommandElements.Count; i++)
            {
                // Remove quotes from command name for simplification
                string elementName = string.Empty;
                if (simplify && i == 0)
                {
                    elementName = CommandElements[i].ToString().Trim('"');
                    result.Append(elementName);
                }
                else
                {
                    elementName = CommandElements[i].ToString();
                }

                if (i == 0)
                {
                    if (elementName.StartsWith("\""))
                        elementName = elementName.Substring(1, elementName.Length - 2);
                    if (Helpers.aliasToCommand.ContainsKey(elementName))
                    {
                        elementName = Helpers.aliasToCommand[elementName];
                    }
                }

                result.Append(elementName);
                result.Append(" ");

                if (i < CommandElements.Count - 1)
                {
                    result.Append(" ");
                }
            }

            return result.ToString().Trim();
        }
    }



    public class CommandExpressionAstWrapper : CommandBaseAstWrapper
    {
        public ExpressionAstWrapper Expression { get; }

        public CommandExpressionAstWrapper(AstWrapper parent, CommandExpressionAst ast)
            : base(parent, ast)
        {
            Expression = ExpressionAstWrapper.Get(this, ast.Expression);
        }

        public override bool CanEvaluate()
        {
            return Expression.CanEvaluate();
        }

        public override DExpression Evaluate()
        {
            return Expression.Evaluate();
        }

        public override string Prettify()
        {
            return Expression.Prettify();
            //string expression = string.Empty;

            //if (CanEvaluate())
            //{
            //    expression = Expression.Evaluate().ToExpressionString();
            //}
            //else
            //{
            //    expression = Expression.ToString();
            //}

            //StringBuilder methodRef = new StringBuilder();

            //methodRef.Append(expression);

            //methodRef.Append(base.ToString()); // Append redirections if any

            //return methodRef.ToString();
        }
    }


    public class AttributeAstWrapper : AttributeBaseAstWrapper
    {
        public List<ExpressionAstWrapper>? PositionalArguments { get; set; } = new List<ExpressionAstWrapper>();
        public List<NamedAttributeArgumentAstWrapper>? NamedArguments { get; }

        public AttributeAstWrapper(AstWrapper Parent, AttributeAst ast)
        : base(Parent, ast)
        {
            NamedArguments = ast.NamedArguments?.Select(v => new NamedAttributeArgumentAstWrapper(this, v))?.ToList();
            PositionalArguments = ast.PositionalArguments?.Select(v => ExpressionAstWrapper.Get(this, v))?.ToList();
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            result.Append($"[{base.TypeName.FullName}");

            if ((PositionalArguments != null && PositionalArguments.Count > 0) || (NamedArguments != null && NamedArguments.Count > 0))
            {
                result.Append("(");

                if (PositionalArguments != null && PositionalArguments.Count > 0)
                {
                    for (int i = 0; i < PositionalArguments.Count; i++)
                    {
                        result.Append(PositionalArguments[i].ToString());
                        if (i < PositionalArguments.Count - 1)
                        {
                            result.Append(", ");
                        }
                    }
                }

                if (NamedArguments != null && NamedArguments.Count > 0)
                {
                    if (PositionalArguments != null && PositionalArguments.Count > 0)
                    {
                        result.Append(", ");
                    }

                    for (int i = 0; i < NamedArguments.Count; i++)
                    {
                        result.Append(NamedArguments[i].ToString());
                        if (i < NamedArguments.Count - 1)
                        {
                            result.Append(", ");
                        }
                    }
                }

                result.Append(")");
            }

            result.Append("]");

            return result.ToString();
        }

    }


    public class NamedAttributeArgumentAstWrapper : AstWrapper<NamedAttributeArgumentAst>
    {
        public bool ExpressionOmitted { get; }
        public ExpressionAstWrapper Argument { get; }
        public string ArgumentName { get; }

        public NamedAttributeArgumentAstWrapper(AstWrapper Parent, NamedAttributeArgumentAst ast)
        : base(Parent, ast)
        {
            ExpressionOmitted = ast.ExpressionOmitted;
            Argument = ExpressionAstWrapper.Get(this, ast.Argument);
            ArgumentName = ast.ArgumentName;

        }

        public override string Prettify()
        {
            if (ExpressionOmitted)
            {
                return ArgumentName;
            }
            else
            {
                return $"{ArgumentName}={Argument}";
            }
        }
    }

    public class ParameterAstWrapper : AstWrapper<ParameterAst>
    {
        public ExpressionAstWrapper DefaultValue { get; }
        public ExpressionAstWrapper Name { get; }
        public Type StaticType { get; }
        public List<AttributeBaseAstWrapper>? Attributes { get; }

        public ParameterAstWrapper(AstWrapper parent, ParameterAst ast)
        : base(parent, ast)
        {

            if (ast.DefaultValue != null)
                DefaultValue = ExpressionAstWrapper.Get(this, ast.DefaultValue);

            if (ast.Name != null)
                Name = ExpressionAstWrapper.Get(this, ast.Name);

            StaticType = ast.StaticType;
            Attributes = ast.Attributes?.Select(v => AttributeBaseAstWrapper.Get(this, v))?.ToList();

        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            if (Attributes != null && Attributes.Count > 0)
            {
                foreach (var attribute in Attributes)
                {
                    result.Append(attribute.ToString()).Append(" ");
                }
            }

            //if (StaticType != null)
            //{
            //    methodRef.Append("[").Append(StaticType.FullName).Append("] ");
            //}

            if (Name != null)
            {
                result.Append(Name.ToString());
            }

            if (DefaultValue != null)
            {
                result.Append(" = ").Append(DefaultValue.ToString());
            }

            return result.ToString();
        }

    }

    public class FunctionDefintionAstWrapper : StatementAstWrapper<FunctionDefinitionAst>
    {
        public string FuncName { get; }
        public bool IsFilter { get; }
        public bool IsWorkflow { get; private set; }
        public List<ParameterAstWrapper>? Parameters { get; set; } = new List<ParameterAstWrapper>();
        public ScriptBlockAstWrapper Body { get; }

        public FunctionDefintionAstWrapper(AstWrapper parent, FunctionDefinitionAst ast)
        : base(parent, ast)
        {
            FuncName = ast.Name;
            IsFilter = ast.IsFilter;
            IsWorkflow = ast.IsWorkflow;

            Parameters = ast.Parameters?.Select(v => new ParameterAstWrapper(this, v))?.ToList();
            Body = new ScriptBlockAstWrapper(this, ast.Body);

        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            if (IsFilter)
                result.Append("filter ");
            else if (IsWorkflow)
                result.Append("workflow ");
            else
                result.Append("function ");

            result.Append(FuncName);
            result.Append("(");

            if (Parameters != null && Parameters.Count > 0)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    result.Append(Parameters[i].ToString());
                    if (i < Parameters.Count - 1)
                    {
                        result.Append(", ");
                    }
                }
            }

            result.Append(") ");
            result.Append(Body.ToString());

            return result.ToString();
        }
    }


    public class PipelineChainAstWrapper : ChainableAstWrapper
    {
        public ChainableAstWrapper LhsPipelineChain { get; }
        public ChainableAstWrapper RhsPipeline { get; }
        bool IsBackground { get; set; }
        TokenKind Operator { get; set; }

        public PipelineChainAstWrapper(AstWrapper parent, PipelineChainAst ast)
        : base(parent, ast)
        {
            LhsPipelineChain = Get(this, ast.LhsPipelineChain);
            RhsPipeline = Get(this, ast.RhsPipeline);
            IsBackground = ast.Background;
            Operator = (TokenKind)ast.Operator;
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            // Append the left-hand side pipeline chain
            result.Append(LhsPipelineChain.ToString());

            // Append the operator
            switch (Operator)
            {
                case TokenKind.AndAnd:
                    result.Append(" && ");
                    break;
                case TokenKind.OrOr:
                    result.Append(" || ");
                    break;
                default:
                    result.Append($" [{Operator.ToString()}] ");
                    break;
            }

            // Append the right-hand side pipeline
            result.Append(RhsPipeline.ToString());

            // Append the background operator if the pipeline is running in the background
            if (IsBackground)
            {
                result.Append(" &");
            }

            return result.ToString();
        }
    }

    public class PipelineAstWrapper : ChainableAstWrapper
    {
        bool IsBackground { get; set; }
        public List<CommandBaseAstWrapper>? Elements { get; set; } = new List<CommandBaseAstWrapper>();

        public PipelineAstWrapper(AstWrapper parent, PipelineAst ast)
        : base(parent, ast)
        {
            if (ast.PipelineElements.Count > 1)
            {
                PipelineContext = new PipelineContext();
            }

            Elements = ast.PipelineElements.Select(v => CommandBaseAstWrapper.Get(this, v)).ToList();
            IsBackground = ast.Background;

        }

        public override bool CanEvaluate()
        {
            if (Elements.Count == 1)
            {
                return Elements[0].CanEvaluate();
            }
            else if (Elements.Count > 1)
            {
                return Elements[1].CanEvaluateFromPipeline(Elements[0]);
            }

            return false;
        }

        public override DExpression Evaluate()
        {
            if (CanEvaluate())
            {
                if (Elements.Count == 1)
                {
                    return Elements[0].Evaluate();
                }
                else if (Elements.Count > 1)
                {
                    return Elements[1].EvaluateFromPipeline(Elements[0]);
                }
            }
            return base.Evaluate();
        }

        public override string ToString()
        {
            if (CanEvaluate())
                return Evaluate().ToExpressionString();

            return Prettify();
        }

        private string Prettify()
        {
            StringBuilder result = new StringBuilder();

            if (Elements != null && Elements.Count > 0)
            {
                for (int i = 0; i < Elements.Count; i++)
                {
                    result.Append(Elements[i].ToString());
                    if (i < Elements.Count - 1)
                    {
                        result.Append(" | "); // Append pipeline operator between elements
                    }
                }

                if (IsBackground)
                {
                    result.Append(" &"); // Append background operator if the pipeline runs in the background
                }
            }

            return result.ToString();
        }
    }


    //public class TrapStatementAstWrapper : StatementAstWrapper
    //{
    //    public TrapStatementAstWrapper(TrapStatementAst ast)
    //    : base(ast)
    //    {
    //    }
    //}


    public abstract class StatementAstWrapper : AstWrapper<StatementAst> //: StatementAstWrapper<StatementAst>
    {
        protected StatementAstWrapper(AstWrapper parent, StatementAst ast)
        : base(parent, ast)
        {
        }

        public StatementAstWrapper AstWrapper { get; set; }

        public static StatementAstWrapper Get(AstWrapper parent, StatementAst ast)
        {
            if (ast is TrapStatementAst)
            {
                return new TrapStatementAstWrapper(parent, ast as TrapStatementAst);
            }
            else if (ast is PipelineBaseAst)
            {
                return PipelineBaseAstWrapper.Get(parent, ast as PipelineBaseAst);
            }
            else if (ast is ThrowStatementAst)
            {
                return new ThrowStatementAstWrapper(parent, ast as ThrowStatementAst);
            }
            else if (ast is FunctionDefinitionAst)
            {
                return new FunctionDefintionAstWrapper(parent, ast as FunctionDefinitionAst);
            }
            else if (ast is LabeledStatementAst)
            {
                return LabeledStatementAstWrapper.Get(parent, ast as LabeledStatementAst);
            }
            else if (ast is IfStatementAst)
            {
                return new IfStatementAstWrapper(parent, ast as IfStatementAst);
            }
            else if (ast is TryStatementAst)
            {
                return new TryStatementAstWrapper(parent, ast as TryStatementAst);
            }
            else if (ast is TypeDefinitionAst)
            {
                return new StatementAstWrapper<TypeDefinitionAst>(parent, ast);
            }
            else if (ast is UsingStatementAst)
            {
                return new StatementAstWrapper<UsingStatementAst>(parent, ast);
            }
            else if (ast is DataStatementAst)
            {
                return new StatementAstWrapper<DataStatementAst>(parent, ast);
            }
            else if (ast is DataStatementAst)
            {
                return new StatementAstWrapper<DataStatementAst>(parent, ast);
            }
            else if (ast is BreakStatementAst)
            {
                return new StatementAstWrapper<BreakStatementAst>(parent, ast);
            }
            else if (ast is ContinueStatementAst)
            {
                return new ContinueStatementAstWrapper(parent, ast as ContinueStatementAst);
            }
            else if (ast is ReturnStatementAst)
            {
                return new StatementAstWrapper<ReturnStatementAst>(parent, ast);
            }
            else if (ast is ExitStatementAst)
            {
                return new StatementAstWrapper<ExitStatementAst>(parent, ast);
            }
            else if (ast is CommandBaseAst)
            {
                return CommandBaseAstWrapper.Get(parent, ast as CommandBaseAst);
            }
            else if (ast is ConfigurationDefinitionAst)
            {
                return new StatementAstWrapper<ConfigurationDefinitionAst>(parent, ast);
            }
            else if (ast is DynamicKeywordStatementAst)
            {
                return new StatementAstWrapper<DynamicKeywordStatementAst>(parent, ast);
            }
            else if (ast is BlockStatementAst)
            {
                return new StatementAstWrapper<BlockStatementAst>(parent, ast);
            }
            else
            {
                return new StatementAstWrapper<StatementAst>(parent, ast);
            }
        }

    }


    public class ChainableAstWrapper : PipelineBaseAstWrapper //: StatementAstWrapper<StatementAst>
    {
        protected ChainableAstWrapper(AstWrapper parent, ChainableAst ast)
            : base(parent, ast)
        {
        }

        public static ChainableAstWrapper Get(AstWrapper parent, ChainableAst ast)
        {
            if (ast is PipelineChainAst)
            {
                return new PipelineChainAstWrapper(parent, ast as PipelineChainAst);
            }
            else if (ast is PipelineAst)
            {
                return new PipelineAstWrapper(parent, ast as PipelineAst);
            }
            else
            {
                return new ChainableAstWrapper(parent, ast);
            }
        }

    }

    public class AssignmentStatementAstWrapper : PipelineBaseAstWrapper
    {
        public StatementAstWrapper Right { get; }
        public ExpressionAstWrapper Left { get; }
        public TokenKind Operator { get; }
        public string OperatorSymbol { get; }

        public AssignmentStatementAstWrapper(AstWrapper parent, AssignmentStatementAst ast)
            : base(parent, ast)
        {
            Right = StatementAstWrapper.Get(this, ast.Right);
            Left = ExpressionAstWrapper.Get(this, ast.Left);
            Operator = ast.Operator;
            OperatorSymbol = GetOperatorStrFrom(Operator);
        }

        public override string Prettify()
        {
            return $"{Left} {OperatorSymbol} {Right}";
        }
    }

    public class PipelineBaseAstWrapper : StatementAstWrapper<PipelineBaseAst> //: StatementAstWrapper<StatementAst>
    {
        protected PipelineBaseAstWrapper(AstWrapper parent, PipelineBaseAst ast)
            : base(parent ,ast)
        {
        }


        public static PipelineBaseAstWrapper Get(AstWrapper parent, PipelineBaseAst ast)
        {
            if (ast is AssignmentStatementAst)
            {
                return new AssignmentStatementAstWrapper(parent, ast as AssignmentStatementAst);
            }
            else if (ast is ErrorStatementAst)
            {
                return new PipelineBaseAstWrapper(parent, ast);
            }
            else if (ast is ChainableAst)
            {
                return ChainableAstWrapper.Get(parent, ast as ChainableAst);
            }
            else
            {
                return new PipelineBaseAstWrapper(parent, ast);
            }
        }

    }

    [Obsolete]
    public class StatementAstWrapper<T> : StatementAstWrapper
    where T : StatementAst
    {
        public StatementAstWrapper(AstWrapper parent, T ast)
        : base(parent, ast)
        {
        }
        public StatementAstWrapper(AstWrapper parent, Ast ast)
        : base(parent, ast as T)
        {
        }

        public override string Prettify()
        {
            return NotImplementedPrettify();
        }
    }

    public class NamedBlockAstWrapper : AstWrapper<NamedBlockAst>
    {
        public List<StatementAstWrapper>? Statements { get; set; } = new List<StatementAstWrapper>();

        public NamedBlockAstWrapper(AstWrapper parent, NamedBlockAst ast)
            : base(parent, ast)
        {
            var traps = ast.Traps?.ToArray() ?? Array.Empty<TrapStatementAst>();
            var statements = ast.Statements?.ToArray() ?? Array.Empty<TrapStatementAst>();

            var allStmt = traps.Union(statements)
                .OrderBy(v => v.Extent.StartOffset).ToArray();


            foreach (var stmt in allStmt)
            {
                Statements.Add(StatementAstWrapper.Get(this, stmt));
            }

        }

        public override bool CanEvaluate()
        {
            if (Statements == null)
                return false;

            foreach (var v in Statements)
            {
                if (v.CanEvaluate())
                    return true;

            }
            return true;
        }

        public override DExpression Evaluate()
        {
            List<DExpression> result = new List<DExpression>();
            foreach (var v in Statements)
            {
                result.Add(v.Evaluate());
            }

            if (result.Count == 1)
                return result.First();
            else
            {
                return new DCodeBlock(string.Join("\r\n", result.Select(v => v.ToExpressionString())));
            }
        }


        public string GetBody()
        {

            if (Statements != null)
            {
                StringBuilder result = new StringBuilder();
                foreach (var statement in Statements)
                {
                    result.AppendLine(Helpers.IndentLines(4, statement.ToString()));
                }
                return result.ToString();
            }
            else
            {
                return string.Empty;
            }

        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            if (Statements != null)
            {
                result.AppendLine($"{{");

                result.Append(GetBody());

                result.AppendLine("}");
            }
            else
            {
                result.AppendLine($"{{ }}");
            }

            return result.ToString();
        }
    }

    public class ScriptBlockAstWrapper : AstWrapper // Childs: OK
    {
        ParamBlockAstWrapper ParamBlock { get; set; }
        NamedBlockAstWrapper BeginBlock { get; set; }
        NamedBlockAstWrapper ProcessBlock { get; set; }
        NamedBlockAstWrapper EndBlock { get; set; }
        NamedBlockAstWrapper DynamicParamBlock { get; set; }

        private readonly ScriptBlockAst _scriptBlockAst;

        public ScriptBlockAstWrapper(AstWrapper parent, ScriptBlockAst scriptBlockAst)
            : base(parent, scriptBlockAst)
        {
            _scriptBlockAst = scriptBlockAst ?? throw new ArgumentNullException(nameof(scriptBlockAst));

            if (_scriptBlockAst.ParamBlock != null)
                ParamBlock = new ParamBlockAstWrapper(this, _scriptBlockAst.ParamBlock);

            if (_scriptBlockAst.BeginBlock != null)
                BeginBlock = new NamedBlockAstWrapper(this, _scriptBlockAst.BeginBlock);

            if (_scriptBlockAst.ProcessBlock != null)
                ProcessBlock = new NamedBlockAstWrapper(this, _scriptBlockAst.ProcessBlock);

            if (_scriptBlockAst.EndBlock != null)
                EndBlock = new NamedBlockAstWrapper(this, _scriptBlockAst.EndBlock);

            if (_scriptBlockAst.DynamicParamBlock != null)
                DynamicParamBlock = new NamedBlockAstWrapper(this, _scriptBlockAst.DynamicParamBlock);
        }

        public override bool CanEvaluate()
        {
            if (ParamBlock != null)
                return false;
            if (BeginBlock != null)
                return false;
            if (ParamBlock != null)
                return false;
            if (ProcessBlock != null)
                return false;
            if (EndBlock == null)
                return false;
            if (DynamicParamBlock != null)
                return false;

            return EndBlock.CanEvaluate();
        }

        public override DExpression Evaluate()
        {
            if (!CanEvaluate())
                return new DCodeBlock(Prettify());

            return EndBlock.Evaluate();
        }

        public override string Prettify()
        {
            StringBuilder result = new StringBuilder();

            // Process the param block if it exists
            if (ParamBlock != null)
            {
                result.AppendLine(ParamBlock.ToString());
            }

            // Process the named blocks (Begin, Process, End, Dynamic)
            if (
                ParamBlock == null &&
                BeginBlock == null &&
                ProcessBlock == null &&
                DynamicParamBlock == null &&
                EndBlock != null)
            {
                return EndBlock.GetBody();
            }

            AppendNamedBlock(result, BeginBlock, "Begin");
            AppendNamedBlock(result, ProcessBlock, "Process");
            AppendNamedBlock(result, EndBlock, "End");
            AppendNamedBlock(result, DynamicParamBlock, "DynamicParam");

            return result.ToString();
        }

        private void AppendNamedBlock(StringBuilder result, NamedBlockAstWrapper blockAst, string blockName)
        {
            if (blockAst != null)
            {
                result.AppendLine($"{blockName} {blockAst.ToString()}");
            }
        }
    }
}
