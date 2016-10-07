using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using VRage.FileSystem;
#if UNSHARPER
using System.Diagnostics;
#endif


namespace VRage.Compiler
{
#if XB1
	[Unsharper.UnsharperDisableReflection()]
	public class IlCompiler
	{
		static IlCompiler()
		{

		}
        public static string[] UpdateCompatibility(string[] files)
		{
			Debug.Assert(false); return null;
		}
        public static bool CompileFileModAPI(string assemblyName, string[] files, out Assembly assembly, List<string> errors)
		{
			assembly = null;
			Debug.Assert(false); return false;
		}

        public static bool CompileStringIngame(string assemblyName, string[] source, out Assembly assembly, List<string> errors)
		{
			assembly = null;
			Debug.Assert(false); return false;
		}
        private static bool CheckResultInternal(ref Assembly assembly, List<string> errors, CompilerResults result,bool isIngameScript)
		{
			assembly = null;
			Debug.Assert(false); return false;
		}
        public static bool Compile(string assemblyName, string[] fileContents, out Assembly assembly, List<string> errors, bool isIngameScript)
		{
			assembly = null;
			Debug.Assert(false); return false;
		}
        public static bool Compile(string[] instructions, out Assembly assembly,bool isIngameScript, bool wrap = true)
		{
			assembly = null;
			Debug.Assert(false); return false;
		}
		public static StringBuilder Buffer = new StringBuilder();
	}
#else
    public class IlCompiler
    {
        // TODO: Move the compatibility stuff into its own system once the Roslyn scripts have been integrated.

        public static System.CodeDom.Compiler.CompilerParameters Options;

        /// <summary>
        /// Usings that will be added to scripts before compilation. Those usings will replace default ones.
        /// </summary>
        public static string CompatibilityUsings = "using VRage;\r\n"
                                                 + "using VRage.Game.Components;\r\n"
                                                 + "using VRage.ObjectBuilders;\r\n"
                                                 + "using VRage.ModAPI;\r\n"
                                                 + "using VRage.Game.ModAPI;\r\n"
                                                 + "using Sandbox.Common.ObjectBuilders;\r\n"
                                                 + "using VRage.Game;\r\n"
                                                 + "using Sandbox.ModAPI;\r\n"
                                                 + "using VRage.Game.ModAPI.Interfaces;\r\n"
                                                 + "using SpaceEngineers.Game.ModAPI;\r\n";

        private static CSharpCodeProvider m_cp = new CSharpCodeProvider();
        private static IlReader m_reader = new IlReader();
        static Dictionary<string, string> m_compatibilityChanges = new Dictionary<string, string>() {
            {"using VRage.Common.Voxels;", "" },
            {"VRage.Common.Voxels.", "" },
            {"Sandbox.ModAPI.IMyEntity","VRage.ModAPI.IMyEntity"},
            {"Sandbox.Common.ObjectBuilders.MyObjectBuilder_EntityBase","VRage.ObjectBuilders.MyObjectBuilder_EntityBase"},
            {"Sandbox.Common.MyEntityUpdateEnum","VRage.ModAPI.MyEntityUpdateEnum"},
            {"using Sandbox.Common.ObjectBuilders.Serializer;",""},
            {"Sandbox.Common.ObjectBuilders.Serializer.",""},
            {"Sandbox.Common.MyMath","VRageMath.MyMath"},
            {"Sandbox.Common.ObjectBuilders.VRageData.SerializableVector3I","VRage.SerializableVector3I"},
            {"VRage.Components","VRage.Game.Components"},
            {"using Sandbox.Common.ObjectBuilders.VRageData;",""},
            {"Sandbox.Common.ObjectBuilders.MyOnlineModeEnum","VRage.Game.MyOnlineModeEnum"},
            {"Sandbox.Common.ObjectBuilders.Definitions.MyDamageType","VRage.Game.MyDamageType"},
            {"Sandbox.Common.ObjectBuilders.VRageData.SerializableBlockOrientation","VRage.Game.SerializableBlockOrientation"},
            {"Sandbox.Common.MySessionComponentDescriptor","VRage.Game.Components.MySessionComponentDescriptor"},
            {"Sandbox.Common.MyUpdateOrder","VRage.Game.Components.MyUpdateOrder"},
            {"Sandbox.Common.MySessionComponentBase","VRage.Game.Components.MySessionComponentBase"},
            {"Sandbox.Common.MyFontEnum","VRage.Game.MyFontEnum"},
            {"Sandbox.Common.MyRelationsBetweenPlayerAndBlock","VRage.Game.MyRelationsBetweenPlayerAndBlock"},
            {"Sandbox.Common.Components","VRage.Game.Components"},
            {"using Sandbox.Common.Input;",""},
            {"using Sandbox.Common.ModAPI;",""},
        };
        
