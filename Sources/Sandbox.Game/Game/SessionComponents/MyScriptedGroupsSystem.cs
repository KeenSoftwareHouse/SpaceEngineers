using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Systems;
using VRage.Network;
using VRage.Plugins;
using VRage.Utils;

namespace Sandbox.Game.SessionComponents
{
    [StaticEventOwner]
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, Priority = 300)]
    public class MyScriptedGroupsSystem : MySessionComponentBase
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private bool m_scriptsLoaded = false;
#endif // !XB1

        private static MyScriptedGroupsSystem Static;
        private Queue<MyStringHash> m_scriptsQueue;

        private Dictionary<MyStringHash, MyGroupScriptBase> m_groupScripts;
        private Dictionary<MyStringHash, MyScriptedGroupDefinition> m_definitions;

        static MyScriptedGroupsSystem()
        {
        }

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.Game == GameEnum.ME_GAME && Sync.IsServer;
            }
        }

        public override void LoadData()
        {
            base.LoadData();

            m_scriptsQueue = new Queue<MyStringHash>();
            m_groupScripts = new Dictionary<MyStringHash, MyGroupScriptBase>(MyStringHash.Comparer);
            m_definitions = new Dictionary<MyStringHash, MyScriptedGroupDefinition>(MyStringHash.Comparer);

#if XB1 // XB1_ALLINONEASSEMBLY
            LoadScripts(MyAssembly.AllInOneAssembly);
#else // !XB1
            LoadScripts(MyPlugins.GameAssembly);
            LoadScripts(MyPlugins.SandboxGameAssembly);
#endif // !XB1

            var definitions = MyDefinitionManager.Static.GetScriptedGroupDefinitions();
            foreach (var def in definitions)
            {
                m_definitions[def.Id.SubtypeId] = def;
            }

            Static = this;
        }

        private void LoadScripts(Assembly assembly)
        {
            if (assembly == null)
                return;

#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_scriptsLoaded == false);
            if (m_scriptsLoaded == true)
                return;
            m_scriptsLoaded = true;
            foreach (var type in MyAssembly.GetTypes())
#else // !XB1
            foreach (var type in assembly.GetTypes())
#endif // !XB1
            {
                var attrs = type.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    if (attr is MyScriptedSystemAttribute)
                    {
                        var scriptAttribute = attr as MyScriptedSystemAttribute;
                        var stringId = MyStringHash.GetOrCompute(scriptAttribute.ScriptName);
                        Debug.Assert(!m_groupScripts.ContainsKey(stringId));
                        m_groupScripts[stringId] = Activator.CreateInstance(type) as MyGroupScriptBase;
                    }
                }
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            Static = null;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (m_scriptsQueue.Count > 0)
            {
                while (m_scriptsQueue.Count > 0)
                {
                    var scriptName = m_scriptsQueue.Dequeue();
                    MyGroupScriptBase script = null;
                    MyScriptedGroupDefinition definition = null;
                    if (m_definitions.TryGetValue(scriptName, out definition))
                    {
                        if (m_groupScripts.TryGetValue(definition.Script, out script))
                            script.ProcessObjects(definition.ListReader);
                    }  
                }
            }
        }

        private void RunScriptInternal(MyStringHash scriptName)
        {
            m_scriptsQueue.Enqueue(scriptName);
        }

        public static void RunScript(string scriptName)
        {
            if (!Sync.IsServer && !MySession.Static.IsUserAdmin(Sync.MyId))
                return;
            var scriptId = MyStringHash.Get(scriptName);
            if (Sync.IsServer)
                Static.RunScriptInternal(scriptId);
            else
                SendScriptRequest(scriptId);
        }

        private static void SendScriptRequest(MyStringHash stringId)
        {
            MyMultiplayer.RaiseStaticEvent(x => RunScriptRequest, stringId);
        }

        [Event,Reliable,Server]
        static void RunScriptRequest(MyStringHash stringId)
        {

            Debug.Assert(Sync.IsServer || MySession.Static.IsUserAdmin(MyEventContext.Current.Sender.Value));
            if (!Sync.IsServer && !MySession.Static.IsUserAdmin(MyEventContext.Current.Sender.Value))
                return;
            Static.RunScriptInternal(stringId);
        }


    }
}
