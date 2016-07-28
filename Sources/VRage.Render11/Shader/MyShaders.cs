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
using VRage.Render11.Shader;

namespace VRageRender
{
    partial class MyRender11
    {
        internal static ShaderMacro[] GlobalShaderMacro = new ShaderMacro[0];
    }

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

        // Needed because the single parameter version of ComputeShader.SetUnorderedAccessView allocates
        internal static UnorderedAccessView[] TmpUav = new UnorderedAccessView[1];
        internal static int[] TmpCount = new int[1] { -1 };
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
        internal const bool DUMP_CODE = false;//MyRender11.DebugMode;

        internal static void Init()
        {

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
        static InputLayout [] ILObjects = new InputLayout[64];

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
                Profile = MyShadersDefines.Profiles.vs_5_0,
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
                Profile = MyShadersDefines.Profiles.ps_5_0,
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
                Profile = MyShadersDefines.Profiles.cs_5_0,
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
                Profile = MyShadersDefines.Profiles.gs_5_0,
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

            var path = Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath, info.File.ToString());
            if (!File.Exists(path))
            {
                string message = "ERROR: Shaders Compile - can not find file: " + path;
                MyRender11.Log.WriteLine(message);
                throw new MyRenderException(message, MyRenderExceptionEnum.Unassigned);
            }

            ShaderMacro[] macros = MyRender11.GlobalShaderMacro;
            if (info.Macros != null && info.Macros.Length > 0 || MyRender11.DebugMode)
            {
                macros = new ShaderMacro[MyRender11.GlobalShaderMacro.Length + (info.Macros != null ? info.Macros.Length : 0)];
                MyRender11.GlobalShaderMacro.CopyTo(macros, 0);
                if (info.Macros != null)
                    info.Macros.CopyTo(macros, MyRender11.GlobalShaderMacro.Length);
            }
            string shaderSource;
            using (var reader = new StreamReader(path))
            {
                shaderSource = reader.ReadToEnd();
            }
            var compiled = Compile(shaderSource, macros, info.Profile, info.File.ToString(), false);

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

