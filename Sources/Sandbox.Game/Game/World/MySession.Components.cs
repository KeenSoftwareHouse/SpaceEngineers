using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Utils;
using VRage.Collections;
using VRage.Game.Definitions;
using VRage.Profiler;
using VRageMath;
using Sandbox.Graphics;

namespace Sandbox.Game.World
{
    public sealed partial class MySession
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private bool m_registered = false;
#endif // XB1

        #region Components

        private class ComponentComparer : IComparer<MySessionComponentBase>
        {
            public int Compare(MySessionComponentBase x, MySessionComponentBase y)
            {
                int prior = x.Priority.CompareTo(y.Priority);
                if (prior == 0) return String.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
                return prior;
            }
        }

        private static readonly ComponentComparer SessionComparer = new ComponentComparer();

        private void PrepareBaseSession(List<MyObjectBuilder_Checkpoint.ModItem> mods, MyScenarioDefinition definition = null)
        {
            ScriptManager.Init(null);
            MyDefinitionManager.Static.LoadData(mods);

            LoadGameDefinition(definition != null ? definition.GameDefinition : MyGameDefinition.Default);

            Scenario = definition;
            if (definition != null)
            {
                WorldBoundaries = definition.WorldBoundaries;

                MySector.EnvironmentDefinition = MyDefinitionManager.Static.GetDefinition<MyEnvironmentDefinition>(definition.Environment);
            }

            MySector.InitEnvironmentSettings();

            LoadDataComponents();
            InitDataComponents();
        }

