using System.Collections.Immutable;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace PowderShell
{
    public class Sanitizer
    {
        private static readonly string[] defaultNamespaces = [
            "System",
            "System.Collections",
            "System.Collections.Generic",
            "System.Management.Automation",
            "System.Linq",
            "System.Text",
            "System.IO",
            "Microsoft.PowerShell.Commands",
            "System.Reflection",
            ];

        private static readonly string[] s_keywordText = new string[] {
        /*1*/    "elseif",                  "if",               "else",             "switch",                     /*1*/
        /*2*/    "foreach",                 "from",             "in",               "for",                        /*2*/
        /*3*/    "while",                   "until",            "do",               "try",                        /*3*/
        /*4*/    "catch",                   "finally",          "trap",             "data",                       /*4*/
        /*5*/    "return",                  "continue",         "break",            "exit",                       /*5*/
        /*6*/    "throw",                   "begin",            "process",          "end",                        /*6*/
        /*7*/    "dynamicparam",            "function",         "filter",           "param",                      /*7*/
        /*8*/    "class",                   "define",           "var",              "using",                      /*8*/
        /*9*/    "workflow",                "parallel",         "sequence",         "inlinescript",               /*9*/
        /*A*/    "configuration",           "public",           "private",          "static",                     /*A*/
        /*B*/    "interface",               "enum",             "namespace",        "module",                     /*B*/
        /*C*/    "type",                    "assembly",         "command",          "hidden",                     /*C*/
        /*D*/    "base",                    "default",          "clean",                                          /*D*/
        };

        private static readonly TokenKind[] s_keywordTokenKind = new TokenKind[] {
        /*1*/    TokenKind.ElseIf,          TokenKind.If,       TokenKind.Else,      TokenKind.Switch,             /*1*/
        /*2*/    TokenKind.Foreach,         TokenKind.From,     TokenKind.In,        TokenKind.For,                /*2*/
        /*3*/    TokenKind.While,           TokenKind.Until,    TokenKind.Do,        TokenKind.Try,                /*3*/
        /*4*/    TokenKind.Catch,           TokenKind.Finally,  TokenKind.Trap,      TokenKind.Data,               /*4*/
        /*5*/    TokenKind.Return,          TokenKind.Continue, TokenKind.Break,     TokenKind.Exit,               /*5*/
        /*6*/    TokenKind.Throw,           TokenKind.Begin,    TokenKind.Process,   TokenKind.End,                /*6*/
        /*7*/    TokenKind.Dynamicparam,    TokenKind.Function, TokenKind.Filter,    TokenKind.Param,              /*7*/
        /*8*/    TokenKind.Class,           TokenKind.Define,   TokenKind.Var,       TokenKind.Using,              /*8*/
        /*9*/    TokenKind.Workflow,        TokenKind.Parallel, TokenKind.Sequence,  TokenKind.InlineScript,       /*9*/
        /*A*/    TokenKind.Configuration,   TokenKind.Public,   TokenKind.Private,   TokenKind.Static,             /*A*/
        /*B*/    TokenKind.Interface,       TokenKind.Enum,     TokenKind.Namespace, TokenKind.Module,             /*B*/
        /*C*/    TokenKind.Type,            TokenKind.Assembly, TokenKind.Command,   TokenKind.Hidden,             /*C*/
        /*D*/    TokenKind.Base,            TokenKind.Default,  TokenKind.Clean,                                   /*D*/
        };

        public static string Sanitize(string scriptContent)
        {
            Sanitizer psBeautifier = new Sanitizer();

            System.Collections.ObjectModel.Collection<PSParseError> errors = null;
            System.Collections.ObjectModel.Collection<PSToken> tokens = null;

            tokens = PSParser.Tokenize(scriptContent, out errors);

            StringBuilder code = new StringBuilder();

            int indent = 0;
            int newEnd = 0;

            string previousToken = "";

            var psTypes = PSSanitizerCollections.PSTypes;
            var psCommands = PSSanitizerCollections.PSCommands;


            foreach (var nn in tokens)
            {
                string extContent = GetExtContent(scriptContent, nn);

                if (newEnd < nn.Start)
                    code.Append(scriptContent.Substring(newEnd, nn.Start - newEnd));

                newEnd = nn.Start + nn.Length;

                PSToken nextToken;
                StringBuilder optStrBuilder = new StringBuilder();

                switch (nn.Type)
                {
                    case PSTokenType.Keyword:
                        string newContent = extContent.ToLower();

                        if (s_keywordText.Contains(newContent))
                        {
                            code.AppendLine();
                            code.Append(GetIndent(ref indent));
                        }

                        code.Append(newContent);

                        break;
                    case PSTokenType.Command:
                        newContent = extContent.ToLower();

                        if (psCommands.ContainsKey(newContent))
                        {
                            newContent = psCommands[newContent].ToString();
                        }
                        else
                        {
                            (0).ToString();
                        }

                        code.Append(newContent);

                        break;
                    case PSTokenType.Type:

                        //var lContent = extContent.ToLower();
                        //newContent = extContent;


                        //if (psTypes.ContainsKey(lContent))
                        //{
                        //    newContent = psTypes[lContent].ToString();
                        //}

                        code.Append(psTypes.GetFixedCaseFullname(extContent));

                        break;
                    case PSTokenType.Attribute:
                    case PSTokenType.CommandArgument:
                    case PSTokenType.CommandParameter:
                        code.Append(extContent);
                        break;
                    default:
                        code.Append(extContent);
                        break;
                }

                previousToken = extContent;
            }

            string result = code.ToString().Replace("'+'", "");


            return result;
        }

        private static string GetExtContent(string text, PSToken nn)
        {
            return text.Substring(nn.Start, nn.Length);
        }

        private static PSToken GetNextToken(System.Collections.ObjectModel.Collection<PSToken> tokens, PSToken nn)
        {
            int currentTokenIdx = tokens.IndexOf(nn);

            if (currentTokenIdx + 1 < tokens.Count)
                return tokens[currentTokenIdx + 1];
            else
                return null;
        }


        public static string GetIndent(ref int num)
        {
            if (num < 0) num = 0;

            return new string(' ', num);
        }

        //private static SortedDictionary<string, string> CollectPSCommands()
        //{
        //    return new SortedDictionary<string, string>();
        //    var psCommands = new SortedDictionary<string, string>();
        //    using (var ps = PowerShell.Create())
        //    {
        //        ps.AddScript("Get-Command");
        //        var results = ps.Invoke();
        //        foreach (PSObject result in results)
        //        {
        //            string name = result.ToString().ToLower();

        //            if (!psCommands.ContainsKey(name))
        //                psCommands.Add(name, result.ToString());
        //        }
        //    }

        //    return psCommands;
        //}

        private static SortedDictionary<string, string> CollectPSNameSpaces()
        {
            var psCommands = new SortedDictionary<string, string>();
            using (var ps = PowerShell.Create())
            {
                ps.AddScript(@"
            [AppDomain]::CurrentDomain.GetAssemblies() |
            ForEach-Object {
                $_.GetTypes() | ForEach-Object {
                    $_.Namespace
                }
            } | Sort-Object -Unique
            ");
                var results = ps.Invoke();
                foreach (PSObject result in results)
                {
                    string name = result.ToString().ToLower();

                    if (!psCommands.ContainsKey(name))
                        psCommands.Add(name, result.ToString());
                }
            }

            return psCommands;
        }


        //private static NestedNamespaceAtom CollectPSTypes()
        //{

        //    NestedNamespaceAtom rootAtom = new NestedNamespaceAtom();

        //    using (var ps = PowerShell.Create())
        //    {
        //        ps.AddScript(@"
        //            [AppDomain]::CurrentDomain.GetAssemblies() |
        //            ForEach-Object {
        //                $_.GetTypes() | Where-Object { $_.IsPublic -and $_.FullName } | ForEach-Object {
        //                    $_.FullName
        //                }
        //            } | Sort-Object -Unique
        //            ");

        //        var results = ps.Invoke();
        //        foreach (PSObject result in results)
        //        {
        //            string formatedName = result.ToString();

        //            rootAtom.AddFullName(formatedName);

        //            string name = "[" + formatedName.ToLower() + "]";

        //            foreach (var defNameSpace in defaultNamespaces)
        //            {
        //                if (formatedName.StartsWith(defNameSpace))
        //                {
        //                    string addName = formatedName.Substring(defNameSpace.Length + 1);


        //                    rootAtom.AddFullName(addName);

        //                }
        //            }

        //        }
        //    }

        //    return rootAtom;
        //}
    }
}