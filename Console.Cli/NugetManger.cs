using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static System.Environment;

namespace ConsoleCli
{
    public class ConsoleLogger : ILogger
    {
        public void Log(LogLevel level, string data)
        {
            Console.WriteLine($"Level:{level} data:{data}");
        }

        public void Log(ILogMessage message)
        {
            Console.WriteLine($"Level:{message.Level} data:{message.Message}");
        }

        public Task LogAsync(LogLevel level, string data)
        {
            Console.WriteLine($"Level:{level} data:{data}");
            return Task.CompletedTask;
        }

        public Task LogAsync(ILogMessage message)
        {
            return Task.CompletedTask;
        }

        public void LogDebug(string data)
        {
            Console.WriteLine($"data:{data}");
        }

        public void LogError(string data)
        {
            Console.WriteLine($"data:{data}");
        }

        public void LogInformation(string data)
        {
            Console.WriteLine($"data:{data}");
        }

        public void LogInformationSummary(string data)
        {
            Console.WriteLine($"data:{data}");
        }

        public void LogMinimal(string data)
        {
            Console.WriteLine($"data:{data}");
        }

        public void LogVerbose(string data)
        {
            Console.WriteLine($"data:{data}");
        }

        public void LogWarning(string data)
        {
            Console.WriteLine($"data:{data}");
        }
    }