        private void PrepareBaseSession(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
        {
            ScriptManager.Init(checkpoint.ScriptManagerData);
            MyDefinitionManager.Static.LoadData(checkpoint.Mods);

            LoadGameDefinition(checkpoint);

            var fonts = MyDefinitionManager.Static.GetFontDefinitions();
            foreach (var font in fonts)
            {
                if (!MyGuiManager.FontExists(font.Id.SubtypeId.String))
                {
                    VRageRender.MyRenderProxy.CreateFont((int)font.Id.SubtypeId, font.Path, false);
                }
            }


            MyDefinitionManager.Static.TryGetDefinition<MyScenarioDefinition>(checkpoint.Scenario, out Scenario);

            WorldBoundaries = checkpoint.WorldBoundaries;

            FixIncorrectSettings(Settings);

            // Use whatever setting is in scenario if there was nothing in the file (0 min and max).
            // SE scenarios have nothing while ME scenarios have size defined.
            if (!WorldBoundaries.HasValue && Scenario != null)
                WorldBoundaries = Scenario.WorldBoundaries;

            MySector.InitEnvironmentSettings(sector.Environment);

            LoadDataComponents();
            LoadObjectBuildersComponents(checkpoint.SessionComponents);
        }

        private void RegisterComponentsFromAssemblies()
        {
#if !XB1 // XB1_ALLINONEASSEMBLY
            var execAssembly = Assembly.GetExecutingAssembly();
            var refs = execAssembly.GetReferencedAssemblies();
#endif // !XB1

            // Prepare final session component lists
            m_componentsToLoad = new HashSet<string>();
            m_componentsToLoad.UnionWith(GameDefinition.SessionComponents.Keys);
            m_componentsToLoad.RemoveWhere(x => SessionComponentDisabled.Contains(x));
            m_componentsToLoad.UnionWith(SessionComponentEnabled);

#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterComponentsFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            foreach (var assemblyName in refs)
            {
                try
                {
                    // TODO: Re-gig this awful code
                    if (assemblyName.Name.Contains("Sandbox") || assemblyName.Name.Equals("VRage.Game"))
                    {
                        Assembly assembly = Assembly.Load(assemblyName);
                        object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                        if (attributes.Length > 0)
                        {
                            AssemblyProductAttribute product = attributes[0] as AssemblyProductAttribute;
                            if (product.Product == "Sandbox" || product.Product == "VRage.Game")
                            {
                                RegisterComponentsFromAssembly(assembly);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("Error while resolving session components assemblies");
                    MyLog.Default.WriteLine(e.ToString());
                }
            }

            try
            {
                foreach (var assembly in ScriptManager.Scripts.Values)
                {
                    RegisterComponentsFromAssembly(assembly, true);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error while loading modded session components");
                MyLog.Default.WriteLine(e.ToString());
            }

            try
            {
                foreach (var plugin in MyPlugins.Plugins)
                {
                    RegisterComponentsFromAssembly(plugin.GetType().Assembly, true);
                }
            }
            catch (Exception e)
            { }

            try { RegisterComponentsFromAssembly(MyPlugins.GameAssembly); }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error while resolving session components MOD assemblies");
                MyLog.Default.WriteLine(e.ToString());
            }
            try { RegisterComponentsFromAssembly(MyPlugins.UserAssembly); }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error while resolving session components MOD assemblies");
                MyLog.Default.WriteLine(e.ToString());
            }

            RegisterComponentsFromAssembly(execAssembly);
#endif // !XB1
        }

        private readonly CachingDictionary<Type, MySessionComponentBase> m_sessionComponents = new CachingDictionary<Type, MySessionComponentBase>();
        private readonly Dictionary<int, SortedSet<MySessionComponentBase>> m_sessionComponentsForUpdate = new Dictionary<int, SortedSet<MySessionComponentBase>>();

        // Session components to be actually loaded
        private HashSet<string> m_componentsToLoad;

        // Session component overrides, these are which components are enabled over the default from definition
        public HashSet<string> SessionComponentEnabled = new HashSet<string>();

        // Session component overrides, these are which components are disabled over the default from definition
        public HashSet<string> SessionComponentDisabled = new HashSet<string>();


        public T GetComponent<T>() where T : MySessionComponentBase
        {
            MySessionComponentBase comp;
            m_sessionComponents.TryGetValue(typeof(T), out comp);
            return comp as T;
        }

        public void RegisterComponent(MySessionComponentBase component, MyUpdateOrder updateOrder, int priority)
        {
            // TODO: Better handling of component overrides
            //if(m_sessionComponents.ContainsKey(component.ComponentType))
            m_sessionComponents[component.ComponentType] = component;
            component.Session = this;
            AddComponentForUpdate(updateOrder, component);
            m_sessionComponents.ApplyChanges();
        }

        public void UnregisterComponent(MySessionComponentBase component)
        {
            component.Session = null;
            m_sessionComponents.Remove(component.ComponentType);
        }

        public void RegisterComponentsFromAssembly(Assembly assembly, bool modAssembly = false)
        {
            if (assembly == null)
                return;

#if XB1 // XB1_ALLINONEASSEMBLY
            MySandboxGame.Log.WriteLine("Registered modules from: N/A (on XB1)");

            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (Type type in MyAssembly.GetTypes())
#else // !XB1
            MySandboxGame.Log.WriteLine("Registered modules from: " + assembly.FullName);

            foreach (Type type in assembly.GetTypes())
#endif // !XB1
            {
                if (Attribute.IsDefined(type, typeof(MySessionComponentDescriptor)))
                {
                    TryRegisterSessionComponent(type, modAssembly);
                }
            }
        }

        private void TryRegisterSessionComponent(Type type, bool modAssembly)
        {
            try
            {
                MyDefinitionId? definition = default(MyDefinitionId?);

                var component = (MySessionComponentBase)Activator.CreateInstance(type);
                Debug.Assert(component != null, "Session component cannot be created by activator");

                if (component.IsRequiredByGame || modAssembly || GetComponentInfo(type, out definition))
                {
                    RegisterComponent(component, component.UpdateOrder, component.Priority);

                    GetComponentInfo(type, out definition);
                    component.Definition = definition;
                }
            }
            catch (Exception)
            {
                MySandboxGame.Log.WriteLine("Exception during loading of type : " + type.Name);
            }
        }

        private bool GetComponentInfo(Type type, out MyDefinitionId? definition)
        {
            string name = null;
            if (m_componentsToLoad.Contains(type.Name)) name = type.Name;
            else if (m_componentsToLoad.Contains(type.FullName)) name = type.FullName;

            if (name != null)
            {
                GameDefinition.SessionComponents.TryGetValue(name, out definition);
                return true;
            }
            definition = default(MyDefinitionId?);
            return false;
        }

        public void AddComponentForUpdate(MyUpdateOrder updateOrder, MySessionComponentBase component)
        {
            for (int i = 0; i <= 2; ++i)
            {
                if (((int)updateOrder & (1 << i)) == 0) continue;

                SortedSet<MySessionComponentBase> componentList = null;

                if (!m_sessionComponentsForUpdate.TryGetValue(1 << i, out componentList))
                {
                    m_sessionComponentsForUpdate.Add(1 << i, componentList = new SortedSet<MySessionComponentBase>(SessionComparer));
                }

                componentList.Add(component);
            }
        }

        public void SetComponentUpdateOrder(MySessionComponentBase component, MyUpdateOrder order)
        {
            for (int i = 0; i <= 2; ++i)
            {
                SortedSet<MySessionComponentBase> componentList = null;
                if ((order & (MyUpdateOrder)(1 << i)) != 0)
                {
                    if (!m_sessionComponentsForUpdate.TryGetValue(1 << i, out componentList))
                    {
                        componentList = new SortedSet<MySessionComponentBase>();
                        m_sessionComponentsForUpdate.Add(i, componentList);
                    }
                    componentList.Add(component);
                }
                else
                {
                    if (m_sessionComponentsForUpdate.TryGetValue(1 << i, out componentList))
                    {
                        componentList.Remove(component);
                    }
                }
            }
        }

        public void LoadObjectBuildersComponents(List<MyObjectBuilder_SessionComponent> objectBuilderData)
        {
            foreach (var obdata in objectBuilderData)
            {
                Type scType;
                MySessionComponentBase comp;
                if ((scType = MySessionComponentMapping.TryGetMappedSessionComponentType(obdata.GetType())) != null
                    && m_sessionComponents.TryGetValue(scType, out comp))
                {
                    comp.Init(obdata);
                }
            }

            InitDataComponents();
        }

        private void InitDataComponents()
        {
            foreach (var comp in m_sessionComponents.Values)
            {
                if (!comp.Initialized)
                {
                    MyObjectBuilder_SessionComponent compob = null;
                    if (comp.ObjectBuilderType != MyObjectBuilderType.Invalid)
                        compob = (MyObjectBuilder_SessionComponent)Activator.CreateInstance(comp.ObjectBuilderType);

                    comp.Init(compob);
                }
            }
        }

        public void RegisterEvents()
        {
            if (SyncLayer.AutoRegisterGameEvents)
                SyncLayer.RegisterGameEvents();

            Sync.Clients.SetLocalSteamId(Sync.MyId, createLocalClient: !(MyMultiplayer.Static is MyMultiplayerClient));
            Sync.Players.RegisterEvents();

            SetAsNotReady();
        }

        public void LoadDataComponents()
        {
            RaiseOnLoading();

            if (SyncLayer.AutoRegisterGameEvents)
                SyncLayer.RegisterGameEvents();

            Sync.Clients.SetLocalSteamId(Sync.MyId, createLocalClient: !(MyMultiplayer.Static is MyMultiplayerClient));
            Sync.Players.RegisterEvents();

            SetAsNotReady();

            HashSet<MySessionComponentBase> processedComponents = new HashSet<MySessionComponentBase>();

            do
            {
                m_sessionComponents.ApplyChanges();
                foreach (var comp in m_sessionComponents.Values)
                {
                    if (processedComponents.Contains(comp)) continue;
                    LoadComponent(comp);
                    processedComponents.Add(comp);
                }
            } while (m_sessionComponents.HasChanges());
        }

        private void LoadComponent(MySessionComponentBase component)
        {
            if (component.Loaded)
                return;

            foreach (var dependency in component.Dependencies)
            {
                MySessionComponentBase comp;
                m_sessionComponents.TryGetValue(dependency, out comp);

                if (comp == null)
                    continue;
                LoadComponent(comp);
            }

            if (!m_loadOrder.Contains(component))
                m_loadOrder.Add(component);
            else
            {
                var message = string.Format("Circular dependency: {0}", component.DebugName);
                MySandboxGame.Log.WriteLine(message);
                throw new Exception(message);
            }

            ProfilerShort.Begin(component.DebugName);
            component.LoadData();
            component.AfterLoadData();
            ProfilerShort.End();
        }

        public void UnloadDataComponents(bool beforeLoadWorld = false)
        {
            // Unload in reverse so dependencies can be relied on.
            for (int i = m_loadOrder.Count - 1; i >= 0; --i)
                m_loadOrder[i].UnloadDataConditional();

            //foreach (var component in m_sessionComponents)
            //{
            //    component.UnloadDataConditional();
            //}

            MySessionComponentMapping.Clear();

            m_sessionComponents.Clear();
            m_loadOrder.Clear();

            foreach (var compList in m_sessionComponentsForUpdate.Values)
            {
                compList.Clear();
            }

            if (!beforeLoadWorld)
            {
                Sync.Players.UnregisterEvents();
                Sync.Clients.Clear();
                MyNetworkReader.Clear();
            }

            m_lasers.Clear();

            Ready = false;
        }

        public void BeforeStartComponents()
        {
            Static.TotalDamageDealt = 0;
            Static.TotalBlocksCreated = 0;

            ElapsedPlayTime = new TimeSpan();
            m_timeOfSave = MySandboxGame.Static.UpdateTime;

            MyFpsManager.Reset();
            MyAnalyticsHelper.ReportGameplayStart();
            foreach (var component in m_sessionComponents.Values)
            {
                component.BeforeStart();
            }
        }

        public void UpdateComponents()
        {
            ProfilerShort.Begin("Before simulation");
            SortedSet<MySessionComponentBase> components = null;
            if (m_sessionComponentsForUpdate.TryGetValue((int)MyUpdateOrder.BeforeSimulation, out components))
            {
                foreach (var component in components)
                {
                    ProfilerShort.Begin(component.ToString());
                    if (component.UpdatedBeforeInit() || MySandboxGame.IsGameReady)
                    {
                        component.UpdateBeforeSimulation();
                    }
                    ProfilerShort.End();
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Simulate");
            if (m_sessionComponentsForUpdate.TryGetValue((int)MyUpdateOrder.Simulation, out components))
            {
                foreach (var component in components)
                {
                    ProfilerShort.Begin(component.ToString());
                    if (component.UpdatedBeforeInit() || MySandboxGame.IsGameReady)
                    {
                        component.Simulate();
                    }
                    ProfilerShort.End();
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("After simulation");
            if (m_sessionComponentsForUpdate.TryGetValue((int)MyUpdateOrder.AfterSimulation, out components))
            {
                foreach (var component in components)
                {
                    ProfilerShort.Begin(component.ToString());
                    if (component.UpdatedBeforeInit() || MySandboxGame.IsGameReady)
                    {
                        component.UpdateAfterSimulation();
                    }
                    ProfilerShort.End();
                }
            }
            ProfilerShort.End();
        }

        #endregion
    }
}
