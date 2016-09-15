
namespace VRageRender
{
    /// <summary>
    /// Defines sprite mirroring options.
    /// </summary>
    /// <remarks>
    /// Description is taken from original XNA <a href='http://msdn.microsoft.com/en-us/library/VRageMath.graphics.spriteeffects.aspx'>SpriteEffects</a> class.
    /// </remarks>
    public enum SpriteEffects
    {
        /// <summary>
        /// No rotations specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Rotate 180 degrees around the Y axis before rendering.
        /// </summary>
        FlipHorizontally = 1,

        /// <summary>
        /// Rotate 180 degrees around the X axis before rendering.
        /// </summary>
        FlipVertically = 2,

        /// <summary>
        /// Rotate 180 degress around both the X and Y axis before rendering.
        /// </summary>
        FlipBoth = FlipHorizontally | FlipVertically,
    };
}
