namespace PowderShell
{
    internal class Helpers
    {
        public static Dictionary<string, string> aliasToCommand = new Dictionary<string, string>()
        {
            { "%", "ForEach-Object" },
            { "?", "Where-Object" },
            { "ac", "Add-Content" },
            { "asnp", "Add-PSSnapin" },
            { "cat", "Get-Content" },
            { "cd", "Set-Location" },
            { "chdir", "Set-Location" },
            { "clc", "Clear-Content" },
            { "clear", "Clear-Host" },
            { "cls", "Clear-Host" },
            { "copy", "Copy-Item" },
            { "cp", "Copy-Item" },
            { "del", "Remove-Item" },
            { "diff", "Compare-Object" },
            { "dir", "Get-ChildItem" },
            { "echo", "Write-Output" },
            { "erase", "Remove-Item" },
            { "fc", "Format-Custom" },
            { "fl", "Format-List" },
            { "foreach", "ForEach-Object" },
            { "ft", "Format-Table" },
            { "fw", "Format-Wide" },
            { "gal", "Get-Alias" },
            { "gc", "Get-Content" },
            { "gci", "Get-ChildItem" },
            { "gcm", "Get-Command" },
            { "gdr", "Get-PSDrive" },
            { "gi", "Get-Item" },
            { "gl", "Get-Location" },
            { "gm", "Get-Member" },
            { "gmo", "Get-Module" },
            { "gp", "Get-ItemProperty" },
            { "gps", "Get-Process" },
            { "group", "Group-Object" },
            { "gsv", "Get-Service" },
            { "gwmi", "Get-WmiObject" },
            { "h", "Get-History" },
            { "history", "Get-History" },
            { "iwr", "Invoke-WebRequest" },
            { "kill", "Stop-Process" },
            { "ls", "Get-ChildItem" },
            { "man", "help" },
            { "md", "mkdir" },
            { "measure", "Measure-Object" },
            { "mi", "Move-Item" },
            { "mv", "Move-Item" },
            { "ni", "New-Item" },
            { "ps", "Get-Process" },
            { "pwd", "Get-Location" },
            { "rm", "Remove-Item" },
            { "rmdir", "Remove-Item" },
            { "sc", "Set-Content" },
            { "select", "Select-Object" },
            { "sort", "Sort-Object" },
            { "tee", "Tee-Object" },
            { "type", "Get-Content" },
            { "where", "Where-Object" },
            { "write", "Write-Output" },
            { "iex", "Invoke-Expression" }
        };

        internal static string IndentLines(int indent, string code)
        {
            List<string> lines = code.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(code))
                    lines[i] = new string(' ', indent) + lines[i];
            }

            if (string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                lines.RemoveAt(lines.Count - 1);

            return string.Join("\r\n", lines);
        }

        internal static string CommentLines(int indent, string code, string commentChr = "# ")
        {
            List<string> lines = code.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(code))
                    lines[i] = commentChr + new string(' ', indent) + lines[i];
            }

            if (string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                lines.RemoveAt(lines.Count - 1);

            return string.Join("\r\n", lines);
        }
    }
}
