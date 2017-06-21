using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Compiler;
using VRage.FileSystem;
using VRage.Game;
using VRage.Scripting.Analyzers;
using VRage.Scripting.Rewriters;

namespace VRage.Scripting
{
    /// <summary>
    ///     Provides a compiler for scripts, with support for a type whitelist and instruction counting.
    /// </summary>
    public class MyScriptCompiler
    {
        public struct Message
        {
            public readonly TErrorSeverity Severity;
            public readonly string Text;

            public Message(TErrorSeverity severity, string text) : this()
            {
                Severity = severity;
                Text = text;
            }
        }

        /// <summary>
        ///     Retrieves the default script compiler.
        /// </summary>
        public static readonly MyScriptCompiler Static = new MyScriptCompiler();

        readonly List<MetadataReference> m_metadataReferences = new List<MetadataReference>();
        readonly MyScriptWhitelist m_whitelist;
        readonly CSharpCompilationOptions m_debugCompilationOptions;
        readonly CSharpCompilationOptions m_runtimeCompilationOptions;
        readonly WhitelistDiagnosticAnalyzer m_ingameWhitelistDiagnosticAnalyzer;
        readonly WhitelistDiagnosticAnalyzer m_modApiWhitelistDiagnosticAnalyzer;
        readonly HashSet<string> m_assemblyLocations = new HashSet<string>();
        readonly HashSet<string> m_implicitScriptNamespaces = new HashSet<string>();
        readonly HashSet<string> m_ignoredWarnings = new HashSet<string>();
        readonly HashSet<Type> m_unblockableIngameExceptions = new HashSet<Type>();
        readonly HashSet<string> m_conditionalCompilationSymbols = new HashSet<string>();
        readonly CSharpParseOptions m_conditionalParseOptions;

