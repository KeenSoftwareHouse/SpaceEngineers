using System.Collections.Generic;
using System.Reflection;
using VRageMath;

namespace Sandbox.Common.Input
{
    /// <summary>
    /// Snapshot of an input state, so it can be pumped to the game for testing
    /// </summary>
    [Obfuscation(Feature = Obfuscator.NoRename, Exclude = true)]
    public class MyInputSnapshot
    {
        public MyMouseSnapshot MouseSnapshot { get; set; }

        /// <summary>
        /// List of keys pressed when the frame was captured.
        /// </summary>
        public List<byte> KeyboardSnapshot { get; set; }

        public MyJoystickStateSnapshot JoystickSnapshot { get; set; }

        /// <summary>
        /// In milliseconds.
        /// </summary>
        public int SnapshotTimestamp { get; set; }

        // Workaround for Hardware mouse/Non-hardware mouse differences
        public Vector2 MouseCursorPosition { get; set; }
    }
}
