using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace PowderShell
{
    public partial class GlobalCollections
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
        /*16*/  TokenKind.Join,         TokenKind.Shl,          TokenKind.Shr,          TokenKind.Equals,
        /*17*/  TokenKind.PlusEquals
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
                /*16*/  "-join",     "-shl",     "-shr",     "=" , "+="           /*16*/
            };

    }
}
