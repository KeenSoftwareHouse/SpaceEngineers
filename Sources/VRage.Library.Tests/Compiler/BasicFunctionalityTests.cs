using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using VRage.Compiler;

namespace VRage.Library.Tests.Compiler
{
    [TestFixture]
    public class BasicFunctionalityTests
    {
        [Test]
        public void CanCompileEmptyClass()
        {
            var code = @"class Empty {}";
            Assembly assembly;
            var errors = new List<string>();

            var options = new IlCompilerOptions();

            var result = new IlCompiler(options).CompileString("IngameScript.dll", new string[] { code }, out assembly, errors);

            Assert.That(errors, Is.Empty);
            Assert.True(result);

        }
    }
}
