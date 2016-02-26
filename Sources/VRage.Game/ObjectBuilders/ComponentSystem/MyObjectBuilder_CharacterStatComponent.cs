﻿using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_CharacterStatComponent : MyObjectBuilder_EntityStatComponent
	{
	}
}
