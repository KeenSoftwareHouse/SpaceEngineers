using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_LaserAntennaDefinition))]
    public class MyLaserAntennaDefinition : MyCubeBlockDefinition
    {
        public float PowerInputIdle;
        public float PowerInputTurning;//turning to target
        public float PowerInputLasing;//laser on

        public float RotationRate;

        public float MaxRange;

        public bool RequireLineOfSight;
        public int MinElevationDegrees;
        public int MaxElevationDegrees;
        public int MinAzimuthDegrees;
        public int MaxAzimuthDegrees;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_LaserAntennaDefinition)builder;

            PowerInputIdle = ob.PowerInputIdle;
            PowerInputTurning = ob.PowerInputTurning;
            PowerInputLasing = ob.PowerInputLasing;
            RotationRate=ob.RotationRate;
            MaxRange = ob.MaxRange;
            RequireLineOfSight = ob.RequireLineOfSight;
            MinElevationDegrees = ob.MinElevationDegrees;
            MaxElevationDegrees = ob.MaxElevationDegrees;
            MinAzimuthDegrees = ob.MinAzimuthDegrees;
            MaxAzimuthDegrees = ob.MaxAzimuthDegrees;
        }
    }
}
