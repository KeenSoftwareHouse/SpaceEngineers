
namespace VRageRender
{
    public class MyRenderMessageExportToObjComplete : IMyRenderMessage
    {
        public bool Success;
        public string Filename;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ExportToObjComplete; } }
    }
}
