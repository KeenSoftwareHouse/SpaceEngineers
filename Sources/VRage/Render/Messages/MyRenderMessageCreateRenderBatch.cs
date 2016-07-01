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

    public class MyRenderMessageUpdateRenderInstanceBufferSettings : MyRenderMessageBase
    {
        public uint ID;

        // Force the buffer lod (for the model), -1 should be automatic.
        public int ForcedLod;

        // Weather instances should be lodded individually.
        public bool SetPerInstanceLod;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderInstanceBufferSettings; } }
    }

    /**
     * This is kinda your universal array operator splice().
     * 
     * The only thing is we cannot move elements with this.
     */
    public class MyRenderMessageUpdateRenderInstanceBufferRange : MyRenderMessageBase
    {
        public uint ID;

        // Instance data
        public MyInstanceData[] InstanceData;

        // Offset of the first instance to set (into the buffer)
        public int StartOffset;

        // Weather to trim the buffer from instances after (StartOffset + InstanceData.Count);
        public bool Trim;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderInstanceBufferRange; } }
    }
}
