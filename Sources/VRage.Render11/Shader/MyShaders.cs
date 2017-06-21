using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using VRage;
using VRage.Utils;
using VRage.FileSystem;
using VRage.Profiler;
using VRage.Render11.Shader;

namespace VRageRender
{
    struct ShaderBytecodeId
    {
        internal int Index;

        public static bool operator ==(ShaderBytecodeId x, ShaderBytecodeId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(ShaderBytecodeId x, ShaderBytecodeId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly ShaderBytecodeId NULL = new ShaderBytecodeId { Index = -1 };
    }

    struct InputLayoutId
    {
        internal int Index;

        public static bool operator ==(InputLayoutId x, InputLayoutId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(InputLayoutId x, InputLayoutId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly InputLayoutId NULL = new InputLayoutId { Index = -1 };


        public static implicit operator InputLayout(InputLayoutId id)
        {
            return MyShaders.GetIL(id);
        }
    }

    struct VertexShaderId
    {
        internal int Index;

        public static bool operator ==(VertexShaderId x, VertexShaderId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(VertexShaderId x, VertexShaderId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly VertexShaderId NULL = new VertexShaderId { Index = -1 };

        public static implicit operator VertexShader(VertexShaderId id)
        {
            return MyShaders.GetVs(id);
        }

        internal ShaderBytecodeId BytecodeId { get { return MyShaders.VertexShaders.Data[this.Index].Bytecode; } }
    }

    public struct PixelShaderId
    {
        internal int Index;

        public static bool operator ==(PixelShaderId x, PixelShaderId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(PixelShaderId x, PixelShaderId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly PixelShaderId NULL = new PixelShaderId { Index = -1 };

        public static implicit operator PixelShader(PixelShaderId id)
        {
            return MyShaders.GetPs(id);
        }
    }

    struct ComputeShaderId
    {
        internal int Index;

        public static bool operator ==(ComputeShaderId x, ComputeShaderId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(ComputeShaderId x, ComputeShaderId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly ComputeShaderId NULL = new ComputeShaderId { Index = -1 };

        public static implicit operator ComputeShader(ComputeShaderId id)
        {
            return MyShaders.GetCs(id);
        }
    }

    struct GeometryShaderId
    {
        internal int Index;

        public static bool operator ==(GeometryShaderId x, GeometryShaderId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(GeometryShaderId x, GeometryShaderId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly GeometryShaderId NULL = new GeometryShaderId { Index = -1 };

        public static implicit operator GeometryShader(GeometryShaderId id)
        {
            return MyShaders.GetGs(id);
        }
    }

    struct MyShaderInfo
    {
        internal ShaderBytecodeId Bytecode;
        internal string File;
    }

    struct MyShaderStreamOutputInfo
    {
        internal StreamOutputElement[] Elements;
        internal int[] Strides;
        internal int RasterizerStreams;
    }

    public static class MyShaders
    {
        public static Dictionary<string, ShaderMacro> m_shaderMacrosDictionary;
        public static ShaderMacro[] m_globalShaderMacros;

        internal const bool DUMP_CODE = false;//MyRender11.DebugMode;
        static string m_shadersPath;
        static List<string> m_includes;

        internal static void Init()
        {
            m_shaderMacrosDictionary = new Dictionary<string, ShaderMacro>();
#if DEBUG
            m_shaderMacrosDictionary.Add("DEBUG", new ShaderMacro("DEBUG", null));
#endif
            m_globalShaderMacros = null;
        }

        private struct MyShaderBytecode
        {
            internal byte[] Bytecode;
        }
        static MyFreelist<MyShaderBytecode> Bytecodes = new MyFreelist<MyShaderBytecode>(512);
        static Dictionary<ShaderBytecodeId, MyShaderCompilationInfo> Shaders = new Dictionary<ShaderBytecodeId, MyShaderCompilationInfo>();

        private struct InputLayoutInfo
        {
            internal ShaderBytecodeId BytecodeId;
            internal VertexLayoutId VLayoutId;
        }
        static HashSet<InputLayoutId> ILIndex = new HashSet<InputLayoutId>();
        static MyFreelist<InputLayoutInfo> InputLayouts = new MyFreelist<InputLayoutInfo>(64);
        static InputLayout[] ILObjects = new InputLayout[64];

        static HashSet<VertexShaderId> VsIndex = new HashSet<VertexShaderId>();
        internal static MyFreelist<MyShaderInfo> VertexShaders = new MyFreelist<MyShaderInfo>(128);
        static VertexShader[] VsObjects = new VertexShader[128];

        static HashSet<PixelShaderId> PsIndex = new HashSet<PixelShaderId>();
        internal static MyFreelist<MyShaderInfo> PixelShaders = new MyFreelist<MyShaderInfo>(128);
        static PixelShader[] PsObjects = new PixelShader[128];

        static HashSet<ComputeShaderId> CsIndex = new HashSet<ComputeShaderId>();
        internal static MyFreelist<MyShaderInfo> ComputeShaders = new MyFreelist<MyShaderInfo>(128);
        static ComputeShader[] CsObjects = new ComputeShader[128];

        static HashSet<GeometryShaderId> GsIndex = new HashSet<GeometryShaderId>();
        internal static MyFreelist<MyShaderInfo> GeometryShaders = new MyFreelist<MyShaderInfo>(128);
        static GeometryShader[] GsObjects = new GeometryShader[128];
        static Dictionary<GeometryShaderId, MyShaderStreamOutputInfo> StreamOutputs = new Dictionary<GeometryShaderId, MyShaderStreamOutputInfo>();

        #region Low level compilation

        static Dictionary<MyStringId, byte[]> CompilationCache = new Dictionary<MyStringId, byte[]>(MyStringId.Comparer);

        internal static VertexShaderId CreateVs(string file, ShaderMacro[] macros = null)
        {
            var bytecode = CreateBytecode();

            var id = new VertexShaderId { Index = VertexShaders.Allocate() };
            VertexShaders.Data[id.Index] = new MyShaderInfo
            {
                Bytecode = bytecode
            };
            MyArrayHelpers.Reserve(ref VsObjects, id.Index + 1);

            // compile at once

            Shaders[bytecode] = new MyShaderCompilationInfo
            {
                File = X.TEXT_(file),
                Profile = MyShaderProfile.vs_5_0,
                Macros = macros
            };

            VsObjects[id.Index] = null;

            InitVs(id, file);
            VsIndex.Add(id);

            return id;
        }

        public static PixelShaderId CreatePs(string file, ShaderMacro[] macros = null)
        {
            var bytecode = CreateBytecode();

            var id = new PixelShaderId { Index = PixelShaders.Allocate() };
            PixelShaders.Data[id.Index] = new MyShaderInfo
            {
                Bytecode = bytecode
            };
            MyArrayHelpers.Reserve(ref PsObjects, id.Index + 1);

            // compile at once

            Shaders[bytecode] = new MyShaderCompilationInfo
            {
                File = X.TEXT_(file),
                Profile = MyShaderProfile.ps_5_0,
                Macros = macros
            };

            PsObjects[id.Index] = null;

            InitPs(id, file);
            PsIndex.Add(id);

            return id;
        }

        internal static ComputeShaderId CreateCs(string file, ShaderMacro[] macros = null)
        {
            var bytecode = CreateBytecode();

            var id = new ComputeShaderId { Index = ComputeShaders.Allocate() };
            ComputeShaders.Data[id.Index] = new MyShaderInfo
            {
                Bytecode = bytecode
            };
            MyArrayHelpers.Reserve(ref CsObjects, id.Index + 1);

            // compile at once

            Shaders[bytecode] = new MyShaderCompilationInfo
            {
                File = X.TEXT_(file),
                Profile = MyShaderProfile.cs_5_0,
                Macros = macros,
            };

            CsObjects[id.Index] = null;

            InitCs(id, file);
            CsIndex.Add(id);

            return id;
        }

        internal static GeometryShaderId CreateGs(string file, ShaderMacro[] macros = null, MyShaderStreamOutputInfo? streamOut = null)
        {
            var bytecode = CreateBytecode();

            var id = new GeometryShaderId { Index = GeometryShaders.Allocate() };
            GeometryShaders.Data[id.Index] = new MyShaderInfo
            {
                Bytecode = bytecode
            };
            MyArrayHelpers.Reserve(ref GsObjects, id.Index + 1);

            // compile at once

            Shaders[bytecode] = new MyShaderCompilationInfo
            {
                File = X.TEXT_(file),
                Profile = MyShaderProfile.gs_5_0,
                Macros = macros
            };

            GsObjects[id.Index] = null;

            if (streamOut.HasValue)
            {
                StreamOutputs[id] = streamOut.Value;
            }

            InitGs(id, file);
            GsIndex.Add(id);
            GsObjects[id.Index].DebugName = file;

            return id;
        }

        internal static void Compile(ShaderBytecodeId bytecode, bool invalidateCache = false)
        {
            var info = Shaders[bytecode];

            var path = Path.Combine(ShadersPath, info.File.ToString());
            if (!File.Exists(path))
            {
                string message = "ERROR: Shaders Compile - can not find file: " + path;
                MyRender11.Log.WriteLine(message);
                Debug.WriteLine(message);
                Debugger.Break();
                if (Debugger.IsAttached)
                {
                    Compile(bytecode, true);
                }
                else throw new MyRenderException(message, MyRenderExceptionEnum.Unassigned);
            }

            var macros = new List<ShaderMacro>();
            if (info.Macros != null)
                macros.AddRange(info.Macros);

            var compiled = Compile(path, macros.ToArray(), info.Profile, info.File.ToString(), false);

            Bytecodes.Data[bytecode.Index].Bytecode = compiled ?? Bytecodes.Data[bytecode.Index].Bytecode;

            if (compiled == null)
            {
                string message = "Failed to compile " + info.File + " @ profile " + info.Profile + " with defines " + macros.GetString();
                MyRender11.Log.WriteLine(message);
                if (Debugger.IsAttached)
                {
                    Compile(bytecode, true);
                }
                else throw new MyRenderException(message, MyRenderExceptionEnum.Unassigned);
            }
        }

        internal static byte[] Compile(string filepath, ShaderMacro[] macros, MyShaderProfile profile, string sourceDescriptor, bool invalidateCache)
        {
            bool wasCached;
            string compileLog;
            var result = Compile(filepath, macros, profile, sourceDescriptor, MyCompilationSymbols.EnableShaderDebuggingInNSight, invalidateCache, out wasCached, out compileLog);

            if (!wasCached)
            {
                string message = "WARNING: Shader was not precompiled - " + sourceDescriptor + " @ profile " + profile + " with defines " + macros.GetString();
                MyRender11.Log.WriteLine(message);
            }
            if (!string.IsNullOrEmpty(compileLog))
            {
                string descriptor = sourceDescriptor + " " + ProfileToString(profile) + " " + macros.GetString();

                if (result != null)
                {
                    Debug.WriteLine(String.Format("Compilation of shader {0} notes:\n{1}", descriptor, compileLog));

                }
                else
                {
                    string message = String.Format("Compilation of shader {0} errors:\n{1}", descriptor, compileLog);
                    MyRender11.Log.WriteLine(message);
                    Debug.WriteLine(message);
                    Debugger.Break();
                }
            }
            return result;
        }

        internal static byte[] Compile(string filepath, ShaderMacro[] macros, MyShaderProfile profile, string sourceDescriptor, bool optimize, bool invalidateCache, out bool wasCached, out string compileLog)
        {
            ProfilerShort.Begin("MyShaders.Compile");

            var globalMacros = GlobalShaderMacros;
            if (globalMacros.Length != 0)
            {
                var macroList = new List<ShaderMacro>();
                macroList.AddRange(globalMacros);
                macroList.AddRange(macros);
                macros = macroList.ToArray();
            }

            string function = ProfileEntryPoint(profile);
            string profileName = ProfileToString(profile);

            wasCached = false;
            compileLog = null;

            ProfilerShort.Begin("MyShaders.Preprocess");
            string errors;
            string preprocessedSource = PreprocessShader(filepath, macros, out errors);
            if (preprocessedSource == null)
            {
                compileLog = errors;
                return null;
            }

            // modify preprocessor to be readable for NSight
            if (MyCompilationSymbols.EnableShaderDebuggingInNSight)
            {
                preprocessedSource = Regex.Replace(preprocessedSource, "#line [^\n]*\n", "");
            }

            MyShaderIdentity identity = null;
            if (!invalidateCache && !MyCompilationSymbols.EnableShaderDebuggingInNSight)
            {
                identity = MyShaderCache.ComputeShaderIdentity(preprocessedSource, profile);
                byte[] cached;
                if (MyShaderCache.TryFetch(identity, out cached))
                {
                    wasCached = true;
                    ProfilerShort.End();
                    ProfilerShort.End();
                    return cached;
                }
            }
            ProfilerShort.End();

            try
            {
                string descriptor = sourceDescriptor + " " + profile + " " + macros.GetString();
                CompilationResult compilationResult;
                if (MyCompilationSymbols.EnableShaderDebuggingInNSight)
                {
                    if (MyCompilationSymbols.EnableShaderPreprocessorInNSight)
                        compilationResult = ShaderBytecode.Compile(preprocessedSource, function, profileName, 0, 0, macros, new MyIncludeProcessor(filepath));
                    else
                    {
                        compilationResult = ShaderBytecode.CompileFromFile(filepath, function, profileName, 0, 0, macros, new MyIncludeProcessor(filepath));
                    }
                }
                else
                    compilationResult = ShaderBytecode.Compile(preprocessedSource, function, profileName, 
                        optimize ? ShaderFlags.OptimizationLevel3 : ShaderFlags.None, 
                        EffectFlags.None, filepath);

                if (DUMP_CODE)
                {
                    var disassembly = compilationResult.Bytecode.Disassemble(DisassemblyFlags.EnableColorCode |
                                                                             DisassemblyFlags.EnableInstructionNumbering);
                    string asmPath;
                    if (MyRender11.DebugMode)
                    {
                        asmPath = Path.GetFileName(descriptor + "__DEBUG.html");
                    }
                    else
                    {
                        asmPath = Path.GetFileName(descriptor + "__O3.html");
                    }

                    using (var writer = new StreamWriter(Path.Combine(MyFileSystem.ContentPath, "ShaderOutput", asmPath)))
                    {
                        writer.Write(disassembly);
                    }
                }

                if (compilationResult.Message != null)
                    compileLog = compilationResult.Message;

                if (!MyCompilationSymbols.EnableShaderDebuggingInNSight && compilationResult.Bytecode != null
                        && compilationResult.Bytecode.Data.Length > 0)
                    MyShaderCache.Store(identity, compilationResult.Bytecode.Data);

                return compilationResult.Bytecode != null ? compilationResult.Bytecode.Data : null;
            }
            catch (Exception e)
            {
                Debug.WriteLine(preprocessedSource);
                compileLog = e.Message;
            }
            finally
            {
                ProfilerShort.End();
            }
            return null;
        }

        internal static byte[] GetBytecode(ShaderBytecodeId id)
        {
            return Bytecodes.Data[id.Index].Bytecode;
        }

        internal static VertexShader GetVs(VertexShaderId id)
        {
            return VsObjects[id.Index];
        }

        internal static PixelShader GetPs(PixelShaderId id)
        {
            return PsObjects[id.Index];
        }

        internal static ComputeShader GetCs(ComputeShaderId id)
        {
            return CsObjects[id.Index];
        }

        internal static GeometryShader GetGs(GeometryShaderId id)
        {
            return GsObjects[id.Index];
        }

        internal static InputLayout GetIL(InputLayoutId id)
        {
            return ILObjects[id.Index];
        }

        internal static void InitVs(VertexShaderId id, string file)
        {
            var bytecodeId = VertexShaders.Data[id.Index].Bytecode;
            Compile(bytecodeId);

            if (VsObjects[id.Index] != null)
            {
                VsObjects[id.Index].Dispose();
                VsObjects[id.Index] = null;
            }

            try
            {
                VsObjects[id.Index] = new VertexShader(MyRender11.Device, GetBytecode(bytecodeId));
            }
            catch (SharpDXException e)
            {
                Compile(bytecodeId, true);
                VsObjects[id.Index] = new VertexShader(MyRender11.Device, GetBytecode(bytecodeId));
            }
            VertexShaders.Data[id.Index].File = file;
            VsObjects[id.Index].DebugName = file;
        }

        internal static void InitPs(PixelShaderId id, string file)
        {
            var bytecodeId = PixelShaders.Data[id.Index].Bytecode;
            Compile(bytecodeId);

            if (PsObjects[id.Index] != null)
            {
                PsObjects[id.Index].Dispose();
                PsObjects[id.Index] = null;
            }
            try
            {
                PsObjects[id.Index] = new PixelShader(MyRender11.Device, GetBytecode(bytecodeId));
            }
            catch (SharpDXException e)
            {
                Compile(bytecodeId, true);
                PsObjects[id.Index] = new PixelShader(MyRender11.Device, GetBytecode(bytecodeId));
            }
            PixelShaders.Data[id.Index].File = file;
            PsObjects[id.Index].DebugName = file;
        }

        internal static void InitCs(ComputeShaderId id, string file)
        {
            var bytecodeId = ComputeShaders.Data[id.Index].Bytecode;
            Compile(bytecodeId);

            if (CsObjects[id.Index] != null)
            {
                CsObjects[id.Index].Dispose();
                CsObjects[id.Index] = null;
            }
            try
            {
                CsObjects[id.Index] = new ComputeShader(MyRender11.Device, GetBytecode(bytecodeId));
            }
            catch (SharpDXException e)
            {
                Compile(bytecodeId, true);
                CsObjects[id.Index] = new ComputeShader(MyRender11.Device, GetBytecode(bytecodeId));
            }
            ComputeShaders.Data[id.Index].File = file;
            CsObjects[id.Index].DebugName = file;
        }

        internal static void InitGs(GeometryShaderId id, string file)
        {
            var bytecodeId = GeometryShaders.Data[id.Index].Bytecode;
            Compile(bytecodeId);

            if (GsObjects[id.Index] != null)
            {
                GsObjects[id.Index].Dispose();
                GsObjects[id.Index] = null;
            }

            try
            {
                if (StreamOutputs.ContainsKey(id))
                {
                    var so = StreamOutputs[id];

                    GsObjects[id.Index] = new GeometryShader(MyRender11.Device, GetBytecode(bytecodeId),
                        so.Elements, so.Strides, so.RasterizerStreams, null);
                }
                else
                {
                    GsObjects[id.Index] = new GeometryShader(MyRender11.Device, GetBytecode(bytecodeId));
                }
            }
            catch (SharpDXException e)
            {
                Compile(bytecodeId, true);

                if (StreamOutputs.ContainsKey(id))
                {
                    var so = StreamOutputs[id];

                    GsObjects[id.Index] = new GeometryShader(MyRender11.Device, GetBytecode(bytecodeId),
                        so.Elements, so.Strides, so.RasterizerStreams, null);
                }
                else
                {
                    GsObjects[id.Index] = new GeometryShader(MyRender11.Device, GetBytecode(bytecodeId));
                }
            }
            GeometryShaders.Data[id.Index].File = file;
            GsObjects[id.Index].DebugName = file;
        }

        internal static InputLayoutId CreateIL(ShaderBytecodeId bytecode, VertexLayoutId layout)
        {
            var id = new InputLayoutId { Index = InputLayouts.Allocate() };
            MyArrayHelpers.Reserve(ref ILObjects, id.Index + 1);

            InputLayouts.Data[id.Index] = new InputLayoutInfo
            {
                BytecodeId = bytecode,
                VLayoutId = layout
            };

            ILObjects[id.Index] = null;

            InitIL(id);
            ILIndex.Add(id);

            return id;
        }

        internal static void InitIL(InputLayoutId id)
        {
            var info = InputLayouts.Data[id.Index];

            if (ILObjects[id.Index] != null)
            {
                ILObjects[id.Index].Dispose();
                ILObjects[id.Index] = null;
            }

            ILObjects[id.Index] = new InputLayout(MyRender11.Device, GetBytecode(info.BytecodeId), MyVertexLayouts.GetElements(info.VLayoutId));
        }

        internal static void Recompile()
        {
            foreach (var id in VsIndex)
            {
                string file = VertexShaders.Data[id.Index].File;
                InitVs(id, file);
            }

            foreach (var id in PsIndex)
            {
                string file = PixelShaders.Data[id.Index].File;
                InitPs(id, file);
            }

            foreach (var id in CsIndex)
            {
                string file = ComputeShaders.Data[id.Index].File;
                InitCs(id, file);
            }

            foreach (var id in GsIndex)
            {
                string file = GeometryShaders.Data[id.Index].File;
                InitGs(id, file);
            }

            foreach (var id in ILIndex)
            {
                InitIL(id);
            }
        }

        internal static void OnDeviceEnd()
        {
            foreach (var id in VsIndex)
            {
                if (VsObjects[id.Index] != null)
                {
                    VsObjects[id.Index].Dispose();
                    VsObjects[id.Index] = null;
                }
            }

            foreach (var id in PsIndex)
            {
                if (PsObjects[id.Index] != null)
                {
                    PsObjects[id.Index].Dispose();
                    PsObjects[id.Index] = null;
                }
            }

            foreach (var id in CsIndex)
            {
                if (CsObjects[id.Index] != null)
                {
                    CsObjects[id.Index].Dispose();
                    CsObjects[id.Index] = null;
                }
            }

            foreach (var id in GsIndex)
            {
                if (GsObjects[id.Index] != null)
                {
                    GsObjects[id.Index].Dispose();
                    GsObjects[id.Index] = null;
                }
            }

            foreach (var id in ILIndex)
            {
                if (ILObjects[id.Index] != null)
                {
                    ILObjects[id.Index].Dispose();
                    ILObjects[id.Index] = null;
                }
            }
        }

        internal static void OnDeviceReset()
        {
            OnDeviceEnd();
            // TODO: just rebuild
            Recompile();
        }

        private static string PreprocessShader(string filepath, ShaderMacro[] macros, out string errors)
        {
            try
            {
                var includes = new MyIncludeProcessor(filepath);
                return ShaderBytecode.PreprocessFromFile(filepath, macros, includes, out errors);
            }
            catch (Exception e)
            {
                errors = e.Message;
                return null;
            }
        }

        #endregion

        public static void SetGlobalMacros(IEnumerable<ShaderMacro> macros)
        {
            m_shaderMacrosDictionary.Clear();
#if DEBUG
            m_shaderMacrosDictionary.Add("DEBUG", new ShaderMacro("DEBUG", null));
#endif
            foreach (var macro in macros)
                m_shaderMacrosDictionary[macro.Name] = macro;

            m_globalShaderMacros = null;
        }

        public static ShaderMacro[] GlobalShaderMacros
        {
            get
            {
                if (m_globalShaderMacros == null)
                {
                    if (m_shaderMacrosDictionary == null)
                        m_globalShaderMacros = new ShaderMacro[0];
                    else
                        m_globalShaderMacros = m_shaderMacrosDictionary.Values.ToArray();
                }

                return m_globalShaderMacros;
            }
        }

        public static ShaderMacro[] ConcatenateMacros(ShaderMacro[] sm1, ShaderMacro[] sm2)
        {
            var smRes = new ShaderMacro[sm1.Length + sm2.Length];
            sm1.CopyTo(smRes, 0);
            sm2.CopyTo(smRes, sm1.Length);
            return smRes;
        }

        public static string ShadersPath
        {
            get
            {
                if (m_shadersPath == null)
                    m_shadersPath = Path.Combine(MyFileSystem.ShadersBasePath, MyShadersDefines.ShadersFolderName);

                return m_shadersPath;
            }
        }

        public static IReadOnlyList<string> Includes
        {
            get
            {
                if (m_includes == null)
                    m_includes = new List<string>(new string[] { ShadersPath });

                return m_includes;
            }
        }

        private static ShaderBytecodeId CreateBytecode()
        {
            var id = new ShaderBytecodeId { Index = Bytecodes.Allocate() };
            return id;
        }

        internal static string ProfileToString(MyShaderProfile val)
        {
            switch (val)
            {
                case MyShaderProfile.vs_5_0:
                    return "vs_5_0";

                case MyShaderProfile.ps_5_0:
                    return "ps_5_0";

                case MyShaderProfile.gs_5_0:
                    return "gs_5_0";

                case MyShaderProfile.cs_5_0:
                    return "cs_5_0";

                default:
                    throw new Exception();
            }
        }

        internal static string ProfileEntryPoint(MyShaderProfile val)
        {
            switch (val)
            {
                case MyShaderProfile.vs_5_0:
                    return "__vertex_shader";

                case MyShaderProfile.ps_5_0:
                    return "__pixel_shader";

                case MyShaderProfile.gs_5_0:
                    return "__geometry_shader";

                case MyShaderProfile.cs_5_0:
                    return "__compute_shader";

                default:
                    throw new Exception();
            }
        }

        private class MyIncludeProcessor : Include
        {
            private string m_basePath;

            internal MyIncludeProcessor(string filepath)
            {
                string basePath = null;
                if (filepath != null)
                    basePath = Path.GetDirectoryName(filepath);

                m_basePath = basePath;
            }

            public void Close(Stream stream)
            {
                stream.Close();
            }

            public Stream Open(IncludeType type, string fileName, Stream parentStream)
            {
                string fullFileName = fileName;
                if (type == IncludeType.Local)
                {
                    string baseDir = null;
                    FileStream fileStream = parentStream as FileStream;
                    if (fileStream != null)
                        baseDir = Path.GetDirectoryName(fileStream.Name);
                    else if (m_basePath != null)
                        baseDir = m_basePath;

                    if (baseDir != null)
                    {
                        fullFileName = Path.Combine(baseDir, fileName);
                        if (MyFileSystem.FileExists(fullFileName))
                            return new FileStream(fullFileName, FileMode.Open, FileAccess.Read);
                    }

                    // If base path is not defined, attempt resolving includes
                    if (m_basePath != null)
                        goto NotFound;
                }

                // Iterate defines in reverse order
                for (int it = MyShaders.Includes.Count - 1; it >= 0; it--)
                {
                    string define = MyShaders.Includes[it];
                    fullFileName = Path.Combine(define, fileName);
                    if (MyFileSystem.FileExists(fullFileName))
                        return new FileStream(fullFileName, FileMode.Open, FileAccess.Read);
                }

            NotFound:
                string message = "Include not found: " + fullFileName;
                MyRender11.Log.WriteLine(message);
                // NOTE: Throwing exception here will enable better error reporting in SharpDX
                throw new Exception(message);
            }

            public void Dispose()
            {
            }

            public IDisposable Shadow { get; set; }
        }


        private struct MyShaderCompilationInfo
        {
            internal MyStringId File;
            internal MyShaderProfile Profile;
            internal ShaderMacro[] Macros;
        }
    }

    internal enum MyShaderProfile
    {
        vs_5_0,
        ps_5_0,
        gs_5_0,
        cs_5_0,

        count
    }
}
