using ProtoBuf;
using System;
using System.ComponentModel;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalObject : MyObjectBuilder_Base
    {
        [ProtoMember, DefaultValue(MyItemFlags.None)]
        public MyItemFlags Flags = MyItemFlags.None;

        /// <summary>
        /// This is used for GUI to show the amount of health points (durability) of the weapons and tools. This is updated through Durability entity component if entity exists..
        /// </summary>
        [ProtoMember, DefaultValue(null)]
        public float? DurabilityHP = null;
        public bool ShouldSerializeDurabilityHP()
        {
            return DurabilityHP.HasValue;
        }

        public virtual bool CanStack(MyObjectBuilder_PhysicalObject a)
        {
            if (a == null) return false;
            return CanStack(a.TypeId, a.SubtypeId, a.Flags);
        }

        public virtual bool CanStack(MyObjectBuilderType typeId, MyStringHash subtypeId, MyItemFlags flags)
        {
            if (flags == Flags &&
                typeId == TypeId &&
                subtypeId == SubtypeId)
            {
                return true;
            }
            return false;
        }

        public MyObjectBuilder_PhysicalObject(): this(MyItemFlags.None) {}

        public MyObjectBuilder_PhysicalObject(MyItemFlags flags)
        {
            Flags = flags;
        }

        public virtual MyDefinitionId GetObjectId()
        {
            return this.GetId();
        }
    }

    [Flags]
    public enum MyItemFlags : byte
    {
        None = 0,
        Damaged = 1 << 1,
    }
}