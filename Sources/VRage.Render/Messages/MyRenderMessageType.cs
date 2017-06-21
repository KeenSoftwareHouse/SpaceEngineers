namespace VRageRender.Messages
{
    public enum MyRenderMessageType
    {
        /// <summary>
        /// Draw message
        /// Skipped when renderer is falling behind and has to process multiple enqueued frames (only handled in last frame before draw)
        /// Draw sprite, Draw light
        /// </summary>
        Draw,

        /// <summary>
        /// Debug Draw message, in render11 these messages are queued internally
        /// Skipped when renderer is falling behind and has to process multiple enqueued frames (only handled in last frame before draw)
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
