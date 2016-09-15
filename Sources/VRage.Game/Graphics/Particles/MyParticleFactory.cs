using System;
using System.Collections.Generic;
using VRageRender.Animations;

namespace VRage.Game
{
    static class MyParticleFactory
    {
        private static Dictionary<string, Type> m_registeredTypes = new Dictionary<string, Type>();

        static void RegisterTypes()
        {
            RegisterType(typeof(MyParticleEffect));
            RegisterType(typeof(MyParticleGeneration));
            RegisterType(typeof(MyParticleEmitter));

            RegisterType(typeof(MyAnimatedPropertyFloat));
            RegisterType(typeof(MyAnimatedPropertyVector3));
            RegisterType(typeof(MyAnimatedPropertyVector4));

            RegisterType(typeof(MyAnimatedProperty2DFloat));
            RegisterType(typeof(MyAnimatedProperty2DVector3));
            RegisterType(typeof(MyAnimatedProperty2DVector4));
        }

        public static void RegisterType(Type type)
        {
            m_registeredTypes.Add(type.Name, type);
        }

        public static object CreateObject(string typeName)
        {
            Type type = m_registeredTypes[typeName];

            return Activator.CreateInstance(type);
        }
    }
}
