using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
	public class MyFuelConverterInfo
	{
		[ProtoMember]
		public SerializableDefinitionId FuelId = new SerializableDefinitionId();

		[ProtoMember]
		public float Efficiency = 1f;
	}

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ThrustDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        static readonly Vector4 DefaultThrustColor = new Vector4(Color.CornflowerBlue.ToVector3() * 0.7f, 0.75f);

	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public MyFuelConverterInfo FuelConverter = new MyFuelConverterInfo();

	    [ProtoMember]
	    public float SlowdownFactor = 10;

        [ProtoMember]
        public float ForceMagnitude;

        [ProtoMember]
        public float MaxPowerConsumption;

        [ProtoMember]
        public float MinPowerConsumption;

        [ProtoMember]
        public float FlameDamageLengthScale = 0.6f;

        [ProtoMember]
        public float FlameLengthScale = 1.15f;

        [ProtoMember]
        public Vector4 FlameFullColor = DefaultThrustColor;

        [ProtoMember]
        public Vector4 FlameIdleColor = DefaultThrustColor;

        [ProtoMember]
        public string FlamePointMaterial = "EngineThrustMiddle";

        [ProtoMember]
        public string FlameLengthMaterial = "EngineThrustMiddle";

        [ProtoMember]
        public string FlameGlareMaterial = "GlareSsThrustSmall";

        [ProtoMember]
        public float FlameVisibilityDistance = 200;

        [ProtoMember]
        public float FlameGlareSize = 0.391f;

        [ProtoMember]
        public float FlameGlareQuerySize = 1;

        [ProtoMember]
        public float FlameDamage = 0.5f;

        [ProtoMember]
        public float EffectivenessAtMaxInfluence = 1f;

        [ProtoMember]
        public float EffectivenessAtMinInfluence = 1f;
    }
}
