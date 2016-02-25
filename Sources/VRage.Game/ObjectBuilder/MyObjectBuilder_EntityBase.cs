using System;
using ProtoBuf;
using VRage.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using System.ComponentModel;
using VRage.Serialization;

namespace VRage.ObjectBuilders
{
    // Do not change numbers, these are saved in DB
    [Flags]
    public enum MyPersistentEntityFlags2
    {
        None = 0,
        Enabled = 1 << 1,
        CastShadows = 1 << 2,
        InScene = 1 << 4,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EntityBase : MyObjectBuilder_Base
    {
        [ProtoMember]
        public long EntityId;

        [ProtoMember]
        public MyPersistentEntityFlags2 PersistentFlags;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string Name;

        [ProtoMember]
        public MyPositionAndOrientation? PositionAndOrientation;

        // Tells XML Serializer that PositionAndOrientation should be serialized only if it has value
        public bool ShouldSerializePositionAndOrientation()
        {
            return PositionAndOrientation.HasValue;
        }

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_ComponentContainer ComponentContainer = null;

        public bool ShouldSerializeComponentContainer()
        {
            return ComponentContainer != null && ComponentContainer.Components != null && ComponentContainer.Components.Count > 0;
        }

        [ProtoMember, DefaultValue(null)]
        [NoSerialize]
        public SerializableDefinitionId? EntityDefinitionId = null;
        public bool ShouldSerializeEntityDefinitionId() { return false; } // Used for backward compatibility only

        /// <summary>
        /// Remaps this entity's entityId to a new value.
        /// If there are cross-referenced between different entities in this object builder, the remapHelper should be used to rememeber these
        /// references and retrieve them.
        /// </summary>
        public virtual void Remap(IMyRemapHelper remapHelper)
        {
            EntityId = remapHelper.RemapEntityId(EntityId);
        }
    }
}
