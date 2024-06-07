using System.Management.Automation.Language;
using System.Text;

namespace PowderShell
{
    [Obsolete]
    public class AstWrapper
    {

        public static readonly List<TokenKind> s_operatorTokenKind = new List<TokenKind> {
        /*1*/   TokenKind.Bnot,         TokenKind.Not,          TokenKind.Ieq,          TokenKind.Ieq,            /*1*/
        /*2*/   TokenKind.Ceq,          TokenKind.Ine,          TokenKind.Ine,          TokenKind.Cne,            /*2*/
        /*3*/   TokenKind.Ige,          TokenKind.Ige,          TokenKind.Cge,          TokenKind.Igt,            /*3*/
        /*4*/   TokenKind.Igt,          TokenKind.Cgt,          TokenKind.Ilt,          TokenKind.Ilt,            /*4*/
        /*5*/   TokenKind.Clt,          TokenKind.Ile,          TokenKind.Ile,          TokenKind.Cle,            /*5*/
        /*6*/   TokenKind.Ilike,        TokenKind.Ilike,        TokenKind.Clike,        TokenKind.Inotlike,       /*6*/
        /*7*/   TokenKind.Inotlike,     TokenKind.Cnotlike,     TokenKind.Imatch,       TokenKind.Imatch,         /*7*/
        /*8*/   TokenKind.Cmatch,       TokenKind.Inotmatch,    TokenKind.Inotmatch,    TokenKind.Cnotmatch,      /*8*/
        /*9*/   TokenKind.Ireplace,     TokenKind.Ireplace,     TokenKind.Creplace,     TokenKind.Icontains,      /*9*/
        /*10*/  TokenKind.Icontains,    TokenKind.Ccontains,    TokenKind.Inotcontains, TokenKind.Inotcontains,   /*10*/
        /*11*/  TokenKind.Cnotcontains, TokenKind.Iin,          TokenKind.Iin,          TokenKind.Cin,            /*11*/
        /*12*/  TokenKind.Inotin,       TokenKind.Inotin,       TokenKind.Cnotin,       TokenKind.Isplit,         /*12*/
        /*13*/  TokenKind.Isplit,       TokenKind.Csplit,       TokenKind.IsNot,        TokenKind.Is,             /*13*/
        /*14*/  TokenKind.As,           TokenKind.Format,       TokenKind.And,          TokenKind.Band,           /*14*/
        /*15*/  TokenKind.Or,           TokenKind.Bor,          TokenKind.Xor,          TokenKind.Bxor,           /*15*/
        /*16*/  TokenKind.Join,         TokenKind.Shl,          TokenKind.Shr,          TokenKind.Equals                          /*16*/
        };

        public static readonly string[] _operatorText = new string[] {
        /*1*/   "bnot",                 "not",                  "eq",                   "ieq",                    /*1*/
        /*2*/   "ceq",                  "ne",                   "ine",                  "cne",                    /*2*/
        /*3*/   "ge",                   "ige",                  "cge",                  "gt",                     /*3*/
        /*4*/   "igt",                  "cgt",                  "lt",                   "ilt",                    /*4*/
        /*5*/   "clt",                  "le",                   "ile",                  "cle",                    /*5*/
        /*6*/   "like",                 "ilike",                "clike",                "notlike",                /*6*/
        /*7*/   "inotlike",             "cnotlike",             "match",                "imatch",                 /*7*/
        /*8*/   "cmatch",               "notmatch",             "inotmatch",            "cnotmatch",              /*8*/
        /*9*/   "replace",              "ireplace",             "creplace",             "contains",               /*9*/
        /*10*/  "icontains",            "ccontains",            "notcontains",          "inotcontains",           /*10*/
        /*11*/  "cnotcontains",         "in",                   "iin",                  "cin",                    /*11*/
        /*12*/  "notin",                "inotin",               "cnotin",               "split",                  /*12*/
        /*13*/  "isplit",               "csplit",               "isnot",                "is",                     /*13*/
        /*14*/  "as",                   "f",                    "and",                  "band",                   /*14*/
        /*15*/  "or",                   "bor",                  "xor",                  "bxor",                   /*15*/
        /*16*/  "join",                 "shl",                  "shr",                  "eq"                          /*16*/
        };

