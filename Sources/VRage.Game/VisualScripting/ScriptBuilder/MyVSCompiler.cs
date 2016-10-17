using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.FileSystem;

namespace VRage.Game.VisualScripting.ScriptBuilder
{
    public class MyVSCompiler
    {
        public static MyDependencyCollector                 DependencyCollector = new MyDependencyCollector();

        private static readonly CSharpCompilationOptions    m_defaultCompilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true).WithOptimizationLevel(OptimizationLevel.Release);

        public readonly List<string>    SourceFiles = new List<string>();
        public readonly List<string>    SourceTexts = new List<string>();

        public string                   AssemblyName { get; private set; }

        public Assembly Assembly
        {
            get { return m_compiledAndLoadedAssembly; }
        }

        private CSharpCompilation       m_compilation;
        private Assembly                m_compiledAndLoadedAssembly;
        //private bool                    m_assemblyLoaded;

        public MyVSCompiler(string assemblyName, IEnumerable<string> sourceFiles) : this(assemblyName) 
        {
            SourceFiles.AddRange(sourceFiles);
        }

        public MyVSCompiler(string assemblyName)
        {
            AssemblyName = assemblyName;
            //m_assemblyLoaded = false;
        }

        /// <summary>
        /// Creates a fresh new compilation of source files.
        /// Does not load any assembly.
        /// </summary>
        /// <returns>Success if no compilation erros were encountered.</returns>
        public bool Compile()
        {
            if (SourceFiles.Count == 0 && SourceTexts.Count == 0) return false;

            SyntaxTree [] syntaxTrees = new SyntaxTree[SourceFiles.Count + SourceTexts.Count];
            int currentIndex = 0;

            try
            {
                foreach (string sourceFile in SourceFiles)
                {
                    using (StreamReader reader = new StreamReader(MyFileSystem.OpenRead(sourceFile)))
                    {
                        var text = reader.ReadToEnd();
                        syntaxTrees[currentIndex] = CSharpSyntaxTree.ParseText(text);
                        currentIndex++;
                    }
                }

                foreach (var sourceText in SourceTexts)
                    syntaxTrees[currentIndex++] = CSharpSyntaxTree.ParseText(sourceText);

                m_compilation = CSharpCompilation.Create(AssemblyName, syntaxTrees, 
                    DependencyCollector.References, m_defaultCompilationOptions);

            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
                return false;
            }

            return true;
        }

        public string GetDiagnosticMessage()
        {
            var res = m_compilation.GetDiagnostics();

            if (!res.IsDefaultOrEmpty)
            {
                var sb = new StringBuilder();
                foreach (var diagnostic in res)
                {
                    if(diagnostic.Severity == DiagnosticSeverity.Error)
                        sb.AppendLine(diagnostic.ToString());
                }

                return sb.ToString();
            }

            return String.Empty;
        }

        /// <summary>
        /// Loads assembly.
        /// <returns>Loading success.</returns>
        /// </summary>
        public bool LoadAssembly()
        {
            try
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var pdbStream = new MemoryStream())
                    {
                        var res = m_compilation.Emit(outputStream, pdbStream);

                        if (res.Success)
                        {
                            m_compiledAndLoadedAssembly = Assembly.Load(outputStream.ToArray(), pdbStream.ToArray());
                            //m_assemblyLoaded = true;
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (var diagnostic in res.Diagnostics)
                            {
                                string className = String.Empty;
                                foreach (var node in diagnostic.Location.SourceTree.GetRoot().DescendantNodes())
                                {
                                    if(node is ClassDeclarationSyntax)
                                    {
                                        className = ((ClassDeclarationSyntax)node).Identifier.Text;
                                        break;
                                    }
                                }

                                sb.AppendLine(className + ": " + diagnostic);
                            }

                            Debug.Fail(sb.ToString());
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Fail("Excetion occured while loading " + AssemblyName + "\n" + e);
                return false;
            }
            return true;
        }

        public List<IMyLevelScript> GetLevelScriptInstances()
        {
            List<IMyLevelScript> instances = new List<IMyLevelScript>();

            if (m_compiledAndLoadedAssembly == null) return instances;

            foreach (var type in m_compiledAndLoadedAssembly.GetTypes())
            {
                if (typeof(IMyLevelScript).IsAssignableFrom(type))
                {
                    instances.Add((IMyLevelScript)Activator.CreateInstance(type));
                }
            }

            return instances;
        }
    }
}
