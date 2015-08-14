﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.ObjectBuilders
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EnvironmentalParticleLogic : MyObjectBuilder_Base
	{
		[ProtoMember]
		public string Material;

		[ProtoMember]
		public Vector4 ParticleColor;

		[ProtoMember]
		public float MaxSpawnDistance;

		[ProtoMember]
		public float DespawnDistance;

		[ProtoMember]
		public float Density;
	}
}
