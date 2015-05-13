using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage
{
    public interface IMyBufferedInputSource
    {
        /// <summary>
        /// Swaps internal buffer with the one passed as argument. This swapping operation
        /// must be implemented in a thread safe manner. Buffer passed into the function will
        /// replaced by the internal buffer and returned in the same variable.
        /// </summary>
        void SwapBufferedTextInput(ref List<char> swappedBuffer);

        Vector2 MousePosition { get; }
        Vector2 MouseAreaSize { get; }
    }
}
