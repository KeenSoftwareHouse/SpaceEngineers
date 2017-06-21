
#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens;
using Sandbox.Game.World.Generator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using System.Xml;

using VRage;
using VRage.Collections;
using VRage;
using VRage.Audio;
using VRage.Plugins;
using VRage.Utils;
using VRage.Data;
using VRage.Filesystem.FindFilesRegEx;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Library.Utils;
using Sandbox.Engine.Networking;
using Sandbox.Game.AI.Pathfinding;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.Components;
using Sandbox.Game;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.Definitions.Animation;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage.Library;
using VRage.Profiler;
using VRageRender.Utils;
using Sandbox.Game.GameSystems;

#endregion

namespace Sandbox.Definitions
{
    [PreloadRequired]
    public partial class MyDefinitionManager : MyDefinitionManagerBase
    {
        #region Fields

        public new static MyDefinitionManager Static
        {
            get { return MyDefinitionManagerBase.Static as MyDefinitionManager; }
        }

        Dictionary<string, DefinitionSet> m_modDefinitionSets = new Dictionary<string, DefinitionSet>();

        private new DefinitionSet m_definitions
        {
            get { return (DefinitionSet)base.m_definitions; }
        }

        DefinitionSet m_currentLoadingSet;

        /**
         * Return the definition set currently being loaded.
         */
        internal DefinitionSet LoadingSet
        {
            get { return m_currentLoadingSet; }
        }

        public override MyDefinitionSet GetLoadingSet()
        {
            return LoadingSet;
        }

        public bool Loading { get; private set; }

        private const string DUPLICATE_ENTRY_MESSAGE = "Duplicate entry of '{0}'";
        private const string UNKNOWN_ENTRY_MESSAGE = "Unknown type '{0}'";
        private const string WARNING_ON_REDEFINITION_MESSAGE = "WARNING: Unexpected behaviour may occur due to redefinition of '{0}'";
        private bool m_transparentMaterialsInitialized = false;

        #endregion

        #region Constructor

        static MyDefinitionManager()
        {
            MyDefinitionManagerBase.Static = new MyDefinitionManager();
        }

        private MyDefinitionManager()
        {
            Loading = false;
            base.m_definitions = new DefinitionSet();
        }

        #endregion

        #region Loading and unloading

        public void PreloadDefinitions()
        {
            MySandboxGame.Log.WriteLine("MyDefinitionManager.PreloadDefinitions() - START");

            m_definitions.Clear();

            using (MySandboxGame.Log.IndentUsing(LoggingOptions.NONE))
            {
                //Load base definitions
                if (!m_modDefinitionSets.ContainsKey(""))
                    m_modDefinitionSets.Add("", new DefinitionSet());
                var baseDefinitionSet = m_modDefinitionSets[""];
                LoadDefinitions(MyModContext.BaseGame, baseDefinitionSet, false, true);
            }

            MySandboxGame.Log.WriteLine("MyDefinitionManager.PreloadDefinitions() - END");
        }

        public void PrepareBaseDefinitions()
        {
            MySandboxGame.Log.WriteLine("MyDefinitionManager.PrepareBaseDefinitions() - START");

            using (MySandboxGame.Log.IndentUsing(LoggingOptions.NONE))
            {
                //Pre-load base definitions
                if (MyFakes.ENABLE_PRELOAD_DEFINITIONS)
                    GetDefinitionBuilders(MyModContext.BaseGame);
            }

            MySandboxGame.Log.WriteLine("MyDefinitionManager.PrepareBaseDefinitions() - END");
        }

        public void LoadScenarios()
        {
            MySandboxGame.Log.WriteLine("MyDefinitionManager.LoadScenarios() - START");

            //ProfilerShort.Begin("Wait for preload to complete");
            while (MySandboxGame.IsPreloading)
            {
                System.Threading.Thread.Sleep(1);
            }
            //ProfilerShort.End();

            using (MySandboxGame.Log.IndentUsing(LoggingOptions.NONE))
            {
                MyDataIntegrityChecker.ResetHash();

                //Load base definitions
                if (!m_modDefinitionSets.ContainsKey(""))
                    m_modDefinitionSets.Add("", new DefinitionSet());
                var baseDefinitionSet = m_modDefinitionSets[""];

                foreach (var def in m_definitions.m_scenarioDefinitions)
                    baseDefinitionSet.m_definitionsById.Remove(def.Id);

                foreach (var def in m_definitions.m_scenarioDefinitions)
                    m_definitions.m_definitionsById.Remove(def.Id);
                m_definitions.m_scenarioDefinitions.Clear();

                LoadScenarios(MyModContext.BaseGame, baseDefinitionSet);

                // Not working yet
                //foreach (var modDir in Directory.GetDirectories(MyFileSystem.ModsPath, "*", SearchOption.TopDirectoryOnly))
                //{
                //    context.ModName = mod.FriendlyName;
                //    context.ModPath = MyFileSystem.ModsPath;
                //    context.ModPathData = Path.Combine(MyFileSystem.ModsPath, mod.Name);

                //    var definitionSet = new DefinitionSet();
                //    string modName = modDir;
                //    m_modDefinitionSets.Add(modName, definitionSet);
                //    LoadScenarios(modDir, definitionSet, true);
                //}
            }
            MySandboxGame.Log.WriteLine("MyDefinitionManager.LoadScenarios() - END");
        }

        public void ReloadDecalMaterials()
        {
            var builder = Load<MyObjectBuilder_Definitions>(Path.Combine(MyModContext.BaseGame.ModPathData, "Decals.sbc"));
            if (builder.Decals != null)
                InitDecals(MyModContext.BaseGame, builder.Decals, true);

            if (builder.DecalGlobals != null)
                InitDecalGlobals(MyModContext.BaseGame, builder.DecalGlobals, true);
        }

        public void LoadData(List<MyObjectBuilder_Checkpoint.ModItem> mods)
        {
            MySandboxGame.Log.WriteLine("MyDefinitionManager.LoadData() - START");

            ProfilerShort.Begin("Wait for preload to complete");
            while (MySandboxGame.IsPreloading)
            {
                System.Threading.Thread.Sleep(1);
            }
            ProfilerShort.End();

            UnloadData();
            Loading = true;
            LoadScenarios();

            using (MySandboxGame.Log.IndentUsing(LoggingOptions.NONE))
            {
                //Load base definitions
                if (!m_modDefinitionSets.ContainsKey(""))
                    m_modDefinitionSets.Add("", new DefinitionSet());
                var baseDefinitionSet = m_modDefinitionSets[""];

                ProfilerShort.Begin("Parse Base Definitions");

                LoadDefinitions(MyModContext.BaseGame, baseDefinitionSet);

                ProfilerShort.End();

                ProfilerShort.Begin("Logging mods");

                MySandboxGame.Log.WriteLine(string.Format("List of used mods ({0}) - START", mods.Count));
                MySandboxGame.Log.IncreaseIndent();
                foreach (var mod in mods)
                    MySandboxGame.Log.WriteLine(string.Format("Id = {0}, Filename = '{1}', Name = '{2}'", mod.PublishedFileId, mod.Name, mod.FriendlyName));

                MySandboxGame.Log.DecreaseIndent();
                MySandboxGame.Log.WriteLine("List of used mods - END");

                MyAnalyticsHelper.SetUsedMods(mods);

                ProfilerShort.End();

                ProfilerShort.Begin("Parse Mod Definitions");

                foreach (var mod in mods)
                {
                    MyModContext context = new MyModContext();
                    context.Init(mod);

                    if (!m_modDefinitionSets.ContainsKey(context.ModPath))
                    {
                        var definitionSet = new DefinitionSet();
                        m_modDefinitionSets.Add(context.ModPath, definitionSet);
                        LoadDefinitions(context, definitionSet);
                    }
                }

                ProfilerShort.BeginNextBlock("Post Process Definitions");
                if (MySandboxGame.Static != null)
                {
                    LoadPostProcess();
                }

                ProfilerShort.BeginNextBlock("Test Models");
                if (MyFakes.TEST_MODELS && (Sandbox.AppCode.MyExternalAppBase.Static == null))
                {
                    var s = Stopwatch.GetTimestamp();
                    TestCubeBlockModels();
                    var delta = (Stopwatch.GetTimestamp() - s) / (double)Stopwatch.Frequency;
                    Debug.WriteLine("Models tested in: {0} seconds", delta);
                }

                CheckCharacterPickup();
                CheckEntityComponents();
                CheckComponentContainers();

                if (MyFakes.ENABLE_ALL_IN_SURVIVAL)
                {
                    foreach (var definition in m_definitions.m_physicalItemDefinitions)
                    {
                        definition.AvailableInSurvival = true;
                    }
                    foreach (var definition in m_definitions.m_behaviorDefinitions.Values)
                    {
                        definition.AvailableInSurvival = true;
                    }
                    foreach (var definition in m_definitions.m_behaviorDefinitions.Values)
                    {
                        definition.AvailableInSurvival = true;
                    }
                    foreach (var definition in m_definitions.m_characters.Values)
                    {
                        definition.AvailableInSurvival = true;
                    }
                }

                ProfilerShort.BeginNextBlock("Postprocess environment item classes.");
                var classes = MyDefinitionManager.Static.GetEnvironmentItemClassDefinitions();
                foreach (var cl in classes)
                {
                    List<MyDefinitionId> classList = null;
                    if (!m_definitions.m_channelEnvironmentItemsDefs.TryGetValue(cl.Channel, out classList))
                    {
                        classList = new List<MyDefinitionId>();
                        m_definitions.m_channelEnvironmentItemsDefs[cl.Channel] = classList;
                    }

                    classList.Add(cl.Id);
                }

                ProfilerShort.End();
            }

            Loading = false;
            MySandboxGame.Log.WriteLine("MyDefinitionManager.LoadData() - END");
        }

        /// <summary>
        /// This is here only for a while to warn modders about the pickup component change that we did.
        /// </summary>
        private void CheckCharacterPickup()
        {
            if (MyPerGameSettings.Game != GameEnum.ME_GAME) return;

            // Add characters that we have in the game and that we know are not picking stuff
            HashSet<MyDefinitionId> ignoredCharacters = new HashSet<MyDefinitionId>();
            ignoredCharacters.Add(new MyDefinitionId(typeof(MyObjectBuilder_Character), "Peasant_male"));
            ignoredCharacters.Add(new MyDefinitionId(typeof(MyObjectBuilder_Character), "Medieval_barbarian"));
            ignoredCharacters.Add(new MyDefinitionId(typeof(MyObjectBuilder_Character), "Medieval_deer"));
            ignoredCharacters.Add(new MyDefinitionId(typeof(MyObjectBuilder_Character), "Medieval_wolf"));

            MyContainerDefinition containerDef = null;
            string error = "Character definition {0} is missing a pickup component! " +
            "You will not be able to pickup things with this character! " +
            "See the player character in EntityContainers.sbc and EntityComponents.sbc for an example.";

            foreach (var pair in m_definitions.m_characters)
            {
                var characterDef = pair.Value;
                if (ignoredCharacters.Contains(characterDef.Id)) continue;

                bool hasContainerDef = TryGetContainerDefinition(characterDef.Id, out containerDef);

                if (!hasContainerDef)
                {
                    MyDefinitionErrors.Add(MyModContext.UnknownContext, String.Format(error, characterDef.Id.ToString()), TErrorSeverity.Warning);
                    continue;
                }

                bool foundPickup = false;
                foreach (var component in containerDef.DefaultComponents)
                {
                    Type componentType = (Type)component.BuilderType;
                    if (typeof(MyObjectBuilder_CharacterPickupComponent).IsAssignableFrom(componentType))
                    {
                        foundPickup = true;
                        break;
                    }
                }

                if (!foundPickup)
                {
                    MyDefinitionErrors.Add(MyModContext.UnknownContext, String.Format(error, characterDef.Id.ToString()), TErrorSeverity.Warning);
                }
            }
        }

        private void TestCubeBlockModels()
        {
            //*

            // Spread testing of models over the available workers
            ParallelTasks.Parallel.ForEach<string>(GetDefinitionPairNames(), delegate(string pair)
            {
                var group = GetDefinitionGroup(pair);
                TestCubeBlockModel(group.Small);
                TestCubeBlockModel(group.Large);
            });
        }

        private void TestCubeBlockModel(MyCubeBlockDefinition block)
        {
            if (block == null)
                return;

            if (block.Model != null)
            {
                var model = VRage.Game.Models.MyModels.GetModelOnlyData(block.Model);
                if (MyFakes.TEST_MODELS_WRONG_TRIANGLES)
                {
                    int triCount = model.GetTrianglesCount();
                    for (int i = 0; i < triCount; ++i)
                    {
                        var triangle = model.GetTriangle(i);
                        if (MyUtils.IsWrongTriangle(model.GetVertex(triangle.I0), model.GetVertex(triangle.I1), model.GetVertex(triangle.I2)))
                        {
                            System.Diagnostics.Debug.Fail("Wrong triangle in " + model.AssetName + "!");
                            break;
                        }
                    }
                }
                model.UnloadData();
            }
            foreach (var c in block.BuildProgressModels)
            {
                var model = VRage.Game.Models.MyModels.GetModelOnlyData(c.File);
                model.UnloadData();
            }
        }

        private HashSet<string> GetPreloadSet()
        {
            HashSet<string> preloadSet = new HashSet<string>();
            string preloadFilePath = Path.Combine(MyModContext.BaseGame.ModPathData, "DefinitionsToPreload.sbc");


            if (!MyFileSystem.FileExists(preloadFilePath))
                return null;

            MyObjectBuilder_Definitions builder = Load<MyObjectBuilder_Definitions>(preloadFilePath);
            if (builder == null)
                return null;

            if (builder.Definitions == null)
                return null;

            MyObjectBuilder_DefinitionsToPreload definitionsToPreload = (MyObjectBuilder_DefinitionsToPreload)builder.Definitions[0];
            foreach (var fileInfo in definitionsToPreload.DefinitionFiles)
            {
                if (MySandboxGame.IsDedicated)
                {
                    if (fileInfo.LoadOnDedicated)
                        preloadSet.Add(fileInfo.Name);
                }
                else
                {
                    preloadSet.Add(fileInfo.Name);
                }
            }

            return preloadSet;
        }

        private List<Tuple<MyObjectBuilder_Definitions, string>> m_preloadedDefinitionBuilders = null;
        private List<Tuple<MyObjectBuilder_Definitions, string>> GetDefinitionBuilders(MyModContext context, HashSet<string> preloadSet = null)
        {

            // Reload definitions from cache on official release
            if (m_preloadedDefinitionBuilders != null && context == MyModContext.BaseGame && preloadSet == null)
                return m_preloadedDefinitionBuilders;

            var definitionBuilders = new List<Tuple<MyObjectBuilder_Definitions, string>>(30);
            foreach (var file in MyFileSystem.GetFiles(context.ModPathData, "*.sbc", VRage.FileSystem.MySearchOption.AllDirectories))
            {
                if (preloadSet != null && !preloadSet.Contains(Path.GetFileName(file))) continue;

                if (Path.GetFileName(file) == "DefinitionsToPreload.sbc") continue;
                context.CurrentFile = file;

                ProfilerShort.Begin("Verify Integrity");
                MyDataIntegrityChecker.HashInFile(file);
                MyObjectBuilder_Definitions builder = null;

                ProfilerShort.BeginNextBlock("Try Parse as Prefab");
                try
                {
                    builder = CheckPrefabs(file);
                }
                catch (Exception e)
                {
                    FailModLoading(context, innerException: e);
                    return null;
                }

                ProfilerShort.BeginNextBlock("Parse");
                if (builder == null)
                {
                    builder = Load<MyObjectBuilder_Definitions>(file);
                }

                if (builder == null)
                {
                    FailModLoading(context);
                    return null;
                }

                definitionBuilders.Add(new Tuple<MyObjectBuilder_Definitions, string>(builder, file));
                ProfilerShort.End();
            }

            if (context == MyModContext.BaseGame && preloadSet == null && MyFakes.ENABLE_PRELOAD_DEFINITIONS)
                m_preloadedDefinitionBuilders = definitionBuilders;

            return definitionBuilders;
        }

        private void LoadDefinitions(MyModContext context, DefinitionSet definitionSet, bool failOnDebug = true, bool isPreload = false)
        {
            HashSet<string> preloadSet = null;

            if (isPreload)
            {
                preloadSet = GetPreloadSet();
                if (preloadSet == null) return;
            }

            if (!MyFileSystem.DirectoryExists(context.ModPathData))
                return;

            m_currentLoadingSet = definitionSet;
            definitionSet.Context = context;
            m_transparentMaterialsInitialized = false;

            ProfilerShort.Begin("Load definitions from files");
            var definitionsBuilders = GetDefinitionBuilders(context, preloadSet);
            if (definitionsBuilders == null)
                return;

            ProfilerShort.BeginNextBlock("Postprocessing");

            var phases = new Action<MyObjectBuilder_Definitions, MyModContext, DefinitionSet, bool>[]
            {
                LoadPhase1,
                LoadPhase2,
                LoadPhase3,
                LoadPhase4,
                LoadPhase5,
            };

            for (int i = 0; i < phases.Length; i++)
            {
                try
                {
                    ProfilerShort.Begin("Phase " + (i + 1));
                    foreach (var builder in definitionsBuilders)
                    {
                        context.CurrentFile = builder.Item2;
                        var phase = phases[i];
                        phase(builder.Item1, context, definitionSet, failOnDebug);
                    }
                    ProfilerShort.End();
                }
                catch (Exception e)
                {
                    FailModLoading(context, phase: i, phaseNum: phases.Length, innerException: e);
                    ProfilerShort.End();
                    ProfilerShort.End();
                    return;
                }
                MergeDefinitions();
            }


            AfterLoad(context, definitionSet);

            ProfilerShort.End();
        }

        private void AfterLoad(MyModContext context, DefinitionSet definitionSet)
        {
            MyDefinitionPostprocessor.Bundle definitions = new MyDefinitionPostprocessor.Bundle()
            {
                Context = context,
                Set = m_currentLoadingSet
            };

            foreach (var postprocessor in m_postProcessors)
            {
                if (definitionSet.Definitions.TryGetValue(postprocessor.DefinitionType, out definitions.Definitions))
                    postprocessor.AfterLoaded(ref definitions);
            }
        }

        [Conditional("DEBUG")]
        private void CheckEntityComponents()
        {
            if (m_definitions.m_entityComponentDefinitions == null)
                return;

            foreach (var componentDefinition in m_definitions.m_entityComponentDefinitions)
            {
                try
                {
                    var instance = MyComponentFactory.CreateInstanceByTypeId(componentDefinition.Key.TypeId);
                    if (instance == null)
                    {
                        System.Diagnostics.Debug.Fail(string.Format("Cannot create instance of component defined as {1} with definition id {0}", componentDefinition.Value.ToString(), componentDefinition.Key));
                    }
                    else
                    {
                        instance.Init(componentDefinition.Value);
                    }
                }
                catch (Exception except)
                {
                    System.Diagnostics.Debug.Fail(string.Format("Cannot create instance of component defined as {1} with definition id {0}", componentDefinition.Value.ToString(), componentDefinition.Key));
                }
            }
        }

        [Conditional("DEBUG")]
        private void CheckComponentContainers()
        {
            if (m_definitions.m_entityContainers == null)
                return;

            foreach (var container in m_definitions.m_entityContainers)
            {
                foreach (var component in container.Value.DefaultComponents)
                {
                    try
                    {
                        System.Diagnostics.Debug.Assert(!component.BuilderType.IsNull || component.InstanceType != null, string.Format("Wrong definition of components container {0}. Either BuilderType or InstanceType must be defined!", container.Key.ToString()));

                        if (MyComponentFactory.CreateInstanceByTypeId(component.BuilderType) == null)
                        {
                            System.Diagnostics.Debug.Fail(string.Format("Defined default component {0} for container {1}, can't be created, check your definition!", component.ToString(), container.Key.ToString()));
                        }
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.Fail(string.Format("Defined default component {0} for container {1}, can't be created, check your definition!", component.ToString(), container.Key.ToString()));
                    }
                }
            }
        }

