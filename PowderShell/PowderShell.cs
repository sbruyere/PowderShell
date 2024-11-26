using System.Management.Automation.Language;

namespace PowderShell
{
    public static class PowderShell
    {
        public static string DeObfuscate(string psCode)
        {
            ParseError[] errors = null;
            Token[] tokens = null;

            psCode = psCode.Replace("$env:cOmspeC", "\"C:\\Windows\\System32\\cmd.exe\"", StringComparison.InvariantCultureIgnoreCase);
            var ast = Parser.ParseInput(psCode, out tokens, out errors);

            var result = new ScriptBlockAstWrapper(null, ast);
            var strResult = result.ToString();

            return strResult;
        }

        public static AstWrapper DeObfuscateInWrapper(string psCode)
        {
            ParseError[] errors = null;
            Token[] tokens = null;

            psCode = psCode.Replace("$env:cOmspeC", "\"C:\\Windows\\System32\\cmd.exe\"", StringComparison.InvariantCultureIgnoreCase);
            var ast = Parser.ParseInput(psCode, out tokens, out errors);

            var result = new ScriptBlockAstWrapper(null, ast);
            return result;
        }
    }
}
