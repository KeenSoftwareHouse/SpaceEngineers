using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamSDK;

namespace Sandbox
{
    public class VRageGameServices
    {
        public readonly MySteamService SteamService;

        public VRageGameServices(MySteamService steam)
        {
            SteamService = steam;
        }
    }
}
