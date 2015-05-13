using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common
{
    [Flags]
    public enum MyEntityUpdateEnum
    {
        NONE = 0,  //no update
        EACH_FRAME = 1,  //each 0.016s, 60 FPS    
        EACH_10TH_FRAME = 2,  //each 0.166s, 6 FPS
        EACH_100TH_FRAME = 4,  //each 1.666s, 0.6 FPS

        /// <summary>
        /// Separate update performed once before any other updates are called.
        /// </summary>
        BEFORE_NEXT_FRAME = 8,
    }
}
