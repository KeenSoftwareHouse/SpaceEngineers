using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Plugins;
using VRage.Utils;
using VRage.Collections;

namespace Sandbox.Game.World
{
    public sealed partial class MySession
    {
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

        private void RegisterComponentsFromAssemblies()
        {
            var execAssembly = Assembly.GetExecutingAssembly();
            var refs = execAssembly.GetReferencedAssemblies();

            // Prepare final session component lists
            Static.m_componentsToLoad = new HashSet<string>();
            m_componentsToLoad.UnionWith(GameDefinition.SessionComponents);
            m_componentsToLoad.RemoveWhere(x => SessionComponentDisabled.Contains(x));
            m_componentsToLoad.UnionWith(SessionComponentEnabled);

            foreach (var assemblyName in refs)
            {
                try
                {
                    //MySandboxGame.Log.WriteLine("a:" + assemblyName.Name);

                    if (assemblyName.Name.Contains("Sandbox") || assemblyName.Name.Equals("VRage.Game"))
                    {
                        //MySandboxGame.Log.WriteLine("b:" + assemblyName.Name);

                        Assembly assembly = Assembly.Load(assemblyName);
                        object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                        if (attributes.Length > 0)
                        {
                            //MySandboxGame.Log.WriteLine("c:" + assemblyName.Name);

                            AssemblyProductAttribute product = attributes[0] as AssemblyProductAttribute;
                            if (product.Product == "Sandbox" || product.Product == "VRage.Game")
                            {
                                //MySandboxGame.Log.WriteLine("d:" + assemblyName.Name);
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
        }

        private readonly CachingDictionary<Type, MySessionComponentBase> m_sessionComponents = new CachingDictionary<Type, MySessionComponentBase>();
        private readonly Dictionary<int, SortedSet<MySessionComponentBase>> m_sessionComponentsForUpdate = new Dictionary<int, SortedSet<MySessionComponentBase>>();

        // Session components to be actually loaded
        private HashSet<string> m_componentsToLoad;

        // Session component overrides, these are which components are enabled over the default from definition
        public HashSet<string> SessionComponentEnabled = new HashSet<string>();

        // Session component overrides, these are which components are disabled over the default from definition
        public HashSet<string> SessionComponentDisabled = new HashSet<string>();


        public T GetSessionComponent<T>() where T : MySessionComponentBase
        {
            MySessionComponentBase comp;
            m_sessionComponents.TryGetValue(typeof(T), out comp);
            return comp as T;
        }

        public void RegisterComponent(MySessionComponentBase component, MyUpdateOrder updateOrder, int priority)
        {
            m_sessionComponents.Add(component.ComponentType, component);

            AddComponentForUpdate(updateOrder, MyUpdateOrder.BeforeSimulation, component);
            AddComponentForUpdate(updateOrder, MyUpdateOrder.Simulation, component);
            AddComponentForUpdate(updateOrder, MyUpdateOrder.AfterSimulation, component);
            AddComponentForUpdate(updateOrder, MyUpdateOrder.NoUpdate, component);
        }

        public void UnregisterComponent(MySessionComponentBase component)
        {
            m_sessionComponents.Remove(component.ComponentType);
        }

        public void RegisterComponentsFromAssembly(Assembly assembly, bool modAssembly = false)
        {
            if (assembly == null)
                return;
            MySandboxGame.Log.WriteLine("Registered modules from: " + assembly.FullName);

            foreach (Type type in assembly.GetTypes())
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
                if (MyFakes.ENABLE_LOAD_NEEDED_SESSION_COMPONENTS)
                {
                    var component = (MySessionComponentBase)Activator.CreateInstance(type);
                    Debug.Assert(component != null, "Session component is cannot be created by activator");

                    if (component.IsRequiredByGame)
                        RegisterComponent(component, component.UpdateOrder, component.Priority);
                }
                else if (modAssembly || m_componentsToLoad.Contains(type.Name) || m_componentsToLoad.Contains(type.AssemblyQualifiedName))
                {
                    var component = (MySessionComponentBase)Activator.CreateInstance(type);
                    Debug.Assert(component != null, "Session component is cannot be created by activator");

                    RegisterComponent(component, component.UpdateOrder, component.Priority);
                }
            }
            catch (Exception)
            {
                MySandboxGame.Log.WriteLine("Exception during loading of type : " + type.Name);
            }
        }

        private void AddComponentForUpdate(MyUpdateOrder updateOrder, MyUpdateOrder insertIfOrder, MySessionComponentBase component)
        {
            if ((updateOrder & insertIfOrder) != insertIfOrder) return;
            SortedSet<MySessionComponentBase> componentList = null;

            if (!m_sessionComponentsForUpdate.TryGetValue((int)insertIfOrder, out componentList))
            {
                m_sessionComponentsForUpdate.Add((int)insertIfOrder, componentList = new SortedSet<MySessionComponentBase>(SessionComparer));
            }

            componentList.Add(component);
        }

        public void LoadObjectBuildersComponents(List<MyObjectBuilder_SessionComponent> objectBuilderData)
        {
            var mappedObjectBuilders = MySessionComponentMapping.GetMappedSessionObjectBuilders(objectBuilderData);
            MyObjectBuilder_SessionComponent tmpOb = null;
            foreach (var comp in m_sessionComponents.Values)
            {
                if (mappedObjectBuilders.TryGetValue(comp.ComponentType, out tmpOb))
                {
                    comp.Init(tmpOb);
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

        public void LoadDataComponents(bool registerEvents = true)
        {
            RaiseOnLoading();

            if (registerEvents)
            {
                if (SyncLayer.AutoRegisterGameEvents)
                    SyncLayer.RegisterGameEvents();

                Sync.Clients.SetLocalSteamId(Sync.MyId, createLocalClient: !(MyMultiplayer.Static is MyMultiplayerClient));
                Sync.Players.RegisterEvents();
            }

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

            var hash = component.DebugName.GetHashCode();
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
            Static.sessionSimSpeedPlayer = 0f;
            Static.sessionSimSpeedServer = 0f;

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
