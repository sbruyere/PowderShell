using CommandLine;
using System.Diagnostics;


namespace PowderShell.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
               .WithParsed(opts => RunOptionsAndReturnExitCode(opts))
               .WithNotParsed(errs => HandleParseError(errs));

        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            if (opts.InputFiles.Count() > 0)
                foreach (var filename in opts.InputFiles)
                {

                    string fileContent = File.ReadAllText(filename);

                    string result = DeobfuscateContent(fileContent);

                    if (!string.IsNullOrWhiteSpace(opts.Output))
                        File.AppendAllText(opts.Output, result);
                    else
                        Console.Out.Write(result);

                }

            if (opts.stdin)
            {
                string fileContent = Console.In.ReadToEnd();

                string result = DeobfuscateContent(fileContent);

                if (!string.IsNullOrWhiteSpace(opts.Output))
                    File.AppendAllText(opts.Output, result);
                else
                    Console.Out.Write(result);
            }
        }

        private static string DeobfuscateContent(string content)
        {
            Stopwatch perfWatch = new Stopwatch();

            perfWatch.Start();

            var result = PowderShell.DeObfuscate(Sanitizer.Sanitize(content));

            perfWatch.Stop();
            Console.WriteLine($"Computed in {perfWatch.ElapsedMilliseconds} ms.");

            return result;
        }
    }
}
