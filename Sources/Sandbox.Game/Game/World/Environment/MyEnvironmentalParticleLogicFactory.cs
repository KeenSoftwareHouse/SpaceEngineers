using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Plugins;

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
            m_objectFactory.RegisterFromAssembly(Assembly.GetAssembly(typeof(MyEnvironmentalParticleLogic)));

            m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
            m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
            m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
        }

		public static MyEnvironmentalParticleLogic CreateEnvironmentalParticleLogic(MyObjectBuilder_EnvironmentalParticleLogic builder)
		{
			var obj = m_objectFactory.CreateInstance(builder.TypeId) as MyEnvironmentalParticleLogic;
			return obj;
		}
	}
}
