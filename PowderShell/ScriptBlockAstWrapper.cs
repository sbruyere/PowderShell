using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PowderShell.EvaluationObjects;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace PowderShell
{
    public static class PowderShellOptions
    {
        public static bool TagUnmanagedAst { get; set; } = true;
    }

    [Obsolete]
    public class AstWrapper
    {
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

        public Ast BaseAst { get; set; }

        [Obsolete]
        public AstWrapper(Ast ast)
        {
        }

        public override string ToString()
        {
            if (PowderShellOptions.TagUnmanagedAst)
                return $"[Unmanaged({BaseAst.GetType().Name})] {BaseAst}";
            else
                return $"{BaseAst}";

        }

        public virtual bool CanEvaluate()
        {
            return false;
        }

        public virtual DExpression Evaluate()
        {
            return new DCodeBlock(ToString());
        }
    }

    [Obsolete]
    public class AstWrapper<T> : AstWrapper
        where T : Ast
    {

        public T TypedAst { get { return (T)BaseAst; } }

        [Obsolete]
        public AstWrapper(T ast)
            : base(ast)
        {
            BaseAst = ast ?? throw new ArgumentNullException(nameof(ast));
        }
    }

    public class AttributedExpressionAstWrapper : ExpressionAstWrapper
    {
        public ExpressionAstWrapper Child { get; }
        public AttributeBaseAstWrapper Attribute { get; }

        public AttributedExpressionAstWrapper(AttributedExpressionAst ast) 
            : base(ast)
        {
            Child = ExpressionAstWrapper.Get(ast.Child);
            Attribute = AttributeBaseAstWrapper.Get(ast.Attribute);
        }

        public static AttributedExpressionAstWrapper Get(AttributedExpressionAst ast)
        {
            if (ast is ConvertExpressionAst)
                return new ConvertExpressionAstWrapper(ast as ConvertExpressionAst);

            return new AttributedExpressionAstWrapper(ast);
        }

        public override string ToString()
        {
            return $"{Attribute} {Child}";
        }
    }

    public class TypeExpressionAstWrapper : ExpressionAstWrapper
    {
        public ITypeName TypeName { get; }

        public TypeExpressionAstWrapper(TypeExpressionAst ast) : base(ast)
        {
            TypeName = ast.TypeName;
        }

        public override string ToString()
        {
            return $"[{TypeName.Name}]";
        }
    }

    public class MemberExpressionAstWrapper : ExpressionAstWrapper
    {
        public ExpressionAstWrapper Expression { get; }
        public bool NullConditional { get; private set; }
        public CommandElementAstWrapper Member { get; }
        public bool Static { get; }

        public MemberExpressionAstWrapper(MemberExpressionAst ast) : base(ast)
        {
            if (ast.Expression != null)
                Expression = ExpressionAstWrapper.Get(ast.Expression);
            

            if (ast.Member != null)
                Member = CommandElementAstWrapper.Get( ast.Member);

            NullConditional = ast.NullConditional;
            Static = ast.Static;
        }

       public static MemberExpressionAstWrapper Get(MemberExpressionAst ast)
        {
            if (ast is InvokeMemberExpressionAst)
            {
                return new InvokeMemberExpressionAstWrapper(ast as InvokeMemberExpressionAst);
            }

            return new MemberExpressionAstWrapper(ast);
        }


        public override string ToString()
        {
            string nullConditionalOperator = NullConditional ? "?." : ".";
            string staticOperator = Static ? "::" : nullConditionalOperator;
            return $"{Expression}{staticOperator}{Member}";
        }

    }

    public class InvokeMemberExpressionAstWrapper : MemberExpressionAstWrapper
    {
        public InvokeMemberExpressionAstWrapper(InvokeMemberExpressionAst ast) 
            : base(ast)
        {
            if (ast.Arguments != null)
                Arguments = ast.Arguments.Select(v => ExpressionAstWrapper.Get(v)).ToList();
            if (ast.GenericTypeArguments != null)
                GenericTypeArguments = ast.GenericTypeArguments;
        }

        public List<ExpressionAstWrapper> Arguments { get; }
        public ReadOnlyCollection<ITypeName> GenericTypeArguments { get; }


        public override bool CanEvaluate()
        {
            if (this.GenericTypeArguments != null && this.GenericTypeArguments.Count > 0)
                return false;

            if (!this.Member.CanEvaluate())
                return false;

            if (this.Static || !this.Expression.CanEvaluate())
                return false;

            bool argumentsEvaluable = Arguments.All(a => a.CanEvaluate());
            if (!argumentsEvaluable)
                return false;

            string memberName = Member.Evaluate().ToValueString().ToLower();

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
            if (!CanEvaluate())
                return new DCodeBlock(GetCode());


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
                        return new DCodeBlock(GetCode());

                    string sResult = dExpression.ToValueString();
                    string p1 = arguments[0].ToValueString();
                    string p2 = arguments[1].ToValueString();

                    sResult = sResult.Replace(p1, p2);

                    return new DSimpleStringExpression(sResult, Encoding.Unicode);
            }

            return new DCodeBlock(GetCode());
        }

        public override string ToString()
        {
            return GetCode();
        }

        private string GetCode()
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

            return $"{base.ToString()}{generics}({args})";
        }
    }

    public class ConvertExpressionAstWrapper: AttributedExpressionAstWrapper
    {
        public TypeConstraintAstWrapper Type { get; }

        public ConvertExpressionAstWrapper(ConvertExpressionAst ast) : base(ast)
        {
            Type = new TypeConstraintAstWrapper(ast.Type);
        }

        public override bool CanEvaluate()
        {
            if (!this.Child.CanEvaluate())
                return false;

            object valueExp = this.Child.Evaluate();

            if (!(valueExp is DMathExpression))
                return false;

            DMathExpression mathExp = (valueExp as DMathExpression);
            if (!mathExp.IsValuable)
                return false;

            string typeName = Type.TypeName.Name;
            double realNumber = mathExp.GetSymExp().RealNumberValue;

            switch (typeName)
            {
                case "char":
                case "int":
                case "uint":
                case "long":
                    return true;
                    break;
                default:
                    return false;
            }
        }

        public override DExpression Evaluate()
        {
            if (!this.Child.CanEvaluate())
                return new DCodeBlock(GetExpression());

            object valueExp = this.Child.Evaluate();

            if (!(valueExp is DMathExpression))
                return new DCodeBlock(GetExpression());

            DMathExpression mathExp = (valueExp as DMathExpression);
            if (!mathExp.IsValuable)
                return new DCodeBlock(GetExpression());

            string typeName = Type.TypeName.Name;
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
                    return new DCodeBlock(GetExpression());
            }
        }

        public override string ToString()
        {
            return GetExpression();
        }

        private string GetExpression()
        {
            return $"{Type} {Child}";
        }
    }

    public class ParamBlockAstWrapper : AstWrapper<ParamBlockAst>
    {
        public List<AttributeAstWrapper>? Attributes { get; }
        public List<ParameterAstWrapper>? Parameters { get; }

        public ParamBlockAstWrapper(ParamBlockAst ast)
            : base(ast)
        {

            Parameters = ast.Parameters?.Select(v => new ParameterAstWrapper(v))?.ToList();
            Attributes = ast.Attributes?.Select(v => new AttributeAstWrapper(v))?.ToList();
            (0).ToString();
        }


        public override string ToString()
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

        public StringConstantExpressionAstWrapper(StringConstantExpressionAst ast)
            : base(ast)
        {
            Value = ast.Value;
            StringConstantType = ast.StringConstantType;
        }

        public override DExpression Evaluate()
        {
            return new DSimpleStringExpression(Value, Encoding.Unicode);
        }

        public override string ToString()
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

        internal ConstantExpressionAstWrapper(ConstantExpressionAst ast)
            : base(ast)
        {
            Value = ast.Value;
        }

        public static ConstantExpressionAstWrapper Get(ConstantExpressionAst ast)
        {
            if (ast is StringConstantExpressionAst)
                return new StringConstantExpressionAstWrapper(ast as StringConstantExpressionAst);

            return new ConstantExpressionAstWrapper(ast);
        }


        public override string ToString()
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

        public ContinueStatementAstWrapper(ContinueStatementAst ast)
            : base(ast)
        {
            if (ast.Label != null)
            Label = ExpressionAstWrapper.Get(ast.Label);
        }

        public ExpressionAstWrapper Label { get; }

        public override string ToString()
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

        public ThrowStatementAstWrapper(ThrowStatementAst ast)
            : base(ast)
        {
            IsRethrow = ast.IsRethrow;
            Pipeline = PipelineBaseAstWrapper.Get(ast.Pipeline);
        }

        public override string ToString()
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

        public ForStatementAstWrapper(ForStatementAst ast)
            : base(ast)
        {
            Initializer = PipelineBaseAstWrapper.Get(ast.Initializer);
            Iterator = PipelineBaseAstWrapper.Get(ast.Iterator);
        }

        public override string ToString()
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

        public ForEachStatementAstWrapper(ForEachStatementAst ast)
            : base(ast)
        {
            if (ast.ThrottleLimit != null)
            ThrottleLimit = ExpressionAstWrapper.Get(ast.ThrottleLimit);
            if (ast.Variable != null)
                Variable = new VariableExpressionAstWrapper(ast.Variable);
            Flags = ast.Flags;
        }

        public override string ToString()
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

        public DoWhileStatementAstWrapper(DoWhileStatementAst ast)
            : base(ast)
        {
            Condition = PipelineBaseAstWrapper.Get(ast.Condition);
        }

        public override string ToString()
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

        public DoUntilStatementAstWrapper(DoUntilStatementAst ast)
            : base(ast)
        {
            Condition = PipelineBaseAstWrapper.Get(ast.Condition);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result
                .AppendLine()
                .Append("do ")
                .Append(Body.ToString())
                .Append(" until (").Append(Condition.ToString()).Append(")")
                .AppendLine();

            return result.ToString();
        }
    }

    public class WhileStatementAstWrapper : LoopStatementAstWrapper
    {
        public PipelineBaseAstWrapper Condition { get; }

        public WhileStatementAstWrapper(WhileStatementAst ast)
            : base(ast)
        {
            Condition = PipelineBaseAstWrapper.Get(ast.Condition);
        }

        public override string ToString()
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

    public class LoopStatementAstWrapper : LabeledStatementAstWrapper
    {
        public LoopStatementAstWrapper(LoopStatementAst ast)
            : base(ast)
        {
            Body = new StatementBlockAstWrapper(ast.Body);
        }

        public StatementBlockAstWrapper Body { get; }

        public static LoopStatementAstWrapper Get(LoopStatementAst ast)
        {
            if (ast is ForEachStatementAst)
                return new ForEachStatementAstWrapper(ast as ForEachStatementAst);

            if (ast is ForStatementAst)
                return new ForStatementAstWrapper(ast as ForStatementAst);

            if (ast is DoWhileStatementAst)
                return new DoWhileStatementAstWrapper(ast as DoWhileStatementAst);

            if (ast is DoUntilStatementAst)
                return new DoUntilStatementAstWrapper(ast as DoUntilStatementAst);

            if (ast is WhileStatementAst)
                return new WhileStatementAstWrapper(ast as WhileStatementAst);

            return new LoopStatementAstWrapper(ast);
        }
    }

    public class LabeledStatementAstWrapper : StatementAstWrapper
    {
        public string Label { get; }
        public PipelineBaseAstWrapper Condition { get; }

        public LabeledStatementAstWrapper(LabeledStatementAst ast)
            : base(ast)
        {
            Label = ast.Label;
            Condition = PipelineBaseAstWrapper.Get(ast.Condition);
        }

        public static LabeledStatementAstWrapper Get(LabeledStatementAst ast)
        {
            if (ast is LoopStatementAst)
                return LoopStatementAstWrapper.Get(ast as LoopStatementAst);

            if (ast is SwitchStatementAst)
                return new LabeledStatementAstWrapper(ast);

            return new LabeledStatementAstWrapper(ast);
        }
    }

    public class TrapStatementAstWrapper : StatementAstWrapper
    {
        public TypeConstraintAstWrapper TrapType { get; }
        public StatementBlockAstWrapper Body { get; }

        public TrapStatementAstWrapper(TrapStatementAst ast)
            : base(ast)
        {
            TrapType = new TypeConstraintAstWrapper(ast.TrapType);
            Body = new StatementBlockAstWrapper(ast.Body);
        }

        public override string ToString()
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

        public ScriptBlockExpressionAstWrapper(ScriptBlockExpressionAst ast)
            : base(ast)
        {
            ScriptBlock = new ScriptBlockAstWrapper(ast.ScriptBlock);
        }

        public override string ToString()
        {
            return ScriptBlock.ToString();
        }

    }


    public class VariableExpressionAstWrapper : ExpressionAstWrapper
    {
        public bool Splatted { get; }
        public VariablePath VariablePath { get; }

        public VariableExpressionAstWrapper(VariableExpressionAst ast)
            : base(ast)
        {
            Splatted = ast.Splatted;
            VariablePath = ast.VariablePath;
        }


        public override string ToString()
        {
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

        public ParenExpressionAstWrapper(ParenExpressionAst ast)
            : base(ast)
        {
            Pipeline = PipelineBaseAstWrapper.Get(ast.Pipeline);
        }

        public override bool CanEvaluate()
        {
            return Pipeline.CanEvaluate();
        }

        public override DExpression Evaluate()
        {
            if (CanEvaluate())
                return Pipeline.Evaluate();

            return base.Evaluate();
        }

        public override string ToString()
        {
            if (CanEvaluate())
                return Evaluate().ToExpressionString();

            return $"({Pipeline})";
        }
    }

    public class ArrayLiteralAstWrapper : ExpressionAstWrapper
    {
        public ArrayLiteralAstWrapper(ArrayLiteralAst ast) : base(ast)
        {
            Elements = ast.Elements.Select(Get).ToList();

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

        public override string ToString()
        {
            return $"@({string.Join(", ", Elements)})";
        }

    }

    public class SubExpressionAstWrapper : ExpressionAstWrapper
    {
        public SubExpressionAstWrapper(SubExpressionAst ast) : base(ast)
        {
            SubExpression = new StatementBlockAstWrapper(ast.SubExpression);
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

        public override string ToString()
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

        public IndexExpressionAstWrapper(IndexExpressionAst ast): base(ast)
        {
            NullConditional = ast.NullConditional;
            Index = ExpressionAstWrapper.Get(ast.Index);
            Target = ExpressionAstWrapper.Get(ast.Target);
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

        public override string ToString()
        {
            return GetStringExp();
        }

        private string GetStringExp()
        {
            string nullConditionalOperator = NullConditional ? "?" : "";
            return $"{Target}{nullConditionalOperator}[{Index}]";
        }
    }

    public class BinaryExpressionAstWrapper : ExpressionAstWrapper
    {
        public BinaryExpressionAstWrapper(BinaryExpressionAst ast) : base(ast)
        {
            Operator = ast.Operator;
            Right = ExpressionAstWrapper.Get(ast.Right);
            Left = ExpressionAstWrapper.Get(ast.Left);
            
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


            return Operation.DoOperation(op, LeftExp, RightExp);
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

        public override string ToString()
        {
            if (CanEvaluate())
                return Evaluate().ToExpressionString();

            return $"{Left} {GetStringOperatorFromToken()} {Right}";
        }
    }

    public class UnknownExpressionAstWrapper: ExpressionAstWrapper
    {
        public UnknownExpressionAstWrapper(ExpressionAst ast) : base(ast)
        {

        }
    }

    public abstract class ExpressionAstWrapper : CommandElementAstWrapper
    {
        public Type StaticType { get; }

        internal ExpressionAstWrapper(ExpressionAst ast)
            : base(ast)
        {
            StaticType = ast.StaticType;
        }


        internal static ExpressionAstWrapper Get(ExpressionAst v)
        {
            if (v is VariableExpressionAst) return new VariableExpressionAstWrapper(v as VariableExpressionAst);
            if (v is ErrorExpressionAst) return new UnknownExpressionAstWrapper(v);

            if (v is UnaryExpressionAst) return new UnknownExpressionAstWrapper(v);
            if (v is BinaryExpressionAst) return new BinaryExpressionAstWrapper(v as BinaryExpressionAst);
            if (v is TernaryExpressionAst) return new UnknownExpressionAstWrapper(v);

            if (v is AttributedExpressionAst) return AttributedExpressionAstWrapper.Get(v as AttributedExpressionAst);
            if (v is MemberExpressionAst) return MemberExpressionAstWrapper.Get(v as MemberExpressionAst);
            if (v is TypeExpressionAst) return new TypeExpressionAstWrapper(v as TypeExpressionAst);
            if (v is VariableExpressionAst) return new UnknownExpressionAstWrapper(v);
            if (v is ConstantExpressionAst) return ConstantExpressionAstWrapper.Get(v as ConstantExpressionAst);
             // if (v is StringConstantExpressionAst) return new ExpressionAstWrapper(v);

            if (v is ExpandableStringExpressionAst) return new ExpandableStringExpressionAstWrapper(v as ExpandableStringExpressionAst);
            if (v is ScriptBlockExpressionAst) return new ScriptBlockExpressionAstWrapper(v as ScriptBlockExpressionAst);
            if (v is ArrayLiteralAst) return new ArrayLiteralAstWrapper(v as ArrayLiteralAst);
            if (v is HashtableAst) return new UnknownExpressionAstWrapper(v);
            if (v is ArrayExpressionAst) return new UnknownExpressionAstWrapper(v);
            if (v is ParenExpressionAst) return new ParenExpressionAstWrapper(v as ParenExpressionAst);
            if (v is SubExpressionAst) return new SubExpressionAstWrapper(v as SubExpressionAst);
            if (v is UsingExpressionAst) return new UnknownExpressionAstWrapper(v);
            if (v is IndexExpressionAst) return new IndexExpressionAstWrapper(v as IndexExpressionAst);

            string typeName = v.GetType().Name;
            return new UnknownExpressionAstWrapper(v);
        }

    }

    public class CommandParameterAstWrapper: CommandElementAstWrapper
    {
        public ExpressionAstWrapper Argument { get; }
        public string ParameterName { get; }

        public CommandParameterAstWrapper(CommandParameterAst ast) 
            : base(ast)
        {
            if (ast.Argument != null)
                Argument = ExpressionAstWrapper.Get(ast.Argument);
            ParameterName = ast.ParameterName;
        }

        public override string ToString()
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

    public class CommandElementAstWrapper : AstWrapper<CommandElementAst>
    {
        public CommandElementAstWrapper(CommandElementAst ast)
        : base(ast)
        {
        }

        internal static CommandElementAstWrapper Get(CommandElementAst v)
        {
            if (v is CommandParameterAst)
            {
                return new CommandParameterAstWrapper(v as CommandParameterAst);
            }
            else if (v is ExpressionAst)
            {
                return ExpressionAstWrapper.Get(v as ExpressionAst);
            }
            else
            {
                return new CommandElementAstWrapper(v);
            }
        }
    }


    public class TypeConstraintAstWrapper : AttributeBaseAstWrapper
    {
        public TypeConstraintAstWrapper(TypeConstraintAst ast)
        : base(ast)
        {
            
        }

        public override string ToString()
        {
            return $"[{TypeName.FullName}]";
        }
    }



    public class AttributeBaseAstWrapper : AstWrapper<AttributeBaseAst>
    {
        public ITypeName TypeName { get; }

        public AttributeBaseAstWrapper(AttributeBaseAst ast)
        : base(ast)
        {
            TypeName = ast.TypeName;
        }

        public override string ToString()
        {
            return $"[{TypeName.FullName}]";
        }

        internal static AttributeBaseAstWrapper Get(AttributeBaseAst v)
        {
            if (v is AttributeAst)
            {
                return new AttributeAstWrapper(v as AttributeAst);
            }
            else if (v is TypeConstraintAst)
            {
                return new TypeConstraintAstWrapper(v as TypeConstraintAst);
            }
            else
            {
                return new AttributeBaseAstWrapper(v);
            }
        }
    }

    public class ExpandableStringExpressionAstWrapper: ExpressionAstWrapper
    {
        public string Value { get; }
        public StringConstantType StringConstantType { get; }
        public List<ExpressionAstWrapper> NestedExpressions { get; }

        public ExpandableStringExpressionAstWrapper(ExpandableStringExpressionAst ast)
            : base(ast)
        {
            Value = ast.Value;
            StringConstantType = ast.StringConstantType;
            NestedExpressions = ast.NestedExpressions.Select(Get).ToList();
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

        public override string ToString()
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

        public StatementBlockAstWrapper(StatementBlockAst ast)
            : base(ast)
        {
            var traps = ast.Traps?.ToArray() ?? Array.Empty<TrapStatementAst>();
            var statements = ast.Statements?.ToArray() ?? Array.Empty<TrapStatementAst>();

            var allStmt = traps.Union(statements)
                .OrderBy(v => v.Extent.StartOffset).ToArray();


            foreach (var stmt in allStmt)
            {
                Statements.Add(StatementAstWrapper.Get(stmt));
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

        public override string ToString()
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

        public CatchClauseAstWrapper(CatchClauseAst ast) : base(ast)
        {
            IsCatchAll = ast.IsCatchAll;
            CatchTypes = ast.CatchTypes.Select(t => new TypeConstraintAstWrapper(t)).ToList();
            Body = new StatementBlockAstWrapper(ast.Body);
        }

        public override string ToString()
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
        public TryStatementAstWrapper(TryStatementAst ast)
            : base(ast)
        {
            if (ast.Finally != null)
                Finally = new StatementBlockAstWrapper(ast.Finally);

            Body = new StatementBlockAstWrapper(ast.Body);
            CatchClauses = ast.CatchClauses.Select(v => new CatchClauseAstWrapper(v)).ToList();
        }

        public StatementBlockAstWrapper Finally { get; }
        public StatementBlockAstWrapper Body { get; }
        public List<CatchClauseAstWrapper> CatchClauses { get; }


        public override string ToString()
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

        public IfStatementAstWrapper(IfStatementAst ast)
            : base(ast)
        {
            Clauses = ast.Clauses?.Select(v => new Tuple<PipelineBaseAstWrapper, StatementBlockAstWrapper>(PipelineBaseAstWrapper.Get(v.Item1), new StatementBlockAstWrapper(v.Item2) )).ToList();
            
            if (ast.ElseClause != null) 
                ElseClause = new StatementBlockAstWrapper(ast.ElseClause);
        }


        public override string ToString()
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

    public class CommandBaseAstWrapper : StatementAstWrapper<CommandBaseAst>
    {
        public List<AstWrapper<RedirectionAst>>? Redirections { get; }

        public CommandBaseAstWrapper(CommandBaseAst ast)
            : base(ast)
        {
            Redirections = ast.Redirections?.Select(v => new AstWrapper<RedirectionAst>(v)).ToList();
        }

        public static CommandBaseAstWrapper Get(CommandBaseAst v)
        {
            if (v is CommandAst commandAst)
            {
                return new CommandAstWrapper(commandAst);
            }
            else if (v is CommandExpressionAst commandExpressionAst)
            {
                return new CommandExpressionAstWrapper(commandExpressionAst);
            }
            else
            {
                return new CommandBaseAstWrapper(v);
            }
        }

        public override string ToString()
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
        public TokenKind InvocationOperator { get; }

        public CommandAstWrapper(CommandAst ast)
            : base(ast)
        {
            CommandElements = ast.CommandElements.Select(v => CommandElementAstWrapper.Get(v)).ToList();
            InvocationOperator = ast.InvocationOperator;
        }


        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            bool simplify = InvocationOperator == TokenKind.Dot && CommandElements.Count > 0 && CommandElements[0].BaseAst is StringConstantExpressionAst;


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
                if (simplify && i == 0)
                {
                    string commandName = CommandElements[i].ToString().Trim('"');
                    result.Append(commandName);
                }
                else
                {
                    result.Append(CommandElements[i].ToString());
                }

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

        public CommandExpressionAstWrapper(CommandExpressionAst ast)
            : base(ast)
        {
            Expression = ExpressionAstWrapper.Get(ast.Expression);
        }

        public override bool CanEvaluate()
        {
            return Expression.CanEvaluate();
        }

        public override DExpression Evaluate()
        {
            return Expression.Evaluate();
        }

        public override string ToString()
        {
            string expression = string.Empty;
            if (CanEvaluate())
            {
                expression = Expression.Evaluate().ToExpressionString();
            } else
            {
                expression = Expression.ToString();
            }

            StringBuilder result = new StringBuilder();

            result.Append(Expression.ToString());

            result.Append(base.ToString()); // Append redirections if any

            return result.ToString();
        }
    }


    public class AttributeAstWrapper : AttributeBaseAstWrapper
    {
        public List<ExpressionAstWrapper>? PositionalArguments { get; set; } = new List<ExpressionAstWrapper>();
        public List<NamedAttributeArgumentAstWrapper>? NamedArguments { get; }

        public AttributeAstWrapper(AttributeAst ast)
        : base(ast)
        {
            NamedArguments = ast.NamedArguments?.Select(v => new NamedAttributeArgumentAstWrapper(v))?.ToList();
            PositionalArguments = ast.PositionalArguments?.Select(ExpressionAstWrapper.Get)?.ToList();
        }

        public override string ToString()
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

        public NamedAttributeArgumentAstWrapper(NamedAttributeArgumentAst ast)
        : base(ast)
        {
            ExpressionOmitted = ast.ExpressionOmitted;
            Argument = ExpressionAstWrapper.Get(ast.Argument);
            ArgumentName = ast.ArgumentName;

        }

        public override string ToString()
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

        public ParameterAstWrapper(ParameterAst ast)
        : base(ast)
        {

            if (ast.DefaultValue != null)
                DefaultValue = ExpressionAstWrapper.Get(ast.DefaultValue);

            if (ast.Name != null)
                Name = ExpressionAstWrapper.Get(ast.Name);

            StaticType = ast.StaticType;
            Attributes = ast.Attributes?.Select(v => AttributeBaseAstWrapper.Get(v))?.ToList();

        }

        public override string ToString()
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
            //    result.Append("[").Append(StaticType.FullName).Append("] ");
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

        public FunctionDefintionAstWrapper(FunctionDefinitionAst ast)
        : base(ast)
        {
            FuncName = ast.Name;
            IsFilter = ast.IsFilter;
            IsWorkflow = ast.IsWorkflow;

            Parameters = ast.Parameters?.Select(v => new ParameterAstWrapper(v))?.ToList();
            Body = new ScriptBlockAstWrapper(ast.Body);

        }

        public override string ToString()
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

        public PipelineChainAstWrapper(PipelineChainAst ast)
        : base(ast)
        {
            LhsPipelineChain = Get(ast.LhsPipelineChain);
            RhsPipeline = Get(ast.RhsPipeline);
            IsBackground = ast.Background;
            Operator = (TokenKind)ast.Operator;
        }

        public override string ToString()
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

        public PipelineAstWrapper(PipelineAst ast)
        : base(ast)
        {
            Elements = ast.PipelineElements.Select(v => CommandBaseAstWrapper.Get(v)).ToList();
            IsBackground = ast.Background;
        }

        public override bool CanEvaluate()
        {
            if (Elements.Count == 1)
            {
                return Elements[0].CanEvaluate();
            }

            return false;
        }

        public override DExpression Evaluate()
        {
            if (CanEvaluate())
            {
                return Elements[0].Evaluate();
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
        protected StatementAstWrapper(StatementAst ast)
        : base(ast)
        {
        }

        public StatementAstWrapper AstWrapper { get; set; }

        public static StatementAstWrapper Get(StatementAst ast)
        {
            if (ast is TrapStatementAst)
            {
                return new TrapStatementAstWrapper(ast as TrapStatementAst);
            }
            else if (ast is PipelineBaseAst)
            {
                return PipelineBaseAstWrapper.Get(ast as PipelineBaseAst);
            }
            else if (ast is ThrowStatementAst)
            {
                return new ThrowStatementAstWrapper(ast as ThrowStatementAst);
            }
            else if (ast is FunctionDefinitionAst)
            {
                return new FunctionDefintionAstWrapper(ast as FunctionDefinitionAst);
            }
            else if (ast is LabeledStatementAst)
            {
                return LabeledStatementAstWrapper.Get(ast as LabeledStatementAst);
            }
            else if (ast is IfStatementAst)
            {
                return new IfStatementAstWrapper(ast as IfStatementAst);
            }
            else if (ast is TryStatementAst)
            {
                return new TryStatementAstWrapper(ast as TryStatementAst);
            }
            else if (ast is TypeDefinitionAst)
            {
                return new StatementAstWrapper<TypeDefinitionAst>(ast);
            }
            else if (ast is UsingStatementAst)
            {
                return new StatementAstWrapper<UsingStatementAst>(ast);
            }
            else if (ast is DataStatementAst)
            {
                return new StatementAstWrapper<DataStatementAst>(ast);
            }
            else if (ast is DataStatementAst)
            {
                return new StatementAstWrapper<DataStatementAst>(ast);
            }
            else if (ast is BreakStatementAst)
            {
                return new StatementAstWrapper<BreakStatementAst>(ast);
            }
            else if (ast is ContinueStatementAst)
            {
                return new ContinueStatementAstWrapper(ast as ContinueStatementAst);
            }
            else if (ast is ReturnStatementAst)
            {
                return new StatementAstWrapper<ReturnStatementAst>(ast);
            }
            else if (ast is ExitStatementAst)
            {
                return new StatementAstWrapper<ExitStatementAst>(ast);
            }
            else if (ast is CommandBaseAst)
            {
                return CommandBaseAstWrapper.Get(ast as CommandBaseAst);
            }
            else if (ast is ConfigurationDefinitionAst)
            {
                return new StatementAstWrapper<ConfigurationDefinitionAst>(ast);
            }
            else if (ast is DynamicKeywordStatementAst)
            {
                return new StatementAstWrapper<DynamicKeywordStatementAst>(ast);
            }
            else if (ast is BlockStatementAst)
            {
                return new StatementAstWrapper<BlockStatementAst>(ast);
            }
            else
            {
                return new StatementAstWrapper<StatementAst>(ast);
            }
        }

    }


    public class ChainableAstWrapper : PipelineBaseAstWrapper //: StatementAstWrapper<StatementAst>
    {
        protected ChainableAstWrapper(ChainableAst ast)
            : base(ast)
        {
        }

        public static ChainableAstWrapper Get(ChainableAst ast)
        {
            if (ast is PipelineChainAst)
            {
                return  new PipelineChainAstWrapper(ast as PipelineChainAst);
            }
            else  if (ast is PipelineAst)
            {
                return new PipelineAstWrapper(ast as PipelineAst);
            }
            else
            {
                return new ChainableAstWrapper(ast);
            }
        }

    }

    public class AssignmentStatementAstWrapper: PipelineBaseAstWrapper
    {
        public StatementAstWrapper Right { get; }
        public ExpressionAstWrapper Left { get; }
        public TokenKind Operator { get; }
        public string OperatorSymbol { get; }

        public AssignmentStatementAstWrapper(AssignmentStatementAst ast)
            : base(ast)
        {
            Right = StatementAstWrapper.Get(ast.Right);
            Left = ExpressionAstWrapper.Get(ast.Left);
            Operator = ast.Operator;
            OperatorSymbol = GetOperatorStrFrom(Operator);
        }

        public override string ToString()
        {
            return $"{Left} {OperatorSymbol} {Right}";
        }
    }

    public  class PipelineBaseAstWrapper : StatementAstWrapper<PipelineBaseAst> //: StatementAstWrapper<StatementAst>
    {
        protected PipelineBaseAstWrapper(PipelineBaseAst ast)
            : base(ast)
        {
        }


        public static PipelineBaseAstWrapper Get(PipelineBaseAst ast)
        {
            if (ast is AssignmentStatementAst)
            {
                return new AssignmentStatementAstWrapper(ast as AssignmentStatementAst);
            }
            else if (ast is ErrorStatementAst)
            {
                return new PipelineBaseAstWrapper(ast);
            }
            else if (ast is ChainableAst)
            {
                return ChainableAstWrapper.Get(ast as ChainableAst);
            }
            else
            {
                return new PipelineBaseAstWrapper(ast);
            }
        }

    }

    [Obsolete]
    public class StatementAstWrapper<T> : StatementAstWrapper
    where T : StatementAst
    {
        public StatementAstWrapper(T ast)
        : base(ast)
        {
        }
        public StatementAstWrapper(Ast ast)
        : base(ast as T)
        {
        }
    }

    public class NamedBlockAstWrapper : AstWrapper<NamedBlockAst>
    {
        public List<StatementAstWrapper>? Statements { get; set; } = new List<StatementAstWrapper>();

        public NamedBlockAstWrapper(NamedBlockAst ast)
            : base(ast)
        {
            var traps = ast.Traps?.ToArray() ?? Array.Empty<TrapStatementAst>();
            var statements = ast.Statements?.ToArray() ?? Array.Empty<TrapStatementAst>();

            var allStmt = traps.Union(statements)
                .OrderBy(v => v.Extent.StartOffset).ToArray();


            foreach (var stmt in allStmt)
            {
                Statements.Add(StatementAstWrapper.Get(stmt));
            }

        }

        public string GetBody() {

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

        public override string ToString()
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

    public class ScriptBlockAstWrapper // Childs: OK
    {
        ParamBlockAstWrapper ParamBlock { get; set; }
        NamedBlockAstWrapper BeginBlock { get; set; }
        NamedBlockAstWrapper ProcessBlock { get; set; }
        NamedBlockAstWrapper EndBlock { get; set; }
        NamedBlockAstWrapper DynamicParamBlock { get; set; }

        private readonly ScriptBlockAst _scriptBlockAst;

        public ScriptBlockAstWrapper(ScriptBlockAst scriptBlockAst)
        {
            _scriptBlockAst = scriptBlockAst ?? throw new ArgumentNullException(nameof(scriptBlockAst));

            if (_scriptBlockAst.ParamBlock != null)
                ParamBlock = new ParamBlockAstWrapper(_scriptBlockAst.ParamBlock);

            if (_scriptBlockAst.BeginBlock != null)
                BeginBlock = new NamedBlockAstWrapper(_scriptBlockAst.BeginBlock);

            if (_scriptBlockAst.ProcessBlock != null)
                ProcessBlock = new NamedBlockAstWrapper(_scriptBlockAst.ProcessBlock);

            if (_scriptBlockAst.EndBlock != null)
                EndBlock = new NamedBlockAstWrapper(_scriptBlockAst.EndBlock);

            if (_scriptBlockAst.DynamicParamBlock != null)
                DynamicParamBlock = new NamedBlockAstWrapper(_scriptBlockAst.DynamicParamBlock);
        }

        public override string ToString()
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
