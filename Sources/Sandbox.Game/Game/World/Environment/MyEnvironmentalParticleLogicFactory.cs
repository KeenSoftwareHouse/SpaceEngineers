using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Game.Common;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Plugins;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace Sandbox.Game.World
{
	public class MyEnvironmentalParticleLogicTypeAttribute : MyFactoryTagAttribute
	{
		public MyEnvironmentalParticleLogicTypeAttribute(Type objectBuilderType, bool mainBuilder = true)
            : base(objectBuilderType, mainBuilder)
        {
        }
	}

	public class MyEnvironmentalParticleLogicFactory
	{
		static MyObjectFactory<MyEnvironmentalParticleLogicTypeAttribute, MyEnvironmentalParticleLogic> m_objectFactory;

		static MyEnvironmentalParticleLogicFactory()
        {
            m_objectFactory = new MyObjectFactory<MyEnvironmentalParticleLogicTypeAttribute, MyEnvironmentalParticleLogic>();
#if XB1 // XB1_ALLINONEASSEMBLY
            m_objectFactory.RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            m_objectFactory.RegisterFromAssembly(Assembly.GetAssembly(typeof(MyEnvironmentalParticleLogic)));

            m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
            m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
            m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
#endif // !XB1
        }

		public static MyEnvironmentalParticleLogic CreateEnvironmentalParticleLogic(MyObjectBuilder_EnvironmentalParticleLogic builder)
		{
			var obj = m_objectFactory.CreateInstance(builder.TypeId) as MyEnvironmentalParticleLogic;
			return obj;
		}
	}
}
