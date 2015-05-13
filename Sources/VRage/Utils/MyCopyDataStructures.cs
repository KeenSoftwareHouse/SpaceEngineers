using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRage.Utils
{
    public static class MyCopyDataStructures
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SelectedTreeMsg
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string BehaviorTreeName;
        }
    }
}
