using CommandLine;
using System;

namespace ConsoleCli
{

    class Program
    {
        static void Main(string[] args)
        {
             args = new[] {
                "--file",
                @"D:\OtherSource\WebApiClient\WebApiClientCore.sln"
            };
            Parser.Default
                .ParseArguments<ProjectOptions>(args)
                .WithParsed<ProjectOptions>(options =>
                {
                    NugetManger.Execute(options);
                });

            Console.ReadLine();
        }
    }
}
