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

namespace VRage.Render11.Shader
{
    internal class MyShaderCacheGenerator
    {
        private const string ANNOTATION_DEFINE = "define";
        private const string ANNOTATION_DEFINE_MANDATORY = "defineMandatory";
        private const string ANNOTATION_SKIP = "@skipCache";

        internal static void Generate(bool clean, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            // remove contents of the whole cache folder
            var outputPath = Path.Combine(MyFileSystem.UserDataPath, MyShadersDefines.CachePath);
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

            GenerateInternal(onShaderCacheProgress);

            GenerateMaterials(onShaderCacheProgress);
        }

        private static bool CheckAnnotation(string source, int idx, string define)
        {
            var sub = source.Substring(idx, define.Length);
            return sub == define;
        }

        private static void GenerateInternal(OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            var shaderPath = Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath);
            string[] files = Directory.GetFiles(shaderPath, "*.hlsl");
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

                    for (MyShadersDefines.Profiles profile = 0; profile < MyShadersDefines.Profiles.count; profile++)
                    {
                        string function = MyShadersDefines.ProfileEntryPoint(profile);
                        if (source.Contains(function))
                        {
                            // generate list of macro variants
                            macroVariants.Clear();
                            macroVariantMandatory.Clear();

                            // global ones
                            //macroVariants.Add(new[] {"DEBUG"});
                            //macroVariantMandatory.Add(false);
                            macroVariants.Add(new[] {"MS_SAMPLE_COUNT 2", "MS_SAMPLE_COUNT 4", "MS_SAMPLE_COUNT 8", "FXAA_ENABLED"});
                            macroVariantMandatory.Add(false);

                            // shader specific ones
                            int defineEndIndex = 0;
                            bool skip = false;
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

                            if (skip)
                                continue;

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
                                PreCompile(source, macros, profile, fileText, "", progress, onShaderCacheProgress);
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

        private static void GenerateMaterials(OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            string filename = null;
            MatCombos matDesc = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof (MatCombos));
                filename = Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath, MyShadersDefines.MaterialComboFile);
                TextReader reader = new StreamReader(filename);
                matDesc = serializer.Deserialize(reader) as MatCombos;
            }
            catch (Exception ex)
            {
                throw new FileLoadException("File " + filename + " not found or invalid: ", ex);
            }
            if (matDesc == null)
                throw new FileLoadException("File " + filename + " not found or invalid: ");

            int combinationsCount = matDesc.Materials.Length*matDesc.Passes.Length*matDesc.Combos.Length;
            int combinationsCounter = 0;
            for (int i = 0; i < matDesc.Materials.Length; i++)
            {
                var materialId = MyStringId.GetOrCompute(matDesc.Materials[i].Id);
                var materialFlags = ParseFlags(matDesc.Materials[i].FlagNames);
                var unsupportedMaterialFlags = ParseFlags(matDesc.Materials[i].UnsupportedFlagNames);
                for (int j = 0; j < matDesc.Passes.Length; j++)
                {
                    var passId = MyStringId.GetOrCompute(matDesc.Passes[j].Id);
                    var passFlags = ParseFlags(matDesc.Passes[j].FlagNames);
                    var unsupportedPassFlags = ParseFlags(matDesc.Passes[j].UnsupportedFlagNames);
                    for (int c = 0; c < matDesc.Combos.Length; c++)
                    {
                        combinationsCounter++;
                        float progress = (float) combinationsCounter*100/combinationsCount;
                        if (string.IsNullOrEmpty(matDesc.Combos[c].Material) && string.IsNullOrEmpty(matDesc.Combos[c].Pass))
                            GenerateCombo(materialId, passId, materialFlags | passFlags, unsupportedMaterialFlags | unsupportedPassFlags, matDesc.Combos[c].ComboList1, matDesc.Combos[c].ComboList2, progress, onShaderCacheProgress);
                    }
                }
            }
            for (int c = 0; c < matDesc.Combos.Length; c++)
            {
                if (!string.IsNullOrEmpty(matDesc.Combos[c].Material) && !string.IsNullOrEmpty(matDesc.Combos[c].Pass))
                {
                    combinationsCounter++;
                    float progress = (float)combinationsCounter * 100 / combinationsCount;
                    var materialId = MyStringId.GetOrCompute(matDesc.Combos[c].Material);
                    var passId = MyStringId.GetOrCompute(matDesc.Combos[c].Pass);
                    GenerateCombo(materialId, passId, MyShaderUnifiedFlags.NONE, MyShaderUnifiedFlags.NONE, matDesc.Combos[c].ComboList1, matDesc.Combos[c].ComboList2, progress, onShaderCacheProgress);
                }
            }
        }

