using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public enum MyRenderMessageType
    {
        /// <summary>
        /// Draw message, skipped when processing multiple updates (only handled in last update before draw)
        /// Draw sprite, debug draw...
        /// </summary>
        Draw,

        /// <summary>
        /// Debug Draw message, in render11 this messages are queued internally 
        /// Draw sprite, debug draw...
        /// </summary>
        DebugDraw,

        /// <summary>
        /// State change which can be applied only once, not applied when rendering same frame second time or more
        /// Add render object, remove render object...
        /// </summary>
        StateChangeOnce,

        /// <summary>
        /// State change which must be applied every time, even when drawing same frame multiple times
        /// Move render object, other interpolation messages
        /// </summary>
        StateChangeEvery,
    }
}
