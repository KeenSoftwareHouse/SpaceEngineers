using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox
{
    class ErrorInfo
    {
        public string Match;
        public string Caption;
        public string Message;

        public ErrorInfo(string match, string caption, string message)
        {
            Match = match;
            Caption = caption;
            Message = message;
        }
    }

    class MyErrorTexts
    {
        public static ErrorInfo[] Infos = new ErrorInfo[]
        {
            new ErrorInfo("Unable to load DLL 'd3dx9_43.dll':",
                "DirectX cannot be loaded",
                "DirectX cannot be loaded, please make sure you have installed latest version. For more information see troubleshooting section on official game website."),
        };
    }
}
