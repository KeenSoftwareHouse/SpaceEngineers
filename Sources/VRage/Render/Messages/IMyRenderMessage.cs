
namespace VRageRender
{
    public interface IMyRenderMessage
    {
        /// <summary>
        /// Get message class
        /// </summary>
        MyRenderMessageType MessageClass { get; }

        /// <summary>
        /// Gets message type
        /// </summary>
        MyRenderMessageEnum MessageType { get; }
    }
}
