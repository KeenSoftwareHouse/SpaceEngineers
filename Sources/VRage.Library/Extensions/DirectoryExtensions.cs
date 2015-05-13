using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VRage
{
    public static class DirectoryExtensions
    {
        public static void CopyAll(string source, string target)
        {
            EnsureDirectoryExists(target);

            foreach (var file in new DirectoryInfo(source).GetFiles())
            {
                file.CopyTo(Path.Combine(target, file.Name), true);
            }

            foreach (DirectoryInfo sourceSubdirectory in new DirectoryInfo(source).GetDirectories())
            {
                DirectoryInfo targetSubdirectory = Directory.CreateDirectory(Path.Combine(target, sourceSubdirectory.Name));
                CopyAll(sourceSubdirectory.FullName, targetSubdirectory.FullName);
            }
        }

        public static void EnsureDirectoryExists(string path)
        {
            var directory = new DirectoryInfo(path);
            if (directory.Parent != null)
            {
                EnsureDirectoryExists(directory.Parent.FullName);
            }

            if (!directory.Exists)
            {
                directory.Create();
            }
        }
    }
}
