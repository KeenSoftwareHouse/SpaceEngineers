using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using VRage.Utils;
using VRageRender;
using VRage.Generics;
using System.Threading;
using System.Security.Cryptography;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.FileSystem;
using System.Text.RegularExpressions;
namespace VRageRender
{
    static class MyShaderDefines
    {
        static StringBuilder m_builder = new StringBuilder();
        internal static string Build(params string [] macros)
        {
            m_builder.Clear();

            foreach(var macro in macros)
            {
                m_builder.AppendFormat("#define {0}{1}", macro, System.Environment.NewLine);
            }

            return m_builder.ToString();
        }
    }

    enum MyShaderProfileEnum
    {
        VS_5_0,
        PS_5_0,
        GS_5_0,
        CS_5_0
    }

    internal static class MyShaderProfileEnumExtensions
    {
        internal static string Value(this MyShaderProfileEnum val)
        {
            switch (val)
            {
                case MyShaderProfileEnum.VS_5_0:
                    return "vs_5_0";

                case MyShaderProfileEnum.PS_5_0:
                    return "ps_5_0";

                case MyShaderProfileEnum.GS_5_0:
                    return "gs_5_0";

                case MyShaderProfileEnum.CS_5_0:
                    return "cs_5_0";
            }

            return "";
        }
    }


//    abstract class MyShaderProvider
//    {
//        #region Fields
//        OnCompileCallbackType m_onCompileDelegate;
//        protected byte[] m_bytecode;
//        #endregion

//        static MyShaderProvider()
//        {
//            Directory.CreateDirectory(Path.Combine(MyFileSystem.UserDataPath, "ShaderCache"));
//        }

//        internal abstract void Compile();

//        internal byte[] GetBytecode()
//        {
//            return m_bytecode;
//        }

//        protected void OnCompileSuccess()
//        {
//            if (m_onCompileDelegate != null)
//            {
//                m_onCompileDelegate(m_bytecode);
//            }
//        }

//        internal void AddCallback(OnCompileCallbackType callback)
//        {
//            if(m_onCompileDelegate == null)
//            {
//                m_onCompileDelegate = callback;
//            }
//            else
//            {
//                m_onCompileDelegate += callback;
//            }
//        }

//        static MD5 m_md5 = System.Security.Cryptography.MD5.Create(); 

//        static string CalculateCacheKey(string src, string func, string profile)
//        {
//            StringBuilder builder = new StringBuilder();
//            builder.Append(func);
//            builder.Append(profile);
//#if DEBUG
//            builder.Append("$DEBUG");
//#endif
//            try
//            {
//                var includes = new MyIncludeProcessor(Path.Combine(MyFileSystem.ContentPath, "Shaders"));
//                builder.Append(ShaderBytecode.Preprocess(src, null, includes));

//                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(builder.ToString());
//                byte[] hash = m_md5.ComputeHash(inputBytes);

//                builder.Clear();

//                for (int i = 0; i < hash.Length; i++)
//                {
//                    builder.Append(hash[i].ToString("X2"));
//                }
//                return builder.ToString();
//            }
//            catch (CompilationException e)
//            {
//                return null;
//            }

//            return null;
//        }

//        static byte[] TryCatcheFetch(string key)
//        {
//            if (key == null)
//                return null;

//            var filename = Path.Combine(MyFileSystem.UserDataPath, "ShaderCache", Path.GetFileName(key + ".cache"));
//            if (File.Exists(filename))
//            {
//                return System.IO.File.ReadAllBytes(filename);
//            }

//            return null;
//        }

//        static void StoreInCache(string key, byte[] value)
//        {
//            if (key == null)
//                return;

//            using (var writer = new BinaryWriter(File.OpenWrite(Path.Combine(MyFileSystem.UserDataPath, "ShaderCache", Path.GetFileName(key + ".cache")))))
//            {
//                writer.Write(value);
//            }
//        }

//        internal static byte[] CompileInternal(string src, string func, string profile, string name)
//        {
//            string errors;

//            var dumpResults = MyShaderFactory.DUMP_CODE;

//            var key = CalculateCacheKey(src, func, profile);
//            var cached = TryCatcheFetch(key);
//            if (cached != null)
//            {
//                return cached;
//            }

//            try
//            {
//                var includes = new MyIncludeProcessor(Path.Combine(MyFileSystem.ContentPath, "Shaders"));

//#if DEBUG
//                var compilationResult = ShaderBytecode.Compile(src, func, profile, 0, 0, null, includes, name);
//#else
//                var compilationResult = ShaderBytecode.Compile(src, func, profile, ShaderFlags.OptimizationLevel3, 0, null, includes, name);
//#endif

//                if(dumpResults)
//                {
//                    var disassembly = compilationResult.Bytecode.Disassemble(DisassemblyFlags.EnableColorCode |
//                                                                       DisassemblyFlags.EnableInstructionNumbering);

//#if DEBUG
//                    string asmPath = Path.GetFileName(name + "__DEBUG.html");
//#else
//                    string asmPath = Path.GetFileName(name + "__O3.html");
//#endif

//                    using (var writer = new StreamWriter(Path.Combine(MyFileSystem.ContentPath, "ShaderOutput", asmPath)))
//                    {
//                        writer.Write(disassembly);
//                    }
//                }

//                if(compilationResult.Bytecode.Data.Length > 0)
//                {
//                    StoreInCache(key, compilationResult.Bytecode.Data);
//                }

//                return compilationResult.Bytecode.Data;
//            }
//            catch (CompilationException e)
//            {
//                MyRender11.Log.WriteLine(String.Format("Compilation of shader {0} failed: {1}", name, e));
//            }
//            catch (System.IO.FileNotFoundException)
//            {
//                throw;
//            }

//            return null;
//        }
//    }

//    class FileShaderProvier : MyShaderProvider
//    {
//        internal string m_name;
//        internal string m_file;
//        internal string m_macros;

//        internal string m_function;
//        internal string m_profile;

//        internal FileShaderProvier(string name, string file, string function, string profile, string macros)
//        {
//            m_name = name;
//            m_file = file;
//            m_macros = macros != null ? macros : "";
//            m_function = function;
//            m_profile = profile;
//        }

//        string GetSource()
//        {
//            using (var reader = new StreamReader(Path.Combine(MyFileSystem.ContentPath, "Shaders", m_file)))
//            {
//                return m_macros + reader.ReadToEnd();
//            }
//        }

//        internal override void Compile()
//        {
//            var compiled = CompileInternal(GetSource(), m_function, m_profile, m_name);

//            if(compiled != null)
//            {
//                m_bytecode = compiled;
//                OnCompileSuccess();
//            }
//        }
//    }

//    abstract class MyShaderBase
//    {
//        internal abstract void SetBytecode(byte[] bytecode);
//    }

//    class MyComputeShader : MyShaderBase
//    {
//        ComputeShader m_shader;

//        public static implicit operator ComputeShader(MyComputeShader v)
//        {
//            return v.m_shader;
//        }

//        internal override void SetBytecode(byte[] bytecode)
//        {
//            if (m_shader != null)
//                m_shader.Dispose();
//            m_shader = new ComputeShader(MyRender11.Device, bytecode);
//        }
//    }

//    class MyPixelShader : MyShaderBase
//    {
//        PixelShader m_shader;

//        public static implicit operator PixelShader(MyPixelShader v)
//        {
//            return v.m_shader;
//        }

//        internal override void SetBytecode(byte[] bytecode)
//        {
//            if (m_shader != null)
//                m_shader.Dispose();
//            m_shader = new PixelShader(MyRender11.Device, bytecode);
//        }
//    }

//    class MyVertexShader : MyShaderBase
//    {
//        VertexShader m_shader;

//        public static implicit operator VertexShader(MyVertexShader v)
//        {
//            return v.m_shader;
//        }

//        internal override void SetBytecode(byte[] bytecode)
//        {
//            if (m_shader != null)
//                m_shader.Dispose();
//            m_shader = new VertexShader(MyRender11.Device, bytecode);
//        }
//    }

//    class MyGeometryShader : MyShaderBase
//    {
//        GeometryShader m_shader;

//        public static implicit operator GeometryShader(MyGeometryShader v)
//        {
//            return v.m_shader;
//        }

//        internal override void SetBytecode(byte[] bytecode)
//        {
//            if (m_shader != null)
//                m_shader.Dispose();
//            m_shader = new GeometryShader(MyRender11.Device, bytecode);
//        }
//    }

//    class MyGeometryShaderWithSO : MyShaderBase
//    {
//        GeometryShader m_shader;

//        StreamOutputElement[] m_elements;
//        int[] m_bufferedStrides;
//        int m_rasterizedStream;

//        internal MyGeometryShaderWithSO(StreamOutputElement[] elements, int[] bufferedStrides, int rasterizedStream)
//        {
//            m_elements = elements;
//            m_bufferedStrides = bufferedStrides;
//            m_rasterizedStream = rasterizedStream;
//        }

//        public static implicit operator GeometryShader(MyGeometryShaderWithSO v)
//        {
//            return v.m_shader;
//        }

//        internal override void SetBytecode(byte[] bytecode)
//        {
//            if (m_shader != null)
//                m_shader.Dispose();
//            m_shader = new GeometryShader(MyRender11.Device, bytecode, m_elements, m_bufferedStrides, m_rasterizedStream);
//        }
//    }

