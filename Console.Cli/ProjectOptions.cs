using CommandLine;
using System.Collections.Generic;

namespace ConsoleCli
{
    [Verb("project", HelpText = "项目")]
    class ProjectOptions
    {
        [Option('f', "file", Required = true, HelpText = "Sln 文件或者csproj 文件。")]
        public IEnumerable<string> InputFiles { get; set; }

        [Option('c', "config", Required = false, HelpText = "配置文件。")]
        public string ConfigFile { get; set; }
    }
}
