using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Sandbox.Game.Entities.Blocks
{
    [ProtoContract]
    struct ToolbarItemParameter: IEquatable<ToolbarItemParameter>
    {
        [ProtoMember(1)] 
        public TypeCode TypeCode;

        [ProtoMember(2)] 
        public string Value;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is ToolbarItemParameter && Equals((ToolbarItemParameter)obj);
        }

        public bool Equals(ToolbarItemParameter other)
        {
            return TypeCode == other.TypeCode && string.Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)TypeCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            }
        }
    }
}