    static class MyShaderHelpers
    {
        static ThreadLocal<StringBuilder> m_strB = new ThreadLocal<StringBuilder>(() => new StringBuilder());

        internal static string FormatMacros(params string[] macros)
        {
            m_strB.Value.Clear();
            Array.Sort(macros);

            foreach (var macro in macros)
            {
                if (macro != null)
                {
                    m_strB.Value.AppendFormat("#define {0}{1}", macro, System.Environment.NewLine);
                }
            }

            return m_strB.Value.ToString();
        }
    }
    
    static class MyShaderFactory
    {
        internal const bool DUMP_CODE = false;//MyRender11.DebugMode;

//        // make thread safe
//        static Dictionary<int, MyShaderProvider> m_cached = new Dictionary<int, MyShaderProvider>();
//        static Dictionary<MyShaderProvider, MyShaderBase> m_shaders = new Dictionary<MyShaderProvider, MyShaderBase>();
//        static Queue<MyShaderProvider> m_compilationQueue = new Queue<MyShaderProvider>();

//        internal static MyComputeShader CreateCS(string file, string func, string macros)
//        {
//            var key = MyHashHelper.Combine(file.GetHashCode(), func.GetHashCode());
//            if(macros != null)
//                key = MyHashHelper.Combine(key, macros.GetHashCode());

//            var provider = m_cached.SetDefault(key);
//            if(provider == null)
//            {
//                string name = String.Format("cs_[{0}][{1}]{2}", file, func, macros != null ? macros.Replace(Environment.NewLine, "_") : "");
//                provider = new FileShaderProvier(name, file, func, "cs_5_0", macros);

//                m_cached[key] = provider;
//                m_shaders[provider] = new MyComputeShader();
//                m_compilationQueue.Enqueue(provider);
//            }

//            return m_shaders[provider] as MyComputeShader;
//        }

//        internal static MyPixelShader CreatePS(string file, string func, string macros)
//        {
//            var key = MyHashHelper.Combine(file.GetHashCode(), func.GetHashCode());
//            if(macros != null)
//                key = MyHashHelper.Combine(key, macros.GetHashCode());

//            var provider = m_cached.SetDefault(key);
//            if(provider == null)
//            {
//                string name = String.Format("ps_[{0}][{1}]{2}", file, func, macros != null ? macros.Replace(Environment.NewLine, "_") : "");
//                provider = new FileShaderProvier(name, file, func, "ps_5_0", macros);

//                m_cached[key] = provider;
//                m_shaders[provider] = new MyPixelShader();
//                m_compilationQueue.Enqueue(provider);
//            }

//            return m_shaders[provider] as MyPixelShader;
//        }

//        internal static MyVertexShader CreateVS(string file, string func, string macros,
//            OnCompileCallbackType callback)
//        {
//            var key = MyHashHelper.Combine(file.GetHashCode(), func.GetHashCode());
//            if (macros != null)
//                key = MyHashHelper.Combine(key, macros.GetHashCode());

//            var provider = m_cached.SetDefault(key);
//            if (provider == null)
//            {
//                string name = String.Format("vs_[{0}][{1}]{2}", file, func, macros != null ? macros.Replace(Environment.NewLine, "_") : "");
//                provider = new FileShaderProvier(name, file, func, "vs_5_0", macros);

//                m_cached[key] = provider;
//                m_shaders[provider] = new MyVertexShader();
//                m_compilationQueue.Enqueue(provider);
//            }
//            if(callback != null)
//            {
//                provider.AddCallback(callback);
//            }

//            return m_shaders[provider] as MyVertexShader;
//        }

//        internal static MyGeometryShader CreateGS(string file, string func, string macros)
//        {
//            var key = MyHashHelper.Combine(file.GetHashCode(), func.GetHashCode());
//            if (macros != null)
//                key = MyHashHelper.Combine(key, macros.GetHashCode());

//            var provider = m_cached.SetDefault(key);
//            if (provider == null)
//            {
//                string name = String.Format("gs_[{0}][{1}]{2}", file, func, macros != null ? macros.Replace(Environment.NewLine, "_") : "");
//                provider = new FileShaderProvier(name, file, func, "gs_5_0", macros);

//                m_cached[key] = provider;
//                m_shaders[provider] = new MyGeometryShader();
//                m_compilationQueue.Enqueue(provider);
//            }

//            return m_shaders[provider] as MyGeometryShader;
//        }

//        internal static MyGeometryShaderWithSO CreateGSWithSO(string file, string func, string macros, StreamOutputElement[] elements = null, int[] bufferedStrides = null, int rasterizedStream = 0)
//        {
//            var key = MyHashHelper.Combine(file.GetHashCode(), func.GetHashCode());
//            if (macros != null)
//                key = MyHashHelper.Combine(key, macros.GetHashCode());

//            var provider = m_cached.SetDefault(key);
//            if (provider == null)
//            {
//                string name = String.Format("gs_[{0}][{1}]{2}", file, func, macros != null ? macros.Replace(Environment.NewLine, "_") : "");
//                provider = new FileShaderProvier(name, file, func, "gs_5_0", macros);

//                m_cached[key] = provider;
//                m_shaders[provider] = new MyGeometryShaderWithSO(elements, bufferedStrides, rasterizedStream);
//                m_compilationQueue.Enqueue(provider);
//            }

//            return m_shaders[provider] as MyGeometryShaderWithSO;
//        }

//        internal static void RecompileAll()
//        {
//            m_compilationQueue.Clear();

//            foreach (var p in m_cached.Values)
//                m_compilationQueue.Enqueue(p);
//        }

//        internal static void RunCompilation()
//        {
//            if (MyRender11.ShaderRebuildFlag)
//            {
//                RecompileAll();
//            }

//            foreach(var k in m_compilationQueue)
//            {
//                k.Compile();
//            }
//            foreach (var k in m_compilationQueue)
//            {
//                var bytecode = k.GetBytecode();
//                if(bytecode != null)
//                {
//                    try { 
//                        m_shaders[k].SetBytecode(bytecode);
//                    }
//                    catch (SharpDXException e)
//                    {
//                        throw new MyRenderException("GPU failed to create shader", MyRenderExceptionEnum.GpuNotSupported);
//                    }
//                }
//            }

//            if (m_compilationQueue.Count > 0) 
//            {
//                Debug.WriteLine(String.Format("Finished processing compilation queue for {0} shaders", m_compilationQueue.Count));
//            }
//            m_compilationQueue.Clear();
//        }
    }


