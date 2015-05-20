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
        //[ProtoMember]
        //public long RotorEntityId;

        //[ProtoMember]
        //public bool Enabled;

        [ProtoMember, DefaultValue(1f)]
        public float Force = 1f;

        [ProtoMember, DefaultValue(0f)]
        public float Friction = 0f;

        [ProtoMember]
        public float TargetVelocity;

        [ProtoMember]
        public float? MinAngle;

        [ProtoMember]
        public float? MaxAngle;

        [ProtoMember]
        public float CurrentAngle;

        [ProtoMember]
        public bool LimitsActive;

        //chaning to 0 deafult is ok for older saves, before there was "hack" that changed 0.2 (previous default value) to 0.0
        //so how when value is not set it's default value will be 0.0 (same as with "hack")
        [ProtoMember, DefaultValue(0.0f)] 
        public float DummyDisplacement = 0.0f;

        //[ProtoMember]
        //public bool ControllableFromCockpit;
    }
}
