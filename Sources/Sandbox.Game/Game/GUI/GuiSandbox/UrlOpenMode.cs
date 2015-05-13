using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Graphics.GUI
{
    [Flags]
    public enum UrlOpenMode
    {
        SteamOverlay = 0x1,
        ExternalBrowser = 0x2,
        ConfirmExternal = 0x4,

        SteamOrExternal = SteamOverlay | ExternalBrowser,
        SteamOrExternalWithConfirm = SteamOverlay | ExternalBrowser | ConfirmExternal,
    }
}
