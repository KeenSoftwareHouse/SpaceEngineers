using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage.Plugins;

namespace VRage.Game.VisualScripting.ScriptBuilder
{
    public static class MyVSAssemblyProvider
    {
        private static MyVSPreprocessor m_defaultPreprocessor = new MyVSPreprocessor();
        private static MyVSCompiler     m_defaultCompiler;
        private static bool             m_firstRun = true;

        public static void Init(IEnumerable<string> fileNames)
        {
            if (m_firstRun)
            {
                MyVSCompiler.DependencyCollector.CollectReferences(MyPlugins.GameAssembly);
                m_firstRun = false;
            }

            m_defaultPreprocessor.Clear();

            foreach (var fileName in fileNames)
                m_defaultPreprocessor.AddFile(fileName);
            
            var tempFiles = new List<string>();
            var filesToCompile = m_defaultPreprocessor.FileSet;
            var scriptBuilder = new MyVisualScriptBuilder();

            foreach (var absoluteFilePath in filesToCompile)
            {
                scriptBuilder.ScriptFilePath = absoluteFilePath;
                if(!scriptBuilder.Load() || !scriptBuilder.Build())
                {
                    Debug.Fail("One of dependencies of " + scriptBuilder.ScriptName + " failed to build. Script wont be built.");
                    continue;
                }

                tempFiles.Add(Path.Combine(Path.GetTempPath(), scriptBuilder.ScriptName + ".cs"));
                File.WriteAllText(tempFiles[tempFiles.Count - 1], scriptBuilder.Syntax);
            }

            m_defaultCompiler = new MyVSCompiler("MyVSDefaultAssembly", tempFiles);

            if (filesToCompile.Length > 0)
                if(!m_defaultCompiler.Compile() || !m_defaultCompiler.LoadAssembly())
                {
                    Debug.Fail("Default Compiler failed to compile or load VS assembly.");
                }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if(!MyFinalBuildConstants.IS_DEBUG)
                foreach (var tempFile in tempFiles)
                    File.Delete(tempFile);
        }

        public static Type GetType(string typeName)
        {
            if(m_defaultCompiler.Assembly == null)
                return null;

            return m_defaultCompiler.Assembly.GetType(typeName);
        }

        public static List<IMyLevelScript> GetLevelScriptInstances()
        {
            if(m_defaultCompiler == null || m_defaultCompiler.Assembly == null)
                return null;

            return m_defaultCompiler.GetLevelScriptInstances();
        }
    }
}