    class MyIncludeProcessor : Include
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
                return Stream.Null;
            }
            
        }

        public void Dispose()
        {
        }

        public IDisposable Shadow { get; set; }
    }

    internal delegate void OnCompileCallbackType(byte[] bytecode);






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

    struct MyShaderCompilationInfo
    {
        internal MyStringId File;
        internal MyStringId Function;
        internal MyShaderProfileEnum Profile;
        internal string Header;
        internal string Name;
    }

    struct MyShaderBytecode
    {
        internal byte[] Bytecode;
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

    struct InputLayoutInfo
    {
        internal ShaderBytecodeId BytecodeId;
        internal VertexLayoutId VLayoutId;
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

    struct PixelShaderId
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
    }

    struct MyShaderStreamOutputInfo
    {
        internal StreamOutputElement[] Elements;
        internal int[] Strides;
        internal int RasterizerStreams;
    }

    static class MyShaders
    {
        internal const string ShadersContentPath = "Shaders";
        internal const string UserCachePath = "ShaderCache";
        static MD5 m_md5 = System.Security.Cryptography.MD5.Create(); 

        static MyShaders()
        {
            Directory.CreateDirectory(Path.Combine(MyFileSystem.UserDataPath, UserCachePath));
        }

        internal static void Init()
        {

        }

        static MyFreelist<MyShaderBytecode> Bytecodes = new MyFreelist<MyShaderBytecode>(512);
        static Dictionary<ShaderBytecodeId, MyShaderCompilationInfo> Shaders = new Dictionary<ShaderBytecodeId, MyShaderCompilationInfo>();

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

        static string CalculateCacheKey(string source, string function, string profile)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(function);
            builder.Append(profile);
            if(MyRender11.DebugMode)
            {
                builder.Append("$DEBUG");
            }
            try
            {
                var includes = new MyIncludeProcessor(Path.Combine(MyFileSystem.ContentPath, ShadersContentPath));
                builder.Append(ShaderBytecode.Preprocess(source, null, includes));


                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(builder.ToString());
                byte[] hash = m_md5.ComputeHash(inputBytes);

                builder.Clear();

                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("X2"));
                }
                return builder.ToString();
            }
            catch (CompilationException e)
            {
                return null;
            }

            return null;
        }

        static byte[] TryCatcheFetch(string key)
        {
            if (key == null)
                return null;

            var filename = Path.Combine(MyFileSystem.UserDataPath, UserCachePath, Path.GetFileName(key + ".cache"));
            if (File.Exists(filename))
            {
                return System.IO.File.ReadAllBytes(filename);
            }

            return null;
        }

        static void StoreInCache(string key, byte[] value)
        {
            if (key == null)
                return;

            using (var writer = new BinaryWriter(File.OpenWrite(Path.Combine(MyFileSystem.UserDataPath, UserCachePath, Path.GetFileName(key + ".cache")))))
            {
                writer.Write(value);
            }
        }

        internal static byte[] Compile(string source, string function, string profile, string name, bool invalidateCache)
        {
            string errors;

            var dumpResults = MyShaderFactory.DUMP_CODE;

            var key = CalculateCacheKey(source, function, profile);
            if(!invalidateCache)
            {
                var cached = TryCatcheFetch(key);
                if (cached != null)
                {
                    return cached;
                }
            }

            try
            {
                var includes = new MyIncludeProcessor(Path.Combine(MyFileSystem.ContentPath, ShadersContentPath));

                CompilationResult compilationResult;

                if(MyRender11.DebugMode)
                {
                    compilationResult = ShaderBytecode.Compile(source, function, profile, 0, 0, null, includes, name);
                }
                else
                {
                    compilationResult = ShaderBytecode.Compile(source, function, profile, ShaderFlags.OptimizationLevel3, 0, null, includes, name);
                }

                if (dumpResults)
                {
                    var disassembly = compilationResult.Bytecode.Disassemble(DisassemblyFlags.EnableColorCode |
                                                                       DisassemblyFlags.EnableInstructionNumbering);
                    string asmPath;
                    if(MyRender11.DebugMode)
                    {
                        asmPath = Path.GetFileName(name + "__DEBUG.html");
                    }
                    else
                    {
                        asmPath = Path.GetFileName(name + "__O3.html");
                    }

                    using (var writer = new StreamWriter(Path.Combine(MyFileSystem.ContentPath, "ShaderOutput", asmPath)))
                    {
                        writer.Write(disassembly);
                    }
                }

                if(compilationResult.Message != null)
                {
                    Debug.WriteLine(String.Format("Compilation of shader {0}: {1}", name, compilationResult.Message));
                    ExtendedErrorMessage(source, compilationResult.Message);
                }

                if (compilationResult.Bytecode.Data.Length > 0)
                {
                    StoreInCache(key.ToString(), compilationResult.Bytecode.Data);
                }

                return compilationResult.Bytecode.Data;
            }
            catch (CompilationException e)
            {
                MyRender11.Log.WriteLine(String.Format("Compilation of shader {0} failed: {1}", name, e));

                Debug.WriteLine(String.Format("Compilation of shader {0} failed", name));
                Debug.WriteLine(e);
                ExtendedErrorMessage(source, e.Message);
            }
            catch (System.IO.FileNotFoundException)
            {
                throw;
            }

            return null;
        }

        static void ExtendedErrorMessage(string code, string errorMsg )
        {
            Regex rx = new Regex(@"\((?<lineNo>\d+),\d+\):",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var match = rx.Match(errorMsg);

            var lineStr = match.Groups["lineNo"].Value;
            if (lineStr != "")
            {
                var line = Int32.Parse(lineStr) - 1;
                var sourceLines = code.Split(new [] { "\r\n" }, StringSplitOptions.None);

                for (int i = -2; i <= 2; i++)
                {
                    var offseted = line + i;
                    if (0 <= offseted && offseted < sourceLines.Count())
                    {
                        Debug.WriteLine(String.Format("{0}: {1}", offseted + 1, sourceLines[offseted]));
                    }
                }
            }
        }

        #endregion

        static ShaderBytecodeId CreateBytecode()
        { 
            var id = new ShaderBytecodeId { Index = Bytecodes.Allocate() };
            return id;
        }

        internal static VertexShaderId CreateVs(string file, string func, string header = "")
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
                File = X.TEXT(file),
                Function = X.TEXT(func),
                Profile = MyShaderProfileEnum.VS_5_0,
                Header = header,
                Name = String.Format("vs_[{0}][{1}]{2}", file, func, header != null ? header.Replace(Environment.NewLine, "_") : "")
            };

            VsObjects[id.Index] = null;

            InitVs(id);
            VsIndex.Add(id);

            return id;
        }

        internal static PixelShaderId CreatePs(string file, string func, string header = "")
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
                File = X.TEXT(file),
                Function = X.TEXT(func),
                Profile = MyShaderProfileEnum.PS_5_0,
                Header = header,
                Name = String.Format("ps_[{0}][{1}]{2}", file, func, header != null ? header.Replace(Environment.NewLine, "_") : "")
            };

            PsObjects[id.Index] = null;

            InitPs(id);
            PsIndex.Add(id);

            return id;
        }

        internal static ComputeShaderId CreateCs(string file, string func, string header = "")
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
                File = X.TEXT(file),
                Function = X.TEXT(func),
                Profile = MyShaderProfileEnum.CS_5_0,
                Header = header,
                Name = String.Format("cs_[{0}][{1}]{2}", file, func, header != null ? header.Replace(Environment.NewLine, "_") : "")
            };

            CsObjects[id.Index] = null;

            InitCs(id);
            CsIndex.Add(id);

            return id;
        }

        internal static GeometryShaderId CreateGs(string file, string func, string header = "", MyShaderStreamOutputInfo ? streamOut = null)
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
                File = X.TEXT(file),
                Function = X.TEXT(func),
                Profile = MyShaderProfileEnum.GS_5_0,
                Header = header,
                Name = String.Format("gs_[{0}][{1}]{2}", file, func, header != null ? header.Replace(Environment.NewLine, "_") : "")
            };

            GsObjects[id.Index] = null;

            if(streamOut.HasValue)
            {
                StreamOutputs[id] = streamOut.Value;
            }

            InitGs(id);
            GsIndex.Add(id);

            return id;
        }

        internal static void Compile(ShaderBytecodeId bytecode, bool invalidateCache = false)
        {
            var info = Shaders[bytecode];

            using (var reader = new StreamReader(Path.Combine(MyFileSystem.ContentPath, ShadersContentPath, info.File.ToString())))
            {
                var compiled = Compile(MyRender11.GlobalShaderHeader + info.Header + reader.ReadToEnd(), info.Function.ToString(), info.Profile.Value(), info.Name, invalidateCache);
                Bytecodes.Data[bytecode.Index].Bytecode = compiled != null ? compiled : Bytecodes.Data[bytecode.Index].Bytecode; 

                if(Bytecodes.Data[bytecode.Index].Bytecode == null)
                {
                    MyRender11.Log.WriteLine("Failed to compile shader" + info.Name);
                    throw new MyRenderException("Failed to compile shader" + info.Name, MyRenderExceptionEnum.Unassigned);
                }
            }
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

        internal static void InitVs(VertexShaderId id)
        {
            var bytecodeId = VertexShaders.Data[id.Index].Bytecode;
            Compile(bytecodeId);

            if(VsObjects[id.Index] != null)
            {
                VsObjects[id.Index].Dispose();
                VsObjects[id.Index] = null;
            }

            try
            {
                VsObjects[id.Index] = new VertexShader(MyRender11.Device, GetBytecode(bytecodeId));
            }
            catch(SharpDXException e)
            {
                Compile(bytecodeId, true);
                VsObjects[id.Index] = new VertexShader(MyRender11.Device, GetBytecode(bytecodeId));
            }
        }

        internal static void InitPs(PixelShaderId id)
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
        }

        internal static void InitCs(ComputeShaderId id)
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
        }

        internal static void InitGs(GeometryShaderId id)
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
                if(StreamOutputs.ContainsKey(id))
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

            if(ILObjects[id.Index] != null)
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
                InitVs(id);
            }

            foreach (var id in PsIndex)
            {
                InitPs(id);
            }

            foreach (var id in CsIndex)
            {
                InitCs(id);
            }

            foreach (var id in GsIndex)
            {
                InitGs(id);
            }

            foreach (var id in ILIndex)
            {
                InitIL(id);
            }
        }

        internal static void OnDeviceEnd()
        {
            foreach(var id in VsIndex)
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
    }
}