        private static void FailModLoading(MyModContext context, int phase = -1, int phaseNum = 0, Exception innerException = null)
        {
            string errorMessage = (innerException != null ? ", Following Error occured:" + MyEnvironment.NewLine + innerException.Message + MyEnvironment.NewLine + innerException.Source + MyEnvironment.NewLine + innerException.StackTrace : "");
            if (phase == -1)
                MyDefinitionErrors.Add(context, "MOD SKIPPED, Cannot load definition file" + errorMessage, TErrorSeverity.Critical);
            else
                MyDefinitionErrors.Add(context, String.Format("MOD PARTIALLY SKIPPED, LOADED ONLY {0}/{1} PHASES" + errorMessage, phase + 1, phaseNum), TErrorSeverity.Critical);

            if (context.IsBaseGame)
            {
                // When original definition fails to load, return to main menu
                throw new MyLoadingException(String.Format(MyTexts.GetString(MySpaceTexts.LoadingError_ModifiedOriginalContent), context.CurrentFile), innerException);
            }
            else
            {
                // When definition from MOD fails to load, skip mod loading
                return;
            }
        }

        private static MyObjectBuilder_Definitions CheckPrefabs(string file)
        {
            List<MyObjectBuilder_PrefabDefinition> prefabs = null;
            using (var fileStream = MyFileSystem.OpenRead(file))
            {
                if (fileStream != null)
                {
                    using (var readStream = fileStream.UnwrapGZip())
                    {
                        if (readStream != null)
                        {
                            CheckXmlForPrefabs(file, ref prefabs, readStream);
                        }
                    }
                }
            }

            MyObjectBuilder_Definitions definitions = null;
            if (prefabs != null)
            {
                definitions = new MyObjectBuilder_Definitions();
                definitions.Prefabs = prefabs.ToArray();
            }
            return definitions;
        }

