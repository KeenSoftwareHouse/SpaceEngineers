using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Xml.Serialization;

using SharpDX.Direct3D;

using VRage.FileSystem;
using VRage.Utils;
using VRageRender;
using VRage.Library.Utils;

namespace VRage.Render11.Shader
{
    internal class MyShaderCacheGenerator
    {
        private const string CacheGeneratorFile = "CacheGenerator.xml";

        private const string ANNOTATION_DEFINE = "define";
        private const string ANNOTATION_DEFINE_MANDATORY = "defineMandatory";
        private const string ANNOTATION_SKIP = "@skipCache";

        internal static void Generate(bool clean, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            // remove contents of the whole cache folder
            var outputPath = Path.Combine(MyFileSystem.UserDataPath, MyShadersDefines.CachePath);
            Directory.CreateDirectory(outputPath);
            if (clean)
            {
#if XB1
                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
                string[] cacheFiles = Directory.GetFiles(outputPath, "*.cache");
                for (int i = 0; i < cacheFiles.Length; i++)
                    File.Delete(cacheFiles[i]);
#endif // !XB1
            }

            string filename = null;
            CacheGenerator generatorDesc = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(CacheGenerator));
                filename = Path.Combine(MyShaders.ShadersPath, CacheGeneratorFile);
                TextReader reader = new StreamReader(filename);
                generatorDesc = serializer.Deserialize(reader) as CacheGenerator;
            }
            catch (Exception ex)
            {
                throw new FileLoadException("File " + filename + " not found or invalid: ", ex);
            }
            if (generatorDesc == null)
                throw new FileLoadException("File " + filename + " not found or invalid: ");

