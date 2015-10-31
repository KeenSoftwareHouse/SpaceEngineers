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
using VRage.Game.Systems;
using VRage.Plugins;
using VRage.Utils;

namespace Sandbox.Game.SessionComponents
{
    [PreloadRequired]
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, Priority = 300)]
    public class MyScriptedGroupsSystem : MySessionComponentBase
    {
        [MessageId(1563, P2PMessageEnum.Reliable)]
        struct RunScriptMsg
        {
            public MyStringHash ScriptId;
        }

        private static MyScriptedGroupsSystem Static;
        private Queue<MyStringHash> m_scriptsQueue;

        private Dictionary<MyStringHash, MyGroupScriptBase> m_groupScripts;
        private Dictionary<MyStringHash, MyScriptedGroupDefinition> m_definitions;

        static MyScriptedGroupsSystem()
        {
            MySyncLayer.RegisterMessage<RunScriptMsg>(OnRunScriptRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
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

            LoadScripts(MyPlugins.GameAssembly);
            LoadScripts(MyPlugins.SandboxGameAssembly);

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

            foreach (var type in assembly.GetTypes())
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
            if (!Sync.IsServer && !MyMultiplayer.Static.IsAdmin(MySteam.UserId))
                return;
            var scriptId = MyStringHash.Get(scriptName);
            if (Sync.IsServer)
                Static.RunScriptInternal(scriptId);
            else
                SendScriptRequest(scriptId);
        }

        private static void SendScriptRequest(MyStringHash stringId)
        {
            var msg = new RunScriptMsg() { ScriptId = stringId };
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private static void OnRunScriptRequest(ref RunScriptMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(Sync.IsServer || MyMultiplayer.Static.IsAdmin(sender.SteamUserId));
            if (!Sync.IsServer && !MyMultiplayer.Static.IsAdmin(sender.SteamUserId))
                return;
            Static.RunScriptInternal(msg.ScriptId);
        }
    }
}
