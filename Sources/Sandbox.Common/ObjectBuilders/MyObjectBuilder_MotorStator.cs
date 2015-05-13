using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MotorStator : MyObjectBuilder_MotorBase
    {
        // We cannot save attached block entity IDs because copy/paste wouldn't work that way
        //[ProtoMember(1)]
        //public long RotorEntityId;

        //[ProtoMember(2)]
        //public bool Enabled;

        [ProtoMember(3), DefaultValue(1f)]
        public float Force = 1f;

        [ProtoMember(4), DefaultValue(0f)]
        public float Friction = 0f;

        [ProtoMember(5)]
        public float TargetVelocity;

        [ProtoMember(6)]
        public float? MinAngle;

        [ProtoMember(7)]
        public float? MaxAngle;

        [ProtoMember(8)]
        public float CurrentAngle;

        [ProtoMember(9)]
        public bool LimitsActive;

        //chaning to 0 deafult is ok for older saves, before there was "hack" that changed 0.2 (previous default value) to 0.0
        //so how when value is not set it's default value will be 0.0 (same as with "hack")
        [ProtoMember(10), DefaultValue(0.0f)] 
        public float DummyDisplacement = 0.0f;

        //[ProtoMember(10)]
        //public bool ControllableFromCockpit;
    }
}
