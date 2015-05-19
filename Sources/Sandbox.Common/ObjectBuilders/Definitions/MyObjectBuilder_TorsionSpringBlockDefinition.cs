using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace Medieval.ObjectBuilders.Definitions
{
    /// <summary>
    /// Definition for one small grid torsion spring (used inside large block as mechanical subblock).
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TorsionSpringBlockDefinition : MyObjectBuilder_MechanicalSubBlockDefinition
    {
        // Max angular impulse in MaxAngle position.
        [ProtoMember]
        public float MaxAngularImpulse;

        // Maximum angle in degrees from origin.
        [ProtoMember]
        public float MaxAngle;

        [ProtoMember, DefaultValue(10f)]
        public float MaxFrictionTorque = 10f;
    }
}
