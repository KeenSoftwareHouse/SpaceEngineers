using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using VRage.Utils;

namespace Sandbox.Common
{
    //  IMPORTANT: These are constants that must be checked before every official FINAL BUILD!
    //  They must be set to proper values, some must be increased. See comments for each one.
    public class MyFinalBuildConstants
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  This is version of application
        //  FINAL BUILD VALUE: Increase before every major build.

        public static MyVersion APP_VERSION = 01088001;        

        // For OnLive, CiiNOW and other cloud gaming services (disabled HW cursor, editor, multiplayer)
        public const bool   IS_CLOUD_GAMING = false;

        // Official builds are built by builder image, always obfuscated
        // Use this constant for example to decide whether post analytics to official stats or not
        // TRACE is set to false when doing official build
#if OFFICIAL_BUILD
        public const bool IS_OFFICIAL = true;
#else
        public const bool IS_OFFICIAL = false;
#endif

        public static StringBuilder APP_VERSION_STRING { get { return APP_VERSION.FormattedText; } }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if DEBUG
        public const bool IS_DEBUG = true;
#else
        public const bool IS_DEBUG = false;
#endif

        public const int IP_ADDRESS_ANY = 0;
        
        public const short DEDICATED_SERVER_PORT = 27015;
        public const short DEDICATED_STEAM_AUTH_PORT = 8766;
    }
}