        public static readonly string[] _operatorSymbols = new string[] {
                /*1*/   "-bnot",     "-not",     "==",      "==",       /*1*/
                /*2*/   "==",        "!=",       "!=",      "!=",       /*2*/
                /*3*/   ">=",        ">=",       ">=",      ">",        /*3*/
                /*4*/   ">",         ">",        "<",       "<",        /*4*/
                /*5*/   "<",         "<=",       "<=",      "<=",       /*5*/
                /*6*/   "-like",     "-like",    "-like",   "-notlike", /*6*/
                /*7*/   "-notlike",  "-notlike", "-match",  "-match",   /*7*/
                /*8*/   "-match",    "-notmatch","-notmatch","-notmatch",/*8*/
                /*9*/   "-replace",  "-replace", "-replace","-contains",/*9*/
                /*10*/  "-contains", "-contains","-notcontains","-notcontains",/*10*/
                /*11*/  "-notcontains","-in",    "-in",     "-in",      /*11*/
                /*12*/  "-notin",    "-notin",   "-notin",  "-split",   /*12*/
                /*13*/  "-split",    "-split",   "-isnot",  "-is",      /*13*/
                /*14*/  "-as",       "-f",       "-and",    "-band",    /*14*/
                /*15*/  "-or",       "-bor",     "-xor",    "-bxor",    /*15*/
                /*16*/  "-join",     "-shl",     "-shr",     "="           /*16*/
            };

        public static string GetOperatorStrFrom(TokenKind tokenKind)
        {
            var index = s_operatorTokenKind.IndexOf(tokenKind);
            if (index == -1)
            {
                return tokenKind.ToString();
            }
            else
            { 
            
                return _operatorSymbols[index];
            }
        }

        public Ast BaseAst { get; set; }

        [Obsolete]
        public AstWrapper(Ast ast)
        {
        }