        private static void CheckXmlForPrefabs(string file, ref List<MyObjectBuilder_PrefabDefinition> prefabs, Stream readStream)
        {
            using (XmlReader reader = XmlReader.Create(readStream))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "SpawnGroups")
                        {
                            break;
                        }
                        else if (reader.Name == "Prefabs")
                        {
                            prefabs = new List<MyObjectBuilder_PrefabDefinition>();
                            while (reader.ReadToFollowing("Prefab"))
                            {
                                ReadPrefabHeader(file, ref prefabs, reader);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private static void ReadPrefabHeader(string file, ref  List<MyObjectBuilder_PrefabDefinition> prefabs, XmlReader reader)
        {
            MyObjectBuilder_PrefabDefinition definition = new MyObjectBuilder_PrefabDefinition();
            definition.PrefabPath = file;
            bool found = reader.ReadToFollowing("Id");
            Debug.Assert(found);

            bool useAttrs = false;

            if (reader.AttributeCount >= 2)
            {
                for (int i = 0; i < reader.AttributeCount; ++i)
                {
                    reader.MoveToAttribute(i);

                    switch (reader.Name)
                    {
                        case "Type":
                            definition.Id.TypeIdString = reader.Value;
                            useAttrs = true;
                            break;
                        case "Subtype":
                            definition.Id.SubtypeId = reader.Value;
                            break;
                    }
                }
            }

            if (!useAttrs)
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "TypeId":
                                reader.Read();
                                definition.Id.TypeIdString = reader.Value;
                                break;
                            case "SubtypeId":
                                reader.Read();
                                definition.Id.SubtypeId = reader.Value;
                                break;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Id")
                    {
                        break;
                    }
                }

            prefabs.Add(definition);
        }

        void LoadPhase1(MyObjectBuilder_Definitions objBuilder, MyModContext context, DefinitionSet definitionSet, bool failOnDebug)
        {
            if (objBuilder.Definitions != null)
            {
                foreach (var defObs in objBuilder.Definitions)
                {
                    var def = InitDefinition<MyDefinitionBase>(context, defObs);

                    m_currentLoadingSet.AddDefinition(def);
                }
            }

            if (objBuilder.GridCreators != null)
            {
                MySandboxGame.Log.WriteLine("Loading grid creators");
                InitGridCreators(context, definitionSet.m_gridCreateDefinitions, definitionSet.m_definitionsById, objBuilder.GridCreators, failOnDebug);
            }

            if (objBuilder.Ammos != null)
            {
                MySandboxGame.Log.WriteLine("Loading ammo definitions");
                InitAmmos(context, definitionSet.m_ammoDefinitionsById, objBuilder.Ammos, failOnDebug);
            }
            if (objBuilder.AmmoMagazines != null)
            {
                MySandboxGame.Log.WriteLine("Loading ammo magazines");
                InitAmmoMagazines(context, definitionSet.m_definitionsById, objBuilder.AmmoMagazines, failOnDebug);
            }
            if (objBuilder.Animations != null)
            {
                MySandboxGame.Log.WriteLine("Loading animations");
                InitAnimations(context, definitionSet.m_definitionsById, objBuilder.Animations, definitionSet.m_animationsBySkeletonType, failOnDebug);
            }
            if (objBuilder.CategoryClasses != null)
            {
                MySandboxGame.Log.WriteLine("Loading category classes");
                InitCategoryClasses(context, definitionSet.m_categoryClasses, objBuilder.CategoryClasses, failOnDebug);
            }
            if (objBuilder.Debris != null)
            {
                MySandboxGame.Log.WriteLine("Loading debris");
                InitDebris(context, definitionSet.m_definitionsById, objBuilder.Debris, failOnDebug);
            }
            if (objBuilder.Edges != null)
            {
                MySandboxGame.Log.WriteLine("Loading edges");
                InitEdges(context, definitionSet.m_definitionsById, objBuilder.Edges, failOnDebug);
            }
            if (objBuilder.Factions != null)
            {
                MySandboxGame.Log.WriteLine("Loading factions");
                InitDefinitionsGeneric<MyObjectBuilder_FactionDefinition, MyFactionDefinition>
                    (context, definitionSet.m_definitionsById, objBuilder.Factions, failOnDebug);
            }
            if (objBuilder.BlockPositions != null)
            {
                MySandboxGame.Log.WriteLine("Loading block positions");
                InitBlockPositions(definitionSet.m_blockPositions, objBuilder.BlockPositions, failOnDebug);
            }
            if (objBuilder.BlueprintClasses != null)
            {
                MySandboxGame.Log.WriteLine("Loading blueprint classes");
                InitBlueprintClasses(context, definitionSet.m_blueprintClasses, objBuilder.BlueprintClasses, failOnDebug);
            }
            if (objBuilder.BlueprintClassEntries != null)
            {
                MySandboxGame.Log.WriteLine("Loading blueprint class entries");
                InitBlueprintClassEntries(context, definitionSet.m_blueprintClassEntries, objBuilder.BlueprintClassEntries, failOnDebug);
            }
            if (objBuilder.Blueprints != null)
            {
                MySandboxGame.Log.WriteLine("Loading blueprints");
                InitBlueprints(context, definitionSet.m_blueprintsById, definitionSet.m_blueprintsByResultId, objBuilder.Blueprints, failOnDebug);
            }
            if (objBuilder.Components != null)
            {
                MySandboxGame.Log.WriteLine("Loading components");
                InitComponents(context, definitionSet.m_definitionsById, objBuilder.Components, failOnDebug);
            }
            if (objBuilder.Configuration != null)
            {
                MySandboxGame.Log.WriteLine("Loading configuration");
                Check(failOnDebug, "Configuration", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitConfiguration(definitionSet, objBuilder.Configuration);
            }
            if (objBuilder.ContainerTypes != null)
            {
                MySandboxGame.Log.WriteLine("Loading container types");
                InitContainerTypes(context, definitionSet.m_containerTypeDefinitions, objBuilder.ContainerTypes, failOnDebug);
            }
            if (objBuilder.Environments != null)
            {
                MySandboxGame.Log.WriteLine("Loading environment definition");
                Check(failOnDebug, "Environment", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitEnvironment(context, definitionSet, objBuilder.Environments, failOnDebug);
            }
            if (objBuilder.DroneBehaviors != null)
            {
                MySandboxGame.Log.WriteLine("Loading drone behaviors");
                Check(failOnDebug, "DroneBehaviors", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                LoadDroneBehaviorPresets(context, definitionSet, objBuilder.DroneBehaviors, failOnDebug);
            }
            if (objBuilder.EnvironmentItemsEntries != null)
            {
                MySandboxGame.Log.WriteLine("Loading environment items entries");
                InitEnvironmentItemsEntries(context, definitionSet.m_environmentItemsEntries, objBuilder.EnvironmentItemsEntries, failOnDebug);
            }
            if (objBuilder.GlobalEvents != null)
            {
                MySandboxGame.Log.WriteLine("Loading event definitions");
                InitGlobalEvents(context, definitionSet.m_definitionsById, objBuilder.GlobalEvents, failOnDebug);
            }
            if (objBuilder.HandItems != null)
            {
                InitHandItems(context, definitionSet.m_handItemsById, objBuilder.HandItems, failOnDebug);
            }
            if (objBuilder.VoxelHands != null)
            {
                InitVoxelHands(context, definitionSet.m_definitionsById, objBuilder.VoxelHands, failOnDebug);
            }
            if (objBuilder.PrefabThrowers != null && MyFakes.ENABLE_PREFAB_THROWER)
            {
                InitPrefabThrowers(context, definitionSet.m_definitionsById, objBuilder.PrefabThrowers, failOnDebug);
            }
            if (objBuilder.PhysicalItems != null)
            {
                MySandboxGame.Log.WriteLine("Loading physical items");
                InitPhysicalItems(context, definitionSet.m_definitionsById, definitionSet.m_physicalItemDefinitions, objBuilder.PhysicalItems, failOnDebug);
            }
            if (objBuilder.Fonts != null)
            {
                InitFonts(context, definitionSet.m_fontsById, objBuilder.Fonts, failOnDebug);
            }

            if (objBuilder.TransparentMaterials != null)
            {
                MySandboxGame.Log.WriteLine("Loading transparent material properties");
                InitTransparentMaterials(context, definitionSet.m_definitionsById, objBuilder.TransparentMaterials);
            }
            if (objBuilder.VoxelMaterials != null)
            {
                if (MySandboxGame.Static != null)
                {
                    MySandboxGame.Log.WriteLine("Loading voxel material definitions");
                    InitVoxelMaterials(context, definitionSet.m_voxelMaterialsByName, objBuilder.VoxelMaterials, failOnDebug);
                }
            }
            if (objBuilder.Characters != null)
            {
                MySandboxGame.Log.WriteLine("Loading character definitions");
                InitCharacters(context, definitionSet.m_characters, definitionSet.m_definitionsById, objBuilder.Characters, failOnDebug);
            }

            if (objBuilder.CompoundBlockTemplates != null)
            {
                MySandboxGame.Log.WriteLine("Loading compound block template definitions");
                InitDefinitionsGeneric<MyObjectBuilder_CompoundBlockTemplateDefinition, MyCompoundBlockTemplateDefinition>
                    (context, definitionSet.m_definitionsById, objBuilder.CompoundBlockTemplates, failOnDebug);
            }

            if (objBuilder.Sounds != null)
            {
                MySandboxGame.Log.WriteLine("Loading sound definitions");
                InitSounds(context, definitionSet.m_sounds, objBuilder.Sounds, failOnDebug);
            }

            if (objBuilder.MultiBlocks != null)
            {
                MySandboxGame.Log.WriteLine("Loading multi cube block definitions");
                InitDefinitionsGeneric<MyObjectBuilder_MultiBlockDefinition, MyMultiBlockDefinition>
                    (context, definitionSet.m_definitionsById, objBuilder.MultiBlocks, failOnDebug);
            }

            if (objBuilder.SoundCategories != null)
            {
                MySandboxGame.Log.WriteLine("Loading sound categories");
                InitSoundCategories(context, definitionSet.m_definitionsById, objBuilder.SoundCategories, failOnDebug);
            }

            if (objBuilder.ShipSoundGroups != null)
            {
                MySandboxGame.Log.WriteLine("Loading ship sound groups");
                InitShipSounds(context, definitionSet.m_shipSounds, objBuilder.ShipSoundGroups, failOnDebug);
            }

            if (objBuilder.ShipSoundSystem != null)
            {
                MySandboxGame.Log.WriteLine("Loading ship sound groups");
                InitShipSoundSystem(context, ref definitionSet.m_shipSoundSystem, objBuilder.ShipSoundSystem, failOnDebug);
            }

            if (objBuilder.LCDTextures != null)
            {
                MySandboxGame.Log.WriteLine("Loading LCD texture categories");
                InitLCDTextureCategories(context, definitionSet.m_definitionsById, objBuilder.LCDTextures, failOnDebug);
            }

            if (objBuilder.AIBehaviors != null)
            {
                MySandboxGame.Log.WriteLine("Loading behaviors");
                InitAIBehaviors(context, definitionSet.m_behaviorDefinitions, objBuilder.AIBehaviors, failOnDebug);
            }

            if (objBuilder.VoxelMapStorages != null)
            {
                MySandboxGame.Log.WriteLine("Loading voxel map storage definitions");
                InitVoxelMapStorages(context, definitionSet.m_voxelMapStorages, objBuilder.VoxelMapStorages, failOnDebug);
            }

            if (objBuilder.RopeTypes != null)
            {
                MySandboxGame.Log.WriteLine("Loading Rope type definitions");
                InitGenericObjects(context, definitionSet.m_definitionsById, objBuilder.RopeTypes, failOnDebug);
            }

            if (objBuilder.Bots != null)
            {
                MySandboxGame.Log.WriteLine("Loading agent definitions");
                InitBots(context, definitionSet.m_definitionsById, objBuilder.Bots, failOnDebug);
            }

            if (objBuilder.PhysicalMaterials != null)
            {
                MySandboxGame.Log.WriteLine("Loading physical material properties");
                InitPhysicalMaterials(context, definitionSet.m_definitionsById, objBuilder.PhysicalMaterials);
            }

            if (objBuilder.AiCommands != null)
            {
                MySandboxGame.Log.WriteLine("Loading bot commands");
                InitBotCommands(context, definitionSet.m_definitionsById, objBuilder.AiCommands, failOnDebug);
            }

            if (objBuilder.AreaMarkerDefinitions != null)
            {
                MySandboxGame.Log.WriteLine("Loading area definitions");
                InitDefinitionsGeneric<MyObjectBuilder_AreaMarkerDefinition, MyAreaMarkerDefinition>
                    (context, definitionSet.m_definitionsById, objBuilder.AreaMarkerDefinitions, failOnDebug);
            }

            if (objBuilder.BlockNavigationDefinitions != null)
            {
                MySandboxGame.Log.WriteLine("Loading navigation definitions");
                InitNavigationDefinitions(context, definitionSet.m_definitionsById, objBuilder.BlockNavigationDefinitions, failOnDebug);
            }

            if (objBuilder.Cuttings != null)
            {
                MySandboxGame.Log.WriteLine("Loading cutting definitions");
                InitGenericObjects(context, definitionSet.m_definitionsById, objBuilder.Cuttings, failOnDebug);
            }

            if (objBuilder.ControllerSchemas != null)
            {
                MySandboxGame.Log.WriteLine("Loading controller schemas definitions");
                InitControllerSchemas(context, definitionSet.m_definitionsById, objBuilder.ControllerSchemas, failOnDebug);
            }

            if (objBuilder.CurveDefinitions != null)
            {
                MySandboxGame.Log.WriteLine("Loading curve definitions");
                InitCurves(context, definitionSet.m_definitionsById, objBuilder.CurveDefinitions, failOnDebug);
            }

            if (objBuilder.CharacterNames != null)
            {
                MySandboxGame.Log.WriteLine("Loading character names");
                InitCharacterNames(context, definitionSet.m_characterNames, objBuilder.CharacterNames, failOnDebug);
            }

            if (objBuilder.Battle != null)
            {
                MySandboxGame.Log.WriteLine("Loading battle definition");
                Check(failOnDebug, "Battle", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitBattle(context, ref definitionSet.m_battleDefinition, objBuilder.Battle, failOnDebug);
            }

            if (objBuilder.DecalGlobals != null)
            {
                MySandboxGame.Log.WriteLine("Loading decal global definitions");
                Check(failOnDebug, "DecalGlobals", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitDecalGlobals(context, objBuilder.DecalGlobals, failOnDebug);
            }

            if (objBuilder.Decals != null)
            {
                MySandboxGame.Log.WriteLine("Loading decal definitions");
                Check(failOnDebug, "Decals", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitDecals(context, objBuilder.Decals, failOnDebug);
            }

            if (objBuilder.PlanetGeneratorDefinitions != null)
            {
                MySandboxGame.Log.WriteLine("Loading planet definition");
                Check(failOnDebug, "Planet", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitPlanetGeneratorDefinitions(context, definitionSet, objBuilder.PlanetGeneratorDefinitions, failOnDebug);
            }

            if (objBuilder.StatDefinitions != null)
            {
                MySandboxGame.Log.WriteLine("Loading stat definitions");
                Check(failOnDebug, "Stat", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitGenericObjects(context, definitionSet.m_definitionsById, objBuilder.StatDefinitions, failOnDebug);
            }

            if (objBuilder.GasProperties != null)
            {
                MySandboxGame.Log.WriteLine("Loading gas property definitions");
                Check(failOnDebug, "Gas", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitGenericObjects(context, definitionSet.m_definitionsById, objBuilder.GasProperties, failOnDebug);
            }

            if (objBuilder.ResourceDistributionGroups != null)
            {
                MySandboxGame.Log.WriteLine("Loading resource distribution groups");
                Check(failOnDebug, "DistributionGroup", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitGenericObjects(context, definitionSet.m_definitionsById, objBuilder.ResourceDistributionGroups, failOnDebug);
            }

            if (objBuilder.ComponentGroups != null)
            {
                MySandboxGame.Log.WriteLine("Loading component group definitions");
                Check(failOnDebug, "Component groups", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitComponentGroups(context, definitionSet.m_componentGroups, objBuilder.ComponentGroups, failOnDebug);
            }

            if (objBuilder.ComponentSubstitutions != null)
            {
                MySandboxGame.Log.WriteLine("Loading component substitution definitions");
                Check(failOnDebug, "Component groups", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitComponentSubstitutions(context, definitionSet.m_componentSubstitutions, objBuilder.ComponentSubstitutions, failOnDebug);
            }

            if (objBuilder.ComponentBlocks != null)
            {
                MySandboxGame.Log.WriteLine("Loading component block definitions");
                InitComponentBlocks(context, definitionSet.m_componentBlockEntries, objBuilder.ComponentBlocks, failOnDebug);
            }

            if (objBuilder.PlanetPrefabs != null)
            {
                MySandboxGame.Log.WriteLine("Loading planet prefabs");
                Check(failOnDebug, "Planet prefabs", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitPlanetPrefabDefinitions(context, ref definitionSet.m_planetPrefabDefinitions, objBuilder.PlanetPrefabs, failOnDebug);
            }

            if (objBuilder.EnvironmentGroups != null)
            {
                MySandboxGame.Log.WriteLine("Loading environment groups");
                Check(failOnDebug, "Environment groups", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitGroupedIds(context, "EnvGroups", definitionSet.m_groupedIds, objBuilder.EnvironmentGroups, failOnDebug);
            }

            if (objBuilder.ScriptedGroups != null)
            {
                MySandboxGame.Log.WriteLine("Loading scripted groups");
                Check(failOnDebug, "Scripted groups", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitDefinitionsGeneric<MyObjectBuilder_ScriptedGroupDefinition, MyScriptedGroupDefinition>
                    (context, definitionSet.m_scriptedGroupDefinitions, objBuilder.ScriptedGroups, failOnDebug);
            }

            if (objBuilder.PirateAntennas != null)
            {
                MySandboxGame.Log.WriteLine("Loading pirate antennas");
                Check(failOnDebug, "Pirate antennas", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitDefinitionsGeneric<MyObjectBuilder_PirateAntennaDefinition, MyPirateAntennaDefinition>
                    (context, definitionSet.m_pirateAntennaDefinitions, objBuilder.PirateAntennas, failOnDebug);
            }

            if (objBuilder.Destruction != null)
            {
                MySandboxGame.Log.WriteLine("Loading destruction definition");
                Check(failOnDebug, "Destruction", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitDestruction(context, ref definitionSet.m_destructionDefinition, objBuilder.Destruction, failOnDebug);
            }

            if (objBuilder.EntityComponents != null)
            {
                MySandboxGame.Log.WriteLine("Loading entity components");
                Check(failOnDebug, "Entity components", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitDefinitionsGeneric<MyObjectBuilder_ComponentDefinitionBase, MyComponentDefinitionBase>(context, definitionSet.m_entityComponentDefinitions, objBuilder.EntityComponents, failOnDebug);
            }

            if (objBuilder.EntityContainers != null)
            {
                MySandboxGame.Log.WriteLine("Loading component containers");
                Check(failOnDebug, "Entity containers", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitDefinitionsGeneric<MyObjectBuilder_ContainerDefinition, MyContainerDefinition>(context, definitionSet.m_entityContainers, objBuilder.EntityContainers, failOnDebug);
            }

            if (objBuilder.ShadowTextureSets != null)
            {
                MySandboxGame.Log.WriteLine("Loading shadow textures definitions");
                Check(failOnDebug, "Text shadow sets", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitShadowTextureSets(context, objBuilder.ShadowTextureSets, failOnDebug);
            }
        }

        void LoadPhase2(MyObjectBuilder_Definitions objBuilder, MyModContext context, DefinitionSet definitionSet, bool failOnDebug)
        {
            if (objBuilder.ParticleEffects != null)
            {
                MySandboxGame.Log.WriteLine("Loading particle effect definitions");
                InitParticleEffects(context, definitionSet.m_definitionsById, objBuilder.ParticleEffects, failOnDebug);
            }

            //Dependent on physical materials
            if (objBuilder.EnvironmentItems != null)
            {
                MySandboxGame.Log.WriteLine("Loading environment item definitions");
                InitDefinitionsEnvItems(context, definitionSet.m_definitionsById, objBuilder.EnvironmentItems, failOnDebug);
            }

            if (objBuilder.EnvironmentItemsDefinitions != null)
            {
                MySandboxGame.Log.WriteLine("Loading environment items definitions");
                InitDefinitionsGeneric<MyObjectBuilder_EnvironmentItemsDefinition, MyEnvironmentItemsDefinition>
                    (context, definitionSet.m_definitionsById, objBuilder.EnvironmentItemsDefinitions, failOnDebug);
            }

            if (objBuilder.MaterialProperties != null)
            {
                MySandboxGame.Log.WriteLine("Loading physical material properties");
                InitMaterialProperties(context, definitionSet.m_definitionsById, objBuilder.MaterialProperties);
            }

            if (objBuilder.Weapons != null)
            {
                MySandboxGame.Log.WriteLine("Loading weapon definitions");
                InitWeapons(context, definitionSet.m_weaponDefinitionsById, objBuilder.Weapons, failOnDebug);
            }

            //dependent on curves
            if (objBuilder.AudioEffects != null)
            {
                MySandboxGame.Log.WriteLine("Audio effects definitions");
                InitAudioEffects(context, definitionSet.m_definitionsById, objBuilder.AudioEffects, failOnDebug);
            }

            if (objBuilder.FloraElements != null)
            {
                MySandboxGame.Log.WriteLine("Loading flora elements definitions");
                Check(failOnDebug, "Flora", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitGenericObjects(context, definitionSet.m_definitionsById, objBuilder.FloraElements, failOnDebug);
            }

            if (objBuilder.ScriptedGroupsMap != null)
            {
                MySandboxGame.Log.WriteLine("Loading scripted groups map");
                Check(failOnDebug, "Scripted groups map", failOnDebug, WARNING_ON_REDEFINITION_MESSAGE);
                InitScriptedGroupsMap(context, objBuilder.ScriptedGroupsMap, failOnDebug);
            }
        }

        void LoadPhase3(MyObjectBuilder_Definitions objBuilder, MyModContext context, DefinitionSet definitionSet, bool failOnDebug)
        {
            if (objBuilder.CubeBlocks != null)
            {
                MySandboxGame.Log.WriteLine("Loading cube blocks");
                InitCubeBlocks(context, definitionSet.m_blockPositions, objBuilder.CubeBlocks);

                ToDefinitions(context, definitionSet.m_definitionsById, definitionSet.m_uniqueCubeBlocksBySize, objBuilder.CubeBlocks, failOnDebug);

                MySandboxGame.Log.WriteLine("Created block definitions");
                foreach (var size in definitionSet.m_uniqueCubeBlocksBySize)
                    PrepareBlockBlueprints(context, definitionSet.m_blueprintsById, size);
            }
        }

        void LoadPhase4(MyObjectBuilder_Definitions objBuilder, MyModContext context, DefinitionSet definitionSet, bool failOnDebug)
        {
            if (objBuilder.Prefabs != null)
            {
                if (MySandboxGame.Static != null)
                {
                    MySandboxGame.Log.WriteLine("Loading prefab: " + context.CurrentFile);
                    InitPrefabs(context, definitionSet.m_prefabs, objBuilder.Prefabs, failOnDebug);
                }
            }

            if (MyFakes.ENABLE_GENERATED_INTEGRITY_FIX)
            {
                foreach (var size in definitionSet.m_uniqueCubeBlocksBySize)
                    FixGeneratedBlocksIntegrity(size);
            }
        }

        void LoadPhase5(MyObjectBuilder_Definitions objBuilder, MyModContext context, DefinitionSet definitionSet, bool failOnDebug)
        {
            if (objBuilder.SpawnGroups != null)
            {
                if (MySandboxGame.Static != null)
                {
                    MySandboxGame.Log.WriteLine("Loading spawn groups");
                    InitSpawnGroups(context, definitionSet.m_spawnGroupDefinitions, definitionSet.m_definitionsById, objBuilder.SpawnGroups);
                }
            }

            if (objBuilder.RespawnShips != null)
            {
                if (MySandboxGame.Static != null)
                {
                    MySandboxGame.Log.WriteLine("Loading respawn ships");
                    InitRespawnShips(context, definitionSet.m_respawnShips, objBuilder.RespawnShips, failOnDebug);
                }
            }
        }

        private void LoadScenarios(MyModContext context, DefinitionSet definitionSet, bool failOnDebug = true)
        {
            var file = Path.Combine(context.ModPathData, "Scenarios.sbx");
            if (!MyFileSystem.FileExists(file))
                return;

            MyDataIntegrityChecker.HashInFile(file);
            var objBuilder = Load<MyObjectBuilder_ScenarioDefinitions>(file);
            if (objBuilder == null)
            {
                MyDefinitionErrors.Add(context, "Scenarios: Cannot load definition file, see log for details", TErrorSeverity.Error);
                return;
            }

            if (objBuilder.Scenarios != null)
            {
                MySandboxGame.Log.WriteLine("Loading scenarios");
                InitScenarioDefinitions(context, definitionSet.m_definitionsById, definitionSet.m_scenarioDefinitions, objBuilder.Scenarios, failOnDebug);
            }

            MergeDefinitions();
        }

        private void LoadPostProcess()
        {
            ProfilerShort.Begin("InitVoxelMaterials");
            InitVoxelMaterials();
            if (m_transparentMaterialsInitialized == false)
            {
                ProfilerShort.BeginNextBlock("CreateTransparentMaterials");
                CreateTransparentMaterials();
                m_transparentMaterialsInitialized = true;
            }
            ProfilerShort.BeginNextBlock("InitRopeDefinitions");
            InitRopeDefinitions();
            ProfilerShort.BeginNextBlock("InitBlockGroups");
            InitBlockGroups();
            ProfilerShort.BeginNextBlock("PostprocessComponentGroups");
            PostprocessComponentGroups();
            ProfilerShort.BeginNextBlock("PostprocessComponentBlocks");
            PostprocessComponentBlocks();
            ProfilerShort.BeginNextBlock("PostprocessBlueprints");
            PostprocessBlueprints();
            ProfilerShort.BeginNextBlock("AddEntriesToBlueprintClasses");
            AddEntriesToBlueprintClasses();
            ProfilerShort.BeginNextBlock("AddEntriesToEnvironmentItemClasses");
            AddEntriesToEnvironmentItemClasses();
            ProfilerShort.BeginNextBlock("PairPhysicalAndHandItems");
            PairPhysicalAndHandItems();
            ProfilerShort.BeginNextBlock("CheckWeaponRelatedDefinitions");
            CheckWeaponRelatedDefinitions();
            ProfilerShort.BeginNextBlock("UpdateShipSounds");
            SetShipSoundSystem();
            ProfilerShort.BeginNextBlock("MoveNonPublicBlocksToSpecialCategory");
            MoveNonPublicBlocksToSpecialCategory();
            if (MyAudio.Static != null)
            {
                ProfilerShort.BeginNextBlock("MyAudio.Static.ReloadData");
                MyAudio.Static.ReloadData(MyAudioExtensions.GetSoundDataFromDefinitions(), MyAudioExtensions.GetEffectData());
            }
            ProfilerShort.BeginNextBlock("PostprocessPirateAntennas");
            PostprocessPirateAntennas();
            ProfilerShort.BeginNextBlock("InitMultiBlockDefinitions");
            InitMultiBlockDefinitions();
            ProfilerShort.BeginNextBlock("CreateMapMultiBlockDefinitionToBlockDefinition");
            CreateMapMultiBlockDefinitionToBlockDefinition();
            ProfilerShort.BeginNextBlock("PostprocessAllDefinitions");
            PostprocessAllDefinitions();

            ProfilerShort.BeginNextBlock("AfterPostprocess");
            AfterPostprocess();
            ProfilerShort.End();
        }

        private void PostprocessAllDefinitions()
        {
            foreach (var definition in m_definitions.m_definitionsById.Values)
            {
                definition.Postprocess();
            }
        }

        private void AfterPostprocess()
        {
            foreach (var postprocessor in m_postProcessors)
            {
                Dictionary<MyStringHash, MyDefinitionBase> defs;
                if (m_definitions.Definitions.TryGetValue(postprocessor.DefinitionType, out defs))
                    postprocessor.AfterPostprocess(m_definitions, defs);
            }
        }

        private void InitMultiBlockDefinitions()
        {
            if (MyFakes.ENABLE_MULTIBLOCKS)
            {
                // Initialize min, max values for blocks in multiblocks
                var multiBlockDefinitions = GetMultiBlockDefinitions();
                foreach (var multiBlockDef in multiBlockDefinitions)
                {
                    multiBlockDef.Min = Vector3I.MaxValue;
                    multiBlockDef.Max = Vector3I.MinValue;

                    foreach (var partDef in multiBlockDef.BlockDefinitions)
                    {
                        MyCubeBlockDefinition blockDefinition;
                        if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(partDef.Id, out blockDefinition) && blockDefinition != null)
                        {
                            MatrixI blockRotation = new MatrixI(partDef.Forward, partDef.Up);
                            Vector3I sizeRotated = Vector3I.Abs(Vector3I.TransformNormal(blockDefinition.Size - Vector3I.One, ref blockRotation));
                            partDef.Max = partDef.Min + sizeRotated;

                            multiBlockDef.Min = Vector3I.Min(multiBlockDef.Min, partDef.Min);
                            multiBlockDef.Max = Vector3I.Max(multiBlockDef.Max, partDef.Max);
                        }
                    }

                    Debug.Assert(multiBlockDef.Min != Vector3I.MaxValue);
                }
            }
        }

        private void CreateMapMultiBlockDefinitionToBlockDefinition()
        {
            if (MyFakes.ENABLE_MULTIBLOCKS)
            {
                var multiBlockDefinitions = GetMultiBlockDefinitions();
                var blockDefinitions = m_definitions.m_definitionsById.Values.OfType<MyCubeBlockDefinition>().ToList();

                foreach (var multiBlockDef in multiBlockDefinitions)
                {
                    foreach (var blockDef in blockDefinitions)
                    {
                        if (blockDef.MultiBlock == multiBlockDef.Id.SubtypeName)
                        {
                            if (!m_definitions.m_mapMultiBlockDefToCubeBlockDef.ContainsKey(multiBlockDef.Id.SubtypeName))
                                m_definitions.m_mapMultiBlockDefToCubeBlockDef.Add(multiBlockDef.Id.SubtypeName, blockDef);
                            else
                                Debug.Fail("Multiblock definition used in more than one block definition!");

                            break;
                        }
                    }
                }
            }
        }

        public MyCubeBlockDefinition GetCubeBlockDefinitionForMultiBlock(string multiBlock)
        {
            MyCubeBlockDefinition def;
            if (m_definitions.m_mapMultiBlockDefToCubeBlockDef.TryGetValue(multiBlock, out def))
                return def;

            return null;
        }

        private void PostprocessPirateAntennas()
        {
            foreach (var antennaDefinition in m_definitions.m_pirateAntennaDefinitions.Values)
            {
                antennaDefinition.Postprocess();
            }
        }

        private void MoveNonPublicBlocksToSpecialCategory()
        {
            if (MyFakes.ENABLE_NON_PUBLIC_BLOCKS)
            {
                var category = new MyGuiBlockCategoryDefinition()
                {
                    DescriptionString = "Non public blocks",
                    DisplayNameString = "Non public",
                    Enabled = true,
                    Id = new MyDefinitionId(typeof(MyObjectBuilder_GuiBlockCategoryDefinition)),
                    IsBlockCategory = true,
                    IsShipCategory = false,
                    Name = "Non public",
                    Public = true,
                    SearchBlocks = true,
                    ShowAnimations = false,
                    ItemIds = new HashSet<string>(),
                };

                foreach (var block in GetDefinitionPairNames())
                {
                    var group = MyDefinitionManager.Static.GetDefinitionGroup(block);

                    category.ItemIds.Add(group.Any.Id.ToString());
                }

                m_definitions.m_categories.Add("NonPublic", category);
            }
        }

        private void PairPhysicalAndHandItems()
        {
            // Bind together physical items (objects in inventory) and hand items (objects held in hand)
            foreach (var entry in m_definitions.m_handItemsById)
            {
                var handItem = entry.Value;
                var physicalItem = GetPhysicalItemDefinition(handItem.PhysicalItemId);
                Check(!m_definitions.m_physicalItemsByHandItemId.ContainsKey(handItem.Id), handItem.Id);
                Check(!m_definitions.m_handItemsByPhysicalItemId.ContainsKey(physicalItem.Id), physicalItem.Id);
                m_definitions.m_physicalItemsByHandItemId[handItem.Id] = physicalItem;
                m_definitions.m_handItemsByPhysicalItemId[physicalItem.Id] = handItem;
            }
        }

        private void CheckWeaponRelatedDefinitions()
        {
            foreach (var weaponEntry in m_definitions.m_weaponDefinitionsById.Values)
            {
                foreach (var ammoMagazineEntry in weaponEntry.AmmoMagazinesId)
                {
                    Check(m_definitions.m_definitionsById.ContainsKey(ammoMagazineEntry), ammoMagazineEntry, true, UNKNOWN_ENTRY_MESSAGE);
                    var ammoMagazine = GetAmmoMagazineDefinition(ammoMagazineEntry);
                    Check(m_definitions.m_ammoDefinitionsById.ContainsKey(ammoMagazine.AmmoDefinitionId), ammoMagazine.AmmoDefinitionId, true, UNKNOWN_ENTRY_MESSAGE);
                    var ammoDefinition = GetAmmoDefinition(ammoMagazine.AmmoDefinitionId);
                    if (!weaponEntry.HasSpecificAmmoData(ammoDefinition))
                    {
                        StringBuilder sb = new StringBuilder("Weapon definition lacks ammo data properties for given ammo definition: ");
                        sb.Append(ammoDefinition.Id.SubtypeName);
                        MyDefinitionErrors.Add(weaponEntry.Context, sb.ToString(), TErrorSeverity.Critical);
                    }
                }
            }
        }

        private void PostprocessComponentGroups()
        {
            foreach (var entry in m_definitions.m_componentGroups)
            {
                MyComponentGroupDefinition group = entry.Value;

                group.Postprocess();
                if (group.IsValid)
                {
                    int maxAmount = group.GetComponentNumber();

                    for (int i = 1; i <= maxAmount; ++i)
                    {
                        MyComponentDefinition component = group.GetComponentDefinition(i);
                        m_definitions.m_componentGroupMembers.Add(component.Id, new MyTuple<int, MyComponentGroupDefinition>(i, group));
                    }
                }
            }
        }

        private void PostprocessComponentBlocks()
        {
            foreach (var entry in m_definitions.m_componentBlockEntries)
            {
                if (!entry.Enabled) continue;

                var type = MyObjectBuilderType.Parse(entry.Type);
                MyDefinitionId blockDefinitionId = new MyDefinitionId(type, entry.Subtype);
                m_definitions.m_componentBlocks.Add(blockDefinitionId);

                if (entry.Main)
                {
                    MyCubeBlockDefinition blockDef = null;
                    TryGetCubeBlockDefinition(blockDefinitionId, out blockDef);

                    if (blockDef.Components.Length == 1 && blockDef.Components[0].Count == 1)
                    {
                        m_definitions.m_componentIdToBlock[blockDef.Components[0].Definition.Id] = blockDef;
                    }
                }
            }

            m_definitions.m_componentBlockEntries.Clear();
        }

        private void PostprocessBlueprints()
        {
            CachingList<MyBlueprintDefinitionBase> blueprintsToPostprocess = new CachingList<MyBlueprintDefinitionBase>();

            // Cyclically postprocess all blueprints over and over as long as the list changes.
            // This is because the blueprint postprocess could depend on other blueprint being postprocessed.
            foreach (var entry in m_definitions.m_blueprintsById)
            {
                var blueprint = entry.Value;
                if (blueprint.PostprocessNeeded) blueprintsToPostprocess.Add(blueprint);
            }
            blueprintsToPostprocess.ApplyAdditions();

            int prevCount = -1;
            while (blueprintsToPostprocess.Count != 0 && blueprintsToPostprocess.Count != prevCount)
            {
                prevCount = blueprintsToPostprocess.Count;
                foreach (var blueprint in blueprintsToPostprocess)
                {
                    if (blueprint is MyCompositeBlueprintDefinition) { }
                    blueprint.Postprocess();
                    if (!blueprint.PostprocessNeeded) blueprintsToPostprocess.Remove(blueprint);
                }
                blueprintsToPostprocess.ApplyRemovals();
            }

            if (blueprintsToPostprocess.Count != 0)
            {
                StringBuilder sb = new StringBuilder("Following blueprints could not be post-processed: ");
                foreach (var blueprint in blueprintsToPostprocess)
                {
                    sb.Append(blueprint.Id.ToString());
                    sb.Append(", ");
                }
                MyDefinitionErrors.Add(MyModContext.BaseGame, sb.ToString(), TErrorSeverity.Error);
            }
        }

        private void AddEntriesToBlueprintClasses()
        {
            foreach (var entry in m_definitions.m_blueprintClassEntries)
            {
                if (!entry.Enabled) continue;

                MyBlueprintClassDefinition blueprintClass = null;
                MyBlueprintDefinitionBase blueprint = null;

                MyDefinitionId classId = new MyDefinitionId(typeof(MyObjectBuilder_BlueprintClassDefinition), entry.Class);
                m_definitions.m_blueprintClasses.TryGetValue(classId, out blueprintClass);

                blueprint = FindBlueprintByClassEntry(entry);

                // Block blueprint can be null if the corresponding block is not public.
                // We don't want to have to hide both the block and it's BP class entry
                if (blueprint == null) continue;

                Debug.Assert(blueprintClass != null);
                if (blueprintClass == null) continue;

                blueprintClass.AddBlueprint(blueprint);
            }
            m_definitions.m_blueprintClassEntries.Clear();

            // Production block inventory constraints need to be initialized here, after the blueprint classes are finished
            foreach (var entry in m_definitions.m_definitionsById)
            {
                var definition = entry.Value as MyProductionBlockDefinition;
                if (definition != null)
                {
                    definition.LoadPostProcess();
                }
            }
        }

        private MyBlueprintDefinitionBase FindBlueprintByClassEntry(BlueprintClassEntry blueprintClassEntry)
        {
            if (blueprintClassEntry.TypeId.IsNull)
            {
                MyBlueprintDefinitionBase blueprint = null;
                MyDefinitionId blueprintDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_BlueprintDefinition), blueprintClassEntry.BlueprintSubtypeId);
                m_definitions.m_blueprintsById.TryGetValue(blueprintDefinitionId, out blueprint);
                if (blueprint == null)
                {
                    blueprintDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_CompositeBlueprintDefinition), blueprintClassEntry.BlueprintSubtypeId);
                    m_definitions.m_blueprintsById.TryGetValue(blueprintDefinitionId, out blueprint);
                }
                return blueprint;
            }
            else
            {
                MyDefinitionId blueprintDefinitionId = new MyDefinitionId(blueprintClassEntry.TypeId, blueprintClassEntry.BlueprintSubtypeId);
                return GetBlueprintDefinition(blueprintDefinitionId);
            }
        }

        private void AddEntriesToEnvironmentItemClasses()
        {
            foreach (var entry in m_definitions.m_environmentItemsEntries)
            {
                if (!entry.Enabled) continue;

                MyEnvironmentItemsDefinition itemsDefinition = null;
                MyEnvironmentItemDefinition itemDefinition = null;

                MyDefinitionId classId = new MyDefinitionId(MyObjectBuilderType.Parse(entry.Type), entry.Subtype);
                if (!TryGetDefinition<MyEnvironmentItemsDefinition>(classId, out itemsDefinition))
                {
                    string errorString = "Environment items definition " + classId.ToString() + " not found!";
                    MyDefinitionErrors.Add(MyModContext.BaseGame, errorString, TErrorSeverity.Warning);
                    Debug.Assert(false, errorString);
                    continue;
                }

                itemDefinition = FindEnvironmentItemByEntry(itemsDefinition, entry);

                // Item definition can be null if it is not public.
                // We don't want to have to hide both the item's definition and it's class entry
                if (itemDefinition == null) continue;

                var subtypeId = MyStringHash.GetOrCompute(entry.ItemSubtype);
                itemsDefinition.AddItemDefinition(subtypeId, entry.Frequency, false);
            }

            // Compute all frequencies at the end.
            foreach (var def in GetDefinitionsOfType<MyEnvironmentItemsDefinition>())
            {
                def.RecomputeFrequencies();
            }

            m_definitions.m_environmentItemsEntries.Clear();
        }

        private MyEnvironmentItemDefinition FindEnvironmentItemByEntry(MyEnvironmentItemsDefinition itemsDefinition, EnvironmentItemsEntry envItemEntry)
        {
            MyDefinitionId definitionId = new MyDefinitionId(itemsDefinition.ItemDefinitionType, envItemEntry.ItemSubtype);
            MyEnvironmentItemDefinition envItemDef = null;
            TryGetDefinition<MyEnvironmentItemDefinition>(definitionId, out envItemDef);
            return envItemDef;
        }

        private void InitBlockGroups()
        {
            m_definitions.m_blockGroups = new Dictionary<string, MyCubeBlockDefinitionGroup>();
            for (int i = 0; i < m_definitions.m_cubeSizes.Length; ++i)
            {
                foreach (var entry in m_definitions.m_uniqueCubeBlocksBySize[i])
                {
                    var block = entry.Value;
                    MyCubeBlockDefinitionGroup group = null;
                    if (!m_definitions.m_blockGroups.TryGetValue(block.BlockPairName, out group))
                    {
                        group = new MyCubeBlockDefinitionGroup();
                        m_definitions.m_blockGroups.Add(block.BlockPairName, group);
                    }
                    group[(MyCubeSize)i] = block;
                }
            }
        }

        public void InitVoxelMaterials()
        {
            MyRenderVoxelMaterialData[] renderMaterials = new MyRenderVoxelMaterialData[m_definitions.m_voxelMaterialsByName.Count];

            MyVoxelMaterialDefinition.ResetIndexing();

            int i = 0;
            foreach (var entry in m_definitions.m_voxelMaterialsByName)
            {
                var material = entry.Value;
                material.AssignIndex();
                m_definitions.m_voxelMaterialsByIndex[material.Index] = material;

                if (material.IsRare)
                    ++m_definitions.m_voxelMaterialRareCount;

                // Create render materials for loaded materials.
                material.CreateRenderData(out renderMaterials[i++]);
            }

            MyRenderProxy.CreateRenderVoxelMaterials(renderMaterials);
        }

        public void UpdateVoxelMaterial(MyVoxelMaterialDefinition material)
        {
            MyRenderVoxelMaterialData[] renderMaterials = new MyRenderVoxelMaterialData[1];

            // Create render materials for loaded materials.
            material.CreateRenderData(out renderMaterials[0]);

            MyRenderProxy.UpdateRenderVoxelMaterials(renderMaterials);
        }

        private MyVoxelMaterialDefinition[] LoadVoxelMaterials(string path, MyModContext context)
        {
            var objBuilder = Load<MyObjectBuilder_Definitions>(path);
            var res = new Medieval.Definitions.MyDx11VoxelMaterialDefinition[objBuilder.VoxelMaterials.Length];

            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = new Medieval.Definitions.MyDx11VoxelMaterialDefinition();
                res[i].Init(objBuilder.VoxelMaterials[i], context);
            }
            return res;
        }

        private FastResourceLock m_voxelMaterialsLock = new FastResourceLock();

        public void ReloadVoxelMaterials()
        {
            using (m_voxelMaterialsLock.AcquireExclusiveUsing())
            {
                MyModContext context = MyModContext.BaseGame;

                MyVoxelMaterialDefinition.ResetIndexing();

                // Load hand items (auto rifle, drill..)
                MySandboxGame.Log.WriteLine("ReloadVoxelMaterials");
                var path = Path.Combine(context.ModPathData, "VoxelMaterials.sbc");
                var voxelMaterials = LoadVoxelMaterials(path, context);
                if (m_definitions.m_voxelMaterialsByIndex == null)
                    m_definitions.m_voxelMaterialsByIndex = new Dictionary<byte, MyVoxelMaterialDefinition>();
                else
                    m_definitions.m_voxelMaterialsByIndex.Clear();

                if (m_definitions.m_voxelMaterialsByName == null)
                    m_definitions.m_voxelMaterialsByName = new Dictionary<string, MyVoxelMaterialDefinition>();
                else
                    m_definitions.m_voxelMaterialsByName.Clear();

                foreach (var item in voxelMaterials)
                {
                    item.AssignIndex();
                    m_definitions.m_voxelMaterialsByIndex[item.Index] = item;
                    m_definitions.m_voxelMaterialsByName[item.Id.SubtypeName] = item;
                }
            }
        }

        private void InitRopeDefinitions()
        {
            var allDefinitions = GetAllDefinitions();
            foreach (var definition in allDefinitions)
            {
                var ropeDefinition = definition as MyRopeDefinition;
                if (ropeDefinition != null)
                {
                    m_definitions.m_idToRope.Add(ropeDefinition.Id, ropeDefinition);
                }
            }
        }

        public void UnloadData()
        {
            MyDebug.AssertDebug(MyDefinitionManager.Static == this);

            m_modDefinitionSets.Clear();
            m_definitions.Clear(true);
            m_definitions.m_channelEnvironmentItemsDefs.Clear();
        }

        // Deserialize from XML.
        private T Load<T>(string path) where T : MyObjectBuilder_Base
        {
            T result = null;
            MyObjectBuilderSerializer.DeserializeXML<T>(path, out result);
            return result;
        }

        // Deserialize from XML. Parameter useType should be same as T or its subclass.
        private T Load<T>(string path, Type useType) where T : MyObjectBuilder_Base
        {
            // Deserialize
            MyObjectBuilder_Base result = null;
            MyObjectBuilderSerializer.DeserializeXML(path, out result, useType);
            // Check type and return.
            if (result == null)
                return null;
            Debug.Assert(result is T, "Definition Manager: Loaded type does not match T.");
            return result as T;
        }

        private static void InitAmmoMagazines(MyModContext context,
            DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_AmmoMagazineDefinition[] magazines, bool failOnDebug = true)
        {
            var res = new MyAmmoMagazineDefinition[magazines.Length];

            for (int i = 0; i < magazines.Length; ++i)
            {
                res[i] = InitDefinition<MyAmmoMagazineDefinition>(context, magazines[i]);

                Check(res[i].Id.TypeId == typeof(MyObjectBuilder_AmmoMagazine), res[i].Id.TypeId, failOnDebug, UNKNOWN_ENTRY_MESSAGE);
                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private static void InitShipSounds(MyModContext context,
            DefinitionDictionary<MyShipSoundsDefinition> output, MyObjectBuilder_ShipSoundsDefinition[] shipGroups, bool failOnDebug = true)
        {
            var res = new MyShipSoundsDefinition[shipGroups.Length];

            for (int i = 0; i < shipGroups.Length; ++i)
            {
                res[i] = InitDefinition<MyShipSoundsDefinition>(context, shipGroups[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private static void InitShipSoundSystem(MyModContext context,
            ref MyShipSoundSystemDefinition output, MyObjectBuilder_ShipSoundSystemDefinition shipSystem, bool failOnDebug = true)
        {
            var res = InitDefinition<MyShipSoundSystemDefinition>(context, shipSystem);
            output = res;
        }

        public void SetShipSoundSystem()
        {
            MyShipSoundComponent.ClearShipSounds();
            foreach (DefinitionSet LoadingSet in m_modDefinitionSets.Values)
            {
                if (LoadingSet.m_shipSounds != null)
                {
                    foreach (var set in LoadingSet.m_shipSounds)
                        MyShipSoundComponent.AddShipSounds(set.Value);
                }
                if (LoadingSet.m_shipSoundSystem != null)
                    MyShipSoundComponent.SetDefinition(LoadingSet.m_shipSoundSystem);
            }
            MyShipSoundComponent.ActualizeGroups();
        }

        private static void InitAnimations(MyModContext context,
            DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_AnimationDefinition[] animations, Dictionary<string, Dictionary<string, MyAnimationDefinition>> animationsBySkeletonType, bool failOnDebug = true)
        {
            var res = new MyAnimationDefinition[animations.Length];

            for (int i = 0; i < animations.Length; ++i)
            {
                res[i] = InitDefinition<MyAnimationDefinition>(context, animations[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];

                if (context.IsBaseGame && !MyFinalBuildConstants.IS_OFFICIAL)
                    Static.m_currentLoadingSet.AddDefinition(res[i]); // compatibility
                else
                    Static.m_currentLoadingSet.AddOrRelaceDefinition(res[i]);
            }

            foreach (var animationDefinition in res)
            {
                foreach (var skeletonType in animationDefinition.SupportedSkeletons)
                {
                    if (!animationsBySkeletonType.ContainsKey(skeletonType))
                        animationsBySkeletonType.Add(skeletonType, new Dictionary<string, MyAnimationDefinition>());

                    animationsBySkeletonType[skeletonType][animationDefinition.Id.SubtypeName] = animationDefinition;
                }
            }
        }

        private static void InitDebris(MyModContext context, DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_DebrisDefinition[] debris, bool failOnDebug = true)
        {
            var res = new MyDebrisDefinition[debris.Length];

            for (int i = 0; i < debris.Length; ++i)
            {
                res[i] = InitDefinition<MyDebrisDefinition>(context, debris[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private static void InitEdges(MyModContext context, DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_EdgesDefinition[] edges, bool failOnDebug = true)
        {
            var res = new MyEdgesDefinition[edges.Length];

            for (int i = 0; i < edges.Length; ++i)
            {
                res[i] = InitDefinition<MyEdgesDefinition>(context, edges[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private void InitBlockPositions(Dictionary<string, Vector2I> outputBlockPositions, MyBlockPosition[] positions, bool failOnDebug = true)
        {
            foreach (var blockPos in positions)
            {
                Check(!outputBlockPositions.ContainsKey(blockPos.Name), blockPos.Name, failOnDebug);
                outputBlockPositions[blockPos.Name] = blockPos.Position;
            }
        }

        private void InitCategoryClasses(MyModContext context, List<MyGuiBlockCategoryDefinition> categories, MyObjectBuilder_GuiBlockCategoryDefinition[] classes, bool failOnDebug = true)
        {
            foreach (var classDef in classes)
            {
                if (classDef.Public || MyFakes.ENABLE_NON_PUBLIC_CATEGORY_CLASSES)
                {
                    var newClass = InitDefinition<MyGuiBlockCategoryDefinition>(context, classDef);
                    categories.Add(newClass);
                }
            }
        }

        private void InitSounds(MyModContext context, DefinitionDictionary<MyAudioDefinition> output, MyObjectBuilder_AudioDefinition[] classes, bool failOnDebug = true)
        {
            foreach (var classDef in classes)
            {
                output[classDef.Id] = InitDefinition<MyAudioDefinition>(context, classDef);
            }
        }

        private void InitParticleEffects(MyModContext context, DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_ParticleEffect[] classes, bool failOnDebug = true)
        {
            if (m_transparentMaterialsInitialized == false)
            {
                ProfilerShort.Begin("CreateTransparentMaterials");
                CreateTransparentMaterials();
                ProfilerShort.End();
                m_transparentMaterialsInitialized = true;
            }

            foreach (var classDef in classes)
            {
                MyParticleEffect effect = MyParticlesManager.EffectsPool.Allocate();
                effect.DeserializeFromObjectBuilder(classDef);
                MyParticlesLibrary.AddParticleEffect(effect);
                //output is not used
            }
        }

        private void InitSoundCategories(MyModContext context, DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_SoundCategoryDefinition[] categories, bool failOnDebug = true)
        {
            foreach (var soundCategory in categories)
            {
                var newCategory = InitDefinition<MySoundCategoryDefinition>(context, soundCategory);
                Check(!output.ContainsKey(soundCategory.Id), soundCategory.Id, failOnDebug);
                output[soundCategory.Id] = newCategory;
            }
        }

        private void InitLCDTextureCategories(MyModContext context, DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_LCDTextureDefinition[] categories, bool failOnDebug = true)
        {
            foreach (var LCDTextureCategory in categories)
            {
                var newCategory = InitDefinition<MyLCDTextureDefinition>(context, LCDTextureCategory);
                Check(!output.ContainsKey(LCDTextureCategory.Id), LCDTextureCategory.Id, failOnDebug);
                output[LCDTextureCategory.Id] = newCategory;
            }
        }
        private void InitBlueprintClasses(MyModContext context,
            DefinitionDictionary<MyBlueprintClassDefinition> output, MyObjectBuilder_BlueprintClassDefinition[] classes, bool failOnDebug = true)
        {
            foreach (var classDef in classes)
            {
                var newClass = InitDefinition<MyBlueprintClassDefinition>(context, classDef);
                Check(!output.ContainsKey(classDef.Id), classDef.Id, failOnDebug);
                output[classDef.Id] = newClass;
            }
        }

        private void InitBlueprintClassEntries(MyModContext context, HashSet<BlueprintClassEntry> output, BlueprintClassEntry[] entries, bool failOnDebug = true)
        {
            foreach (var entry in entries)
            {
                Check(!output.Contains(entry), entry, failOnDebug);
                output.Add(entry);
            }
        }

        private void InitEnvironmentItemsEntries(MyModContext context, HashSet<EnvironmentItemsEntry> output, EnvironmentItemsEntry[] entries, bool failOnDebug = true)
        {
            foreach (var entry in entries)
            {
                Check(!output.Contains(entry), entry, failOnDebug);
                output.Add(entry);
            }
        }

        private void InitBlueprints(MyModContext context,
            Dictionary<MyDefinitionId, MyBlueprintDefinitionBase> output,
            DefinitionDictionary<MyBlueprintDefinitionBase> blueprintsByResult,
            MyObjectBuilder_BlueprintDefinition[] blueprints, bool failOnDebug = true)
        {
            for (int i = 0; i < blueprints.Length; ++i)
            {
                var blueprint = InitDefinition<MyBlueprintDefinitionBase>(context, blueprints[i]);
                Check(!output.ContainsKey(blueprint.Id), blueprint.Id, failOnDebug);
                output[blueprint.Id] = blueprint;
                if (blueprint.Results.Length == 1)
                {
                    blueprintsByResult[blueprint.Results[0].Id] = blueprint;
                }
            }
        }

        private void InitFonts(MyModContext context,
            Dictionary<MyDefinitionId, MyFontDefinition> output,
            MyObjectBuilder_FontDefinition[] fonts, bool failOnDebug = true)
        {
            for (int i = 0; i < fonts.Length; ++i)
            {
                var font = InitDefinition<MyFontDefinition>(context, fonts[i]);
                Check(!output.ContainsKey(font.Id), font.Id, failOnDebug);
                output[font.Id] = font;
            }
        }

        private static void InitComponents(MyModContext context,
            DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_ComponentDefinition[] components, bool failOnDebug = true)
        {
            var res = new MyComponentDefinition[components.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyComponentDefinition>(context, components[i]);

                Check(res[i].Id.TypeId == typeof(MyObjectBuilder_Component), res[i].Id.TypeId, failOnDebug, UNKNOWN_ENTRY_MESSAGE);
                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];

                if (!context.IsBaseGame)
                    MySandboxGame.Log.WriteLine("Loaded component: " + res[i].Id);
            }
        }

        private void InitConfiguration(DefinitionSet definitionSet, MyObjectBuilder_Configuration configuration)
        {
            definitionSet.m_cubeSizes[(int)MyCubeSize.Small] = configuration.CubeSizes.Small;
            definitionSet.m_cubeSizes[(int)MyCubeSize.Large] = configuration.CubeSizes.Large;
            definitionSet.m_cubeSizesOriginal[(int)MyCubeSize.Small] = configuration.CubeSizes.SmallOriginal > 0 ? configuration.CubeSizes.SmallOriginal : configuration.CubeSizes.Small;
            definitionSet.m_cubeSizesOriginal[(int)MyCubeSize.Large] = configuration.CubeSizes.Large;

            for (int i = 0; i < 2; ++i)
            {
                bool creative = i == 0;
                MyObjectBuilder_Configuration.BaseBlockSettings baseBlockSettings = creative ? configuration.BaseBlockPrefabs : configuration.BaseBlockPrefabsSurvival;
                AddBasePrefabName(definitionSet, size: MyCubeSize.Small, isStatic: true, isCreative: creative, prefabName: baseBlockSettings.SmallStatic);
                AddBasePrefabName(definitionSet, size: MyCubeSize.Small, isStatic: false, isCreative: creative, prefabName: baseBlockSettings.SmallDynamic);
                AddBasePrefabName(definitionSet, size: MyCubeSize.Large, isStatic: true, isCreative: creative, prefabName: baseBlockSettings.LargeStatic);
                AddBasePrefabName(definitionSet, size: MyCubeSize.Large, isStatic: false, isCreative: creative, prefabName: baseBlockSettings.LargeDynamic);
            }

            if (configuration.LootBag != null)
            {
                definitionSet.m_lootBagDefinition = new MyLootBagDefinition();
                definitionSet.m_lootBagDefinition.Init(configuration.LootBag);
            }
        }

        private static void InitContainerTypes(MyModContext context,
            DefinitionDictionary<MyContainerTypeDefinition> output, MyObjectBuilder_ContainerTypeDefinition[] containers, bool failOnDebug = true)
        {
            foreach (var typeBuilder in containers)
            {
                Check(!output.ContainsKey(typeBuilder.Id), typeBuilder.Id, failOnDebug);
                MyContainerTypeDefinition typeDefinition = InitDefinition<MyContainerTypeDefinition>(context, typeBuilder);
                output[typeBuilder.Id] = typeDefinition;
            }
        }

        private static void InitCubeBlocks(MyModContext context, Dictionary<string, Vector2I> outputBlockPositions, MyObjectBuilder_CubeBlockDefinition[] cubeBlocks)
        {
            foreach (var block in cubeBlocks)
            {
                block.BlockPairName = block.BlockPairName ?? block.DisplayName;
                if (block.Components.Where((component) => component.Subtype == "Computer").Count() != 0)
                {
                    StringBuilder sb = new StringBuilder();
                    var blockType = MyCubeBlockFactory.GetProducedType(block.Id.TypeId);
                    if (!blockType.IsSubclassOf(typeof(MyTerminalBlock)) && blockType != typeof(MyTerminalBlock))
                        MyDefinitionErrors.Add(context, sb.AppendFormat(MySpaceTexts.DefinitionError_BlockWithComputerNotTerminalBlock, block.DisplayName).ToString(), TErrorSeverity.Error);
                }
            }
        }

        private static void InitWeapons(MyModContext context,
            DefinitionDictionary<MyWeaponDefinition> output, MyObjectBuilder_WeaponDefinition[] weapons, bool failOnDebug = true)
        {
            var res = new MyWeaponDefinition[weapons.Length];

            for (int i = 0; i < weapons.Length; ++i)
            {
                res[i] = InitDefinition<MyWeaponDefinition>(context, weapons[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private void InitGridCreators(MyModContext context, DefinitionDictionary<MyGridCreateToolDefinition> gridCreateDefinitions, DefinitionDictionary<MyDefinitionBase> definitionsById, MyObjectBuilder_GridCreateToolDefinition[] gridCreators, bool failOnDebug)
        {
            foreach (var creator in gridCreators)
            {
                if (gridCreateDefinitions.ContainsKey(creator.Id) && failOnDebug)
                {
                    Debug.Fail("Duplicate definition for grid creator " + creator.Id);
                }
                var def = InitDefinition<MyGridCreateToolDefinition>(context, creator);
                gridCreateDefinitions[creator.Id] = def;
                definitionsById[creator.Id] = def;
            }
        }

        private static void InitAmmos(MyModContext context, DefinitionDictionary<MyAmmoDefinition> output, MyObjectBuilder_AmmoDefinition[] ammos, bool failOnDebug = true)
        {
            var res = new MyAmmoDefinition[ammos.Length];

            for (int i = 0; i < ammos.Length; i++)
            {
                res[i] = InitDefinition<MyAmmoDefinition>(context, ammos[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }
        private void FixGeneratedBlocksIntegrity(DefinitionDictionary<MyCubeBlockDefinition> cubeBlocks)
        {
            foreach (var entry in cubeBlocks)
            {
                var block = entry.Value;
                if (block.GeneratedBlockDefinitions == null) continue;

                foreach (var gen in block.GeneratedBlockDefinitions)
                {
                    MyCubeBlockDefinition generatedBlock;
                    if (!TryGetCubeBlockDefinition(gen, out generatedBlock)) continue;
                    if (generatedBlock.GeneratedBlockType == MyStringId.GetOrCompute("pillar"))
                        continue;
                    generatedBlock.Components = block.Components;
                    generatedBlock.MaxIntegrity = block.MaxIntegrity;
                }
            }
        }

        private static void PrepareBlockBlueprints(MyModContext context,
            Dictionary<MyDefinitionId, MyBlueprintDefinitionBase> output, Dictionary<MyDefinitionId, MyCubeBlockDefinition> cubeBlocks, bool failOnDebug = true)
        {
            foreach (var entry in cubeBlocks)
            {
                var cubeBlock = entry.Value;

                if (!context.IsBaseGame)
                    MySandboxGame.Log.WriteLine("Loading cube block: " + entry.Key);

                if (!MyFakes.ENABLE_NON_PUBLIC_BLOCKS && cubeBlock.Public == false) continue;

                var uniqueCubeBlock = cubeBlock.UniqueVersion;
                Check(!output.ContainsKey(cubeBlock.Id), cubeBlock.Id, failOnDebug);
                if (output.ContainsKey(uniqueCubeBlock.Id))
                    continue;
                var definition = MakeBlueprintFromComponentStack(context, uniqueCubeBlock);
                if (definition != null)
                    output[definition.Id] = definition;
            }
        }

        private static void InitEnvironment(MyModContext context,
            DefinitionSet defSet, MyObjectBuilder_EnvironmentDefinition[] objBuilder, bool failOnDebug = true)
        {
            foreach (var ob in objBuilder)
            {
                var environmentDef = InitDefinition<MyEnvironmentDefinition>(context, ob);

                defSet.AddDefinition(environmentDef);
            }
        }

        private static void LoadDroneBehaviorPresets(MyModContext context, DefinitionSet defSet, MyObjectBuilder_DroneBehaviorDefinition[] objBuilder, bool failOnDebug = true)
        {
            foreach (var ob in objBuilder)
            {
                MySpaceStrafeData preset = new MySpaceStrafeData(ob);
                MySpaceStrafeDataStatic.SavePreset(ob.Id.SubtypeId, preset);
            }
        }

        public MyEnvironmentDefinition EnvironmentDefinition
        {
            get { return MySector.EnvironmentDefinition; }
        }

        private static void InitGlobalEvents(MyModContext context,
            DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_GlobalEventDefinition[] events, bool failOnDebug = true)
        {
            var definitions = new MyGlobalEventDefinition[events.Length];

            for (int i = 0; i < events.Length; ++i)
            {
                definitions[i] = InitDefinition<MyGlobalEventDefinition>(context, events[i]);

                Check(!output.ContainsKey(definitions[i].Id), definitions[i].Id, failOnDebug);
                output[definitions[i].Id] = definitions[i];
            }
        }

        private static void InitHandItems(MyModContext context,
            DefinitionDictionary<MyHandItemDefinition> output, MyObjectBuilder_HandItemDefinition[] items, bool failOnDebug = true)
        {
            MyHandItemDefinition[] res = new MyHandItemDefinition[items.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyHandItemDefinition>(context, items[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private static void InitVoxelHands(MyModContext context,
            DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_VoxelHandDefinition[] items, bool failOnDebug = true)
        {
            MyVoxelHandDefinition[] res = new MyVoxelHandDefinition[items.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyVoxelHandDefinition>(context, items[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private void InitPrefabThrowers(MyModContext context,
            DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_DefinitionBase[] items, bool failOnDebug)
        {
            MyPrefabThrowerDefinition[] res = new MyPrefabThrowerDefinition[items.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyPrefabThrowerDefinition>(context, items[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private void InitAIBehaviors(MyModContext context,
            DefinitionDictionary<MyBehaviorDefinition> output, MyObjectBuilder_DefinitionBase[] items, bool failOnDebug)
        {
            MyBehaviorDefinition[] res = new MyBehaviorDefinition[items.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyBehaviorDefinition>(context, items[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private void InitBots(MyModContext context, DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_BotDefinition[] bots, bool failOnDebug = true)
        {
            MyBotDefinition[] res = new MyBotDefinition[bots.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyBotDefinition>(context, bots[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private void InitBotCommands(MyModContext context, DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_AiCommandDefinition[] commands, bool failOnDebug = true)
        {
            MyAiCommandDefinition[] res = new MyAiCommandDefinition[commands.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyAiCommandDefinition>(context, commands[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private void InitNavigationDefinitions(MyModContext context, DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_BlockNavigationDefinition[] definitions, bool failOnDebug = true)
        {
            MyBlockNavigationDefinition[] res = new MyBlockNavigationDefinition[definitions.Length];
            for (int i = 0; i < definitions.Length; ++i)
            {
                res[i] = InitDefinition<MyBlockNavigationDefinition>(context, definitions[i]);

                Check(!output.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                output[res[i].Id] = res[i];
            }
        }

        private static void InitBattle(MyModContext context,
            ref MyBattleDefinition output, MyObjectBuilder_BattleDefinition objBuilder, bool failOnDebug = true)
        {
            var battleDef = InitDefinition<MyBattleDefinition>(context, objBuilder);
            output = battleDef;
        }

        private static void InitDestruction(MyModContext context,
            ref MyDestructionDefinition output, MyObjectBuilder_DestructionDefinition objBuilder, bool failOnDebug = true)
        {
            var destructionDef = InitDefinition<MyDestructionDefinition>(context, objBuilder);
            output = destructionDef;
        }

        private static void InitDecals(MyModContext context, MyObjectBuilder_DecalDefinition[] objBuilders, bool failOnDebug = true)
        {
            List<string> names = new List<string>();
            Dictionary<string, List<MyDecalMaterialDesc>> descriptions = new Dictionary<string, List<MyDecalMaterialDesc>>();
            MyDecalMaterials.ClearMaterials();
            foreach (var obj in objBuilders)
            {
                if (obj.MaxSize < obj.MinSize)
                    obj.MaxSize = obj.MinSize;

                MyDecalMaterial material = new MyDecalMaterial(obj.Material, obj.Transparent,
                    MyStringHash.GetOrCompute(obj.Target), MyStringHash.GetOrCompute(obj.Source),
                    obj.MinSize, obj.MaxSize, obj.Depth, obj.Rotation);

                List<MyDecalMaterialDesc> list;
                bool found = descriptions.TryGetValue(material.StringId, out list);
                if (!found)
                {
                    list = new List<MyDecalMaterialDesc>();
                    descriptions[material.StringId] = list;
                }

                list.Add(obj.Material);
                MyDecalMaterials.AddDecalMaterial(material);
            }

            VRageRender.MyRenderProxy.RegisterDecals(descriptions);
        }

        private static void InitDecalGlobals(MyModContext context, MyObjectBuilder_DecalGlobalsDefinition objBuilder, bool failOnDebug = true)
        {
            MyDecalGlobals globals = new MyDecalGlobals();
            globals.DecalQueueSize = objBuilder.DecalQueueSize;
            VRageRender.MyRenderProxy.SetDecalGlobals(globals);
        }

        private static void InitShadowTextureSets(MyModContext context, MyObjectBuilder_ShadowTextureSetDefinition[] objBuilders, bool failOnDebug = true)
        {
            MyGuiTextShadows.ClearShadowTextures();
            foreach (var obj in objBuilders)
            {
                List<ShadowTexture> textures = new List<ShadowTexture>();
                foreach (var texture in obj.ShadowTextures)
                    textures.Add(new ShadowTexture(texture.Texture, texture.MinWidth, texture.GrowFactorWidth, texture.GrowFactorHeight, texture.DefaultAlpha));

                MyGuiTextShadows.AddTextureSet(obj.Id.SubtypeName, textures);
            }
        }

        public void SetDefaultNavDef(MyCubeBlockDefinition blockDefinition)
        {
            var ob = MyBlockNavigationDefinition.GetDefaultObjectBuilder(blockDefinition);

            MyBlockNavigationDefinition def;
            TryGetDefinition(ob.Id, out def);

            if (def != null)
            {
                blockDefinition.NavigationDefinition = def;
                return;
            }

            MyBlockNavigationDefinition.CreateDefaultTriangles(ob);

            var navigationDefinition = InitDefinition<MyBlockNavigationDefinition>(blockDefinition.Context, ob);

            Check(!m_definitions.m_definitionsById.ContainsKey(ob.Id), ob.Id);
            m_definitions.m_definitionsById[ob.Id] = navigationDefinition;

            blockDefinition.NavigationDefinition = navigationDefinition;
        }

        private void InitVoxelMapStorages(MyModContext context,
            Dictionary<string, MyVoxelMapStorageDefinition> output, MyObjectBuilder_VoxelMapStorageDefinition[] items, bool failOnDebug)
        {
            foreach (var voxelMapStorage in items)
            {
                var definition = InitDefinition<MyVoxelMapStorageDefinition>(context, voxelMapStorage);
                if (definition.StorageFile != null)
                {
                    var id = definition.Id.SubtypeName;
                    Check(!output.ContainsKey(id), id, failOnDebug);
                    output[id] = definition;
                }
            }
        }

        private MyHandItemDefinition[] LoadHandItems(string path, MyModContext context)
        {
            var objBuilder = Load<MyObjectBuilder_Definitions>(path);
            MyHandItemDefinition[] res = new MyHandItemDefinition[objBuilder.HandItems.Length];

            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = new MyHandItemDefinition();
                res[i].Init(objBuilder.HandItems[i], context);
            }
            return res;
        }

        public void ReloadHandItems()
        {
            MyModContext context = MyModContext.BaseGame;

            // Load hand items (auto rifle, drill..)
            MySandboxGame.Log.WriteLine("Loading hand items");
            var path = Path.Combine(context.ModPathData, "HandItems.sbc");
            var handItems = LoadHandItems(path, context);
            if (m_definitions.m_handItemsById == null)
                m_definitions.m_handItemsById = new DefinitionDictionary<MyHandItemDefinition>(handItems.Length);
            else
                m_definitions.m_handItemsById.Clear();
            foreach (var item in handItems)
            {
                MyDebug.AssertDebug(!m_definitions.m_handItemsById.ContainsKey(item.Id));
                m_definitions.m_handItemsById[item.Id] = item;
            }
        }

        public void ReloadParticles()
        {
            MyModContext context = MyModContext.BaseGame;

            MySandboxGame.Log.WriteLine("Loading particles");
            var path = Path.Combine(context.ModPathData, "Particles.sbc");

            if (m_transparentMaterialsInitialized == false)
            {
                ProfilerShort.Begin("CreateTransparentMaterials");
                CreateTransparentMaterials();
                m_transparentMaterialsInitialized = true;
            }

            var objBuilder = Load<MyObjectBuilder_Definitions>(path);

            MyParticlesLibrary.Close();

            foreach (var classDef in objBuilder.ParticleEffects)
            {
                MyParticleEffect effect = MyParticlesManager.EffectsPool.Allocate();
                effect.DeserializeFromObjectBuilder(classDef);
                MyParticlesLibrary.AddParticleEffect(effect);
            }
        }

        public void SaveHandItems()
        {
            var objBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Definitions>();
            List<MyObjectBuilder_HandItemDefinition> defList = new List<MyObjectBuilder_HandItemDefinition>();

            foreach (var handDef in m_definitions.m_handItemsById.Values)
            {
                MyObjectBuilder_HandItemDefinition ob = (MyObjectBuilder_HandItemDefinition)handDef.GetObjectBuilder();
                defList.Add(ob);
            }

            objBuilder.HandItems = defList.ToArray();

            string filepath = Path.Combine(MyFileSystem.ContentPath, "Data", "HandItems.sbc");
            objBuilder.Save(filepath);
        }

        private static void InitPhysicalItems(MyModContext context,
            DefinitionDictionary<MyDefinitionBase> outputDefinitions, List<MyPhysicalItemDefinition> outputWeapons,
            MyObjectBuilder_PhysicalItemDefinition[] items, bool failOnDebug = true)
        {
            MyPhysicalItemDefinition[] res = new MyPhysicalItemDefinition[items.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyPhysicalItemDefinition>(context, items[i]);

                Check(!outputDefinitions.ContainsKey(res[i].Id), res[i].Id, failOnDebug);

                if (res[i].Id.TypeId == typeof(MyObjectBuilder_PhysicalGunObject))
                {
                    outputWeapons.Add(res[i]);
                }
                outputDefinitions[res[i].Id] = res[i];
            }
        }

        private static void InitScenarioDefinitions(MyModContext context,
            DefinitionDictionary<MyDefinitionBase> outputDefinitions, List<MyScenarioDefinition> outputScenarios,
            MyObjectBuilder_ScenarioDefinition[] scenarios, bool failOnDebug = true)
        {
            var res = new MyScenarioDefinition[scenarios.Length];

            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyScenarioDefinition>(context, scenarios[i]);

                outputScenarios.Add(res[i]);
                Check(!outputDefinitions.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                outputDefinitions[res[i].Id] = res[i];
            }
        }

        private static void InitSpawnGroups(
            MyModContext context,
            List<MySpawnGroupDefinition> outputSpawnGroups,
            DefinitionDictionary<MyDefinitionBase> outputDefinitions,
            MyObjectBuilder_SpawnGroupDefinition[] spawnGroups)
        {
            foreach (var groupBuilder in spawnGroups)
            {
                var groupDefinition = InitDefinition<MySpawnGroupDefinition>(context, groupBuilder);
                groupDefinition.Init(groupBuilder, context);
                if (groupDefinition.IsValid)
                {
                    outputSpawnGroups.Add(groupDefinition);
                    outputDefinitions.AddDefinitionSafe(groupDefinition, context, context.CurrentFile);
                }
                else
                {
                    MySandboxGame.Log.WriteLine("Error loading spawn group " + groupDefinition.DisplayNameString);
                    MyDefinitionErrors.Add(context, "Error loading spawn group " + groupDefinition.DisplayNameString, TErrorSeverity.Warning);
                }
            }
        }

        private static void InitRespawnShips(MyModContext context, Dictionary<string, MyRespawnShipDefinition> outputDefinitions, MyObjectBuilder_RespawnShipDefinition[] respawnShips, bool failOnDebug)
        {
            foreach (var ship in respawnShips)
            {
                var shipDefinition = InitDefinition<MyRespawnShipDefinition>(context, ship);
                var id = shipDefinition.Id.SubtypeName;
                Check(!outputDefinitions.ContainsKey(id), id, failOnDebug);
                outputDefinitions[id] = shipDefinition;
            }
        }

        private static void InitPrefabs(MyModContext context, Dictionary<string, MyPrefabDefinition> outputDefinitions, MyObjectBuilder_PrefabDefinition[] prefabs, bool failOnDebug)
        {
            foreach (var prefabBuilder in prefabs)
            {
                var prefabDefinition = InitDefinition<MyPrefabDefinition>(context, prefabBuilder);
                var id = prefabDefinition.Id.SubtypeName;
                Check(!outputDefinitions.ContainsKey(id), id, failOnDebug);
                outputDefinitions[id] = prefabDefinition;
                if (prefabBuilder.RespawnShip)
                {
                    MyDefinitionErrors.Add(context, "Tag <RespawnShip /> is obsolete in prefabs. Use file \"RespawnShips.sbc\" instead.", TErrorSeverity.Warning);
                }
            }
        }


        private void InitControllerSchemas(MyModContext context, DefinitionDictionary<MyDefinitionBase> outputDefinitions, MyObjectBuilder_ControllerSchemaDefinition[] schemas, bool failOnDebug)
        {
            foreach (var schema in schemas)
            {
                var schemaDefinition = InitDefinition<MyControllerSchemaDefinition>(context, schema);
                var id = schemaDefinition.Id;
                Check(!outputDefinitions.ContainsKey(id), id, failOnDebug);
                outputDefinitions.AddDefinitionSafe(schemaDefinition, context, "<ControllerSchema>");
            }
        }

        private void InitCurves(MyModContext context, DefinitionDictionary<MyDefinitionBase> outputDefinitions, MyObjectBuilder_CurveDefinition[] curves, bool failOnDebug)
        {
            foreach (var curve in curves)
            {
                var curveDefinition = InitDefinition<MyCurveDefinition>(context, curve);
                var id = curveDefinition.Id;
                Check(!outputDefinitions.ContainsKey(id), id, failOnDebug);
                outputDefinitions.AddDefinitionSafe(curveDefinition, context, "<Curve>");
            }
        }

        private void InitCharacterNames(MyModContext context, List<MyCharacterName> output, MyCharacterName[] names, bool failOnDebug)
        {
            foreach (var nameEntry in names)
            {
                output.Add(nameEntry);
            }
        }

        private void InitAudioEffects(MyModContext context, DefinitionDictionary<MyDefinitionBase> outputDefinitions, MyObjectBuilder_AudioEffectDefinition[] audioEffects, bool failOnDebug)
        {
            foreach (var effect in audioEffects)
            {
                var effectDefinition = InitDefinition<MyAudioEffectDefinition>(context, effect);
                var id = effectDefinition.Id;
                Check(!outputDefinitions.ContainsKey(id), id, failOnDebug);
                outputDefinitions.AddDefinitionSafe(effectDefinition, context, "<AudioEffect>");
            }
        }

        private static void InitTransparentMaterials(MyModContext context, DefinitionDictionary<MyDefinitionBase> outputDefinitions, MyObjectBuilder_TransparentMaterialDefinition[] materials)
        {
            foreach (var material in materials)
            {
                var materialDefinition = InitDefinition<MyTransparentMaterialDefinition>(context, material);
                materialDefinition.Init(material, context);
                outputDefinitions.AddDefinitionSafe(materialDefinition, context, "<TransparentMaterials>");
            }
        }

        private void InitPhysicalMaterials(MyModContext context, DefinitionDictionary<MyDefinitionBase> outputDefinitions, MyObjectBuilder_PhysicalMaterialDefinition[] materials)
        {
            foreach (var material in materials)
            {
                MyPhysicalMaterialDefinition materialDefinition;
                if (!TryGetDefinition<MyPhysicalMaterialDefinition>(material.Id, out materialDefinition))
                {
                    materialDefinition = InitDefinition<MyPhysicalMaterialDefinition>(context, material);
                    outputDefinitions.AddDefinitionSafe(materialDefinition, context, "<PhysicalMaterials>");
                }
                else
                    materialDefinition.Init(material, context);
                m_definitions.m_physicalMaterialsByName[materialDefinition.Id.SubtypeName] = materialDefinition;
            }
        }

        private void InitMaterialProperties(MyModContext context, DefinitionDictionary<MyDefinitionBase> outputDefinitions, MyObjectBuilder_MaterialPropertiesDefinition[] properties)
        {
            foreach (var property in properties)
            {
                MyPhysicalMaterialDefinition materialDefinition;
                if (!TryGetDefinition<MyPhysicalMaterialDefinition>(property.Id, out materialDefinition))
                {
                    Debug.Fail(string.Format("Material does not exist: {0}", property.Id));
                    continue;
                }
                else
                    materialDefinition.Init(property, context);
            }
        }

        static void CreateTransparentMaterials()
        {
            foreach (var material in Static.GetTransparentMaterialDefinitions())
            {
                MyTransparentMaterials.AddMaterial(new MyTransparentMaterial(
                    material.Id.SubtypeName,
                    material.TextureType,
                    material.Texture,
                    material.SoftParticleDistanceScale,
                    material.CanBeAffectedByLights,
                    material.AlphaMistingEnable,
                    material.Color,
                    material.IgnoreDepth,
                    material.UseAtlas,
                    material.AlphaMistingStart,
                    material.AlphaMistingEnd,
                    material.AlphaSaturation,
                    material.Reflectivity,
                    material.AlphaCutout,
                    material.TargetSize
                ));
            }

            MyTransparentMaterials.Update();
        }

        private static void InitVoxelMaterials(MyModContext context,
            Dictionary<string, MyVoxelMaterialDefinition> output, MyObjectBuilder_VoxelMaterialDefinition[] materials, bool failOnDebug = true)
        {
            var res = new MyVoxelMaterialDefinition[materials.Length];

            for (int i = 0; i < materials.Length; ++i)
            {
                res[i] = InitDefinition<MyVoxelMaterialDefinition>(context, materials[i]);

                Check(!output.ContainsKey(res[i].Id.SubtypeName), res[i].Id.SubtypeName, failOnDebug);
                output[res[i].Id.SubtypeName] = res[i];

                if (!context.IsBaseGame)
                    MySandboxGame.Log.WriteLine("Loaded voxel material: " + res[i].Id.SubtypeName);
            }
        }

        private static void InitCharacters(MyModContext context,
            Dictionary<string, MyCharacterDefinition> outputCharacters,
            DefinitionDictionary<MyDefinitionBase> outputDefinitions,
            MyObjectBuilder_CharacterDefinition[] characters, bool failOnDebug = true)
        {
            var res = new MyCharacterDefinition[characters.Length];

            for (int i = 0; i < characters.Length; ++i)
            {
                // Modders can have their character defined with MyObjectBuilder_CharacterDefinition
                if (typeof(MyObjectBuilder_CharacterDefinition).IsAssignableFrom(characters[i].Id.TypeId))
                {
                    characters[i].Id.TypeId = typeof(MyObjectBuilder_Character);
                }
                res[i] = InitDefinition<MyCharacterDefinition>(context, characters[i]);
                if (res[i].Id.TypeId.IsNull)
                {
                    MySandboxGame.Log.WriteLine("Invalid character Id found in mod !");
                    MyDefinitionErrors.Add(context, "Invalid character Id found in mod ! ", TErrorSeverity.Error);
                    continue;
                }

                Check(!outputCharacters.ContainsKey(res[i].Name), res[i].Name, failOnDebug);
                outputCharacters[res[i].Name] = res[i];

                Check(!outputDefinitions.ContainsKey(characters[i].Id), res[i].Name, failOnDebug);
                outputDefinitions[characters[i].Id] = res[i];
            }
        }

        private static void InitDefinitionsEnvItems
            (MyModContext context, DefinitionDictionary<MyDefinitionBase> outputDefinitions, MyObjectBuilder_EnvironmentItemDefinition[] items, bool failOnDebug = true)
        {
            // TODO: Kill me please!!!!
            MyEnvironmentItemDefinition[] res = new MyEnvironmentItemDefinition[items.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<MyEnvironmentItemDefinition>(context, items[i]);

                res[i].PhysicalMaterial = MyDestructionData.GetPhysicalMaterial(res[i], items[i].PhysicalMaterial);

                Check(!outputDefinitions.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                outputDefinitions[res[i].Id] = res[i];
            }
        }

        private static void InitDefinitionsGeneric<OBDefType, DefType>
            (MyModContext context, DefinitionDictionary<MyDefinitionBase> outputDefinitions, OBDefType[] items, bool failOnDebug = true)
            where OBDefType : MyObjectBuilder_DefinitionBase
            where DefType : MyDefinitionBase
        {
            DefType[] res = new DefType[items.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<DefType>(context, items[i]);

                Check(!outputDefinitions.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                outputDefinitions[res[i].Id] = res[i];
            }
        }

        private static void InitDefinitionsGeneric<OBDefType, DefType>
            (MyModContext context, DefinitionDictionary<DefType> outputDefinitions, OBDefType[] items, bool failOnDebug = true)
            where OBDefType : MyObjectBuilder_DefinitionBase
            where DefType : MyDefinitionBase
        {
            DefType[] res = new DefType[items.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = InitDefinition<DefType>(context, items[i]);

                Check(!outputDefinitions.ContainsKey(res[i].Id), res[i].Id, failOnDebug);
                outputDefinitions[res[i].Id] = res[i];
            }
        }

        [Obsolete]
        private void InitPlanetGeneratorDefinitions(MyModContext context, DefinitionSet defset, MyObjectBuilder_PlanetGeneratorDefinition[] planets, bool failOnDebug)
        {
            foreach (var planet in planets)
            {
                var planetDefinition = InitDefinition<MyPlanetGeneratorDefinition>(context, planet);

                if (planetDefinition.Enabled)
                {
                    defset.AddOrRelaceDefinition(planetDefinition);
                }
                else
                {
                    defset.RemoveDefinition(ref planetDefinition.Id);
                }
            }
        }

        private static void InitComponentGroups(MyModContext context, DefinitionDictionary<MyComponentGroupDefinition> output, MyObjectBuilder_ComponentGroupDefinition[] objects, bool failOnDebug = true)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                var definition = InitDefinition<MyComponentGroupDefinition>(context, objects[i]);

                Check(!output.ContainsKey(definition.Id), definition.Id, failOnDebug);
                output[definition.Id] = definition;
            }
        }

        private static void InitComponentSubstitutions(MyModContext context, Dictionary<MyDefinitionId, MyComponentSubstitutionDefinition> output, MyObjectBuilder_ComponentSubstitutionDefinition[] objects, bool failOnDebug = true)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                var definition = InitDefinition<MyComponentSubstitutionDefinition>(context, objects[i]);

                Check(!output.ContainsKey(definition.Id), definition.Id, failOnDebug);
                output[definition.Id] = definition;
            }
        }


        private void InitComponentBlocks(MyModContext context, HashSet<MyComponentBlockEntry> output, MyComponentBlockEntry[] objects, bool failOnDebug = true)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                var entry = objects[i];
                Check(!output.Contains(entry), entry, failOnDebug);
                output.Add(entry);
            }
        }

        private void InitPlanetPrefabDefinitions(MyModContext context, ref DefinitionDictionary<MyPlanetPrefabDefinition> m_planetDefinitions, MyObjectBuilder_PlanetPrefabDefinition[] planets, bool failOnDebug)
        {
            foreach (var planet in planets)
            {
                var planetDefinition = InitDefinition<MyPlanetPrefabDefinition>(context, planet);
                var id = planetDefinition.Id;
                if (planetDefinition.Enabled)
                {
                    m_planetDefinitions[id] = planetDefinition;
                }
                else
                {
                    m_planetDefinitions.Remove(id);
                }

            }
        }

        private void InitGroupedIds(MyModContext context, string setName, Dictionary<string, Dictionary<string, MyGroupedIds>> output, MyGroupedIds[] groups, bool failOnDebug)
        {
            Dictionary<string, MyGroupedIds> groupedIds;
            if (!output.TryGetValue(setName, out groupedIds))
            {
                groupedIds = new Dictionary<string, MyGroupedIds>();
                output.Add(setName, groupedIds);
            }

            foreach (var group in groups)
            {
                groupedIds[group.Tag] = group;
            }
        }

        private void InitScriptedGroupsMap(MyModContext context, MyMappedId[] map, bool failOnDebug)
        {
            var currentGroups = GetScriptedGroupDefinitions();
            foreach (var val in map)
            {
                MyObjectBuilderType type;
                var groupDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_ScriptedGroupDefinition), val.Group);
                MyScriptedGroupDefinition groupDefinition = null;

                if (!MyObjectBuilderType.TryParse(val.TypeId, out type) || !currentGroups.TryGetValue(groupDefinitionId, out groupDefinition))
                {
                    if (failOnDebug)
                        Debug.Fail("Scripted group failed to load");
                    MyLog.Default.WriteLine("Scripted group failed to load");
                }

                MyDefinitionId id = new MyDefinitionId(type, val.SubtypeName);
                groupDefinition.Add(context, id);
            }
        }

        public bool IsComponentBlock(MyDefinitionId blockDefinitionId)
        {
            return m_definitions.m_componentBlocks.Contains(blockDefinitionId);
        }

        public MyDefinitionId GetComponentId(MyCubeBlockDefinition blockDefinition)
        {
            var components = blockDefinition.Components;

            if (components == null || components.Length == 0)
                return new MyDefinitionId();

            return components[0].Definition.Id;
        }

        public MyDefinitionId GetComponentId(MyDefinitionId defId)
        {
            MyCubeBlockDefinition blockDefinition = null;
            if (!TryGetCubeBlockDefinition(defId, out blockDefinition))
            {
                return new MyDefinitionId();
            }


            return GetComponentId(blockDefinition);
        }

        public MyCubeBlockDefinition TryGetComponentBlockDefinition(MyDefinitionId componentDefId)
        {
            MyCubeBlockDefinition retval = null;
            m_definitions.m_componentIdToBlock.TryGetValue(componentDefId, out retval);
            return retval;
        }

        public MyCubeBlockDefinition GetComponentBlockDefinition(MyDefinitionId componentDefId)
        {
            MyCubeBlockDefinition retval = null;
            m_definitions.m_componentIdToBlock.TryGetValue(componentDefId, out retval);

            Debug.Assert(retval != null, "Could not find a block that contains component " + componentDefId + "!");

            return retval;
        }

        public DictionaryValuesReader<string, MyCharacterDefinition> Characters
        {
            get { return new DictionaryValuesReader<string, MyCharacterDefinition>(m_definitions.m_characters); }
        }

        public string GetRandomCharacterName()
        {
            if (m_definitions.m_characterNames.Count == 0) return "";

            int index = MyUtils.GetRandomInt(m_definitions.m_characterNames.Count);
            return m_definitions.m_characterNames[index].Name;
        }

        public MyAudioDefinition GetSoundDefinition(MyStringHash subtypeId)
        {
            return m_definitions.m_sounds[new MyDefinitionId(typeof(MyObjectBuilder_AudioDefinition), subtypeId)];
        }

        public DictionaryValuesReader<MyDefinitionId, MyHandItemDefinition> GetHandItemDefinitions()
        {
            return new DictionaryValuesReader<MyDefinitionId, MyHandItemDefinition>(m_definitions.m_handItemsById);
        }

        public MyHandItemDefinition TryGetHandItemDefinition(ref MyDefinitionId id)
        {
            MyHandItemDefinition def;
            m_definitions.m_handItemsById.TryGetValue(id, out def);
            return def;
        }

        public ListReader<MyVoxelHandDefinition> GetVoxelHandDefinitions()
        {
            return new ListReader<MyVoxelHandDefinition>(m_definitions.m_definitionsById.Values.OfType<MyVoxelHandDefinition>().ToList());
        }

        public ListReader<MyPrefabThrowerDefinition> GetPrefabThrowerDefinitions()
        {
            return new ListReader<MyPrefabThrowerDefinition>(m_definitions.m_definitionsById.Values.OfType<MyPrefabThrowerDefinition>().ToList());
        }

        private static MyBlueprintDefinitionBase MakeBlueprintFromComponentStack(MyModContext context, MyCubeBlockDefinition cubeBlockDefinition)
        {
            Type blockType = MyCubeBlockFactory.GetProducedType(cubeBlockDefinition.Id.TypeId);

            MyObjectBuilder_CompositeBlueprintDefinition ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CompositeBlueprintDefinition>();

            ob.Id = new SerializableDefinitionId(typeof(MyObjectBuilder_BlueprintDefinition), cubeBlockDefinition.Id.ToString().Replace("MyObjectBuilder_", "")); /*blockType.Name.Substring(2) + "/" + cubeBlockDefinition.Id.SubtypeName*/

            var prerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>();
            foreach (var item in cubeBlockDefinition.Components)
            {
                var id = item.Definition.Id;
                if (!prerequisites.ContainsKey(id))
                    prerequisites[id] = 0;
                prerequisites[id] += item.Count;
            }

            ob.Blueprints = new BlueprintItem[prerequisites.Count];
            int i = 0;
            foreach (var prerequisite in prerequisites)
            {
                MyBlueprintDefinitionBase prerequisiteBlueprint = null;
                if ((prerequisiteBlueprint = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(prerequisite.Key)) == null)
                {
                    MyDefinitionErrors.Add(context, "Could not find component blueprint for " + prerequisite.Key.ToString(), TErrorSeverity.Error);
                    return null;
                }

                ob.Blueprints[i] = new BlueprintItem()
                {
                    Id = new SerializableDefinitionId(prerequisiteBlueprint.Id.TypeId, prerequisiteBlueprint.Id.SubtypeName),
                    Amount = prerequisite.Value.ToString()
                };
                i++;
            }

            ob.Icons = cubeBlockDefinition.Icons;
            ob.DisplayName = cubeBlockDefinition.DisplayNameEnum.HasValue ? cubeBlockDefinition.DisplayNameEnum.Value.ToString() : cubeBlockDefinition.DisplayNameText;
            ob.Public = cubeBlockDefinition.Public;

            MyBlueprintDefinitionBase blueprint = InitDefinition<MyBlueprintDefinitionBase>(context, ob);

            return blueprint;
        }

        public MyObjectBuilder_DefinitionBase GetObjectBuilder(MyDefinitionBase definition)
        {
            return GetObjectFactory().CreateObjectBuilder<MyObjectBuilder_DefinitionBase>(definition);
        }

        private static void Check<T>(bool conditionResult, T identifier, bool failOnDebug = true, string messageFormat = DUPLICATE_ENTRY_MESSAGE)
        {
            if (!conditionResult)
            {
                var msg = string.Format(messageFormat, identifier.ToString());
                if (failOnDebug)
                    Debug.Fail(msg);
                MySandboxGame.Log.WriteLine(msg);
            }
        }

        void MergeDefinitions()
        {
            m_definitions.Clear();

            foreach (var definitionSet in m_modDefinitionSets)
            {
                m_definitions.OverrideBy(definitionSet.Value);
            }
        }

        private static void InitGenericObjects(MyModContext context, DefinitionDictionary<MyDefinitionBase> output, MyObjectBuilder_DefinitionBase[] objects, bool failOnDebug = true)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                var definition = InitDefinition<MyDefinitionBase>(context, objects[i]);

                Check(!output.ContainsKey(definition.Id), definition.Id, failOnDebug);
                output[definition.Id] = definition;
            }
        }

        #endregion

        #region Getters

        public MyGridCreateToolDefinition GetGridCreator(MyStringHash name)
        {
            MyGridCreateToolDefinition def;
            m_definitions.m_gridCreateDefinitions.TryGetValue(new MyDefinitionId(typeof(MyObjectBuilder_GridCreateToolDefinition), name), out def);
            return def;
        }

        public IEnumerable<MyGridCreateToolDefinition> GetGridCreatorDefinitions()
        {
            return m_definitions.m_gridCreateDefinitions.Values;
        }

        public void GetBaseBlockPrefabName(MyCubeSize size, bool isStatic, bool isCreative, out string prefabName)
        {
            prefabName = m_definitions.m_basePrefabNames[ComputeBasePrefabIndex(size, isStatic, isCreative)];
        }

        private void AddBasePrefabName(DefinitionSet definitionSet, MyCubeSize size, bool isStatic, bool isCreative, string prefabName)
        {
            if (!string.IsNullOrEmpty(prefabName))
                definitionSet.m_basePrefabNames[ComputeBasePrefabIndex(size, isStatic, isCreative)] = prefabName;
        }

        private static int ComputeBasePrefabIndex(MyCubeSize size, bool isStatic, bool isCreative)
        {
            return (int)size * 4 + (isStatic ? 2 : 0) + (isCreative ? 1 : 0);
        }

        public MyCubeBlockDefinitionGroup GetDefinitionGroup(string groupName)
        {
            return m_definitions.m_blockGroups[groupName];
        }

        public MyCubeBlockDefinitionGroup TryGetDefinitionGroup(string groupName)
        {
            return m_definitions.m_blockGroups.ContainsKey(groupName) ? m_definitions.m_blockGroups[groupName] : null;
        }

        public DictionaryKeysReader<string, MyCubeBlockDefinitionGroup> GetDefinitionPairNames()
        {
            return new DictionaryKeysReader<string, MyCubeBlockDefinitionGroup>(m_definitions.m_blockGroups);
        }

        public bool TryGetDefinition<T>(MyDefinitionId defId, out T definition) where T : MyDefinitionBase
        {
            if (!defId.TypeId.IsNull)
            {
                definition = base.GetDefinition<T>(defId);
                if (definition != null) return true;

                MyDefinitionBase definitionBase;
                if (m_definitions.m_definitionsById.TryGetValue(defId, out definitionBase))
                {
                    definition = definitionBase as T;
                    return definition != null;
                }
            }

            definition = default(T);
            return false;
        }

        public MyDefinitionBase GetDefinition(MyDefinitionId id)
        {
            // TODO: Kill m_definitionsById
            var def = base.GetDefinition<MyDefinitionBase>(id);
            if (def != null) return def;

            MyDebug.AssertDebug(m_definitions.m_definitionsById.ContainsKey(id), "No definition for given ID.");
            CheckDefinition(ref id);
            return m_definitions.m_definitionsById[id];
        }

        public Vector2I GetCubeBlockScreenPosition(string pairName)
        {
            Vector2I result;
            if (!m_definitions.m_blockPositions.TryGetValue(pairName, out result))
                result = new Vector2I(-1, -1);
            return result;
        }

        public bool TryGetCubeBlockDefinition(MyDefinitionId defId, out MyCubeBlockDefinition blockDefinition)
        {
            MyDefinitionBase definition;
            if (!m_definitions.m_definitionsById.TryGetValue(defId, out definition))
            {
                blockDefinition = null;
                return false;
            }
            else
            {
                blockDefinition = definition as MyCubeBlockDefinition;
                return blockDefinition != null;
            }
        }

        public MyCubeBlockDefinition GetCubeBlockDefinition(MyObjectBuilder_CubeBlock builder)
        {
            return GetCubeBlockDefinition(builder.GetId());
        }

        public MyCubeBlockDefinition GetCubeBlockDefinition(MyDefinitionId id)
        {
            CheckDefinition<MyCubeBlockDefinition>(ref id);
            if (m_definitions.m_definitionsById.ContainsKey(id))
            {
                return m_definitions.m_definitionsById[id] as MyCubeBlockDefinition;
            }
            else
            {
                Debug.Assert(false, "Key not in dictionary! " + id.ToString());
                return null;
            }
        }

        public MyComponentDefinition GetComponentDefinition(MyDefinitionId id)
        {
            Debug.Assert(id.TypeId == typeof(MyObjectBuilder_Component));
            CheckDefinition<MyComponentDefinition>(ref id);
            return m_definitions.m_definitionsById[id] as MyComponentDefinition;
        }

        public void GetDefinedEntityComponents(ref List<MyDefinitionId> definedComponents)
        {
            foreach (var def in m_definitions.m_entityComponentDefinitions)
            {
                definedComponents.Add(def.Key);
            }
        }

        public bool TryGetComponentDefinition(MyDefinitionId id, out MyComponentDefinition definition)
        {
            MyDefinitionBase definitionBase = definition = null;
            if (m_definitions.m_definitionsById.TryGetValue(id, out definitionBase))
            {
                definition = definitionBase as MyComponentDefinition;
                return definition != null;
            }
            return false;
        }

        public MyBlueprintDefinitionBase TryGetBlueprintDefinitionByResultId(MyDefinitionId resultId)
        {
            return m_definitions.m_blueprintsByResultId.GetValueOrDefault(resultId);
        }

        public bool HasBlueprint(MyDefinitionId blueprintId)
        {
            return m_definitions.m_blueprintsById.ContainsKey(blueprintId);
        }

        public MyBlueprintDefinitionBase GetBlueprintDefinition(MyDefinitionId blueprintId)
        {
            if (!m_definitions.m_blueprintsById.ContainsKey(blueprintId))
            {
                MySandboxGame.Log.WriteLine(string.Format("No blueprint with Id '{0}'", blueprintId));
                return null;
            }
            return m_definitions.m_blueprintsById[blueprintId];
        }

        public MyBlueprintClassDefinition GetBlueprintClass(string className)
        {
            MyBlueprintClassDefinition classDefinition = null;

            MyDefinitionId classId = new MyDefinitionId(typeof(MyObjectBuilder_BlueprintClassDefinition), className);
            m_definitions.m_blueprintClasses.TryGetValue(classId, out classDefinition);

            return classDefinition;
        }

        // CH: TODO: Do these methods in a more generic fashion
        // e.g. by being able to find a blueprint that creates something from the given item as the only ingredient
        public bool TryGetIngotBlueprintDefinition(MyObjectBuilder_Base oreBuilder, out MyBlueprintDefinitionBase ingotBlueprint)
        {
            Debug.Assert(oreBuilder.TypeId == typeof(MyObjectBuilder_Ore));
            return TryGetIngotBlueprintDefinition(oreBuilder.GetId(), out ingotBlueprint);
        }

        public Dictionary<string, MyGuiBlockCategoryDefinition> GetCategories()
        {
            return this.m_definitions.m_categories;
        }

        public bool TryGetIngotBlueprintDefinition(MyDefinitionId oreId, out MyBlueprintDefinitionBase ingotBlueprint)
        {
            Debug.Assert(oreId.TypeId == typeof(MyObjectBuilder_Ore));
            var ingotClass = GetBlueprintClass("Ingots");
            foreach (var blueprint in ingotClass)
            {
                if (blueprint.InputItemType != typeof(MyObjectBuilder_Ore))
                    continue;

                Debug.Assert(blueprint.Prerequisites.Length == 1);
                if (blueprint.Prerequisites[0].Id.SubtypeId == oreId.SubtypeId)
                {
                    ingotBlueprint = blueprint;
                    return true;
                }
            }
            ingotBlueprint = null;
            return false;
        }

        public bool TryGetComponentBlueprintDefinition(MyDefinitionId componentId, out MyBlueprintDefinitionBase componentBlueprint)
        {
            Debug.Assert(componentId.TypeId == typeof(MyObjectBuilder_Component));
            var componentClass = GetBlueprintClass("Components");
            foreach (var blueprint in componentClass)
            {
                if (blueprint.InputItemType != typeof(MyObjectBuilder_Ingot))
                    continue;

                Debug.Assert(blueprint.Results.Length == 1);
                if (blueprint.Results[0].Id.SubtypeId == componentId.SubtypeId)
                {
                    componentBlueprint = blueprint;
                    return true;
                }
            }
            componentBlueprint = null;
            return false;
        }

        public DictionaryValuesReader<MyDefinitionId, MyBlueprintDefinitionBase> GetBlueprintDefinitions()
        {
            return new DictionaryValuesReader<MyDefinitionId, MyBlueprintDefinitionBase>(m_definitions.m_blueprintsById);
        }

        public DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> GetAllDefinitions()
        {
            return new DictionaryValuesReader<MyDefinitionId, MyDefinitionBase>(m_definitions.m_definitionsById);
        }

        public ListReader<MyPhysicalItemDefinition> GetWeaponDefinitions()
        {
            return new ListReader<MyPhysicalItemDefinition>(m_definitions.m_physicalItemDefinitions);
        }

        public ListReader<MySpawnGroupDefinition> GetSpawnGroupDefinitions()
        {
            return new ListReader<MySpawnGroupDefinition>(m_definitions.m_spawnGroupDefinitions);
        }

        public ListReader<MyScenarioDefinition> GetScenarioDefinitions()
        {
            return new ListReader<MyScenarioDefinition>(m_definitions.m_scenarioDefinitions);
        }

        public ListReader<MyAnimationDefinition> GetAnimationDefinitions()
        {
            return new ListReader<MyAnimationDefinition>(m_definitions.m_definitionsById.Values.OfType<MyAnimationDefinition>().ToList());
        }

        public Dictionary<string, MyAnimationDefinition> GetAnimationDefinitions(string skeleton)
        {
            return m_definitions.m_animationsBySkeletonType[skeleton];
        }
        public ListReader<MyDebrisDefinition> GetDebrisDefinitions()
        {
            return new ListReader<MyDebrisDefinition>(m_definitions.m_definitionsById.Values.OfType<MyDebrisDefinition>().ToList());
        }

        public ListReader<MyTransparentMaterialDefinition> GetTransparentMaterialDefinitions()
        {
            return new ListReader<MyTransparentMaterialDefinition>(m_definitions.m_definitionsById.Values.OfType<MyTransparentMaterialDefinition>().ToList());
        }

        public ListReader<MyPhysicalMaterialDefinition> GetPhysicalMaterialDefinitions()
        {
            return new ListReader<MyPhysicalMaterialDefinition>(m_definitions.m_definitionsById.Values.OfType<MyPhysicalMaterialDefinition>().ToList());
        }

        public ListReader<MyEdgesDefinition> GetEdgesDefinitions()
        {
            return new ListReader<MyEdgesDefinition>(m_definitions.m_definitionsById.Values.OfType<MyEdgesDefinition>().ToList());
        }

        public ListReader<MyPhysicalItemDefinition> GetPhysicalItemDefinitions()
        {
            return new ListReader<MyPhysicalItemDefinition>(m_definitions.m_definitionsById.Values.OfType<MyPhysicalItemDefinition>().ToList());
        }

        public ListReader<MyEnvironmentItemDefinition> GetEnvironmentItemDefinitions()
        {
            return new ListReader<MyEnvironmentItemDefinition>(m_definitions.m_definitionsById.Values.OfType<MyEnvironmentItemDefinition>().ToList());
        }

        public ListReader<MyEnvironmentItemsDefinition> GetEnvironmentItemClassDefinitions()
        {
            return new ListReader<MyEnvironmentItemsDefinition>(m_definitions.m_definitionsById.Values.OfType<MyEnvironmentItemsDefinition>().ToList());
        }

        public ListReader<MyCompoundBlockTemplateDefinition> GetCompoundBlockTemplateDefinitions()
        {
            return new ListReader<MyCompoundBlockTemplateDefinition>(m_definitions.m_definitionsById.Values.OfType<MyCompoundBlockTemplateDefinition>().ToList());
        }

        public DictionaryValuesReader<MyDefinitionId, MyAudioDefinition> GetSoundDefinitions()
        {
            return m_definitions.m_sounds;
        }

        internal VRage.Collections.ListReader<MyAudioEffectDefinition> GetAudioEffectDefinitions()
        {
            return new ListReader<MyAudioEffectDefinition>(m_definitions.m_definitionsById.Values.OfType<MyAudioEffectDefinition>().ToList());
        }

        public ListReader<MyMultiBlockDefinition> GetMultiBlockDefinitions()
        {
            return new ListReader<MyMultiBlockDefinition>(m_definitions.m_definitionsById.Values.OfType<MyMultiBlockDefinition>().ToList());
        }

        public ListReader<MySoundCategoryDefinition> GetSoundCategoryDefinitions()
        {
            return new ListReader<MySoundCategoryDefinition>(m_definitions.m_definitionsById.Values.OfType<MySoundCategoryDefinition>().ToList());
        }

        public ListReader<MyLCDTextureDefinition> GetLCDTexturesDefinitions()
        {
            return new ListReader<MyLCDTextureDefinition>(m_definitions.m_definitionsById.Values.OfType<MyLCDTextureDefinition>().ToList());
        }

        public ListReader<MyBehaviorDefinition> GetBehaviorDefinitions()
        {
            return new ListReader<MyBehaviorDefinition>(m_definitions.m_behaviorDefinitions.Values.ToList());
        }

        public ListReader<MyBotDefinition> GetBotDefinitions()
        {
            return new ListReader<MyBotDefinition>(m_definitions.m_definitionsById.Values.OfType<MyBotDefinition>().ToList());
        }

        public ListReader<T> GetDefinitionsOfType<T>() where T : MyDefinitionBase
        {
            return new ListReader<T>(m_definitions.m_definitionsById.Values.OfType<T>().ToList());
        }

        public ListReader<MyVoxelMapStorageDefinition> GetVoxelMapStorageDefinitions()
        {
            return new ListReader<MyVoxelMapStorageDefinition>(m_definitions.m_voxelMapStorages.Values.ToList());
        }

        public bool TryGetVoxelMapStorageDefinition(string name, out MyVoxelMapStorageDefinition definition)
        {
            return m_definitions.m_voxelMapStorages.TryGetValue(name, out definition);
        }

        public MyScenarioDefinition GetScenarioDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyScenarioDefinition>(ref id);
            return (MyScenarioDefinition)m_definitions.m_definitionsById[id];
        }

        public MyEdgesDefinition GetEdgesDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyEdgesDefinition>(ref id);
            return (MyEdgesDefinition)m_definitions.m_definitionsById[id];
        }

        public void RegisterFactionDefinition(MyFactionDefinition definition)
        {
            Debug.Assert(Loading, "Cannot register faction definition when not loading!");
            if (!Loading) return;

            if (m_definitions.m_factionDefinitionsByTag.ContainsKey(definition.Tag))
            {
                string msg = "Faction with tag " + definition.Tag + " is already registered in the definition manager. Overwriting...";
                Debug.Assert(false, msg);

                MySandboxGame.Log.WriteLine(msg);
            }
            m_definitions.m_factionDefinitionsByTag.Add(definition.Tag, definition);
        }

        public MyFactionDefinition TryGetFactionDefinition(String tag)
        {
            MyFactionDefinition factionDef = null;
            m_definitions.m_factionDefinitionsByTag.TryGetValue(tag, out factionDef);
            return factionDef;
        }

        /// <summary>
        /// Gets default factions.
        /// </summary>
        /// <returns>List of default factions.</returns>
        public List<MyFactionDefinition> GetDefaultFactions()
        {
            var defaultFactions = new List<MyFactionDefinition>();

            foreach (var faction in m_definitions.m_factionDefinitionsByTag.Values)
            {
                if (faction.IsDefault)
                    defaultFactions.Add(faction);
            }

            return defaultFactions;
        }

        public MyContainerTypeDefinition GetContainerTypeDefinition(String containerName)
        {
            MyContainerTypeDefinition def;
            if (!m_definitions.m_containerTypeDefinitions.TryGetValue(new MyDefinitionId(typeof(MyObjectBuilder_ContainerTypeDefinition), containerName), out def))
            {
                return null;
            }
            return def;
        }

        public MySpawnGroupDefinition GetSpawnGroupDefinition(int index)
        {
            return m_definitions.m_spawnGroupDefinitions[index];
        }

        public bool HasRespawnShip(string id)
        {
            return m_definitions.m_respawnShips.ContainsKey(id);
        }

        public MyRespawnShipDefinition GetRespawnShipDefinition(string id)
        {
            MyRespawnShipDefinition def;
            m_definitions.m_respawnShips.TryGetValue(id, out def);

            if (def == null) return null;

            var prefabDef = def.Prefab;
            Debug.Assert(prefabDef != null, "Prefab definition in respawn ship definition is null!");
            if (prefabDef == null) return null;

            return def;
        }

        public MyPrefabDefinition GetPrefabDefinition(string id)
        {
            MyPrefabDefinition res;
            m_definitions.m_prefabs.TryGetValue(id, out res);
            return res;
        }

        public void ReloadPrefabsFromFile(string filePath)
        {
            MyObjectBuilder_Definitions definitions = Load<MyObjectBuilder_Definitions>(filePath);
            if (definitions.Prefabs != null)
            {
                foreach (var prefab in definitions.Prefabs)
                {
                    MyPrefabDefinition definition = GetPrefabDefinition(prefab.Id.SubtypeId);
                    if (definition != null)
                    {
                        definition.InitLazy(prefab);
                    }
                }
            }
        }

        public DictionaryReader<string, MyPrefabDefinition> GetPrefabDefinitions()
        {
            return new DictionaryReader<string, MyPrefabDefinition>(m_definitions.m_prefabs);
        }

        public DictionaryReader<string, MyRespawnShipDefinition> GetRespawnShipDefinitions()
        {
            return new DictionaryReader<string, MyRespawnShipDefinition>(m_definitions.m_respawnShips);
        }

        public string GetFirstRespawnShip()
        {
            if (m_definitions.m_respawnShips.Count > 0)
            {
                var respawnShip = m_definitions.m_respawnShips.FirstOrDefault();
                return respawnShip.Value.Id.SubtypeName;
            }

            return null;
        }

        public MyGlobalEventDefinition GetEventDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyGlobalEventDefinition>(ref id);
            MyDefinitionBase definition = null;
            m_definitions.m_definitionsById.TryGetValue(id, out definition);
            return (MyGlobalEventDefinition)definition;
        }

        public bool TryGetPhysicalItemDefinition(MyDefinitionId id, out MyPhysicalItemDefinition definition)
        {
            MyDefinitionBase tmp;
            if (!TryGetDefinition(id, out tmp))
            {
                definition = null;
                return false;
            }

            definition = tmp as MyPhysicalItemDefinition;
            return definition != null;
        }

        public MyPhysicalItemDefinition TryGetPhysicalItemDefinition(MyDefinitionId id)
        {
            MyDefinitionBase tmp;
            if (!TryGetDefinition(id, out tmp))
                return null;
            return tmp as MyPhysicalItemDefinition;
        }

        public MyPhysicalItemDefinition GetPhysicalItemDefinition(MyObjectBuilder_Base objectBuilder)
        {
            return GetPhysicalItemDefinition(objectBuilder.GetId());
        }

        public MyAmmoDefinition GetAmmoDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_ammoDefinitionsById.ContainsKey(id));
            //CheckDefinition<MyAmmoDefinition>(ref id);
            return m_definitions.m_ammoDefinitionsById[id];
        }

        public MyPhysicalItemDefinition GetPhysicalItemDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyPhysicalItemDefinition>(ref id);
            return m_definitions.m_definitionsById[id] as MyPhysicalItemDefinition;
        }

        public void TryGetDefinitionsByTypeId(MyObjectBuilderType typeId, HashSet<MyDefinitionId> definitions)
        {
            foreach (MyDefinitionId definition in m_definitions.m_definitionsById.Keys)
            {
                if (definition.TypeId == typeId && !definitions.Contains(definition))
                    definitions.Add(definition);
            }
        }

        public MyEnvironmentItemDefinition GetEnvironmentItemDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyEnvironmentItemDefinition>(ref id);
            return m_definitions.m_definitionsById[id] as MyEnvironmentItemDefinition;
        }

        public MyCompoundBlockTemplateDefinition GetCompoundBlockTemplateDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyCompoundBlockTemplateDefinition>(ref id);
            return m_definitions.m_definitionsById[id] as MyCompoundBlockTemplateDefinition;
        }

        public MyAmmoMagazineDefinition GetAmmoMagazineDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyAmmoMagazineDefinition>(ref id);
            return m_definitions.m_definitionsById[id] as MyAmmoMagazineDefinition;
        }

        public MyShipSoundsDefinition GetShipSoundsDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyShipSoundsDefinition>(ref id);
            return m_definitions.m_definitionsById[id] as MyShipSoundsDefinition;
        }

        public MyShipSoundSystemDefinition GetShipSoundSystemDefinition
        {
            get { return m_definitions.m_shipSoundSystem; }
        }

        public MyWeaponDefinition GetWeaponDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_weaponDefinitionsById.ContainsKey(id));
            //CheckDefinition<MyWeaponDefinition>(ref id);
            return m_definitions.m_weaponDefinitionsById[id];
        }

        public bool TryGetWeaponDefinition(MyDefinitionId defId, out MyWeaponDefinition definition)
        {
            if (!defId.TypeId.IsNull)
            {
                MyWeaponDefinition weaponDefinition;
                if (m_definitions.m_weaponDefinitionsById.TryGetValue(defId, out weaponDefinition))
                {
                    definition = weaponDefinition;
                    return definition != null;
                }
            }

            definition = default(MyWeaponDefinition);
            return false;
        }

        public MyBehaviorDefinition GetBehaviorDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_behaviorDefinitions.ContainsKey(id));
            return m_definitions.m_behaviorDefinitions[id];
        }

        public MyBotDefinition GetBotDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyBotDefinition>(ref id);
            if (m_definitions.m_definitionsById.ContainsKey(id))
                return m_definitions.m_definitionsById[id] as MyBotDefinition;
            return null;
        }

        public bool TryGetBotDefinition(MyDefinitionId id, out MyBotDefinition botDefinition)
        {
            if (m_definitions.m_definitionsById.ContainsKey(id))
            {
                botDefinition = m_definitions.m_definitionsById[id] as MyBotDefinition;
                return true;
            }
            else
            {
                botDefinition = null;
                return false;
            }
        }

        public MyAnimationDefinition TryGetAnimationDefinition(string animationSubtypeName)
        {
            MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_AnimationDefinition), animationSubtypeName);
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id), "Animation definition with name: " + animationSubtypeName + " is missing");
            CheckDefinition<MyAnimationDefinition>(ref id);
            MyDefinitionBase output = null;
            m_definitions.m_definitionsById.TryGetValue(id, out output);
            return output as MyAnimationDefinition;
        }

        public string GetAnimationDefinitionCompatibility(string animationSubtypeName)
        {
            MyDefinitionBase animationDefinition;
            string returnAnimation = animationSubtypeName;
            if (!MyDefinitionManager.Static.TryGetDefinition(new MyDefinitionId(typeof(MyObjectBuilder_AnimationDefinition), animationSubtypeName), out animationDefinition))
            {   // Backward compatibility, animation is stored as filepath
                // Lets find adequate animation
                foreach (var animDef in MyDefinitionManager.Static.GetAnimationDefinitions())
                {
                    if (animDef.AnimationModel == animationSubtypeName)
                    {
                        returnAnimation = animDef.Id.SubtypeName;
                        break;
                    }
                }
            }
            return returnAnimation;
        }

        public MyMultiBlockDefinition GetMultiBlockDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyMultiBlockDefinition>(ref id);
            return m_definitions.m_definitionsById[id] as MyMultiBlockDefinition;
        }

        public MyMultiBlockDefinition TryGetMultiBlockDefinition(MyDefinitionId id)
        {
            if (!m_definitions.m_definitionsById.ContainsKey(id))
            {
                MySandboxGame.Log.WriteLine(string.Format("No multiblock definition '{0}'", id));
                return null;
            }
            return m_definitions.m_definitionsById[id] as MyMultiBlockDefinition;
        }

        public MyPhysicalItemDefinition GetPhysicalItemForHandItem(MyDefinitionId handItemId)
        {
            if (!m_definitions.m_physicalItemsByHandItemId.ContainsKey(handItemId))
            {
                return null;
            }
            return m_definitions.m_physicalItemsByHandItemId[handItemId];
        }

        public MyHandItemDefinition TryGetHandItemForPhysicalItem(MyDefinitionId physicalItemId)
        {
            if (!m_definitions.m_handItemsByPhysicalItemId.ContainsKey(physicalItemId))
            {
                MySandboxGame.Log.WriteLine(string.Format("No hand item for physical item '{0}'", physicalItemId));
                return null;
            }
            return m_definitions.m_handItemsByPhysicalItemId[physicalItemId];
        }

        public bool HandItemExistsFor(MyDefinitionId physicalItemId)
        {
            return m_definitions.m_handItemsByPhysicalItemId.ContainsKey(physicalItemId);
        }

        public MyDefinitionId? ItemIdFromWeaponId(MyDefinitionId weaponDefinition)
        {
            MyDefinitionId? retval = null;
            if (weaponDefinition.TypeId != typeof(MyObjectBuilder_PhysicalGunObject))
            {
                var physItem = MyDefinitionManager.Static.GetPhysicalItemForHandItem(weaponDefinition);
                if (physItem != null)
                {
                    retval = physItem.Id;
                }
            }
            else
            {
                retval = weaponDefinition;
            }
            return retval;
        }

        public float GetCubeSize(MyCubeSize gridSize)
        {
            return m_definitions.m_cubeSizes[(int)gridSize];
        }

        public float GetCubeSizeOriginal(MyCubeSize gridSize)
        {
            return m_definitions.m_cubeSizesOriginal[(int)gridSize];
        }

        public MyLootBagDefinition GetLootBagDefinition()
        {
            return m_definitions.m_lootBagDefinition;
        }

        public MyPhysicalMaterialDefinition GetPhysicalMaterialDefinition(MyDefinitionId id)
        {
            Debug.Assert(m_definitions.m_definitionsById.ContainsKey(id));
            CheckDefinition<MyPhysicalMaterialDefinition>(ref id);
            return m_definitions.m_definitionsById[id] as MyPhysicalMaterialDefinition;
        }

        public MyPhysicalMaterialDefinition GetPhysicalMaterialDefinition(string name)
        {
            MyPhysicalMaterialDefinition def = null;
            m_definitions.m_physicalMaterialsByName.TryGetValue(name, out def);
            return def;
        }

        public void GetOreTypeNames(out string[] outNames)
        {
            List<string> names = new List<string>();
            foreach (var value in m_definitions.m_definitionsById.Values)
            {
                if (value.Id.TypeId == typeof(MyObjectBuilder_Ore))
                    names.Add(value.Id.SubtypeName);
            }
            outNames = names.ToArray();
        }

        private void CheckDefinition(ref MyDefinitionId id)
        {
            CheckDefinition<MyDefinitionBase>(ref id);
        }

        public MyEnvironmentItemsDefinition GetRandomEnvironmentClass(int channel)
        {
            MyEnvironmentItemsDefinition classDef = null;

            List<MyDefinitionId> definitionList = null;
            m_definitions.m_channelEnvironmentItemsDefs.TryGetValue(channel, out definitionList);
            Debug.Assert(definitionList != null, "No environment items definitions for channel " + channel.ToString());
            if (definitionList == null)
            {
                return classDef;
            }

            Debug.Assert(definitionList.Count > 0);
            int randomIndex = MyRandom.Instance.Next(0, definitionList.Count);
            Debug.Assert(randomIndex < definitionList.Count);
            MyDefinitionId classId = definitionList[randomIndex];
            MyDefinitionManager.Static.TryGetDefinition(classId, out classDef);
            return classDef;
        }

        public ListReader<MyDefinitionId> GetEnvironmentItemsDefinitions(int channel)
        {
            List<MyDefinitionId> definitionList = null;
            m_definitions.m_channelEnvironmentItemsDefs.TryGetValue(channel, out definitionList);
            return definitionList;
        }

        private void CheckDefinition<T>(ref MyDefinitionId id) where T : MyDefinitionBase
        {
            MyDefinitionBase definitionBase;
            try
            {
                definitionBase = base.GetDefinition<T>(id.SubtypeId);
                var definitionFound = definitionBase != null || m_definitions.m_definitionsById.TryGetValue(id, out definitionBase);

                if (!definitionFound)
                {
                    string message = String.Format("No definition '{0}'. Maybe a mistake in XML?", id);
                    MySandboxGame.Log.WriteLine(message);
                    Debug.Fail(message);
                    return;
                }

                if (!(definitionBase is T))
                {
                    string message = String.Format("Definition '{0}' is not of desired type.", id);
                    MySandboxGame.Log.WriteLine(message);
                    Debug.Fail(string.Format("{0} Type: {1}; Wanted: {2}", message, definitionBase.GetType().Name, typeof(T).Name));
                }
            }
            catch (KeyNotFoundException ex)
            {
                Debug.Fail("Definition is invalid, but cannot display error message because id.ToString() fails! Exception message: {0}", ex.Message);
            }
        }

        public IEnumerable<MyPlanetGeneratorDefinition> GetPlanetsGeneratorsDefinitions()
        {
            return m_definitions.GetDefinitionsOfType<MyPlanetGeneratorDefinition>();
        }

        public DictionaryValuesReader<MyDefinitionId, MyPlanetPrefabDefinition> GetPlanetsPrefabsDefinitions()
        {
            return new DictionaryValuesReader<MyDefinitionId, MyPlanetPrefabDefinition>(m_definitions.m_planetPrefabDefinitions);
        }

        public DictionaryValuesReader<string, MyGroupedIds> GetGroupedIds(string superGroup)
        {
            return new DictionaryValuesReader<string, MyGroupedIds>(m_definitions.m_groupedIds[superGroup]);
        }

        public DictionaryValuesReader<MyDefinitionId, MyScriptedGroupDefinition> GetScriptedGroupDefinitions()
        {
            return new DictionaryValuesReader<MyDefinitionId, MyScriptedGroupDefinition>(m_definitions.m_scriptedGroupDefinitions);
        }

        public DictionaryValuesReader<MyDefinitionId, MyPirateAntennaDefinition> GetPirateAntennaDefinitions()
        {
            return new DictionaryValuesReader<MyDefinitionId, MyPirateAntennaDefinition>(m_definitions.m_pirateAntennaDefinitions);
        }

        public MyComponentGroupDefinition GetComponentGroup(MyDefinitionId groupDefId)
        {
            MyComponentGroupDefinition group = null;
            m_definitions.m_componentGroups.TryGetValue(groupDefId, out group);
            return group;
        }

        /// <summary>
        /// Returns the substitution definition for a "base" (i.e. required) component that has multiple other components (i.e. providing components) substituting it
        /// </summary>
        public bool TryGetComponentSubstitutionDefinition(MyDefinitionId componentDefId, out MyComponentSubstitutionDefinition substitutionDefinition)
        {
            substitutionDefinition = null;
            // TODO: If there is likely to be a lot of these, change the searching..
            foreach (var def in m_definitions.m_componentSubstitutions)
            {
                if (def.Value.RequiredComponent == componentDefId)
                {
                    substitutionDefinition = def.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the substitution definition for a component that can serve as a providing component for some other "base" (required) component
        /// </summary>
        public bool TryGetProvidingComponentDefinition(MyDefinitionId componentDefId, out MyComponentSubstitutionDefinition substitutionDefinition)
        {
            substitutionDefinition = null;
            // TODO: If there is likely to be a lot of these, change the searching..
            foreach (var def in m_definitions.m_componentSubstitutions)
            {
                if (def.Value.ProvidingComponents.Keys.Contains(componentDefId))
                {
                    substitutionDefinition = def.Value;
                    return true;
                }
            }
            return false;
        }

        public MyComponentGroupDefinition GetGroupForComponent(MyDefinitionId componentDefId, out int amount)
        {
            MyTuple<int, MyComponentGroupDefinition> result;
            if (m_definitions.m_componentGroupMembers.TryGetValue(componentDefId, out result))
            {
                amount = result.Item1;
                return result.Item2;
            }
            amount = 0;
            return null;
        }

        public MyRopeDefinition GetRopeDefinition(MyDefinitionId ropeDefId)
        {
            if (!m_definitions.m_idToRope.ContainsKey(ropeDefId))
                MySandboxGame.Log.WriteLine(string.Format("No rope definition found '{0}'", ropeDefId));

            return m_definitions.m_idToRope[ropeDefId];
        }

        public bool TryGetGetRopeDefinition(MyDefinitionId ropeDefId, out MyRopeDefinition definition)
        {
            return m_definitions.m_idToRope.TryGetValue(ropeDefId, out definition);
        }

        public DictionaryValuesReader<MyDefinitionId, MyRopeDefinition> GetRopeDefinitions()
        {
            return m_definitions.m_idToRope;
        }

        public bool TryGetEntityComponentDefinition(MyDefinitionId componentId, out MyComponentDefinitionBase definition)
        {
            return m_definitions.m_entityComponentDefinitions.TryGetValue(componentId, out definition);
        }

        public MyComponentDefinitionBase GetEntityComponentDefinition(MyDefinitionId componentId)
        {
            return m_definitions.m_entityComponentDefinitions[componentId];
        }

        public ListReader<MyComponentDefinitionBase> GetEntityComponentDefinitions()
        {
            return GetEntityComponentDefinitions<MyComponentDefinitionBase>();
        }

        public ListReader<T> GetEntityComponentDefinitions<T>()
        {
            return new ListReader<T>(m_definitions.m_entityComponentDefinitions.Values.OfType<T>().ToList());
        }

        public bool TryGetContainerDefinition(MyDefinitionId containerId, out MyContainerDefinition definition)
        {
            return m_definitions.m_entityContainers.TryGetValue(containerId, out definition);
        }

        public MyContainerDefinition GetContainerDefinition(MyDefinitionId containerId)
        {
            return m_definitions.m_entityContainers[containerId];
        }

        public void GetDefinedEntityContainers(ref List<MyDefinitionId> definedContainers)
        {
            foreach (var def in m_definitions.m_entityContainers)
            {
                definedContainers.Add(def.Key);
            }
        }

        public DictionaryValuesReader<MyDefinitionId, MyFontDefinition> GetFontDefinitions()
        {
            return new DictionaryValuesReader<MyDefinitionId, MyFontDefinition>(m_definitions.m_fontsById);
        }

        public MyFontDefinition GetFontSafe(string fontName)
        {
            var id = new MyDefinitionId(typeof(MyObjectBuilder_FontDefinition), fontName);
            MyFontDefinition font;
            if (!m_definitions.m_fontsById.TryGetValue(id, out font))
            {
                //Debug must be always provided
                id = new MyDefinitionId(typeof(MyObjectBuilder_FontDefinition), "Debug");
                font = m_definitions.m_fontsById[id];
            }

            return font;
        }

        #endregion

        #region Setters

        internal void SetEntityContainerDefinition(MyContainerDefinition newDefinition)
        {
            if (m_definitions != null && m_definitions.m_entityContainers != null)
            {
                if (!m_definitions.m_entityContainers.ContainsKey(newDefinition.Id))
                {
                    m_definitions.m_entityContainers.Add(newDefinition.Id, newDefinition);
                }
                else
                {
                    m_definitions.m_entityContainers[newDefinition.Id] = newDefinition;
                }
            }
        }

        #endregion

        #region Voxel Materials
        public MyVoxelMaterialDefinition GetVoxelMaterialDefinition(byte materialIndex)
        {
            using (m_voxelMaterialsLock.AcquireSharedUsing())
            {
                MyVoxelMaterialDefinition res = null;
                m_definitions.m_voxelMaterialsByIndex.TryGetValue(materialIndex, out res);
                return res;
            }
        }

        public MyVoxelMaterialDefinition GetVoxelMaterialDefinition(string name)
        {
            using (m_voxelMaterialsLock.AcquireSharedUsing())
            {
                MyVoxelMaterialDefinition def = null;
                m_definitions.m_voxelMaterialsByName.TryGetValue(name, out def);
                return def;
            }
        }

        public bool TryGetVoxelMaterialDefinition(string name, out MyVoxelMaterialDefinition definition)
        {
            using (m_voxelMaterialsLock.AcquireSharedUsing())
            {
                return m_definitions.m_voxelMaterialsByName.TryGetValue(name, out definition);
            }
        }

        public DictionaryValuesReader<string, MyVoxelMaterialDefinition> GetVoxelMaterialDefinitions()
        {
            using (m_voxelMaterialsLock.AcquireSharedUsing())
            {
                return m_definitions.m_voxelMaterialsByName;
            }
        }

        public int VoxelMaterialCount
        {
            get
            {
                using (m_voxelMaterialsLock.AcquireSharedUsing())
                {
                    return m_definitions.m_voxelMaterialsByName.Count;
                }
            }
        }

        public int VoxelMaterialRareCount
        {
            get
            {
                return m_definitions.m_voxelMaterialRareCount;
            }
        }

        public MyVoxelMaterialDefinition GetDefaultVoxelMaterialDefinition()
        {
            return m_definitions.m_voxelMaterialsByIndex[0];
        }

        public MyBattleDefinition BattleDefinition
        {
            get { return m_definitions.m_battleDefinition; }
        }

        public MyDestructionDefinition DestructionDefinition
        {
            get { return m_definitions.m_destructionDefinition; }
        }

        #endregion

        #region Conversions to/from builders
        private static void ToDefinitions(MyModContext context,
            DefinitionDictionary<MyDefinitionBase> outputDefinitions,
            DefinitionDictionary<MyCubeBlockDefinition>[] outputCubeBlocks,
            MyObjectBuilder_CubeBlockDefinition[] cubeBlocks,
            bool failOnDebug = true)
        {
            for (int i = 0; i < cubeBlocks.Length; ++i)
            {
                var currentDef = cubeBlocks[i];

                var result = InitDefinition<MyCubeBlockDefinition>(context, currentDef);
                result.UniqueVersion = result;

                // add to cubeBlocks without variant
                Debug.Assert((int)result.CubeSize < outputCubeBlocks.Length, "CubeSize >= cubeBlocksBySize.Length");
                outputCubeBlocks[(int)result.CubeSize][result.Id] = result;

                // add to definitions (including variants for backward compatibility)
                Check(!outputDefinitions.ContainsKey(result.Id), result.Id, failOnDebug);
                outputDefinitions[result.Id] = result;

                if (!context.IsBaseGame)
                    MySandboxGame.Log.WriteLine("Created definition for: " + result.DisplayNameText);

                //if (currentDef.Variants != null)
                //{
                //    result.Color = Color.Gray;
                //    foreach (var variant in currentDef.Variants)
                //    {
                //        var variantResult = InitDefinition<MyCubeBlockDefinition>(context, currentDef, false);
                //        variantResult.UniqueVersion = result;

                //        variantResult.Id = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), variantResult.Id.SubtypeName + variant.Color);
                //        var systemColor = System.Drawing.Color.FromName(variant.Color);
                //        variantResult.Color = new Color((int)systemColor.R, (int)systemColor.G, (int)systemColor.B, (int)systemColor.A);
                //        variantResult.DisplayNameVariant = (MyStringId)Enum.Parse(typeof(MyStringId), variant.Color);

                //        result.Variants.Add(variantResult);

                //        // add variant to definitions
                //        Check(!outputDefinitions.ContainsKey(variantResult.Id), variantResult.Id, failOnDebug);
                //        outputDefinitions[variantResult.Id] = variantResult;
                //    }
                //}
            }
        }

        private static T InitDefinition<T>(MyModContext context, MyObjectBuilder_DefinitionBase builder) where T : MyDefinitionBase
        {
            T result = GetObjectFactory().CreateInstance<T>(builder.GetType());
            result.Context = new MyModContext();
            result.Context.Init(context);
            if (!context.IsBaseGame)
                UpdateModableContent(result.Context, builder);

            result.Init(builder, result.Context);

            if (MyFakes.ENABLE_ALL_IN_SURVIVAL)
            {
                result.AvailableInSurvival = true;
            }

            return result;
        }

        static void UpdateModableContent(MyModContext context, MyObjectBuilder_DefinitionBase builder)
        {
            using (Stats.Generic.Measure("UpdateModableContent", VRage.Stats.MyStatTypeEnum.CounterSum | VRage.Stats.MyStatTypeEnum.KeepInactiveLongerFlag))
            {
                foreach (FieldInfo field in builder.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    ProcessField(context, builder, field);
                }
            }
        }

        private static void ProcessField(MyModContext context, object fieldOwnerInstance, FieldInfo field, bool includeMembers = true)
        {
            var extensions = field.GetCustomAttributes(typeof(ModdableContentFileAttribute), true).Cast<ModdableContentFileAttribute>().Select(s => "." + s.FileExtension).ToArray();
            if (extensions.Length > 0 && field.FieldType == typeof(string))
            {
                string contentFile = (string)field.GetValue(fieldOwnerInstance);
                ProcessContentFilePath(context, ref contentFile, extensions, true);
                field.SetValue(fieldOwnerInstance, contentFile);
            }
            else if (field.FieldType == typeof(string[]))
            {
                string[] stringArray = (string[])field.GetValue(fieldOwnerInstance);

                if (stringArray != null)
                {
                    for (int fileIndex = 0; fileIndex < stringArray.Length; ++fileIndex)
                        ProcessContentFilePath(context, ref stringArray[fileIndex], extensions, false);

                    field.SetValue(fieldOwnerInstance, stringArray);
                }
            }
            else if (includeMembers && (field.FieldType.IsClass || (field.FieldType.IsValueType && !field.FieldType.IsPrimitive)))
            {
                var instance = field.GetValue(fieldOwnerInstance);
                var enumerable = instance as System.Collections.IEnumerable;
                if (enumerable != null)
                {
                    foreach (var x in enumerable)
                    {
                        var subfields = x.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                        if (subfields.Length > 0)
                        {
                            foreach (FieldInfo subField in subfields)
                            {
                                ProcessField(context, x, subField, false); // Process only one level members
                            }
                        }
                    }
                }
                else if (instance != null)
                {
                    ProcessSubfields(context, field, instance);
                }
            }
        }

        private static void ProcessContentFilePath(MyModContext context, ref string contentFile, object[] extensions, bool logNoExtensions)
        {
            if (string.IsNullOrEmpty(contentFile))
                return;

            string ext = Path.GetExtension(contentFile);

            if (extensions.IsNullOrEmpty())
            {
                if (logNoExtensions)
                    MyDefinitionErrors.Add(context, "No file extensions.", TErrorSeverity.Warning);
                return;
            }

            if (string.IsNullOrEmpty(ext))
            {
                MyDefinitionErrors.Add(context, "Missing file extension: " + contentFile, TErrorSeverity.Warning);
                return;
            }

            if (!extensions.Contains(ext))
            {
                MyDefinitionErrors.Add(context, "Missing file extension of: " + contentFile + ", is not contained in expected set.", TErrorSeverity.Warning);
                return;
            }

            string modedContentFile = Path.Combine(context.ModPath, contentFile);

            if (MyFileSystem.DirectoryExists(Path.GetDirectoryName(modedContentFile)) && MyFileSystem.GetFiles(Path.GetDirectoryName(modedContentFile), Path.GetFileName(modedContentFile), VRage.FileSystem.MySearchOption.TopDirectoryOnly).Count() > 0)
            {
                contentFile = modedContentFile;
                //MySandboxGame.Log.WriteLine(string.Format("ProcessField() '{0}', '{1}', '{2}'", context.ModPath, contentFile, modedContentFile));
            }
            else if (MyFileSystem.FileExists(Path.Combine(MyFileSystem.ContentPath, contentFile)))
            {
                // We might add extension
                //field.SetValue(fieldOwnerInstance, contentFile);
                //MySandboxGame.Log.WriteLine(string.Format("ProcessField() couldnt find: '{0}', '{1}', '{2}'", context.ModPath, contentFile, modedContentFile));
            }
            else
            {
                MyDefinitionErrors.Add(context, "Resource not found, setting to null or error model. Resource name: " + contentFile, TErrorSeverity.Error);

                if (contentFile.EndsWith(".mwm"))
                {
                    contentFile = @"Models\Debug\Error.mwm";
                }
                else
                {
                    contentFile = null;
                }
            }
        }

        private static void ProcessSubfields(MyModContext context, FieldInfo field, object instance)
        {
            foreach (FieldInfo subField in field.FieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                ProcessField(context, instance, subField, true);
            }
        }

        public void Save(string filePattern = "*.*")
        {
#if XB1
            Debug.Assert(false, "Save Definitions not supported on XB1.");
#else
            Regex regex = FindFilesPatternToRegex.Convert(filePattern);

            Dictionary<string, List<MyDefinitionBase>> defs = new Dictionary<string, List<MyDefinitionBase>>();
            foreach (var defPair in m_definitions.m_definitionsById)
            {
                if (string.IsNullOrEmpty(defPair.Value.Context.CurrentFile))
                    continue;

                string fileName = Path.GetFileName(defPair.Value.Context.CurrentFile);

                if (!regex.IsMatch(fileName))
                    continue;

                List<MyDefinitionBase> defList = null;
                if (!defs.ContainsKey(defPair.Value.Context.CurrentFile))
                    defs.Add(defPair.Value.Context.CurrentFile, defList = new List<MyDefinitionBase>());
                else
                    defList = defs[defPair.Value.Context.CurrentFile];

                defList.Add(defPair.Value);
            }

            foreach (var defPair in defs)
            {
                var objBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Definitions>();
                var defList = new List<MyObjectBuilder_DefinitionBase>();

                foreach (var def in defPair.Value)
                {
                    var ob = def.GetObjectBuilder();
                    defList.Add(ob);
                }

                //TODO: Add here all needed properties
                objBuilder.CubeBlocks = defList.OfType<MyObjectBuilder_CubeBlockDefinition>().ToArray();

                MyObjectBuilderSerializer.SerializeXML(defPair.Key, false, objBuilder);
            }
#endif
        }

        #endregion

    }
}
