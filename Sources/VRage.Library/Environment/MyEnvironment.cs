using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Library
{
#if XB1
    public static class MyEnvironment
    {
        public static bool Is64BitProcess
        {
            get
            {
                return true;
            }
        }
        public static string NewLine
        {
            get
            {
                return "\r\n";
            }
        }
        public static int ProcessorCount //TODO
        {
            get
            {
                return Environment.ProcessorCount;
            }
        }
        public static int TickCount //TODO
        {
            get
            {
                return Environment.TickCount;
            }
        }
        public static long WorkingSetForMyLog // TODO?
        {
            get
            {
                return 0;
            }
        }
    }
#else // !XB1
    public static class MyEnvironment
    {
        public static bool Is64BitProcess
        {
            get
            {
                return Environment.Is64BitProcess;
            }
        }
        public static string NewLine
        {
            get
            {
                return Environment.NewLine;
            }
        }
        public static int ProcessorCount
        {
            get
            {
                return Environment.ProcessorCount;
            }
        }
        public static int TickCount
        {
            get
            {
                return Environment.TickCount;
            }
        }
        public static long WorkingSetForMyLog
        {
            get
            {
                return Environment.WorkingSet;
            }
        }
    }
#endif // !XB1
}
