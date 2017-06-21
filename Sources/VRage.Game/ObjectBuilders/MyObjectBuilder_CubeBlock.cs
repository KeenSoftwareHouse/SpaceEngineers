using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.Utils;
using VRageMath;
using System.Diagnostics;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CubeBlock : MyObjectBuilder_Base
    {
        [ProtoMember, DefaultValue(0)]
        [Serialize(MyObjectFlags.DefaultZero)]
        public long EntityId = 0;
        public bool ShouldSerializeEntityId() { return EntityId != 0; }

        // TODO: Hotfixed, should inherit entity OB
        [Serialize(MyObjectFlags.Nullable)]
        public string Name;

        [ProtoMember]
        [Serialize(MyPrimitiveFlags.Variant, Kind = MySerializeKind.Item)]
        public SerializableVector3I Min = new SerializableVector3I(0, 0, 0);
        public bool ShouldSerializeMin() { return Min != new SerializableVector3I(0, 0, 0); }
        //[ProtoMember]
        //public SerializableVector3I Max;

        // Backward compatibility orientation.
        private SerializableQuaternion m_orientation;
        //[ProtoMember]
        [NoSerialize]
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
        [Serialize(MyPrimitiveFlags.Normalized | MyPrimitiveFlags.FixedPoint16)]
        public float IntegrityPercent = 1.0f;

        [ProtoMember, DefaultValue(1.0f)]
        [Serialize(MyPrimitiveFlags.Normalized | MyPrimitiveFlags.FixedPoint16)]
        public float BuildPercent = 1.0f;

        [ProtoMember]
        public SerializableBlockOrientation BlockOrientation = SerializableBlockOrientation.Identity;
        public bool ShouldSerializeBlockOrientation() { return BlockOrientation != SerializableBlockOrientation.Identity; }

        [ProtoMember, DefaultValue(null)]
        [NoSerialize]
        public MyObjectBuilder_Inventory ConstructionInventory = null;
        public bool ShouldSerializeConstructionInventory() { return false; }

        [ProtoMember]
        [NoSerialize]
        public SerializableVector3 ColorMaskHSV = new SerializableVector3(0f, -1f, 0f);
        public bool ShouldSerializeColorMaskHSV() { return ColorMaskHSV != new SerializableVector3(0f, -1f, 0f); }

        [Serialize]
        private byte m_colorH { get { return (byte)(ColorMaskHSV.X * 255); } set { ColorMaskHSV.X = value / 255.0f; } }

        [Serialize]
        private byte m_colorS { get { return (byte)((ColorMaskHSV.Y * 0.5f + 0.5f) * 255); } set { ColorMaskHSV.Y = value / 255.0f * 2 - 1; } }

        [Serialize]
        private byte m_colorV { get { return (byte)((ColorMaskHSV.Z * 0.5f + 0.5f) * 255); } set { ColorMaskHSV.Z = value / 255.0f * 2 - 1; } }

        public static MyObjectBuilder_CubeBlock Upgrade(MyObjectBuilder_CubeBlock cubeBlock, MyObjectBuilderType newType, string newSubType)
        {
            var upgraded = MyObjectBuilderSerializer.CreateNewObject(newType, newSubType) as MyObjectBuilder_CubeBlock;
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
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_ConstructionStockpile ConstructionStockpile = null;
        public bool ShouldSerializeConstructionStockpile() { return ConstructionStockpile != null; }

        [ProtoMember, DefaultValue(0)]
        [Serialize(MyObjectFlags.DefaultZero)]
        public long Owner = 0;

        [ProtoMember, DefaultValue(0)]
        [Serialize(MyObjectFlags.DefaultZero)]
        public long BuiltBy = 0;

        //[ProtoMember, DefaultValue(false)]
        //public bool ShareWithFaction = false;

        //[ProtoMember, DefaultValue(false)]
        //public bool ShareWithAll = false;

        [ProtoMember, DefaultValue(MyOwnershipShareModeEnum.None)]
        public MyOwnershipShareModeEnum ShareMode = MyOwnershipShareModeEnum.None;

        [ProtoMember, DefaultValue(0)]
        [NoSerialize]
        public float DeformationRatio = 0;

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
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public MySubBlockId[] SubBlocks = null;

        [ProtoMember, DefaultValue(0)]
        [Serialize(MyObjectFlags.DefaultZero)]
        public int MultiBlockId = 0;
        public bool ShouldSerializeMultiBlockId() { return MultiBlockId != 0; }

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public SerializableDefinitionId? MultiBlockDefinition = null;
        public bool ShouldSerializeMultiBlockDefinition() { return MultiBlockId != 0 && MultiBlockDefinition != null; }

        [ProtoMember, DefaultValue(-1)]
        [Serialize]
        public int MultiBlockIndex = -1;

        [ProtoMember, DefaultValue(1f)]
        [Serialize]
        public float BlockGeneralDamageModifier = 1f;

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_ComponentContainer ComponentContainer = null;
        public bool ShouldSerializeComponentContainer()
        {
            return ComponentContainer != null && ComponentContainer.Components != null && ComponentContainer.Components.Count > 0;
        }

        public virtual void Remap(IMyRemapHelper remapHelper)
        {
            if (EntityId != 0)
                EntityId = remapHelper.RemapEntityId(EntityId);

            if (SubBlocks != null)
            {
                for (int i = 0; i < SubBlocks.Length; ++i)
                {
                    if (SubBlocks[i].SubGridId != 0)
                        SubBlocks[i].SubGridId = remapHelper.RemapEntityId(SubBlocks[i].SubGridId);
                }
            }

            if (MultiBlockId != 0 && MultiBlockDefinition != null)
                MultiBlockId = remapHelper.RemapGroupId("MultiBlockId", MultiBlockId);
        }

        public virtual void SetupForProjector()
        {
            //GK: Only for projector remove ownership of blueprint block at initialization (Or else can cause incosistences to ownership manager)
            Owner = 0;
            ShareMode = MyOwnershipShareModeEnum.None;
            EntityId = 0; //GK: Will cause new allocation of ID for projector blueprints. In some cases the id will be the same as in projected grid when welded and entity will not be added
        }
    }
}