        internal static byte[] Compile(string source, ShaderMacro[] macros, MyShadersDefines.Profiles profile, string sourceDescriptor, bool invalidateCache)
        {
            bool wasCached;
            string compileLog;
            var result = Compile(source, macros, profile, sourceDescriptor, !MyRender11.DebugMode, invalidateCache, out wasCached, out compileLog);

            if (!wasCached)
            {
                string message = "WARNING: Shader was not precompiled - " + sourceDescriptor + " @ profile " + profile + " with defines " + macros.GetString();
                MyRender11.Log.WriteLine(message);
            }
            if (!string.IsNullOrEmpty(compileLog))
            {
                string descriptor = sourceDescriptor + " " + MyShadersDefines.ProfileToString(profile) + " " + macros.GetString();

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

        internal static byte[] Compile(string source, ShaderMacro[] macros, MyShadersDefines.Profiles profile, string sourceDescriptor, bool optimize, bool invalidateCache, out bool wasCached, out string compileLog)
        {
            ProfilerShort.Begin("MyShaders.Compile");
            string function = MyShadersDefines.ProfileEntryPoint(profile);
            string profileName = MyShadersDefines.ProfileToString(profile);

            wasCached = false;
            compileLog = null;

            ProfilerShort.Begin("MyShaders.Preprocess");
            string preprocessedSource = PreprocessShader(source, macros);

            var key = MyShaderCache.CalculateKey(preprocessedSource, function, profileName);
            if (!invalidateCache)
            {
                var cached = MyShaderCache.TryFetch(key);
                if (cached != null)
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
                CompilationResult compilationResult = ShaderBytecode.Compile(preprocessedSource, function, profileName, optimize ? ShaderFlags.OptimizationLevel3 : 0, 0, null, null, descriptor);

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
                {
                    compileLog = ExtendedErrorMessage(source, compilationResult.Message) + DumpShaderSource(key, preprocessedSource);
                }

                if (compilationResult.Bytecode != null && compilationResult.Bytecode.Data.Length > 0)
                    MyShaderCache.Store(key.ToString(), compilationResult.Bytecode.Data);

                return compilationResult.Bytecode != null ? compilationResult.Bytecode.Data : null;
            }
            catch (CompilationException e)
            {
                Debug.WriteLine(preprocessedSource);
                compileLog = ExtendedErrorMessage(source, e.Message) + DumpShaderSource(key, preprocessedSource);
            }
            finally
            {
                ProfilerShort.End();
            }
            return null;
        }

        internal static string DumpShaderSource(string key, string preprocessedSource)
        {
            string fileName = Path.Combine(MyFileSystem.UserDataPath, MyShadersDefines.CachePath, Path.GetFileName(key + ".hlsl"));
            using (var writer = new StreamWriter(fileName, false))
            {
                writer.Write(preprocessedSource);
            }
            return "Preprocessed source written to " + fileName + "\n";
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

        private static string PreprocessShader(string source, ShaderMacro[] macros)
        {
            try
            {
                var includes = new MyIncludeProcessor(Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath));
                return ShaderBytecode.Preprocess(source, macros, includes);
            }
            catch (CompilationException e)
            {
                return null;
            }
        }

        private static string ExtendedErrorMessage(string originalCode, string errorMsg)
        {
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
            return "";
#else // !XB1
            Regex rx = new Regex(@"^(?<fileName>\S*)\((?<lineNo>\d+),\d+\):(?<message>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var sb = new StringBuilder();
            var errorLines = errorMsg.Split(new[] { "\n" }, StringSplitOptions.None);
            for(int j = 0; j < errorLines.Length; j++)
            {
                var match = rx.Match(errorLines[j]);
                if (match == null)
                {
                    sb.AppendLine(errorLines[j]);
                    continue;
                }

                var lineStr = match.Groups["lineNo"].Value;
                if (lineStr != "")
                {
                    var code = originalCode;
                    var fileName = match.Groups["fileName"].Value;
                    if (fileName != "")
                    {
                        try
                        {
                            var includes = new MyIncludeProcessor(Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath));
                            var stream = includes.Open(IncludeType.System, fileName, null);
                            if (stream == null)
                                return "";
                            using (var reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                code = reader.ReadToEnd();
                            }
                        }
                        catch (Exception ex)
                        {
                            return "";
                        }
                    }

                    sb.AppendFormat("{0} @ {1}: {2}\n", fileName, lineStr, match.Groups["message"].Value);

                    var line = Int32.Parse(lineStr) - 1;
                    var sourceLines = code.Split(new[] { "\n" }, StringSplitOptions.None);

                    for (int i = -2; i <= 2; i++)
                    {
                        var offseted = line + i;
                        if (0 <= offseted && offseted < sourceLines.Length)
                        {
                            sb.AppendFormat("{0}: {1}\n", offseted + 1, sourceLines[offseted]);
                        }
                    }
                    sb.AppendFormat("\n");
                }
                else
                {
                    sb.AppendLine(errorLines[j]);
                }
            }
            return sb.ToString();
#endif // !XB1
        }

        #endregion

        private static ShaderBytecodeId CreateBytecode()
        { 
            var id = new ShaderBytecodeId { Index = Bytecodes.Allocate() };
            return id;
        }

        private class MyIncludeProcessor : Include
        {
            private List<string> m_pathStacks;

            internal MyIncludeProcessor(string path)
            {
                m_pathStacks = new List<string>();
                m_pathStacks.Add(path);
            }

            public void Close(Stream stream)
            {
                stream.Close();
                m_pathStacks.RemoveAt(m_pathStacks.Count - 1);
            }

            public Stream Open(IncludeType type, string fileName, Stream parentStream)
            {
                string baseDir;
                if (type == IncludeType.Local)
                {
                    baseDir = String.Concat(m_pathStacks.FindAll(item => item != "\\"));
                }
                else
                {
                    baseDir = m_pathStacks.First();
                }

                string fullFileName = Path.Combine(baseDir, fileName);
                string localPath = Path.GetDirectoryName(fullFileName.Substring(baseDir.Length));
                m_pathStacks.Add(localPath);

                if (MyFileSystem.FileExists(fullFileName))
                {
                    return new FileStream(fullFileName, FileMode.Open, FileAccess.Read);
                }
                else
                {
                    string message = "Include not found: " + fullFileName;
                    MyRender11.Log.WriteLine(message);
                    Debug.WriteLine(message);
                    return Stream.Null;
                }

            }

            public void Dispose()
            {
            }

            public IDisposable Shadow { get; set; }
        }

        private struct MyShaderCompilationInfo
        {
            internal MyStringId File;
            internal MyShadersDefines.Profiles Profile;
            internal ShaderMacro[] Macros;
        }
    }
}
