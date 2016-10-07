using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System
{
    public static class SteamHelpers
    {
        public static bool IsSteamPath(string path)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                return dir.Parent.Name.Equals("Common", StringComparison.InvariantCultureIgnoreCase) &&
                        dir.Parent.Parent.Name.Equals("SteamApps", StringComparison.InvariantCultureIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAppManifestPresent(string path, uint appId)
        {
#if XB1
            //XB1_TODO?
            return false;
#else // !XB1
            try
            {
                var gameDir = new DirectoryInfo(path);
                return IsSteamPath(path) && Directory.GetFiles(gameDir.Parent.Parent.FullName).Contains("AppManifest_" + appId + ".acf", StringComparer.InvariantCultureIgnoreCase);
            }
            catch
            {
                return false;
            }
#endif // !XB1
        }
    }
}
