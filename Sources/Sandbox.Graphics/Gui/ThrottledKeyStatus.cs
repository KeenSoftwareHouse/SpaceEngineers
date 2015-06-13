namespace Sandbox.Graphics.GUI
{
    public enum ThrottledKeyStatus
    {
        /// <summary>
        /// The requested key is not pressed.
        /// </summary>
        UNPRESSED,

        /// <summary>
        /// The requested key is pressed, but it's waiting for a delay.
        /// </summary>
        PRESSED_AND_WAITING,

        /// <summary>
        /// The key is pressed and any time delay has passed.
        /// </summary>
        PRESSED_AND_READY
    }
}