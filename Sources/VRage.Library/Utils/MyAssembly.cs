using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using VRage.FileSystem;


#if XB1 // XB1_ALLINONEASSEMBLY
namespace VRage.Utils
{
    public static class MyAssembly
    {
        private static Assembly m_SpaceEngineersGameDLL;
        private static Assembly m_SandboxGraphicsDLL;
        private static Assembly m_VRageGameDLL;
        private static Assembly m_SandboxGameDLL;
        private static Assembly m_SandboxCommonDLL;
        private static Assembly m_SpaceEngineersObjectBuildersDLL;
        private static Assembly m_VRageDLL;
        private static Assembly m_VRageRender11DLL;


        public static void Init()
        {
            //TEMPORARY - this is not needed on real Xbox (we just need it in XB1 version on PC):
            m_SpaceEngineersGameDLL = Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.Game.dll"));
            m_SandboxGraphicsDLL = Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, "Sandbox.Graphics.dll"));
            m_VRageGameDLL = Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, "VRage.Game.Dll"));
            m_SandboxGameDLL = Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, "Sandbox.Game.Dll"));
            m_SandboxCommonDLL = Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, "Sandbox.Common.Dll"));
            m_SpaceEngineersObjectBuildersDLL = Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.ObjectBuilders.Dll"));
            m_VRageDLL = Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, "VRage.Dll"));
            m_VRageRender11DLL = Assembly.LoadFrom(Path.Combine(MyFileSystem.ExePath, "VRage.Render11.Dll"));
        }

        public static Assembly AllInOneAssembly
        {
            get
            {
                System.Diagnostics.Debug.Assert(m_SandboxCommonDLL != null);
                return m_SandboxCommonDLL;
            }
        }

        public static Type GetType(string name, bool throwOnError)
        {
            //TEMPORARY - on real Xbox we just need to (have to) call GetType once (we need multiple calls - call per assembly - in XB1 version on PC, though)
            Type type = m_SpaceEngineersGameDLL.GetType(name, throwOnError);
            if (type != null) return type;
            type = m_SandboxGraphicsDLL.GetType(name, throwOnError);
            if (type != null) return type;
            type = m_VRageGameDLL.GetType(name, throwOnError);
            if (type != null) return type;
            type = m_SandboxGameDLL.GetType(name, throwOnError);
            if (type != null) return type;
            type = m_SandboxCommonDLL.GetType(name, throwOnError);
            if (type != null) return type;
            type = m_SpaceEngineersObjectBuildersDLL.GetType(name, throwOnError);
            if (type != null) return type;
            type = m_VRageDLL.GetType(name, throwOnError);
            if (type != null) return type;
            type = m_VRageRender11DLL.GetType(name, throwOnError);
            if (type != null) return type;
            System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
            return null;
        }

        public static Type[] GetTypes()
        {
            //TEMPORARY - on real Xbox we just need to (have to) call GetTypes once (we need multiple calls - call per assembly - in XB1 version on PC, though)
            var types = m_SpaceEngineersGameDLL.GetTypes()
                .Concat(m_SandboxGraphicsDLL.GetTypes())
                .Concat(m_VRageGameDLL.GetTypes())
                .Concat(m_SandboxGameDLL.GetTypes())
                .Concat(m_SandboxCommonDLL.GetTypes())
                .Concat(m_SpaceEngineersObjectBuildersDLL.GetTypes())
                .Concat(m_VRageDLL.GetTypes())
                .Concat(m_VRageRender11DLL.GetTypes());
            return types.ToArray();
        }
    }
}
#endif // !XB1
