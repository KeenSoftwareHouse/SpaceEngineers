using System;
using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;
using VRage.Utils;
using VRageMath;
using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Serializer;

namespace Sandbox.Common.ObjectBuilders
{

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CubeBlock : MyObjectBuilder_Base
    {
        [ProtoMember, DefaultValue(0)]
        public long EntityId = 0;
        public bool ShouldSerializeEntityId() { return EntityId != 0; }

        [ProtoMember]
        public SerializableVector3I Min;
        //[ProtoMember]
        //public SerializableVector3I Max;

        // Backward compatibility orientation.
        private SerializableQuaternion m_orientation;
        //[ProtoMember]
        public SerializableQuaternion Orientation
        {
            get { return m_orientation; }
            set
            {
                if (!MyUtils.IsZero(value))
                    m_orientation = value;
                else
                    m_orientation = Quaternion.Identity;
                BlockOrientation = new SerializableBlockOrientation(
                    Base6Directions.GetForward(m_orientation),
                    Base6Directions.GetUp(m_orientation));
            }
        }
        public bool ShouldSerializeOrientation() { return false; }

        [ProtoMember, DefaultValue(1.0f)]
        public float IntegrityPercent = 1.0f;

        [ProtoMember, DefaultValue(1.0f)]
        public float BuildPercent = 1.0f;

        [ProtoMember]
        public SerializableBlockOrientation BlockOrientation;

        [ProtoMember, DefaultValue(null)]
        public MyObjectBuilder_Inventory ConstructionInventory = null;
        public bool ShouldSerializeConstructionInventory() { return false; }

        [ProtoMember]
        public SerializableVector3 ColorMaskHSV = new SerializableVector3(0f, -1f, 0f);

        public static MyObjectBuilder_CubeBlock Upgrade(MyObjectBuilder_CubeBlock cubeBlock, MyObjectBuilderType newType, string newSubType)
        {
            var upgraded = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject(newType, newSubType) as MyObjectBuilder_CubeBlock;
            if (upgraded == null)
            {
                Debug.Fail("Cannot upgrade cube block, upgraded block is not derived from " + typeof(MyObjectBuilder_CubeBlock).Name);
                return null;
            }

            upgraded.EntityId = cubeBlock.EntityId;
            upgraded.Min = cubeBlock.Min;
            upgraded.m_orientation = cubeBlock.m_orientation;
            upgraded.IntegrityPercent = cubeBlock.IntegrityPercent;
            upgraded.BuildPercent = cubeBlock.BuildPercent;
            upgraded.BlockOrientation = cubeBlock.BlockOrientation;
            upgraded.ConstructionInventory = cubeBlock.ConstructionInventory;
            upgraded.ColorMaskHSV = cubeBlock.ColorMaskHSV;

            return upgraded;
        }

        [ProtoMember, DefaultValue(null)]
        public MyObjectBuilder_ConstructionStockpile ConstructionStockpile = null;
        public bool ShouldSerializeConstructionStockpile() { return ConstructionStockpile != null; }

        [ProtoMember, DefaultValue(0)]
        public long Owner = 0;

        //[ProtoMember, DefaultValue(false)]
        //public bool ShareWithFaction = false;

        //[ProtoMember, DefaultValue(false)]
        //public bool ShareWithAll = false;

        [ProtoMember]
        public MyOwnershipShareModeEnum ShareMode;

        [ProtoMember]
        public float DeformationRatio;

        [ProtoContract]
        public struct MySubBlockId
        {
            [ProtoMember]
            public long SubGridId;

            [ProtoMember]
            public string SubGridName;

            [ProtoMember]
            public SerializableVector3I SubBlockPosition;
        }

        [XmlArrayItem("SubBlock")]
        [ProtoMember]
        public MySubBlockId[] SubBlocks;


        public virtual void Remap(IMyRemapHelper remapHelper)
        {
            if (EntityId != 0) EntityId = remapHelper.RemapEntityId(EntityId);

            if (SubBlocks != null)
            {
                for (int i=0; i<SubBlocks.Length; ++i)
                {
                    if (SubBlocks[i].SubGridId != 0)
                        SubBlocks[i].SubGridId = remapHelper.RemapEntityId(SubBlocks[i].SubGridId);
                }
            }
        }

        public virtual void SetupForProjector()
        {
        }
    }
}