        public override string ToString()
        {
            return BaseAst.ToString();
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

        public override string ToString()
        {
            return /*$"[UnManaged:{BaseAst.GetType().Name}] " +*/ BaseAst.ToString();
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

        public override string ToString()
        {
            switch (StringConstantType)
            {
                case StringConstantType.DoubleQuoted:
                    return $"\"{Value}\""; // Wrap the value in double quotes
                case StringConstantType.SingleQuoted:
                    return $"'{Value}'"; // Wrap the value in single quotes
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

        public ConstantExpressionAstWrapper(ConstantExpressionAst ast)
            : base(ast)
        {
            Value = ast.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static ConstantExpressionAstWrapper Get(ConstantExpressionAst ast)
        {
            if (ast is StringConstantExpressionAst)
                return new StringConstantExpressionAstWrapper(ast as StringConstantExpressionAst);

            return new ConstantExpressionAstWrapper(ast);
        }
    }

    public class ExpressionAstWrapper : CommandElementAstWrapper
    {
        public ITypeName TypeName { get; }
        public Type StaticType { get; }

        internal ExpressionAstWrapper(ExpressionAst ast)
            : base(ast)
        {
            StaticType = ast.StaticType;
        }


        internal static ExpressionAstWrapper Get(ExpressionAst v)
        {
            if (v is VariableExpressionAst) return new ExpressionAstWrapper(v);
            if (v is ErrorExpressionAst) return new ExpressionAstWrapper(v);
            if (v is BinaryExpressionAst) return new ExpressionAstWrapper(v);
            if (v is UnaryExpressionAst) return new ExpressionAstWrapper(v);

            if (v is TernaryExpressionAst) return new ExpressionAstWrapper(v);
            if (v is UnaryExpressionAst) return new ExpressionAstWrapper(v);
            if (v is AttributedExpressionAst) return new ExpressionAstWrapper(v);
            if (v is MemberExpressionAst) return new ExpressionAstWrapper(v);
            if (v is TypeExpressionAst) return new ExpressionAstWrapper(v);
            if (v is VariableExpressionAst) return new ExpressionAstWrapper(v);
            if (v is ConstantExpressionAst) return ConstantExpressionAstWrapper.Get(v as ConstantExpressionAst);
             // if (v is StringConstantExpressionAst) return new ExpressionAstWrapper(v);

            if (v is ExpandableStringExpressionAst) return new ExpressionAstWrapper(v);
            if (v is ScriptBlockExpressionAst) return new ExpressionAstWrapper(v);
            if (v is ArrayLiteralAst) return new ExpressionAstWrapper(v);
            if (v is HashtableAst) return new ExpressionAstWrapper(v);
            if (v is ArrayExpressionAst) return new ExpressionAstWrapper(v);
            if (v is ParenExpressionAst) return new ExpressionAstWrapper(v);
            if (v is SubExpressionAst) return new ExpressionAstWrapper(v);
            if (v is UsingExpressionAst) return new ExpressionAstWrapper(v);
            if (v is IndexExpressionAst) return new ExpressionAstWrapper(v);

            string typeName = v.GetType().Name;
            return new ExpressionAstWrapper(v);
        }
    }


    public class CommandElementAstWrapper : AstWrapper<CommandElementAst>
    {
        public CommandElementAstWrapper(CommandElementAst ast)
        : base(ast)
        {
        }

        public override string ToString()
        {
            return BaseAst.ToString() ;
        }

        internal static CommandElementAstWrapper Get(CommandElementAst v)
        {
            if (v is CommandParameterAst)
            {
                return new CommandElementAstWrapper(v as CommandParameterAst);
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
                return new AttributeBaseAstWrapper(v);
            }
            else
            {
                return new AttributeBaseAstWrapper(v);
            }
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

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (BaseAst != null)
            {
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
            }
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

        public CommandAstWrapper(CommandAst ast)
            : base(ast)
        {
            CommandElements = ast.CommandElements.Select(v => CommandElementAstWrapper.Get(v)).ToList();
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < CommandElements.Count; i++)
            {
                result.Append(CommandElements[i].ToString());
                if (i < CommandElements.Count - 1)
                {
                    result.Append(" ");
                }
            }

            result.Append(base.ToString()); // Append redirections if any

            return result.ToString();
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

        public override string ToString()
        {
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
            PositionalArguments = ast.PositionalArguments?.Select(v => new ExpressionAstWrapper(v))?.ToList();
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

        public override string ToString()
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



    public class TrapStatementAstWrapper : StatementAstWrapper
    {
        public TrapStatementAstWrapper(TrapStatementAst ast)
        : base(ast)
        {
        }
    }


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
                return new StatementAstWrapper<ThrowStatementAst>(ast);
            }
            else if (ast is FunctionDefinitionAst)
            {
                return new FunctionDefintionAstWrapper(ast as FunctionDefinitionAst);
            }
            else if (ast is ForEachStatementAst)
            {
                return new StatementAstWrapper<ForEachStatementAst>(ast);
            }
            else if (ast is IfStatementAst)
            {
                return new IfStatementAstWrapper(ast as IfStatementAst);
            }
            else if (ast is TryStatementAst)
            {
                return new StatementAstWrapper<TryStatementAst>(ast);
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
                return new StatementAstWrapper<ContinueStatementAst>(ast);
            }
            else if (ast is ReturnStatementAst)
            {
                return new StatementAstWrapper<ReturnStatementAst>(ast);
            }
            else if (ast is ExitStatementAst)
            {
                return new StatementAstWrapper<ExitStatementAst>(ast);
            }
            else if (ast is ThrowStatementAst)
            {
                return new StatementAstWrapper<ThrowStatementAst>(ast);
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

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (BaseAst != null)
            {
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
