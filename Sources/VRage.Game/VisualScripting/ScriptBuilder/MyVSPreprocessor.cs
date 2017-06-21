using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage.FileSystem;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder
{
    // ReSharper disable once InconsistentNaming
    public class MyVSPreprocessor
    {
        private readonly HashSet<string> m_filePaths = new HashSet<string>();
        private readonly HashSet<string> m_classNames = new HashSet<string>(); 

        public string[] FileSet
        {
            get
            {
                var array = new string[m_filePaths.Count];
                var counter = 0;
                foreach (var filePath in m_filePaths)
                {
                    array[counter++] = filePath;
                }

                return array;
            }
        }

        public void AddFile(string filePath)
        {
            // If file already exists no need for processing
            if(filePath == null || !m_filePaths.Add(filePath)) return;

            MyObjectBuilder_VSFiles bundle = null;

            using(var stream = MyFileSystem.OpenRead(filePath))
            {
                if (stream == null)
                {
                    MyLog.Default.WriteLine("VisualScripting Preprocessor: " + filePath + " is Missing.");
                    Debug.Fail("VisualScripting Preprocessor: " + filePath + " is Missing.");
                }

                // Try deserialization of known types
                if (!MyObjectBuilderSerializer.DeserializeXML(stream, out bundle))
                {
                    Debug.Fail("File " + filePath + " does not belong to object builder of known type.");
                    m_filePaths.Remove(filePath);
                    return;
                }
            }

            // add all dependency files.
            if (bundle.VisualScript != null)
            {
                // Check for duplicite class names Mods are bringing this to play
                if(m_classNames.Add(bundle.VisualScript.Name))
                {
                    foreach (var dependencyFilePath in bundle.VisualScript.DependencyFilePaths)
                        AddFile(Path.Combine(MyFileSystem.ContentPath, dependencyFilePath));
                }
                else
                {
                    m_filePaths.Remove(filePath);
                }
            }

            // add all contained scripts and their dependencies.
            if (bundle.StateMachine != null)
            {
                foreach (var missionSmNode in bundle.StateMachine.Nodes)
                    if(!(missionSmNode is MyObjectBuilder_ScriptSMSpreadNode || missionSmNode is MyObjectBuilder_ScriptSMBarrierNode) && !string.IsNullOrEmpty(missionSmNode.ScriptFilePath))
                        AddFile(Path.Combine(MyFileSystem.ContentPath, missionSmNode.ScriptFilePath));

                m_filePaths.Remove(filePath);
            }

            if (bundle.LevelScript != null)
            {
                // Check for duplicite class names Mods are bringing this to play
                if(m_classNames.Add(bundle.LevelScript.Name))
                {
                    foreach (var dependencyFilePath in bundle.LevelScript.DependencyFilePaths)
                        AddFile(Path.Combine(MyFileSystem.ContentPath, dependencyFilePath));
                }
                else
                {
                    m_filePaths.Remove(filePath);
                }
            }
        }

        public void Clear()
        {
            m_filePaths.Clear();
            m_classNames.Clear();
        }
    }
}
