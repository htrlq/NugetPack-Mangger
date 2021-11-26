using System.Collections.Generic;

namespace ConsoleCli
{
    class ProjectFile
    {
        public string RootDirectory { get; }
        public IEnumerable<string> Projecs { get; }

        public ProjectFile(string rootDirectory, IEnumerable<string> projecs)
        {
            RootDirectory = rootDirectory;
            Projecs = projecs;
        }
    }
}