        private static void GenerateCombo(MyStringId materialId, MyStringId passId, MyShaderUnifiedFlags additionalFlags, MyShaderUnifiedFlags unsupportedFlags, MatCombos.Combo[] comboList1, MatCombos.Combo[] comboList2, 
            float progress, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            if (comboList1 == null || comboList1.Length == 0)
                comboList1 = new[] { new MatCombos.Combo() };
            if (comboList2 == null || comboList2.Length == 0)
                comboList2 = new[] { new MatCombos.Combo() };
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

                    // return errors & skipped info
                    string vsSource;
                    string psSource;
                    MyMaterialShaders.Preprocess(materialId, passId, vertexLayout.Info, out vsSource, out psSource);

                    MyShaderUnifiedFlags flags = ParseFlags(comboList2[l].FlagNames) | flags1 | additionalFlags;
                    if ((flags & unsupportedFlags) != 0)
                        continue;

                    var macros = MyMaterialShaders.GenerateMaterialShaderFlagMacros(flags);
                    var descriptor = String.Format("{0}_{1}", materialId.ToString(), passId.ToString());
                    var vertexLayoutString = vertexLayout.Info.Components.GetString();

                    PreCompile(vsSource, macros, MyShadersDefines.Profiles.vs_5_0, descriptor, vertexLayoutString, progress, onShaderCacheProgress);
                    PreCompile(psSource, macros, MyShadersDefines.Profiles.ps_5_0, descriptor, vertexLayoutString, progress, onShaderCacheProgress);
                    macros.Add(m_globalMacros[0]);
                    for (int m = 0; m < m_globalMacros.Length; m++)
                    {
                        macros[macros.Count - 1] = m_globalMacros[m];
                        //PreCompile(vsSource, macros, MyShadersDefines.Profiles.vs_5_0, descriptor, vertexLayoutString, progress, onShaderCacheProgress);
                        PreCompile(psSource, macros, MyShadersDefines.Profiles.ps_5_0, descriptor, vertexLayoutString, progress, onShaderCacheProgress);
                    }
                }
            }
        }

        private static void PreCompile(string source, List<ShaderMacro> macros, MyShadersDefines.Profiles profile, string descriptor, string vertexLayoutString, float progress, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            var macrosArray = macros.ToArray();
            bool wasCached;
            string compileLog;
            if (onShaderCacheProgress != null)
                onShaderCacheProgress(progress, descriptor, MyShadersDefines.ProfileToString(profile), vertexLayoutString, macrosArray.GetString(), "", false);
            var compiled = MyShaders.Compile(source, macrosArray, profile, descriptor, true, false, out wasCached, out compileLog);
            if (onShaderCacheProgress != null)
            {
                string message = "";
                if (wasCached)
                    onShaderCacheProgress(progress, descriptor + vertexLayoutString, MyShadersDefines.ProfileToString(profile), vertexLayoutString, macrosArray.GetString(), "skipped", false);
                else if (compileLog != null)
                {
                    onShaderCacheProgress(progress, descriptor + vertexLayoutString, MyShadersDefines.ProfileToString(profile), vertexLayoutString, macrosArray.GetString(),
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
    public class MatCombos
    {
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
