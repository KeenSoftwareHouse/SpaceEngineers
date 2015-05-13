using System;
using System.IO;
using VRage;

//  Our build numbers are internaly stored as int32.
//  But sometimes we transfer them as string in format: MAJOR_MINOR1_MINOR2 (two digits _ three digits _ three digits)
//  MAJOR - changes with each released game
//  MINOR1 - important release, important milestone
//  MINOR2 - especialy for builds or small fixes
//  In fact, minor1 and minor2 contain a lot of reserve.

//  Examples:
//  00_000_015 = 00000015
//  01_000_005 = 01000005
//  01_000_062 = 01000062
//  01_001_000 = 01001000
//  22_005_100 = 22005100


namespace VRage.Utils
{
    public static class MyMwcConstants
    {
        //  Number of bytes that our MAC password have
        public const int MAC_PASSWORD_LENGTH = 8;
    }

    public static class MyBuildNumbers
    {
        const int LENGTH_MAJOR = 2;
        const int LENGTH_MINOR1 = 3;
        const int LENGTH_MINOR2 = 3;
        public const string SEPARATOR = "_";

        public static int GetBuildNumberWithoutMajor(int buildNumberInt)
        {
            int mask = 1;
            for (int i = 0; i < LENGTH_MINOR1 + LENGTH_MINOR2; i++)
                mask *= 10;
            return buildNumberInt - ((buildNumberInt / mask) * mask);
        }

        public static string ConvertBuildNumberFromIntToString(int buildNumberInt)
        {
            string str = MyUtils.AlignIntToRight(buildNumberInt, LENGTH_MAJOR + LENGTH_MINOR1 + LENGTH_MINOR2, '0');
            return 
                str.Substring(0, LENGTH_MAJOR) +
                SEPARATOR + str.Substring(LENGTH_MAJOR, LENGTH_MINOR1) +
                SEPARATOR + str.Substring(LENGTH_MAJOR + LENGTH_MINOR1, LENGTH_MINOR2);
        }

        //  Check if specified build number is valid
        public static bool IsValidBuildNumber(string buildNumberString)
        {
            return ConvertBuildNumberFromStringToInt(buildNumberString) != null;
        }

        public static int? ConvertBuildNumberFromStringToInt(string buildNumberString)
        {
            if (buildNumberString.Length < (2 * SEPARATOR.Length + LENGTH_MAJOR + LENGTH_MINOR1 + LENGTH_MINOR2))
            {
                //  String is too short to contain the number
                return null;
            }

            if ((buildNumberString.Substring(LENGTH_MAJOR, SEPARATOR.Length) != SEPARATOR) ||
                (buildNumberString.Substring(LENGTH_MAJOR + SEPARATOR.Length + LENGTH_MINOR1, SEPARATOR.Length) != SEPARATOR))
            {
                //  Separators aren't where they should be
                return null;
            }

            string major = buildNumberString.Substring(0, LENGTH_MAJOR);
            string minor1 = buildNumberString.Substring(LENGTH_MAJOR + SEPARATOR.Length, LENGTH_MINOR1);
            string minor2 = buildNumberString.Substring(LENGTH_MAJOR + SEPARATOR.Length + LENGTH_MINOR1 + SEPARATOR.Length, LENGTH_MINOR2);

            int majorInt;
            if (Int32.TryParse(major, out majorInt) == false)
            {
                return null;
            }

            int minor1Int;
            if (Int32.TryParse(minor1, out minor1Int) == false)
            {
                return null;
            }

            int minor2Int;
            if (Int32.TryParse(minor2, out minor2Int) == false)
            {
                return null;
            }

            return Int32.Parse(major + minor1 + minor2);
        }

        //  Gets build number from filename (used by launcher). If not possible because filename is of different format, return null.
        public static int? GetBuildNumberFromFileName(string filename, string executableFileName, string extensionName)
        {
            if (filename.Length < (executableFileName.Length + 3 * SEPARATOR.Length + LENGTH_MAJOR + LENGTH_MINOR1 + LENGTH_MINOR2))
            {
                //  Filename is too short to contain build number
                return null;
            }

            if (filename.Substring(executableFileName.Length, SEPARATOR.Length) != SEPARATOR)
            {
                //  Separators aren't where they should be
                return null;
            }

            if (new FileInfo(filename).Extension != extensionName)
            {
                //  Wrong extension
                return null;
            }

            return ConvertBuildNumberFromStringToInt(
                filename.Substring(
                executableFileName.Length + SEPARATOR.Length,
                LENGTH_MAJOR + SEPARATOR.Length + LENGTH_MINOR1 + SEPARATOR.Length + LENGTH_MINOR2));
        }

        public static string GetFilenameFromBuildNumber(int buildNumber, string executableFileName)
        {
            return executableFileName + SEPARATOR + ConvertBuildNumberFromIntToString(buildNumber) + ".exe";
        }
    }
}