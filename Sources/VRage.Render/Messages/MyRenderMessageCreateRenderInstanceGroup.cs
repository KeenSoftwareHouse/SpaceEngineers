using System.Collections.Generic;
using VRage;

namespace VRageRender.Messages
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
        public uint ParentID;
        public string DebugName;
        public MyRenderInstanceBufferType Type;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateRenderInstanceBuffer; } }
    }

    public class MyRenderMessageUpdateRenderCubeInstanceBuffer : MyRenderMessageBase
    {
        public uint ID;
        public List<MyCubeInstanceData> InstanceData = new List<MyCubeInstanceData>();
        public List<MyCubeInstanceDecalData> DecalsData = new List<MyCubeInstanceDecalData>();
        public int Capacity;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer; } }

        public override void Init()
        {
            DecalsData.Clear();
        }

        public override void Close()
        {
            InstanceData.Clear();

            base.Close();
        }
    }

    public struct MyCubeInstanceDecalData
    {
        public uint DecalId;
        public int InstanceIndex;
    }



    public class MyRenderMessageUpdateRenderInstanceBufferSettings : MyRenderMessageBase
    {
        public uint ID;

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
