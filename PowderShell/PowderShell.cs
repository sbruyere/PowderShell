using System.Management.Automation.Language;

namespace PowderShell
{
    public static class PowderShell
    {
        public static string DeObfuscate(string psCode)
        {
            ParseError[] errors = null;
            Token[] tokens = null;

            var ast = Parser.ParseInput(psCode, out tokens, out errors);

            var result = new ScriptBlockAstWrapper(ast);
            var strResult = result.ToString();

            return strResult;
        }
    }
}
