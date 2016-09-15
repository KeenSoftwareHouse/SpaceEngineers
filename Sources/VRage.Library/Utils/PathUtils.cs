using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Library.Utils
{
    public static class PathUtils
    {
        public static string[] GetFilesRecursively(string path, string searchPath)
        {
            List<string> paths = new List<string>();
            GetfGetFilesRecursively(path, searchPath, paths);
            return paths.ToArray();
        }

        public static void GetfGetFilesRecursively(string path, string searchPath, List<string> paths)
        {
            paths.AddRange(Directory.GetFiles(path, searchPath));

            foreach (var directory in Directory.GetDirectories(path))
            {
                GetfGetFilesRecursively(directory, searchPath, paths);
            }
        }
    }
}
