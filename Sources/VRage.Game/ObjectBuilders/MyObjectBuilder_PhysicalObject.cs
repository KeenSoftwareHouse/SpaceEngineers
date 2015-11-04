using ProtoBuf;
using System;
using System.ComponentModel;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    // Do not change numbers, these are saved in DB
    [Flags]
    public enum MyItemFlags : byte
    {
        None = 0,
        Damaged = 1 << 1,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalObject : MyObjectBuilder_Base
    {
        [ProtoMember, DefaultValue(MyItemFlags.None)]
        public MyItemFlags Flags = MyItemFlags.None;

        public virtual bool CanStack(MyObjectBuilder_PhysicalObject a)
        {
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

        public virtual Sandbox.Definitions.MyDefinitionId GetObjectId()
        {
            return this.GetId();
        }
    }
}