    class NugetManger
    {
        public static void Execute(ProjectOptions options)
        {
            if (TryProjectFiles(options, out IEnumerable<ProjectFile> projectFiles, out NugetJsonConfig nugetJson))
            {
                var logger = new ConsoleLogger();

                foreach (var file in projectFiles)
                {
                    var fileInfo = new FileInfo(file.RootDirectory);
                    var targetDir = Path.Combine(fileInfo.Directory.Parent.ToString(), "nupkgs");

                    Clear(targetDir);

                    foreach (var project in file.Projecs)
                    {
                        var propsFile = Path.Combine(file.RootDirectory, "Directory.Build.props");
                        if (IsNuget(project) || (File.Exists(propsFile) && IsNuget(propsFile)))
                        {
                            var packCommand = GeneratorPackCommand(project, targetDir);
                            RunCommand(packCommand);
                        }
                    }

                    var repository = Repository.Factory.GetCoreV3(nugetJson.Url);
                    var resource = repository.GetResource<PackageUpdateResource>();

                    var nupkgFiles = Directory.EnumerateFiles(targetDir, "*.nupkg").ToList();

                    resource
                    .Push(
                        nupkgFiles,
                        symbolSource: null,
                        timeoutInSecond: 5 * 60,
                        disableBuffering: false,
                        getApiKey: packageSource => nugetJson.ApiKey,
                        getSymbolApiKey: packageSource => null,
                        noServiceEndpoint: false,
                        skipDuplicate: false,
                        symbolPackageUpdateResource: null,
                        logger
                     )
                    .ContinueWith((task) =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion)
                            Console.WriteLine("处理完成");

                        if (task.Status == TaskStatus.Faulted)
                            Console.WriteLine(task.Exception.InnerException);
                    });
                }
            }
        }

        private static void Clear(string targetDir)
        {
            if (Directory.Exists(targetDir))
            {
                foreach(var file in Directory.EnumerateFiles(targetDir))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {

                    }
                }
            }
        }

        private static string GeneratorPackCommand(string project,string targetDir)
        {
            return $"dotnet pack {project} -o {targetDir}";
        }

        private static void RunCommand(string command)
        {
            using (var cmd = new Process())
            {
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.Start();

                cmd.StandardInput.WriteLine(command);
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();
                cmd.WaitForExit();

                //var result = cmd.StandardOutput.ReadToEnd();
            }
        }

        private static bool TryPackable(XmlDocument xmldoc, out bool result)
        {
            result = false;

            var node = xmldoc.SelectSingleNode("Project/PropertyGroup/IsPackable");

            if (node == null)
                return false;

            var value = node.InnerText;

            if (bool.TryParse(value, out result))
            {
                return true;
            }

            return false;
        }

        private static bool TryGeneratePackageOnBuild(XmlDocument xmldoc, out bool result)
        {
            result = false;

            var node = xmldoc.SelectSingleNode("Project/PropertyGroup/GeneratePackageOnBuild");

            if (node == null)
                return false;

            var value = node.InnerText;

            if (bool.TryParse(value, out result))
            {
                return true;
            }

            return false;
        }

        private static bool IsNuget(string projectFile)
        {
            var text = File.ReadAllText(projectFile);
            var xmldoc = new XmlDocument();

            xmldoc.LoadXml(text);

            if (TryGeneratePackageOnBuild(xmldoc, out bool result))
                return result;

            if (TryPackable(xmldoc, out result))
                return result;

            return false;
        }

        private static NugetJsonConfig ReaderConfig(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"全局配置文件:{filename}不存在。");
                Console.WriteLine($"会自动创建");

                var json = JsonConvert.SerializeObject(new NugetJsonConfig() { Url = "url", ApiKey = "apikey" });

                File.AppendAllText(filename, json);

                return null;
            }
            else
            {
                try
                {
                    var text = File.ReadAllText(filename);
                    var instance = JsonConvert.DeserializeObject<NugetJsonConfig>(text);

                    if (instance == null)
                    {
                        Console.WriteLine($"配置文件为空。");
                        return null;
                    }

                    if (instance.Url != "url" && !string.IsNullOrWhiteSpace(instance.Url))
                        return instance;

                    return null;
                }
                catch
                {
                    Console.WriteLine($"读取配置文件错误。");

                    return null;
                }
            }
        }

        private static NugetJsonConfig Reader(ProjectOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ConfigFile))
            {
                var basePath = Environment.GetFolderPath(SpecialFolder.UserProfile);
                var filename = Path.Combine(basePath, "Config.json");

                return ReaderConfig(filename);
            }
            else
            {
                return ReaderConfig(options.ConfigFile);
            }
        }

        private static bool TryProjectFiles(ProjectOptions options, out IEnumerable<ProjectFile> projectFiles, out NugetJsonConfig nugetJson)
        {
            nugetJson = null;
            var result = options.InputFiles.All(_file => IsSlnFile(_file) || IsProjectFile(_file));

            projectFiles = null;

            if (result)
            {
                projectFiles = OptiosToProjectFile(options);
                nugetJson = Reader(options);
            }

            return result;
        }

        private static bool IsSlnFile(string inputFile)
        {
            var filename = Path.GetFileName(inputFile);

            return File.Exists(inputFile) && filename.EndsWith(".sln");
        }

        private static bool IsProjectFile(string inputFile)
        {
            var filename = Path.GetFileName(inputFile);

            return File.Exists(inputFile) && filename.EndsWith(".csproj");
        }

        private static IEnumerable<string> Projects(string inputFile)
        {
            var directory = Path.GetDirectoryName(inputFile);

            var lines = File.ReadAllLines(inputFile);
            var regex = new Regex("^Project.+?=.*?\",\\s*?\"([^\"]+)");

            foreach (var line in lines)
            {
                if (regex.IsMatch(line))
                {
                    var match = regex.Match(line);
                    var value = match.Groups[1].Value;
                    var target = Path.Combine(directory, value);

                    if (File.Exists(target))
                        yield return target;
                }
            }
        }

        private static IEnumerable<ProjectFile> OptiosToProjectFile(ProjectOptions options)
        {
            foreach(var file in options.InputFiles)
            {
                if (IsSlnFile(file))
                {
                    var directory = Path.GetDirectoryName(file);
                    yield return new ProjectFile(directory, Projects(file));
                }

                if (IsProjectFile(file))
                {
                    var directory = Path.GetDirectoryName(file);
                    yield return new ProjectFile(directory, new List<string>() { file });
                }
            }
        }
    }
}
