using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VRage.FileSystem
{
    public class MyFileHelper
    {
        public static bool CanWrite(string path)
        {
            if (!File.Exists(path))
                return true;

            try
            {
                using (var f = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
