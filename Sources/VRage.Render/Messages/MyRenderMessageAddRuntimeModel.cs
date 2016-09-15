namespace VRageRender.Messages
{    
    public class MyRenderMessageAddRuntimeModel : MyRenderMessageBase
    {
        public string Name;
        public string ReplacedModel;
        public MyModelData ModelData = new MyModelData();

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.AddRuntimeModel; } }
    }
}
