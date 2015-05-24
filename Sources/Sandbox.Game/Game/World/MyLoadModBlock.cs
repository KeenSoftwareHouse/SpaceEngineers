using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Compiler;
using VRage.FileSystem;
using VRage.Utils;

namespace Sandbox.Game.World
{
    class MyLoadModBlock
    {
        public static MyLoadModBlock Static;
        public Dictionary<MyStringId, Assembly> Scripts = new Dictionary<MyStringId, Assembly>();
        List<string> m_cachedFiles = new List<string>();
        List<string> m_errors = new List<string>();

        static MyLoadModBlock()
        {
            Static = new MyLoadModBlock();
        }



        internal void LoadData(List<Common.ObjectBuilders.MyObjectBuilder_Checkpoint.ModItem> modlist)
        {
            foreach (var mod in modlist)
            {
                LoadScripts(Path.Combine(MyFileSystem.ModsPath, mod.Name), mod.Name);
            }

        }

        private void LoadScripts(string path, string modName = null)
        {
            if (!MyFakes.ENABLE_SCRIPTS)
                return;

            var fsPath = Path.Combine(path, "Data", "Blocks");
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
            var isZip = VRage.FileSystem.MyZipFileProvider.IsZipFile(path);

            Compile(scriptFiles.ToArray(), string.Format("{0}_{1}", modName, "blocks"), isZip);

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
                        MyDefinitionErrors.Add(c, string.Format("Cannot load {0}", Path.GetFileName(file)), ErrorSeverity.Error);
                        MyDefinitionErrors.Add(c, e.Message, ErrorSeverity.Error);
                    }
                }
                IlCompiler.CompileFile(assemblyName, m_cachedFiles.ToArray(), out assembly, m_errors);
            }
            else
            {
                IlCompiler.CompileFile(assemblyName, scriptFiles.ToArray(), out assembly, m_errors);
            }
            if (assembly != null)
            {
                AddAssembly(MyStringId.GetOrCompute(assemblyName), assembly);
            }
            else
            {
                MyDefinitionErrors.Add(c, string.Format("Compilation of {0} failed:", assemblyName), ErrorSeverity.Error);
                MySandboxGame.Log.IncreaseIndent();
                foreach (var error in m_errors)
                    MyDefinitionErrors.Add(c, error.ToString(), ErrorSeverity.Error);
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
                //Debug.Fail(string.Format("Script already in list {0}", myStringId.ToString()));
                return;
            }
            Scripts.Add(myStringId, assembly);

            Sandbox.Common.ObjectBuilders.MyObjectBuilderType.RegisterFromAssembly(assembly, true);
            Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.LoadSerializers(assembly);
            Sandbox.Game.Entities.Cube.MyCubeBlockFactory.RegisterFromAssembly(assembly);
            MyDefinitionManager.RegisterFromAssembly(assembly);
            Sandbox.Engine.Multiplayer.MyTransportLayer.RegisterFromAssembly(assembly);
            MySandboxGame.PreloadTypesFrom(assembly);
        }
    }
}
