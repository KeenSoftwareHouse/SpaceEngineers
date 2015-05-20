using System;
using ProtoBuf;
using VRageMath;

//  Object builder is object that defines how to create instance of particular MyPhysObject**
//  Every object builder class must implement parameter-less constructor (needed when loading objects)

namespace Sandbox.Common.ObjectBuilders
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
    public abstract class MyObjectBuilder_EntityBase : MyObjectBuilder_Base
    {
        [ProtoMember]
        public long EntityId;

        [ProtoMember]
        public MyPersistentEntityFlags2 PersistentFlags;

        [ProtoMember]
        public string Name;

        [ProtoMember]
        public MyPositionAndOrientation? PositionAndOrientation;

        // Tells XML Serializer that PositionAndOrientation should be serialized only if it has value
        public bool ShouldSerializePositionAndOrientation()
        {
            return PositionAndOrientation.HasValue;
        }

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
