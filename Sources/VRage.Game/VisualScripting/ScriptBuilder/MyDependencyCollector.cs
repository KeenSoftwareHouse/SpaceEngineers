using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using VRage.Collections;

namespace VRage.Game.VisualScripting.ScriptBuilder
{
    public class MyDependencyCollector
    {
        private HashSet<MetadataReference> m_references;

        public HashSetReader<MetadataReference> References { get { return new HashSetReader<MetadataReference>(m_references);} } 

        public MyDependencyCollector(IEnumerable<Assembly> assemblies) : this()
        {
            foreach (var assembly in assemblies)
            {
                CollectReferences(assembly);
            }
        }

        public MyDependencyCollector()
        {
            m_references = new HashSet<MetadataReference>();
        }

        public void CollectReferences(Assembly assembly)
        {
            if(assembly == null) return;
            var refAssemblyNames = assembly.GetReferencedAssemblies();

            foreach (var refAssemblyName in refAssemblyNames)
            {
                var loadedAssembly = Assembly.Load(refAssemblyName);
                m_references.Add(MetadataReference.CreateFromFile(loadedAssembly.Location));
            }

            m_references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        public void RegisterAssembly(Assembly assembly)
        {
            if (assembly != null)
            {
                m_references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }
    }
}
