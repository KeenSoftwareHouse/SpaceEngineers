using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Plugins;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders;
using VRage.Game.Common;
#if XB1
using VRage.Utils;
#endif // XB1

namespace Sandbox.Game.Entities
{
	public class MyEntityStatEffectTypeAttribute : MyFactoryTagAttribute
	{
		public readonly Type MemoryType;

		public MyEntityStatEffectTypeAttribute(Type objectBuilderType)
			: base(objectBuilderType)
		{
			MemoryType = typeof(MyEntityStatRegenEffect);
		}

		public MyEntityStatEffectTypeAttribute(Type objectBuilderType, Type memoryType)
			: base(objectBuilderType)
		{
			MemoryType = memoryType;
		}

	}

	internal static class MyEntityStatEffectFactory
	{
		private static MyObjectFactory<MyEntityStatEffectTypeAttribute, MyEntityStatRegenEffect> m_objectFactory;

		static MyEntityStatEffectFactory()
		{
			m_objectFactory = new MyObjectFactory<MyEntityStatEffectTypeAttribute, MyEntityStatRegenEffect>();
#if XB1 // XB1_ALLINONEASSEMBLY
            m_objectFactory.RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
			m_objectFactory.RegisterFromAssembly(Assembly.GetAssembly(typeof(MyEntityStatRegenEffect)));
#endif // !XB1
		}

		public static MyEntityStatRegenEffect CreateInstance(MyObjectBuilder_EntityStatRegenEffect builder)
		{
			var obj = m_objectFactory.CreateInstance(builder.TypeId);
			return obj;
		}

		public static MyObjectBuilder_EntityStatRegenEffect CreateObjectBuilder(MyEntityStatRegenEffect effect)
		{
			return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_EntityStatRegenEffect>(effect);
		}

		public static Type GetProducedType(MyObjectBuilderType objectBuilderType)
		{
			return m_objectFactory.GetProducedType(objectBuilderType);
		}

	}
}
