using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Win32;

namespace VRage.Utils
{
    public static class MyWMCodes
    {
        public const uint COPYDATA = (uint)WinApi.WM.COPYDATA;

        public const uint BEHAVIOR_TOOL_SET_DATA = (uint)WinApi.WM.USER;
        public const uint BEHAVIOR_TOOL_CLEAR_NODES = (uint)WinApi.WM.USER + 1;
        public const uint BEHAVIOR_TOOL_END_SENDING_DATA = (uint)WinApi.WM.USER + 2;
        public const uint BEHAVIOR_TOOL_VALIDATE_TREE = (uint)WinApi.WM.USER + 3;
        public const uint BEHAVIOR_TOOL_TREE_UPLOAD_SUCCESS = (uint)WinApi.WM.USER + 4;
        public const uint BEHAVIOR_TOOL_SELECT_TREE = (uint)WinApi.WM.USER + 5;

        public const uint BEHAVIOR_GAME_UPLOAD_TREE = (uint)WinApi.WM.USER + 10;
        public const uint BEHAVIOR_GAME_RESUME_SENDING = (uint)WinApi.WM.USER + 11;
        public const uint BEHAVIOR_GAME_STOP_SENDING = (uint)WinApi.WM.USER + 12;

        public const uint GAME_IS_RUNNING_REQUEST = (uint)WinApi.WM.USER + 30;
        public const uint GAME_IS_RUNNING_RESULT = (uint)WinApi.WM.USER + 31;
    }
}
