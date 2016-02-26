using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public enum MyRenderInstanceBufferType
    {
        Cube,
        Generic,
        Invalid,
    }

    public class MyRenderMessageCreateRenderInstanceBuffer: MyRenderMessageBase
    {
        public uint ID;
        public string DebugName;
        public MyRenderInstanceBufferType Type;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateRenderInstanceBuffer; } }
    }

    public class MyRenderMessageUpdateRenderCubeInstanceBuffer : MyRenderMessageBase
    {
        public uint ID;
        public List<MyCubeInstanceData> InstanceData = new List<MyCubeInstanceData>();
        public int Capacity;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer; } }

        public override void Close()
        {
            InstanceData.Clear();

            base.Close();
        }
    }

    public class MyRenderMessageUpdateRenderInstanceBuffer : MyRenderMessageBase
    {
        public uint ID;
        public List<MyInstanceData> InstanceData = new List<MyInstanceData>();
        public int Capacity;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderInstanceBuffer; } }

        public override void Close()
        {
            InstanceData.SetSize(0);

            base.Close();
        }
    }
}
