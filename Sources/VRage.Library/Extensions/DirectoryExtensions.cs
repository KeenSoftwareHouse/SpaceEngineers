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

        public static bool IsParentOf(this DirectoryInfo dir, string absPath)
        {
            string parentPath = dir.FullName.TrimEnd(Path.DirectorySeparatorChar);
            var currentDir = new DirectoryInfo(absPath);

            while (currentDir.Exists)
            {
                if(currentDir.FullName.TrimEnd(Path.DirectorySeparatorChar).Equals(parentPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                if(!currentDir.FullName.TrimEnd(Path.DirectorySeparatorChar).StartsWith(parentPath))
                    return false;

                if(currentDir.Parent == null)
                    return false;

                currentDir = currentDir.Parent;
            }

            return false;
        }
    }
}
