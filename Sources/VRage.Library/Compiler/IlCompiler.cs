using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using VRage.FileSystem;

namespace VRage.Compiler
{
    public class IlCompiler
    {
        public static System.CodeDom.Compiler.CompilerParameters Options;

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
        {"Sandbox.Common.ObjectBuilders.VRageData.SerializableVector3I","VRage.SerializableVector3I"}};

        static IlCompiler()
        {
            Options = new System.CodeDom.Compiler.CompilerParameters(new string[] { "System.Xml.dll", "Sandbox.Game.dll", "Sandbox.Common.dll", "Sandbox.Graphics.dll", "VRage.dll", "VRage.Library.dll", "VRage.Math.dll","VRage.Game.dll", "System.Core.dll", "System.dll"/*, "Microsoft.CSharp.dll" */});
            Options.GenerateInMemory = true;
            //Options.IncludeDebugInformation = true;
        }

        public static string[] UpdateCompatibility(string[] files)
        {
            string[] sources = new string[files.Length];
            for (int i = 0; i < files.Length; ++i)
            {
                using (Stream stream = MyFileSystem.OpenRead(files[i]))
                {
                    if (stream != null)
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            string source = sr.ReadToEnd();
                            source = source.Insert(0, "using VRage;\r\nusing VRage.Components;\r\nusing VRage.ObjectBuilders;\r\nusing VRage.ModAPI;\r\n");
                            foreach (var value in m_compatibilityChanges)
                            {
                                source = source.Replace(value.Key,value.Value);
                            }
                            sources[i] = source;
                        }
                    }
                }
            }
            return sources;
        }

        public static bool CompileFileModAPI(string assemblyName, string[] files, out Assembly assembly, List<string> errors)
        {
            assembly = null;
            Options.OutputAssembly = assemblyName;
            Options.GenerateInMemory = true;
            string[] sources = UpdateCompatibility(files);
            var result = m_cp.CompileAssemblyFromSource(Options, sources);
            return CheckResultInternal(ref assembly, errors, result,false);
        }

        public static bool CompileStringIngame(string assemblyName, string[] source, out Assembly assembly, List<string> errors)
        {
            assembly = null;
            Options.OutputAssembly = assemblyName;
            Options.GenerateInMemory = true;
            var result = m_cp.CompileAssemblyFromSource(Options, source);
            return CheckResultInternal(ref assembly, errors, result,true);
        }

        private static bool CheckResultInternal(ref Assembly assembly, List<string> errors, CompilerResults result,bool isIngameScript)
        {
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
            assembly = result.CompiledAssembly;
            Type failedType;
            var dic = new Dictionary<Type, List<MemberInfo>>();
            foreach (var t in assembly.GetTypes()) //allows calls inside assembly
                dic.Add(t, null);

            List<MethodBase> typeMethods = new List<MethodBase>();
            BindingFlags bfAllMembers = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

            foreach (var t in assembly.GetTypes())
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
                        assembly = null;
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool Compile(string assemblyName, string[] fileContents, out Assembly assembly, List<string> errors, bool isIngameScript)
        {
            assembly = null;
            Options.OutputAssembly = assemblyName;
            var result = m_cp.CompileAssemblyFromSource(Options, fileContents);
            return CheckResultInternal(ref assembly, errors, result,isIngameScript);
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
            var result = m_cp.CompileAssemblyFromSource(Options, m_cache.ToString());
            if (result.Errors.HasErrors)
                return false;
            assembly = result.CompiledAssembly;
            Type failedType;
            var dic = new Dictionary<Type, List<MemberInfo>>();
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
}