        public MyScriptCompiler()
        {
            AddReferencedAssemblies(
                this.GetType().Assembly.Location,
                typeof(int).Assembly.Location,
                typeof(System.Xml.XmlEntity).Assembly.Location,
                typeof(System.Collections.Generic.HashSet<>).Assembly.Location,
                typeof(System.Uri).Assembly.Location
                );

            AddImplicitIngameNamespacesFromTypes(
                typeof(System.Object),
                typeof(System.Text.StringBuilder),
                typeof(System.Collections.IEnumerable),
                typeof(System.Collections.Generic.IEnumerable<>)
                );

            AddUnblockableIngameExceptions(typeof(ScriptOutOfRangeException));

            m_debugCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug, platform: Platform.X64);
            m_runtimeCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, platform: Platform.X64);
            m_whitelist = new MyScriptWhitelist(this);
            m_ingameWhitelistDiagnosticAnalyzer = new WhitelistDiagnosticAnalyzer(m_whitelist, MyWhitelistTarget.Ingame);
            m_modApiWhitelistDiagnosticAnalyzer = new WhitelistDiagnosticAnalyzer(m_whitelist, MyWhitelistTarget.ModApi);
            m_conditionalParseOptions = new CSharpParseOptions();
        }

        /// <summary>
        ///     Gets the assembly locations to be referenced by the scripts
        /// </summary>
        public HashSetReader<string> AssemblyLocations
        {
            get { return m_assemblyLocations; }
        }

        /// <summary>
        ///     Gets the namespaces that are to be added to the ingame script using list
        /// </summary>
        public HashSetReader<string> ImplicitIngameScriptNamespaces
        {
            get { return m_implicitScriptNamespaces; }
        }

        /// <summary>
        ///     Gets the exception types that are to be made unblockable in ingame scripts
        /// </summary>
        public HashSetReader<Type> UnblockableIngameExceptions
        {
            get { return m_unblockableIngameExceptions; }
        }

        /// <summary>
        ///     Gets the conditional compilation symbols scripts are compiled with.
        /// </summary>
        public HashSetReader<string> ConditionalCompilationSymbols
        {
            get { return m_conditionalCompilationSymbols; }
        }

        /// <summary>
        ///     If this property is set, the compiler will write altered scripts and diagnostics to this
        ///     folder.
        /// </summary>
        public string DiagnosticOutputPath { get; set; }

        /// <summary>
        ///     Gets the whitelist being used for this compiler.
        /// </summary>
        public MyScriptWhitelist Whitelist
        {
            get { return m_whitelist; }
        }

        /// <summary>
        ///     Contains the diagnostic codes of warnings that should not be reported by the compiler.
        /// </summary>
        public HashSet<string> IgnoredWarnings
        {
            get { return m_ignoredWarnings; }
        }

        /// <summary>
        ///     Determines whether debug information is enabled on a global level. This decision can be made on a per-script
        ///     fashion on each of the compile methods, but if this property is set to <c>true</c>, it will override any
        ///     parameter value.
        /// </summary>
        public bool EnableDebugInformation { get; set; }

        /// <summary>
        ///     Compiles a script.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assemblyName"></param>
        /// <param name="script"></param>
        /// <param name="messages"></param>
        /// <param name="enableDebugInformation"></param>
        /// <returns></returns>
        public Task<Assembly> Compile(MyApiTarget target, string assemblyName, Script script, List<Message> messages, bool enableDebugInformation = false)
        {
            return Compile(target, assemblyName, new[] {script}, messages, enableDebugInformation);
        }

        /// <summary>
        ///     Compiles a script.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assemblyName"></param>
        /// <param name="scripts"></param>
        /// <param name="messages"></param>
        /// <param name="enableDebugInformation"></param>
        /// <returns></returns>
        public async Task<Assembly> Compile(MyApiTarget target, string assemblyName, IEnumerable<Script> scripts, List<Message> messages, bool enableDebugInformation = false)
        {
            messages.Clear();
            CompilationWithAnalyzers analyticCompilation;
            CSharpCompilation compilation;
            switch (target)
            {
                case MyApiTarget.None:
                    compilation = CreateCompilation(assemblyName, scripts, EnableDebugInformation || enableDebugInformation);
                    await WriteDiagnostics(target, assemblyName, compilation.SyntaxTrees).ConfigureAwait(false);
                    analyticCompilation = null;
                    break;

                case MyApiTarget.Mod:
                    analyticCompilation = CreateCompilation(assemblyName, scripts, EnableDebugInformation || enableDebugInformation)
                        .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(m_modApiWhitelistDiagnosticAnalyzer));
                    await WriteDiagnostics(target, assemblyName, analyticCompilation.Compilation.SyntaxTrees).ConfigureAwait(false);
                    compilation = (CSharpCompilation)analyticCompilation.Compilation;
                    break;

                case MyApiTarget.Ingame:
                    compilation = CreateCompilation(assemblyName, scripts, EnableDebugInformation || enableDebugInformation);
                    await WriteDiagnostics(target, assemblyName, compilation.SyntaxTrees).ConfigureAwait(false);
                    var newSyntaxTrees = await Task.WhenAll(compilation.SyntaxTrees.Select(tree => InjectInstructionCounter(compilation, tree))).ConfigureAwait(false);
                    await WriteDiagnostics(target, assemblyName, newSyntaxTrees, ".injected").ConfigureAwait(false);

                    analyticCompilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(newSyntaxTrees).WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(m_ingameWhitelistDiagnosticAnalyzer));
                    compilation = (CSharpCompilation)analyticCompilation.Compilation;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("target", target, "Invalid compilation target");
            }

            using (var assemblyStream = new MemoryStream())
            {
                var result = compilation.Emit(assemblyStream);
                var success = result.Success;

                //GR: Check for finalizers after sucessfull compilation
                if (success)
                {
                    foreach (var script in scripts)
                    {
                        success &= HasFinalizers(script.Code, messages);
                    }
                }

                AnalyzeDiagnostics(result.Diagnostics, messages, ref success);
                if (analyticCompilation != null)
                {
                    AnalyzeDiagnostics(await analyticCompilation.GetAllDiagnosticsAsync().ConfigureAwait(false), messages, ref success);
                }

                await WriteDiagnostics(target, assemblyName, messages, success).ConfigureAwait(false);

                if (success)
                {
                    assemblyStream.Seek(0, SeekOrigin.Begin);
                    return Assembly.Load(assemblyStream.ToArray());
                }

                return null;
            }
        }

        /// <summary>
        ///     Injects instruction counter code into the given syntax tree.
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="tree"></param>
        /// <returns></returns>
        private async Task<SyntaxTree> InjectInstructionCounter(CSharpCompilation compilation, SyntaxTree tree)
        {
            var rewriter = new InstructionCountingRewriter(this, compilation, tree);
            return await rewriter.Rewrite().ConfigureAwait(false);
        }

        /// <summary>
        ///     Analyzes the given diagnostics and places errors and warnings in the messages lists.
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="messages"></param>
        /// <param name="success"></param>
        private void AnalyzeDiagnostics(ImmutableArray<Diagnostic> diagnostics, List<Message> messages, ref bool success)
        {
            success = success && !diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

            var orderedDiagnostics = diagnostics
                .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                .OrderByDescending(d => d.Severity);

            foreach (var diagnostic in orderedDiagnostics)
            {
                // Warnings are only added for a successful build to reduce confusion.
                if (diagnostic.Severity == DiagnosticSeverity.Warning)
                {
                    if (!success || m_ignoredWarnings.Contains(diagnostic.Id))
                    {
                        continue;
                    }
                }

                var severity = diagnostic.Severity == DiagnosticSeverity.Warning ? TErrorSeverity.Warning : TErrorSeverity.Error;
                var lineSpan = diagnostic.Location.GetMappedLineSpan();
                var message = string.Format("{0}({1},{2}): {3}: {4}",
                    lineSpan.Path,
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character,
                    severity,
                    diagnostic.GetMessage());

                messages.Add(new Message(severity, message));
            }
        }

        bool GetDiagnosticsOutputPath(MyApiTarget target, string assemblyName, out string outputPath)
        {
            outputPath = this.DiagnosticOutputPath;
            if (outputPath == null)
            {
                return false;
            }
            if (assemblyName == null)
            {
                throw new ArgumentNullException("assemblyName");
            }
            outputPath = Path.Combine(DiagnosticOutputPath, target.ToString(), Path.GetFileNameWithoutExtension(assemblyName));
            return true;
        }

        /// <summary>
        ///     If diagnostic output is enabled, this method writes the log of a compilation.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assemblyName"></param>
        /// <param name="messages"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        async Task WriteDiagnostics(MyApiTarget target, string assemblyName, IEnumerable<Message> messages, bool success)
        {
            string outputPath;
            if (!GetDiagnosticsOutputPath(target, assemblyName, out outputPath))
            {
                return;
            }
            var fileName = Path.Combine(outputPath, "log.txt");
            var builder = new StringBuilder();
            builder.AppendLine("Success: " + success);
            builder.AppendLine();
            foreach (var line in messages)
            {
                builder.AppendLine(line.Severity + " " + line.Text);
            }
            using (var stream = MyFileSystem.OpenWrite(fileName))
            {
                var writer = new StreamWriter(stream);
                await writer.WriteAsync(builder.ToString()).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     If diagnostics is enabled, this method writes
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assemblyName"></param>
        /// <param name="syntaxTrees"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        async Task WriteDiagnostics(MyApiTarget target, string assemblyName, IEnumerable<SyntaxTree> syntaxTrees, string suffix = null)
        {
            string outputPath;
            if (!GetDiagnosticsOutputPath(target, assemblyName, out outputPath))
            {
                return;
            }
            suffix = suffix ?? "";
            foreach (var syntaxTree in syntaxTrees)
            {
                var root = await syntaxTree.GetRootAsync().ConfigureAwait(false);
                var normalizedTree = CSharpSyntaxTree.Create((CSharpSyntaxNode)root.NormalizeWhitespace(), path: syntaxTree.FilePath);
                var fileName = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(syntaxTree.FilePath) + suffix + Path.GetExtension(syntaxTree.FilePath));
                if (!fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".cs";
                }
                using (var stream = MyFileSystem.OpenWrite(fileName))
                {
                    var writer = new StreamWriter(stream);
                    await writer.WriteAsync(normalizedTree.ToString()).ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        ///     Creates a ModAPI script compilation for the given script set.
        /// </summary>
        /// <param name="assemblyFileName"></param>
        /// <param name="scripts"></param>
        /// <param name="enableDebugInformation"></param>
        /// <returns></returns>
        internal CSharpCompilation CreateCompilation(string assemblyFileName, IEnumerable<Script> scripts, bool enableDebugInformation)
        {
            IEnumerable<SyntaxTree> syntaxTrees = null;
            if (scripts != null)
            {
                syntaxTrees = scripts.Select(s => CSharpSyntaxTree.ParseText(s.Code, options: m_conditionalParseOptions.WithPreprocessorSymbols(ConditionalCompilationSymbols), path: s.Name));
            }
            if (assemblyFileName != null)
            {
                assemblyFileName = Path.GetFileName(assemblyFileName);
            }
            else
            {
                assemblyFileName = "scripts.dll";
            }
            var compilation = CSharpCompilation.Create(assemblyFileName, syntaxTrees, m_metadataReferences, EnableDebugInformation || enableDebugInformation ? m_debugCompilationOptions : m_runtimeCompilationOptions);
            return compilation;
        }

        /// <summary>
        ///     Adds assemblyLocations to be referenced by scripts.
        /// </summary>
        /// <param name="assemblyLocations"></param>
        public void AddReferencedAssemblies(params string[] assemblyLocations)
        {
            for (var i = 0; i < assemblyLocations.Length; i++)
            {
                var location = assemblyLocations[i];
                if (location == null)
                {
                    throw new ArgumentNullException("assemblyLocations");
                }
                if (m_assemblyLocations.Add(location))
                {
                    m_metadataReferences.Add(MetadataReference.CreateFromFile(location));
                }
            }
        }

        /// <summary>
        ///     Adds the given namespaces for automatic inclusion in the ingame script wrapper.
        ///     **This method does NOT whitelist namespaces!
        /// </summary>
        /// <param name="types"></param>
        public void AddImplicitIngameNamespacesFromTypes(params Type[] types)
        {
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type == null)
                {
                    throw new ArgumentNullException("types");
                }
                m_implicitScriptNamespaces.Add(types[i].Namespace);
            }
        }

        /// <summary>
        ///     Adds the given exceptions to the unblockable list for ingame scripts. These exceptions
        ///     will be added to try/catch expressions so they cannot be caught in-game.
        /// </summary>
        /// <param name="types"></param>
        public void AddUnblockableIngameExceptions(params Type[] types)
        {
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type == null)
                {
                    throw new ArgumentNullException("types");
                }
                if (!typeof(Exception).IsAssignableFrom(type))
                {
                    throw new ArgumentException(type.FullName + " is not an exception", "types");
                }
                if (type.IsGenericType || type.IsGenericTypeDefinition)
                {
                    throw new ArgumentException("Generic exceptions are not supported", "types");
                }
                m_unblockableIngameExceptions.Add(type);
            }
        }

        /// <summary>
        ///     Adds a conditional compilation symbol
        /// </summary>
        /// <param name="symbols"></param>
        public void AddConditionalCompilationSymbols(params string[] symbols)
        {
            for (var i = 0; i < symbols.Length; i++)
            {
                var symbol = symbols[i];
                if (symbol == null)
                {
                    throw new ArgumentNullException("symbols");
                }
                else if (symbol == string.Empty)
                {
                    continue;
                }
                m_conditionalCompilationSymbols.Add(symbols[i]);
            }
        }

        /// <summary>
        ///     Creates a complete code file from an ingame script.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="className"></param>
        /// <param name="inheritance"></param>
        /// <param name="modifiers"></param>
        /// <returns></returns>
        public Script GetIngameScript(string code, string className, string inheritance, string modifiers = "sealed")
        {
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentException("Argument is null or empty", "className");
            }
            var usings = string.Join(";\nusing ", m_implicitScriptNamespaces);
            modifiers = modifiers ?? "";
            inheritance = string.IsNullOrEmpty(inheritance) ? "" : ": " + inheritance;
            code = code ?? "";
            return new Script(className, string.Format("using {0};\n" +
                                                       "public {1} class {2} {3}{{\n" +
                                                       "#line 1 \"{2}\"\n" +
                                                       "{4}\n" +
                                                       "}}\n",
                usings,
                modifiers,
                className,
                inheritance,
                code));
            //return new Script(className, $"using {usings};\n" +
            //                             $"public {modifiers} class {className} {inheritance}{{\n" +
            //                             $"#line 1 \"{className}\"\n" +
            //                             $"{code}\n" +
            //                             $"}}\n");
        }

        /// <summary>
        /// GR: Manual Checking for finalizers (destructors) in code to avoid crashes. No use of regular expressions!!!
        /// Maybe change to the future (if can be done by Roslyn scripts simpler).
        /// </summary>
        private bool HasFinalizers(string code, List<Message> messages)
        {
            var nextIndx = 0;
            var indx = code.IndexOf('~', nextIndx);
            while (indx != -1)
            {
                nextIndx = NextNonSpaceCharIndx(code, indx + 1);
                if (code[nextIndx] == '@' || code[nextIndx] == '_' || Char.IsLetter(code[nextIndx]))
                {
                    while (!WhiteSpaceRelated(code[nextIndx]) && code[nextIndx] != '(') { ++nextIndx; if (nextIndx > code.Length) break; }
                    nextIndx = NextNonSpaceCharIndx(code, nextIndx);
                    if (code[nextIndx] == '(')
                    {
                        nextIndx = NextNonSpaceCharIndx(code, ++nextIndx);
                        if (code[nextIndx] == ')')
                        {
                            nextIndx = NextNonSpaceCharIndx(code, ++nextIndx);
                            if (code[nextIndx] == '{')
                            {
                                messages.Add(new Message(TErrorSeverity.Error, "Error at : " + code.Substring(indx, (code.IndexOf('\n', indx) - indx)) + ". Finalizers are not allowed! Remove any destructors and try again!"));
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    indx = code.IndexOf('~', nextIndx);
                }
                indx = code.IndexOf('~', nextIndx);
            }
            return true;
        }

        /// <summary>
        /// Helper Fuction find next non space related character in string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="indx"></param>
        /// <returns></returns>
        private static int NextNonSpaceCharIndx(string str, int indx)
        {
            while (true)
            {
                if (indx >= str.Length)
                {
                    return -1;
                }
                if (WhiteSpaceRelated(str[indx]))
                {
                    ++indx;
                }
                else
                {
                    return indx;
                }
            }
        }

        /// <summary>
        /// Returns True if a character is whitespace or newline related
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool WhiteSpaceRelated(char ch)
        {
            return ch == '\r' || ch == '\n' || ch == ' ' || ch == '\t';
        }
    }
}