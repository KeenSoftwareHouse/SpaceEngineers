using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VRage.FileSystem
{
    // Summary:
    //     Specifies whether to search the current directory, or the current directory
    //     and all subdirectories.
    public enum MySearchOption
    {
        // Summary:
        //     Includes only the current directory in a search.
        TopDirectoryOnly = 0,
        //
        // Summary:
        //     Includes the current directory and all the subdirectories in a search operation.
        //     This option includes reparse points like mounted drives and symbolic links
        //     in the search.
        AllDirectories = 1,
    }


    public interface IFileProvider
    {
        /// <summary>
        /// Opens file, returns null when file does not exists
        /// </summary>
        Stream Open(string path, FileMode mode, FileAccess access, FileShare share);

        /// <summary>
        /// True if directory exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Returns list of files in directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        IEnumerable<string> GetFiles(string path, string filter, MySearchOption searchOption);


        /// <summary>
        /// True if file exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool FileExists(string path);
    }
}
