using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace VRage.Compiler
{
    public class IlCompilerOptions
    {
        public IlCompilerOptions()
        {
            AssemblyNames = new List<string>();
        }

        public List<string> AssemblyNames { get; private set; }

        public bool Debug { get; set; }

        public CompilerParameters CreateCodeDomCompilerParameters(string outputAssembly)
        {
            var options = new CompilerParameters(AssemblyNames.ToArray())
            {
                GenerateInMemory = true,
                OutputAssembly = outputAssembly
            };
            if (Debug) options.CompilerOptions = "/debug " + options.CompilerOptions;
            return options;
        }
    }

    
}