            GenerateInternal(generatorDesc, onShaderCacheProgress);
            GenerateMaterials(generatorDesc, onShaderCacheProgress);
        }

        private static bool CheckAnnotation(string source, int idx, string define)
        {
            var sub = source.Substring(idx, define.Length);
            return sub == define;
        }

        private static void GenerateInternal(CacheGenerator generatorDesc, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            var shaderPath = Path.Combine(MyShaders.ShadersPath);
            string[] files = GetShadersRecursively(shaderPath, generatorDesc.Ignores);
            var macroVariants = new List<string[]>();
            var macroVariantMandatory = new List<bool>();
            for (int i = 0; i < files.Length; i++)
            {
                float progress = (float) i*100/files.Length;
                string file = files[i];
                string fileText = file.Remove(0, shaderPath.Length);
                using (var reader = new StreamReader(file))
                {
                    var source = reader.ReadToEnd();
                    bool any = false;
                    if (source.IndexOf(ANNOTATION_SKIP) != -1)
                        continue;

                    for (MyShaderProfile profile = 0; profile < MyShaderProfile.count; profile++)
                    {
                        string function = MyShaders.ProfileEntryPoint(profile);
                        if (source.Contains(function))
                        {
                            // generate list of macro variants
                            macroVariants.Clear();
                            macroVariantMandatory.Clear();

                            // global ones
                            macroVariants.Add(new[] {"MS_SAMPLE_COUNT 2", "MS_SAMPLE_COUNT 4", "MS_SAMPLE_COUNT 8", "FXAA_ENABLED"});
                            macroVariantMandatory.Add(false);

                            // shader specific ones
                            int defineEndIndex = 0;
                            while (true)
                            {
                                int defineIndex = source.IndexOf('@', defineEndIndex);
                                if (defineIndex == -1)
                                    break;
                                int advance = -1;
                                bool mandatory = false;
                                defineIndex++;
                                if (CheckAnnotation(source, defineIndex, ANNOTATION_DEFINE_MANDATORY))
                                {
                                    advance = ANNOTATION_DEFINE_MANDATORY.Length;
                                    mandatory = true;
                                }
                                else if (CheckAnnotation(source, defineIndex, ANNOTATION_DEFINE))
                                    advance = ANNOTATION_DEFINE.Length;
                                if (advance == -1)
                                {
                                    defineEndIndex++;
                                    continue;
                                }
                                defineIndex += advance;
                                defineEndIndex = source.IndexOf("\n", defineIndex);
                                if (defineEndIndex == -1)
                                    defineEndIndex = source.Length;
                                var define = source.Substring(defineIndex, defineEndIndex - defineIndex);
                                define = define.Trim();
                                var defineEntries = define.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                                macroVariants.Add(defineEntries);
                                macroVariantMandatory.Add(mandatory);
                            }

                            // init macro counters based on mandatory flag
                            var counters = new int[macroVariants.Count];
                            for (int j = 0; j < macroVariants.Count; j++)
                                counters[j] = macroVariantMandatory[j] ? 1 : 0;

                            // compile all variants
                            bool finished = false;
                            while (!finished)
                            {
                                // prepare macros
                                var macros = new List<ShaderMacro>();
                                for (int j = 0; j < macroVariants.Count; j++)
                                {
                                    if (counters[j] > 0)
                                    {
                                        var define = macroVariants[j][counters[j] - 1];
                                        var defineSplit = define.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
                                        MyDebug.AssertDebug(defineSplit.Length == 1 || defineSplit.Length == 2, "Invalid define @ " + fileText + ": " + define);
                                        if (defineSplit.Length > 1)
                                            macros.Add(new ShaderMacro(defineSplit[0].Trim(), defineSplit[1].Trim()));
                                        else macros.Add(new ShaderMacro(defineSplit[0].Trim(), null));
                                    }
                                }

                                // compile
                                PreCompile(file, macros, profile, fileText, "", progress, onShaderCacheProgress);
                                any = true;

                                // increase variants counters
                                int countersIndex = counters.Length - 1;
                                do
                                {
                                    counters[countersIndex]++;
                                    if (counters[countersIndex] > macroVariants[countersIndex].Length)
                                    {
                                        counters[countersIndex] = macroVariantMandatory[countersIndex] ? 1 : 0;
                                        if (countersIndex == 0)
                                            finished = true;
                                        countersIndex--;
                                    }
                                    else break;
                                } while (countersIndex >= 0);
                            }
                        }
                    }
                    if (!any && onShaderCacheProgress != null)
                        onShaderCacheProgress(progress, file, "", "", "", "No entry point found.", true);
                }
            }
#endif // !XB1
        }

        private static ShaderMacro[] m_globalMacros = new[] {new ShaderMacro("MS_SAMPLE_COUNT", 2), new ShaderMacro("MS_SAMPLE_COUNT", 4), new ShaderMacro("MS_SAMPLE_COUNT", 8)};//, new ShaderMacro("FXAA_ENABLED",null)};

        private static void GenerateMaterials(CacheGenerator generatorDesc, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            int combinationsCount = generatorDesc.Materials.Length * generatorDesc.Passes.Length * generatorDesc.Combos.Length;
            int combinationsCounter = 0;
            for (int i = 0; i < generatorDesc.Materials.Length; i++)
            {
                var materialId = MyStringId.GetOrCompute(generatorDesc.Materials[i].Id);
                var materialFlags = ParseFlags(generatorDesc.Materials[i].FlagNames);
                var unsupportedMaterialFlags = ParseFlags(generatorDesc.Materials[i].UnsupportedFlagNames);
                for (int j = 0; j < generatorDesc.Passes.Length; j++)
                {
                    var passId = MyStringId.GetOrCompute(generatorDesc.Passes[j].Id);
                    var passFlags = ParseFlags(generatorDesc.Passes[j].FlagNames);
                    var unsupportedPassFlags = ParseFlags(generatorDesc.Passes[j].UnsupportedFlagNames);
                    for (int c = 0; c < generatorDesc.Combos.Length; c++)
                    {
                        combinationsCounter++;
                        float progress = (float) combinationsCounter*100/combinationsCount;
                        if (string.IsNullOrEmpty(generatorDesc.Combos[c].Material) && string.IsNullOrEmpty(generatorDesc.Combos[c].Pass))
                            GenerateCombo(materialId, passId, materialFlags | passFlags, unsupportedMaterialFlags | unsupportedPassFlags, generatorDesc.Combos[c].ComboList1, generatorDesc.Combos[c].ComboList2, progress, onShaderCacheProgress);
                    }
                }
            }
            for (int c = 0; c < generatorDesc.Combos.Length; c++)
            {
                if (!string.IsNullOrEmpty(generatorDesc.Combos[c].Material) && !string.IsNullOrEmpty(generatorDesc.Combos[c].Pass))
                {
                    combinationsCounter++;
                    float progress = (float)combinationsCounter * 100 / combinationsCount;
                    var materialId = MyStringId.GetOrCompute(generatorDesc.Combos[c].Material);
                    var passId = MyStringId.GetOrCompute(generatorDesc.Combos[c].Pass);
                    GenerateCombo(materialId, passId, MyShaderUnifiedFlags.NONE, MyShaderUnifiedFlags.NONE, generatorDesc.Combos[c].ComboList1, generatorDesc.Combos[c].ComboList2, progress, onShaderCacheProgress);
                }
            }
        }

        private static void GenerateCombo(MyStringId materialId, MyStringId passId, MyShaderUnifiedFlags additionalFlags, MyShaderUnifiedFlags unsupportedFlags, CacheGenerator.Combo[] comboList1, CacheGenerator.Combo[] comboList2, 
            float progress, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            if (comboList1 == null || comboList1.Length == 0)
                comboList1 = new[] { new CacheGenerator.Combo() };
            if (comboList2 == null || comboList2.Length == 0)
                comboList2 = new[] { new CacheGenerator.Combo() };
            for (int k = 0; k < comboList1.Length; k++)
            {
                MyVertexInputComponentType[] vertexInput1 = comboList1[k].VertexInput;
                if (vertexInput1 == null)
                    vertexInput1 = new MyVertexInputComponentType[0];
                int[] vertexInputOrder1 = comboList1[k].VertexInputOrder;
                if (vertexInputOrder1 == null || vertexInputOrder1.Length != vertexInput1.Length)
                {
                    vertexInputOrder1 = new int[vertexInput1.Length];
                    for (int x = 0; x < vertexInput1.Length; x++)
                        vertexInputOrder1[x] = x;
                }
                MyShaderUnifiedFlags flags1 = ParseFlags(comboList1[k].FlagNames);
                if ((flags1 & unsupportedFlags) != 0)
                    continue;

                // go through all combinations of shader flags
                for (int l = 0; l < comboList2.Length; l++)
                {
                    MyVertexInputComponentType[] vertexInput2 = comboList2[l].VertexInput;
                    if (vertexInput2 == null)
                        vertexInput2 = new MyVertexInputComponentType[0];
                    int[] vertexInputOrder2 = comboList2[l].VertexInputOrder;
                    if (vertexInputOrder2 == null || vertexInputOrder2.Length != vertexInput2.Length)
                    {
                        vertexInputOrder2 = new int[vertexInput2.Length];
                        for (int x = 0; x < vertexInput2.Length; x++)
                            vertexInputOrder2[x] = x;
                    }
                    var vertexInput = vertexInput1.Concat(vertexInput2).ToArray();
                    var vertexInputOrder = vertexInputOrder1.Concat(vertexInputOrder2).ToArray();
                    Array.Sort(vertexInputOrder, vertexInput, m_intComparer);
                    VertexLayoutId vertexLayout;
                    if (vertexInput.Length != 0)
                        vertexLayout = MyVertexLayouts.GetLayout(vertexInput);
                    else vertexLayout = MyVertexLayouts.Empty;

                    MyShaderUnifiedFlags flags = ParseFlags(comboList2[l].FlagNames) | flags1 | additionalFlags;
                    var macros = new List<ShaderMacro>();
                    macros.Add(MyMaterialShaders.GetRenderingPassMacro(passId.String));
                    MyMaterialShaders.AddMaterialShaderFlagMacrosTo(macros, flags);
                    macros.AddRange(vertexLayout.Info.Macros);

                    // return errors & skipped info
                    MyMaterialShaderInfo sources;
                    MyMaterialShaders.GetMaterialSources(materialId, out sources);

                    if ((flags & unsupportedFlags) != 0)
                        continue;

                    var vertexLayoutString = vertexLayout.Info.Components.GetString();

                    string vsDescriptor = MyMaterialShaders.GetShaderDescriptor(sources.VertexShaderFilename, materialId.String, passId.String, vertexLayout);
                    PreCompile(sources.VertexShaderFilepath, macros, MyShaderProfile.vs_5_0, vsDescriptor, vertexLayoutString, progress, onShaderCacheProgress);

                    string psDescriptor = MyMaterialShaders.GetShaderDescriptor(sources.PixelShaderFilename, materialId.String, passId.String, vertexLayout);
                    PreCompile(sources.PixelShaderFilepath, macros, MyShaderProfile.ps_5_0, psDescriptor, vertexLayoutString, progress, onShaderCacheProgress);
                    macros.Add(m_globalMacros[0]);
                    for (int m = 0; m < m_globalMacros.Length; m++)
                    {
                        macros[macros.Count - 1] = m_globalMacros[m];
                        //PreCompile(sources.VertexShaderFilepath, macros, MyShadersDefines.Profiles.vs_5_0, vsDescriptor, vertexLayoutString, progress, onShaderCacheProgress);
                        PreCompile(sources.PixelShaderFilepath, macros, MyShaderProfile.ps_5_0, psDescriptor, vertexLayoutString, progress, onShaderCacheProgress);
                    }
                }
            }
        }

        private static string[] GetShadersRecursively(string shaderPath, string[] ignoresConf)
        {
            List<string> ignores = new List<string>();
            foreach (var ignore in ignoresConf)
            {
                var uri = new System.Uri(Path.Combine(shaderPath, ignore));
                var localPath = uri.LocalPath;
                ignores.Add(localPath);
            }

            List<string> files = new List<string>();
            foreach (var file in PathUtils.GetFilesRecursively(shaderPath, "*.hlsl"))
            {
                bool ignored = false;
                foreach (var ignore in ignores)
                {
                    if (file.StartsWith(ignore))
                        ignored = true;
                }

                if (!ignored)
                    files.Add(file);
            }

            return files.ToArray();
        }

        private static void PreCompile(string filepath, List<ShaderMacro> macros, MyShaderProfile profile, string descriptor, string vertexLayoutString, float progress, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            var macrosArray = macros.ToArray();
            bool wasCached;
            string compileLog;
            if (onShaderCacheProgress != null)
                onShaderCacheProgress(progress, descriptor, MyShaders.ProfileToString(profile), vertexLayoutString, macrosArray.GetString(), "", false);
            var compiled = MyShaders.Compile(filepath, macrosArray, profile, descriptor, true, false, out wasCached, out compileLog);
            if (onShaderCacheProgress != null)
            {
                if (wasCached)
                    onShaderCacheProgress(progress, descriptor, MyShaders.ProfileToString(profile), vertexLayoutString, macrosArray.GetString(), "skipped", false);
                else if (compileLog != null)
                {
                    onShaderCacheProgress(progress, descriptor, MyShaders.ProfileToString(profile), vertexLayoutString, macrosArray.GetString(),
                        (compiled == null ? "errors:\n" : "warnings:\n") + compileLog, compiled == null);
                }
            }
        }

        private static MyShaderUnifiedFlags ParseFlags(string flagNames)
        {
            if (string.IsNullOrEmpty(flagNames) || flagNames.Trim() == "")
                return MyShaderUnifiedFlags.NONE;

            MyShaderUnifiedFlags flags = MyShaderUnifiedFlags.NONE, flag;
            var flagNameList = flagNames.Split('|');
            for (int i = 0; i < flagNameList.Length; i++)
            {
                if (MyShaderUnifiedFlags.TryParse(flagNameList[i], out flag))
                    flags |= flag;
                else MyDebug.AssertDebug(false, "Invalid flags enum: " + flagNameList[i]);
            }
            return flags;
        }

        public class IntComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                // Compare y and x in reverse order.
                return -y.CompareTo(x);
            }
        }
        private static readonly IntComparer m_intComparer = new IntComparer();
    }
    [Serializable]
    public class CacheGenerator
    {
        [XmlArrayItem("Ignore")]
        public string[] Ignores;
        public Material[] Materials;
        public Pass[] Passes;
        public ComboGroup[] Combos;

        [Serializable]
        public class ComboGroup
        {
            [XmlAttribute]
            public string Material;
            [XmlAttribute]
            public string Pass;

            public Combo[] ComboList1;
            public Combo[] ComboList2;
        }

        [Serializable]
        public class Material
        {
            [XmlAttribute]
            public string Id;
            [XmlAttribute]
            public string FlagNames;
            [XmlAttribute]
            public string UnsupportedFlagNames;
        }

        [Serializable]
        public class Pass
        {
            [XmlAttribute]
            public string Id;
            [XmlAttribute]
            public string FlagNames;
            [XmlAttribute]
            public string UnsupportedFlagNames;
        }

        [Serializable]
        public class Combo
        {
            [XmlAttribute]
            public MyVertexInputComponentType[] VertexInput;
            [XmlAttribute]
            public int[] VertexInputOrder;
            [XmlAttribute]
            public string FlagNames;
        }
    }
}
