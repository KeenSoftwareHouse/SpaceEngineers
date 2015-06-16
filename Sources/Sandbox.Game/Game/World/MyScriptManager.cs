using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Utils;
using VRage.Compiler;
using VRage.Library.Utils;
using VRage.Serialization;
using VRage.FileSystem;
using VRage.Components;
using VRage.ObjectBuilders;
using Sandbox.Game.Components;

namespace Sandbox.Game.World
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate,1000,typeof(MyObjectBuilder_ScriptManager))]
    class MyScriptManager : MySessionComponentBase
    {
        public static MyScriptManager Static;
        string[] Separators = new string[] { " " };
       
        public Dictionary<MyStringId, Assembly> Scripts = new Dictionary<MyStringId, Assembly>(MyStringId.Comparer);
        public Dictionary<Type, HashSet<Type>> EntityScripts = new Dictionary<Type, HashSet<Type>>(); //Binds object builder type with Game Logic component type
        public Dictionary<Tuple<Type, string>, HashSet<Type>> SubEntityScripts = new Dictionary<Tuple<Type, string>, HashSet<Type>>();
		public Dictionary<string, Type> StatScripts = new Dictionary<string, Type>();
        public Dictionary<MyStringId, Type> InGameScripts = new Dictionary<MyStringId, Type>(MyStringId.Comparer); //Ingame script is just game logic component
        public Dictionary<MyStringId, StringBuilder> InGameScriptsCode = new Dictionary<MyStringId, StringBuilder>(MyStringId.Comparer);
        private List<string> m_errors = new List<string>();
        private List<string> m_cachedFiles = new List<string>();
        static Dictionary<string, bool> testFiles = new Dictionary<string, bool>();

        public override void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyScriptManager.LoadData");
            MySandboxGame.Log.WriteLine("MyScriptManager.LoadData() - START");
            MySandboxGame.Log.IncreaseIndent();
            base.LoadData();
            Static = this;
            Scripts.Clear();
            EntityScripts.Clear();
            SubEntityScripts.Clear();
            if(Sync.IsServer)
                LoadScripts(MyFileSystem.ContentPath);
            LoadScripts(MySession.Static.CurrentPath);
            ReadScripts(MySession.Static.CurrentPath);
            foreach (var mod in MySession.Static.Mods)
                LoadScripts(Path.Combine(MyFileSystem.ModsPath, mod.Name), mod.Name);
            foreach (var ass in Scripts.Values)
            {
                MySession.Static.RegisterComponentsFromAssembly(ass);
                MySandboxGame.Log.WriteLine(string.Format("Script loaded: {0}", ass.FullName));
            }
            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyScriptManager.LoadData() - END");

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private void LoadScripts(string path, string modName = null)
        {
            if (!MyFakes.ENABLE_SCRIPTS)
                return;

            var fsPath = Path.Combine(path, "Data", "Scripts");
            var scriptFiles = MyFileSystem.GetFiles(fsPath, "*.cs" );//, searchOption: VRage.FileSystem.SearchOption.TopDirectoryOnly);
            try
            {
                if (scriptFiles.Count() == 0)
                    return;
            }
            catch (Exception)
            {
                MySandboxGame.Log.WriteLine(string.Format("Failed to load scripts from: {0}", path));
                return;
            }
            var isZip = VRage.FileSystem.MyZipFileProvider.IsZipFile(path);
            List<string> files = new List<string>();
            var split = scriptFiles.First().Split('\\');
            string scriptDir = split[split.Length - 2];
            foreach (var scriptFile in scriptFiles)
            {
                split = scriptFile.Split('\\');
                var extension = split[split.Length - 1].Split('.').Last();
                if (extension != "cs")
                    continue;
                var idx = Array.IndexOf(split, "Scripts") + 1; //index of script directory (there can be any dir hierarchy above it)
                if (split[idx] == scriptDir)
                    files.Add(scriptFile);
                else
                {
                    Compile(files, string.Format("{0}_{1}", modName, scriptDir), isZip);
                    files.Clear();
                    scriptDir = split[split.Length - 2];
                    files.Add(scriptFile);
                }
            }
            Compile(files.ToArray(), string.Format("{0}_{1}",modName,scriptDir), isZip);
            files.Clear();
        }

        private void Compile(IEnumerable<string> scriptFiles, string assemblyName, bool zipped)
        {
            Assembly assembly = null;
            var c = new MyModContext();
            c.Init(assemblyName, assemblyName);
            if (zipped)
            {
                var tmp = Path.GetTempPath();
                foreach (var file in scriptFiles)
                {
                    try
                    {
                        var newPath = string.Format("{0}{1}", tmp, Path.GetFileName(file));
                        var stream = MyFileSystem.OpenRead(file);
                        using (var sr = new StreamReader(stream))
                        {
                            stream = MyFileSystem.OpenWrite(newPath);// (newPath);
                            using (var sw = new StreamWriter(stream))
                            {
                                sw.Write(sr.ReadToEnd()); //create file in tmp for debugging
                            }
                        }
                        m_cachedFiles.Add(newPath);
                    }
                    catch (Exception e)
                    {
                        MySandboxGame.Log.WriteLine(e);
                        MyDefinitionErrors.Add(c, string.Format("Cannot load {0}",Path.GetFileName(file)) , ErrorSeverity.Error);
                        MyDefinitionErrors.Add(c, e.Message, ErrorSeverity.Error);
                    }
                }
                IlCompiler.CompileFileModAPI(assemblyName, m_cachedFiles.ToArray(), out assembly, m_errors);
            }
            else
            {
                IlCompiler.CompileFileModAPI(assemblyName, scriptFiles.ToArray(), out assembly, m_errors);
            }
            if(assembly != null)
                AddAssembly(MyStringId.GetOrCompute(assemblyName), assembly);
            else
            {
                MyDefinitionErrors.Add(c, string.Format("Compilation of {0} failed:", assemblyName), ErrorSeverity.Error);
                MySandboxGame.Log.IncreaseIndent();
				foreach (var error in m_errors)
				{
					MyDefinitionErrors.Add(c, error.ToString(), ErrorSeverity.Error);
					Debug.Assert(false, error.ToString());
				}
                MySandboxGame.Log.DecreaseIndent();
                m_errors.Clear();
            }
            m_cachedFiles.Clear();
        }

        private void AddAssembly(MyStringId myStringId, Assembly assembly)
        {
            if (Scripts.ContainsKey(myStringId))
            {
                MySandboxGame.Log.WriteLine(string.Format("Script already in list {0}", myStringId.ToString()));
                Debug.Fail(string.Format("Script already in list {0}", myStringId.ToString()));
                return;
            }
            Scripts.Add(myStringId, assembly);
            foreach (var type in assembly.GetTypes())
            {
                MyConsole.AddCommand(new MyCommandScript(type));
            }
            TryAddEntityScripts(assembly);
			AddStatScripts(assembly);
        }

        private void TryAddEntityScripts(Assembly assembly)
        {
            var gameLogicType = typeof(MyGameLogicComponent);
            var builderType = typeof(MyObjectBuilder_Base);
            foreach (var type in assembly.GetTypes())
            {
                var descriptorArray = type.GetCustomAttributes(typeof(MyEntityComponentDescriptor), false);
                if (descriptorArray != null && descriptorArray.Length > 0)
                {
                    var descriptor = (MyEntityComponentDescriptor)descriptorArray[0];
                    var component = (MyGameLogicComponent)Activator.CreateInstance(type);

                    if (descriptor.EntityBuilderSubTypeNames != null && descriptor.EntityBuilderSubTypeNames.Length > 0)
                    {
                        foreach (string subTypeName in descriptor.EntityBuilderSubTypeNames)
                        {
                            if (gameLogicType.IsAssignableFrom(type) && builderType.IsAssignableFrom(descriptor.EntityBuilderType))
                            {
                                if (!SubEntityScripts.ContainsKey(new Tuple<Type, string>(descriptor.EntityBuilderType, subTypeName)))
                                {
                                    SubEntityScripts.Add(new Tuple<Type, string>(descriptor.EntityBuilderType, subTypeName), new HashSet<Type>());
                                }
                                else
                                {
                                    var c = new MyModContext();
                                    c.Init(assembly.FullName, assembly.FullName);
                                    MyDefinitionErrors.Add(c, "Possible entity type script logic collision", ErrorSeverity.Warning);
                                }

                                SubEntityScripts[new Tuple<Type, string>(descriptor.EntityBuilderType, subTypeName)].Add(type);
                            }
                        }
                    }
                    else
                    {
                        if (gameLogicType.IsAssignableFrom(type) && builderType.IsAssignableFrom(descriptor.EntityBuilderType))
                        {
                            if (!EntityScripts.ContainsKey(descriptor.EntityBuilderType))
                            {
                                EntityScripts.Add(descriptor.EntityBuilderType, new HashSet<Type>());
                            }
                            else
                            {
                                var c = new MyModContext();
                                c.Init(assembly.FullName, assembly.FullName);
                                MyDefinitionErrors.Add(c, "Possible entity type script logic collision", ErrorSeverity.Warning);
                            }

                            EntityScripts[descriptor.EntityBuilderType].Add(type);
                        }
                    }
                }
            }
        }

		private void AddStatScripts(Assembly assembly)
        {
			var logicType = typeof(MyStatLogic);
            foreach (var type in assembly.GetTypes())
            {
                var descriptorArray = type.GetCustomAttributes(typeof(MyStatLogicDescriptor), false);
                if (descriptorArray != null && descriptorArray.Length > 0)
                {
                    var descriptor = (MyStatLogicDescriptor)descriptorArray[0];
					var scriptName = descriptor.ComponentName;

					if (logicType.IsAssignableFrom(type) && !StatScripts.ContainsKey(scriptName))
					{
						StatScripts.Add(scriptName, type);
					}
				}
			}
		}

        public bool CompileIngameScript(MyStringId id, StringBuilder errors)
        {
            if (!MyFakes.ENABLE_SCRIPTS)
                return false;
            Assembly assembly;
            bool success;
            success = IlCompiler.Compile(new string[] { InGameScriptsCode[id].ToString() }, out assembly, false);
            if (success)
            {
                var scriptType = typeof(MyIngameScript);
                if (InGameScripts.ContainsKey(id))
                    InGameScripts.Remove(id);
                foreach (var type in assembly.GetTypes())
                {
                    if (scriptType.IsAssignableFrom(type))
                    {
                        InGameScripts.Add(id, type);
                        return true;
                    }
                }
            }
            return false;

        }

        public void CallScript(string message)
        {
            if (!CallScriptInternal(message))
                Sandbox.Game.Gui.MyHud.Chat.ShowMessage("Call", "Failed");
        }

        private bool CallScriptInternal(string message)
        {
            Assembly ass;
            if (IlCompiler.Buffer.Length > 0)
            {
                if (IlCompiler.Compile(new string[] { IlCompiler.Buffer.ToString() }, out ass,true))
                {
                    var retval = ass.GetType("wrapclass").GetMethod("run").Invoke(null, null);
                    if (!string.IsNullOrEmpty(message))
                        Sandbox.Game.Gui.MyHud.Chat.ShowMessage("returned", retval.ToString());
                    return true;
                    IlCompiler.Buffer.Clear();
                }
                else
                {
                    IlCompiler.Buffer.Clear();
                    return false;
                }
            }
            var parts = message.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                MyAPIGateway.Utilities.ShowNotification("Not enought parameters for script please provide following paramaters : Sriptname Classname MethodName",5000);
                return false;
            }
            if (!Scripts.ContainsKey(MyStringId.TryGet(parts[1])))
            {
                string availableScripts = "";
                foreach(var scriptName in Scripts)
                {
                    availableScripts += scriptName.Key + "\n";
                }
                MyAPIGateway.Utilities.ShowMissionScreen("Script not found", "", "Available scripts:", availableScripts);
                return false;
            }
            ass = Scripts[MyStringId.Get(parts[1])];
            var type = ass.GetType(parts[2]);
            if (type == null)
            {
                string availableScripts = "";
                var types = ass.GetTypes();
                foreach (var scriptType in types)
                {
                    availableScripts += scriptType.FullName + "\n";
                }
                MyAPIGateway.Utilities.ShowMissionScreen("Class not found", "", "Available classes:", availableScripts);
                return false;
            }
            var method = type.GetMethod(parts[3]);
            if (method == null)
            {
                string availableScripts = "";
                var types = type.GetMethods(BindingFlags.Static|BindingFlags.Public);
                foreach (var scriptType in types)
                {
                    availableScripts += scriptType.Name + "\n";
                }
                MyAPIGateway.Utilities.ShowMissionScreen("Method not found", "", "Available methods:", availableScripts);
                return false;
            }
            var paramInfos = method.GetParameters();
            List<object> parameters = new List<object>();
            for (int i = 4; i < paramInfos.Length + 4 && i < parts.Length; i++)
            {
                var paramType = paramInfos[i - 4].ParameterType;
                var parseMet = paramType.GetMethod("TryParse", new Type[] { typeof(System.String), paramType.MakeByRefType() });
                if (parseMet != null)
                {
                    var output = Activator.CreateInstance(paramType);
                    var args = new object[] { parts[i], output };
                    var par = parseMet.Invoke(null, args);
                    parameters.Add(args[1]);
                }
                else
                    parameters.Add(parts[i]);
            }
            if (paramInfos.Length == parameters.Count)
            {
                var retval = method.Invoke(null, parameters.ToArray());
                if (retval != null)
                    Sandbox.Game.Gui.MyHud.Chat.ShowMessage("return value", retval.ToString());
                Sandbox.Game.Gui.MyHud.Chat.ShowMessage("Call", "Success");
                return true;
            }
            return false;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Scripts.Clear();
            InGameScripts.Clear();
            InGameScriptsCode.Clear();
            EntityScripts.Clear();
            m_scriptsToSave.Clear();
        }

        public override void SaveData()
        {
            base.SaveData();
            WriteScripts(MySession.Static.CurrentPath);
        }

        //mission/savegame script sources
        private Dictionary<String, String> m_scriptsToSave = new Dictionary<String, String>();
        private void ReadScripts(string path)
        {
            var fsPath = Path.Combine(path, "Data", "Scripts");
            var scriptFiles = MyFileSystem.GetFiles(fsPath, "*.cs");//, searchOption: VRage.FileSystem.SearchOption.TopDirectoryOnly);
            try
            {
                if (scriptFiles.Count() == 0)
                    return;
            }
            catch (Exception)
            {
                MySandboxGame.Log.WriteLine(string.Format("Failed to load scripts from: {0}", path));
                return;
            }
            foreach (var file in scriptFiles)
            {
                try
                {
                    var stream = MyFileSystem.OpenRead(file);
                    using (var sr = new StreamReader(stream))
                    {
                        m_scriptsToSave.Add(file.Substring(fsPath.Length+1),sr.ReadToEnd());
                    }
                }
                catch (Exception e)
                {
                    MySandboxGame.Log.WriteLine(e);
                }
            }
        }
        private void WriteScripts(string path)
        {
            try
            {
                var fsPath = Path.Combine(path, "Data", "Scripts");
                foreach (var file in m_scriptsToSave)
                {
                    var newPath = string.Format("{0}\\{1}", fsPath, file.Key);
                    var stream = MyFileSystem.OpenWrite(newPath);
                    using (var sw = new StreamWriter(stream))
                    {
                        sw.Write(file.Value);
                    }
                }
            }
            catch (Exception e)
            {
                MySandboxGame.Log.WriteLine(e);
            }
        }
        //variables for scripts:
        public override void Init(MyObjectBuilder_SessionComponent sessionComponentBuilder)
        {
            base.Init(sessionComponentBuilder);

            var ob = (MyObjectBuilder_ScriptManager)sessionComponentBuilder;
            MyAPIUtilities.Static.Variables = ob.variables.Dictionary;
        }
        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            MyObjectBuilder_ScriptManager ob = (MyObjectBuilder_ScriptManager)base.GetObjectBuilder();
            ob.variables.Dictionary = MyAPIUtilities.Static.Variables;
            return ob;
        }

    }
}
