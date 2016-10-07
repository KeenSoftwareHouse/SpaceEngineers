using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using VRage.Utils;

namespace VRage.Utils
{
    /// <summary>
    /// MyFileSystemUtils
    /// </summary>
    public static partial class MyUtils
    {
        /// <summary>
        /// Vytvori zadany adresar. Automaticky povytvara celu adresarovu strukturu, teda ak chcem vytvorit c:\volaco\opica
        /// a c:\volaco zatial neexistuje, tak tato metoda ho vytvori.
        /// </summary>
        /// <param name="folderPath"></param>
        public static void CreateFolder(String folderPath)
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        // SHALLOW copy of a directory
        public static void CopyDirectory(string source, string destination)
        {
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            if (System.IO.Directory.Exists(source))
            {
                if (!Directory.Exists(destination))
                    Directory.CreateDirectory(destination);

                string[] files = Directory.GetFiles(source);

                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    string fileName = Path.GetFileName(s);
                    string destFile = Path.Combine(destination, fileName);
                    File.Copy(s, destFile, true);
                }
            }
#endif // !XB1
        }

        // Strips invalid chars in a filename (:, @, /, etc...)
        public static string StripInvalidChars(string filename)
        {
            return Path.GetInvalidFileNameChars().Aggregate(filename, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
    }
}
