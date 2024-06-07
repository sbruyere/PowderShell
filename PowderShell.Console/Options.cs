using CommandLine;

namespace PowderShell.CLI
{
    class Options
    {
        [Option('p', "path", Group = "inputGroup", HelpText = "Path of directory or script file(s) to be deobfuscated.")]
        public IEnumerable<string> InputFiles { get; set; }

        [Option("stdin",
          Default = false,
          Group = "inputGroup",
          HelpText = "Read from stdin")]
        public bool stdin { get; set; }

        [Option('o', "output", Required = false, Default = null, HelpText = "File offset.")]
        public string Output { get; set; }

    }
}
