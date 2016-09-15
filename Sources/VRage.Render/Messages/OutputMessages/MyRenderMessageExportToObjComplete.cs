
namespace VRageRender.Messages
{
    public class MyRenderMessageExportToObjComplete : MyRenderMessageBase
    {
        public bool Success;
        public string Filename;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ExportToObjComplete; } }
    }
}
