using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using ProtoBuf;
using VRage.Utils;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// This structure contains all information about damage being done.
    /// </summary>
    
    [ProtoContract]
    public struct MyDamageInformation
    {
        public MyDamageInformation(bool isDeformation, float amount, MyStringHash type, long attackerId)
        {
            IsDeformation = isDeformation;
            Amount = amount;
            Type = type;
            AttackerId = attackerId;
        }

        [ProtoMember]
        public bool IsDeformation;

        [ProtoMember]
        public float Amount;

        [ProtoMember]
        public MyStringHash Type;

        [ProtoMember]
        public long AttackerId;
    }
}