        static IlCompiler()
        {
            Options = new System.CodeDom.Compiler.CompilerParameters(new string[] {
                "System.Xml.dll"
                , "Sandbox.Game.dll"
                , "Sandbox.Common.dll"
                , "Sandbox.Graphics.dll"
                , "VRage.dll"
                , "VRage.Library.dll"
                , "VRage.Math.dll"
                , "VRage.Game.dll"
                , "VRage.Render.dll"
                , "System.Core.dll"
                , "System.dll"
                , "SpaceEngineers.ObjectBuilders.dll"
                , "SpaceEngineers.Game.dll"
                //, "Microsoft.CSharp.dll"
            });
            Options.GenerateInMemory = true;
        }
        
        public static string[] UpdateCompatibility(string[] files)
        {
            string[] sources = new string[files.Length];
            for (int i = 0; i < files.Length; ++i)
            {
                var filename = files[i];
                sources[i] = UpdateCompatibility(filename);
            }
            return sources;
        }
        
        public static string UpdateCompatibility(string filename)
        {
            using (Stream stream = MyFileSystem.OpenRead(filename))
            {
                if (stream != null)
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        string source = sr.ReadToEnd();
                        Debug.Assert(CompatibilityUsings != null, "Compatibility usings can't be null");
                        source = source.Insert(0, CompatibilityUsings);

                        foreach (var value in m_compatibilityChanges)
                        {
                            source = source.Replace(value.Key, value.Value);
                        }
                        return source;
                    }
                }
            }
            return null;
        }

        public static bool CompileFileModAPI(string assemblyName, string[] files, out Assembly assembly, List<string> errors)
        {
            Options.OutputAssembly = assemblyName;
            Options.GenerateInMemory = true;
            Options.TempFiles = new TempFileCollection(null, false);
            Options.IncludeDebugInformation = false;
            string[] sources = UpdateCompatibility(files);
            var result = m_cp.CompileAssemblyFromSource(Options, sources);
            return CheckResultInternal(out assembly, errors, result, false);
        }
        
        public static bool CompileStringIngame(string assemblyName, string[] source, out Assembly assembly, List<string> errors)
        {
            Options.OutputAssembly = assemblyName;
            Options.GenerateInMemory = true;
            Options.GenerateExecutable = false;
            Options.IncludeDebugInformation = false;
            Options.TempFiles = new TempFileCollection(null, false);
            var result = m_cp.CompileAssemblyFromSource(Options, source);
            return CheckResultInternal(out assembly, errors, result,true);
        }

        /// <summary>
        /// Checks assembly for not allowed operations (ie. accesing file system, network)
        /// </summary>
        /// <param name="tmpAssembly">output assembly</param>
        /// <param name="errors">compilation or check errors</param>
        /// <param name="result">compiled assembly</param>
        /// <param name="isIngameScript"></param>
        /// <returns>wheter the check was sucessflu (false AND null asembly on fail)</returns>
        private static bool CheckResultInternal(out Assembly assembly, List<string> errors, CompilerResults result,bool isIngameScript)
        {
            assembly = null;
            if (result.Errors.HasErrors)
            {
                var en = result.Errors.GetEnumerator();
                while (en.MoveNext())
                {
                    if (!(en.Current as CompilerError).IsWarning)
                        errors.Add((en.Current as CompilerError).ToString());
                }
                return false;
            }
            var tmpAssembly = result.CompiledAssembly;
            Type failedType;
            var dic = new Dictionary<Type, HashSet<MemberInfo>>();
            foreach (var t in tmpAssembly.GetTypes()) //allows calls inside assembly
                dic.Add(t, null);

            List<MethodBase> typeMethods = new List<MethodBase>();
            BindingFlags bfAllMembers = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

            foreach (var t in tmpAssembly.GetTypes())
            {
                typeMethods.Clear();
                typeMethods.AddArray(t.GetMethods(bfAllMembers));
                typeMethods.AddArray(t.GetConstructors(bfAllMembers));

                foreach (var m in typeMethods)
                {
                    if (IlChecker.IsMethodFromParent(t,m))
                    {
                        if (IlChecker.CheckTypeAndMember(m.DeclaringType, isIngameScript) == false)
                        {
                            errors.Add(string.Format("Class {0} derives from class {1} that is not allowed in script", t.Name, m.DeclaringType.Name));
                            return false;
                        }
                        continue;
                    }
                    if ((!IlChecker.CheckIl(m_reader.ReadInstructions(m), out failedType,isIngameScript, dic)) || IlChecker.HasMethodInvalidAtrributes(m.Attributes))
                    {
                        // CH: TODO: This message does not make much sense when we test not only allowed types, but also method attributes
                        errors.Add(string.Format("Type {0} used in {1} not allowed in script", failedType == null ? "FIXME" : failedType.ToString(), m.Name));
                        return false;
                    }
                }
            }
            assembly = tmpAssembly;
            return true;
        }

        public static bool Compile(string assemblyName, string[] fileContents, out Assembly assembly, List<string> errors, bool isIngameScript)
        {
            Options.OutputAssembly = assemblyName;
            Options.TempFiles = new TempFileCollection(null, false);
            var result = m_cp.CompileAssemblyFromSource(Options, fileContents);
            return CheckResultInternal(out assembly, errors, result,isIngameScript);
        }

        public static bool Compile(string[] instructions, out Assembly assembly,bool isIngameScript, bool wrap = true)
        {
            //m_options = new System.CodeDom.Compiler.CompilerParameters(new string[] { "Sandbox.Game.dll", "Sandbox.Common.dll", "VRage.Common.dll", "System.Core.dll", "System.dll" });
            //m_options.GenerateInMemory = true;
            assembly = null;
            m_cache.Clear();
            if(wrap)
                m_cache.AppendFormat(invokeWrapper, instructions);
            else
                m_cache.Append(instructions[0]);
            Options.TempFiles = new TempFileCollection(null, false);
            var result = m_cp.CompileAssemblyFromSource(Options, m_cache.ToString());
            if (result.Errors.HasErrors)
                return false;
            assembly = result.CompiledAssembly;
            Type failedType;
            var dic = new Dictionary<Type, HashSet<MemberInfo>>();
            foreach (var t in assembly.GetTypes()) //allows calls inside assembly
                dic.Add(t, null);
            foreach (var t in assembly.GetTypes())
                foreach (var m in t.GetMethods())
                {
                    if (t == typeof(MulticastDelegate))
                        continue;
                    if (!IlChecker.CheckIl(m_reader.ReadInstructions(m), out failedType,isIngameScript, dic))
                    {
                        assembly = null;
                        return false;
                    }
                }
            return true;
        }
        private static StringBuilder m_cache = new StringBuilder();
        private const String invokeWrapper = "public static class wrapclass{{ public static object run() {{ {0} return null;}} }}";
        public static StringBuilder Buffer = new StringBuilder();    
    }
#endif
}
