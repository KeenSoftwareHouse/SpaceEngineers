using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using VRage.FileSystem;
using VRage.Import;
using VRage.ModAPI;
using VRage.Plugins;
using VRageRender.Import;

#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace VRage.Game.Entity.UseObject
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class MyUseObjectAttribute : System.Attribute
    {
        public readonly string DummyName;

        public MyUseObjectAttribute(string dummyName)
        {
            DummyName = dummyName;
        }
    }

    [PreloadRequired]
    public static class MyUseObjectFactory
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private static bool m_registered = false;
#endif // XB1

        private static Dictionary<string, Type> m_useObjectTypesByDummyName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        static MyUseObjectFactory()
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterAssemblyTypes(MyAssembly.AllInOneAssembly);
#else // !XB1
            RegisterAssemblyTypes(Assembly.GetExecutingAssembly());
            RegisterAssemblyTypes(MyPlugins.GameAssembly);
            RegisterAssemblyTypes(MyPlugins.SandboxAssembly);
            RegisterAssemblyTypes(MyPlugins.UserAssembly);
            RegisterAssemblyTypes(Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, "Sandbox.Game.dll")));
#endif // !XB1
        }

        private static void RegisterAssemblyTypes(Assembly assembly)
        {
            if (assembly == null)
                return;

            var iMyUseObject = typeof(IMyUseObject);
#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (var type in MyAssembly.GetTypes())
#else // !XB1
            foreach (var type in assembly.GetTypes())
#endif // !XB1
            {
                if (!iMyUseObject.IsAssignableFrom(type))
                    continue;

                var attributes = (MyUseObjectAttribute[])type.GetCustomAttributes(typeof(MyUseObjectAttribute), false);
                if (attributes.IsNullOrEmpty())
                    continue;

                foreach (var attribute in attributes)
                {
                    AssertHasCorrectCtor(type);
                    Debug.Assert(!m_useObjectTypesByDummyName.ContainsKey(attribute.DummyName) || m_useObjectTypesByDummyName[attribute.DummyName].Assembly != type.Assembly,
                        "Overriding use object with class in same assembly! This should not happen.");
                    m_useObjectTypesByDummyName[attribute.DummyName] = type;
                }
            }
        }

        [Conditional("DEBUG")]
        private static void AssertHasCorrectCtor(Type type)
        {
            foreach (var ctorInfo in type.GetConstructors())
            {
                var args = ctorInfo.GetParameters();
                if (args.Length != 4)
                    continue;

                if (args[0].ParameterType == typeof(IMyEntity) &&
                    args[1].ParameterType == typeof(string) &&
                    args[2].ParameterType == typeof(MyModelDummy) &&
                    args[3].ParameterType == typeof(uint))
                {
                    return; // found correct constructor so no need to assert
                }
            }

            Debug.Fail(string.Format("No appropriate constructor defined for type {0}.", type.FullName));
        }

        public static IMyUseObject CreateUseObject(string detectorName, IMyEntity owner, string dummyName, MyModelDummy dummyData, uint shapeKey)
        {
            Type type;
            if (!m_useObjectTypesByDummyName.TryGetValue(detectorName, out type) || type == null)
                return null;

            return (IMyUseObject)Activator.CreateInstance(type, owner, dummyName, dummyData, shapeKey);
        }

    }
}
