using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Components.Session;
using VRage.Game.ObjectBuilders.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.SessionComponents
{
    [StaticEventOwner]
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, 666, typeof(MyObjectBuilder_SessionComponentResearch))]
    public class MySessionComponentResearch : MySessionComponentBase
    {
        public bool DEBUG_SHOW_RESEARCH = false;
        public bool DEBUG_SHOW_RESEARCH_PRETTY = true;

        public static MySessionComponentResearch Static;

        public Dictionary<long, HashSet<MyDefinitionId>> m_unlockedResearch; // unlocked definitions for players
        public List<MyDefinitionId> m_requiredResearch; // definitions that are only available after being researched

        public MyHudNotification m_unlockedResearchNotification;
        public MyHudNotification m_knownResearchNotification;

        public bool WhitelistMode { get; private set; }

        public override bool IsRequiredByGame
        {
            get { return MyPerGameSettings.EnableResearch; }
        }
        
        public override Type[] Dependencies
        {
            get { return base.Dependencies; }
        }

        public MySessionComponentResearch()
        {
            Static = this;

            m_unlockedResearch = new Dictionary<long, HashSet<MyDefinitionId>>();
            m_requiredResearch = new List<MyDefinitionId>();

            m_unlockedResearchNotification = new MyHudNotification(font: MyFontEnum.White, priority: 2, text: MyCommonTexts.NotificationResearchUnlocked);
            m_knownResearchNotification = new MyHudNotification(font: MyFontEnum.Red, priority: 2, text: MyCommonTexts.NotificationResearchKnown);

        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            if (MyMultiplayer.Static != null && !MyMultiplayer.Static.IsServer)
                MyMultiplayer.RaiseStaticEvent(x => RequestCurrentState);

            var builder = sessionComponent as MyObjectBuilder_SessionComponentResearch;

            if (builder == null || builder.Researches == null)
                return;

            WhitelistMode = builder.WhitelistMode;
            foreach (var research in builder.Researches)
            {
                var definitions = new HashSet<MyDefinitionId>();
                foreach (var definition in research.Definitions)
                    definitions.Add(definition);

                m_unlockedResearch.Add(research.IdentityId, definitions);
            }
        }

        public override void InitFromDefinition(MySessionComponentDefinition definition)
        {
            base.InitFromDefinition(definition);

            if (MyMultiplayer.Static != null && !MyMultiplayer.Static.IsServer)
            {
                MyMultiplayer.RaiseStaticEvent(x => RequestCurrentState);
            }
            else
            {
                var def = definition as MySessionComponentResearchDefinition;
                if (def == null)
                    return;

                WhitelistMode = def.WhitelistMode;

                foreach (var id in def.Researches)
                {
                    var researchDef = MyDefinitionManager.Static.GetDefinition<MyResearchDefinition>(id);
                    foreach (var defId in researchDef.Entries)
                        m_requiredResearch.Add(defId);
                }
            }
        }

        [Event, Reliable, Server]
        private static void RequestCurrentState()
        {
            List<SerializableDefinitionId> ids = new List<SerializableDefinitionId>();
            foreach (var id in Static.m_requiredResearch)
                ids.Add((SerializableDefinitionId)id);
            MyMultiplayer.RaiseStaticEvent(x => SendCurrentState, ids, Static.WhitelistMode, MyEventContext.Current.Sender);
        }

        [Event, Reliable, Client]
        private static void SendCurrentState(List<SerializableDefinitionId> requiredList, bool whitelist)
        {
            Static.m_requiredResearch.Clear();
            foreach (var id in requiredList)
            {
                MyDefinitionBase definition;
                if (!MyDefinitionManager.Static.TryGetDefinition(id, out definition))
                    continue;
                Static.m_requiredResearch.Add(definition.Id);
            }
            Static.WhitelistMode = whitelist;
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            var ob = new MyObjectBuilder_SessionComponentResearch();

            ob.Researches = new List<MyObjectBuilder_SessionComponentResearch.ResearchData>();
            ob.WhitelistMode = WhitelistMode;
            foreach (var research in m_unlockedResearch)
            {
                if (research.Value.Count == 0)
                    continue;

                var definitions = new List<SerializableDefinitionId>();
                foreach (var definition in research.Value) {
                    definitions.Add(definition);
                }

                ob.Researches.Add(new MyObjectBuilder_SessionComponentResearch.ResearchData()
                {
                    IdentityId = research.Key,
                    Definitions = definitions
                });
            }

            return ob;
        }

        //public override void LoadData()
        //{
        //    base.LoadData();

        //    m_unlockedResearch = new Dictionary<long, HashSet<MyDefinitionId>>();
        //    m_requiredResearch = new List<MyDefinitionId>();

        //    var researchDefinitions = MyDefinitionManager.Static.GetDefinitions<MyResearchDefinition>();
        //    if (researchDefinitions != null)
        //    {
        //        foreach (var research in researchDefinitions)
        //            foreach (var defId in research.Entries)
        //                m_requiredResearch.Add(defId);
        //    }
        //}

        protected override void UnloadData()
        {
            base.UnloadData();

            m_unlockedResearch = null;
            m_requiredResearch = null;
        }

        public bool TryGetResearch(MyCharacter character, out HashSet<MyDefinitionId> research)
        {
            if (character == null)
            {
                research = null;
                return false;
            }

            return TryGetResearch(character.GetPlayerIdentityId(), out research);
        }

        public bool TryGetResearch(long identityId, out HashSet<MyDefinitionId> research)
        {
            if (m_unlockedResearch.TryGetValue(identityId, out research))
                return true;

            research = null;
            return false;
        }

        public bool UnlockResearch(MyCharacter character, MyDefinitionId id)
        {
            Debug.Assert(Sync.IsServer);
            if (character == null)
                return false;

            var definition = MyDefinitionManager.Static.GetDefinition(id);
            SerializableDefinitionId serializableId = id;
            if (CanUnlockResearch(character.GetPlayerIdentityId(), definition))
            {
                MyMultiplayer.RaiseStaticEvent(x => UnlockResearchSuccess, character.GetPlayerIdentityId(), serializableId);
                return true;
            }
            else if (character.ControllerInfo.Controller != null)
            {
                var endpoint = new EndpointId(character.ControllerInfo.Controller.Player.Client.SteamUserId);
                MyMultiplayer.RaiseStaticEvent(x => UnlockResearchFailed, serializableId, targetEndpoint: endpoint);
            }

            return false;
        }

        private bool CanUnlockResearch(long identityId, MyDefinitionBase definition)
        {
            HashSet<MyDefinitionId> unlockedItems;
            if (!m_unlockedResearch.TryGetValue(identityId, out unlockedItems) || unlockedItems == null)
                unlockedItems = new HashSet<MyDefinitionId>();

            if (unlockedItems.Contains(definition.Id))
                return false;

            return true;
        }

        [Event, Reliable, Server, Broadcast]
        private static void UnlockResearchSuccess(long identityId, SerializableDefinitionId id)
        {
            MyDefinitionBase definition;
            if (!MyDefinitionManager.Static.TryGetDefinition(id, out definition))
            {
                Debug.Assert(false, "Stooooooopid Definition is not here!");
                return;
            }

            HashSet<MyDefinitionId> unlockedItems;
            if (!Static.m_unlockedResearch.TryGetValue(identityId, out unlockedItems) || unlockedItems == null)
                unlockedItems = new HashSet<MyDefinitionId>();

            var research = definition as MyResearchDefinition;
            if (research != null)
            {
                foreach (var entry in research.Entries)
                    unlockedItems.Add(entry);
            }
            unlockedItems.Add(definition.Id);
            Static.m_unlockedResearch[identityId] = unlockedItems;

            if (MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.GetPlayerIdentityId() == identityId)
            {
                Static.m_unlockedResearchNotification.SetTextFormatArguments(definition.DisplayNameText);
                MyHud.Notifications.Add(Static.m_unlockedResearchNotification);
            }
        }

        [Event, Reliable, Client]
        private static void UnlockResearchFailed(SerializableDefinitionId id)
        {
            MyDefinitionBase definition;
            if (!MyDefinitionManager.Static.TryGetDefinition(id, out definition))
            {
                Debug.Assert(false, "Stooooooopid Definition is not here!");
                return;
            }

            Static.m_knownResearchNotification.SetTextFormatArguments(definition.DisplayNameText);
            MyHud.Notifications.Add(Static.m_knownResearchNotification);
        }

        public bool CanUse(MyCharacter character, MyDefinitionId id)
        {
            if (character == null)
                return true;

            return CanUse(character.GetPlayerIdentityId(), id);
        }

        public bool CanUse(long identityId, MyDefinitionId id)
        {
            if (RequiresResearch(id))
                return IsResearchUnlocked(identityId, id);
            else
                return true;
        }

        public bool RequiresResearch(MyDefinitionId id)
        {
            if(WhitelistMode)
                return !m_requiredResearch.Contains(id);
            else
                return m_requiredResearch.Contains(id);
        }

        public bool IsResearchUnlocked(MyCharacter character, MyDefinitionId id)
        {
            if (character == null)
                return true;

            return IsResearchUnlocked(character.GetPlayerIdentityId(), id);
        }

        public bool IsResearchUnlocked(long identityId, MyDefinitionId id)
        {
            HashSet<MyDefinitionId> unlockedItems;
            if (!m_unlockedResearch.TryGetValue(identityId, out unlockedItems))
                return false;

            return (unlockedItems != null) && unlockedItems.Contains(id);
        }

        public void ResetResearch(MyCharacter character)
        {
            if (character == null)
                return;

            ResetResearch(character.GetPlayerIdentityId());
        }

        public void ResetResearch(long identityId)
        {
            MyMultiplayer.RaiseStaticEvent(x => ResetResearchSync, identityId);
        }

        public void DebugUnlockAllResearch(MyCharacter character)
        {
            if (character == null)
                return;

            long identityId = character.GetPlayerIdentityId();
            HashSet<MyDefinitionId> unlockedItems;
            if (!m_unlockedResearch.TryGetValue(identityId, out unlockedItems))
                unlockedItems = new HashSet<MyDefinitionId>();

            foreach (var research in m_requiredResearch)
                unlockedItems.Add(research);

            m_unlockedResearch[identityId] = unlockedItems;
        }

        public override void Draw()
        {
            base.Draw();

            if (DEBUG_SHOW_RESEARCH)
            {
                var character = MySession.Static.LocalCharacter;
                if (character == null)
                    return;

                var identityId = character.GetPlayerIdentityId();
                HashSet<MyDefinitionId> unlockedItems;
                if (!m_unlockedResearch.TryGetValue(identityId, out unlockedItems))
                    return;

                MyRenderProxy.DebugDrawText2D(new Vector2(10, 180), String.Format("=== {0}'s Research ===", MySession.Static.LocalHumanPlayer.DisplayName), Color.DarkViolet, 0.8f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                int yShift = 200;
                foreach (var defId in unlockedItems)
                {
                    if (DEBUG_SHOW_RESEARCH_PRETTY)
                    {
                        var definition = MyDefinitionManager.Static.GetDefinition(defId);
                        if (definition is MyResearchDefinition)
                            MyRenderProxy.DebugDrawText2D(new Vector2(10, yShift), String.Format("[R] {0}", definition.DisplayNameText), Color.DarkViolet, 0.7f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                        else
                            MyRenderProxy.DebugDrawText2D(new Vector2(10, yShift), definition.DisplayNameText, Color.DarkViolet, 0.7f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                    } else
                    {
                        MyRenderProxy.DebugDrawText2D(new Vector2(10, yShift), defId.ToString(), Color.DarkViolet, 0.7f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                    }
                    yShift += 16;
                }
            }
        }

        #region Visual Scripting methods

        //add item to research list
        public void AddRequiredResearch(MyDefinitionId itemId)
        {
            if (itemId.TypeId.IsNull || itemId.SubtypeId == null)
            {
                Debug.Fail("Provided typeId is invalid: " + itemId.ToString());
                return;
            }
            SerializableDefinitionId serializableId = itemId;
            MyMultiplayer.RaiseStaticEvent(x => AddRequiredResearchSync, serializableId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void AddRequiredResearchSync(SerializableDefinitionId itemId)
        {
            MyDefinitionBase definition;
            if (!MyDefinitionManager.Static.TryGetDefinition(itemId, out definition))
                return;

            if (!Static.m_requiredResearch.Contains(definition.Id))
                Static.m_requiredResearch.Add(definition.Id);
        }

        //remove item from research list
        public void RemoveRequiredResearch(MyDefinitionId itemId)
        {
            if (itemId.TypeId.IsNull || itemId.SubtypeId == null)
            {
                Debug.Fail("Provided typeId is invalid: " + itemId.ToString());
                return;
            }
            SerializableDefinitionId serializableId = itemId;
            MyMultiplayer.RaiseStaticEvent(x => RemoveRequiredResearchSync, serializableId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void RemoveRequiredResearchSync(SerializableDefinitionId itemId)
        {
            MyDefinitionBase definition;
            if (!MyDefinitionManager.Static.TryGetDefinition(itemId, out definition))
                return;

            Static.m_requiredResearch.Remove(definition.Id);
        }

        //clear research list
        public void ClearRequiredResearch()
        {
            MyMultiplayer.RaiseStaticEvent(x => ClearRequiredResearchSync);
        }
        [Event, Reliable, Server, Broadcast]
        private static void ClearRequiredResearchSync()
        {
            Static.m_requiredResearch.Clear();
        }

        //clear player unlocks for all players
        public void ResetResearchForAll()
        {
            MyMultiplayer.RaiseStaticEvent(x => ResetResearchForAllSync);
        }
        [Event, Reliable, Server, Broadcast]
        private static void ResetResearchForAllSync()
        {
            Static.m_unlockedResearch.Clear();
        }

        //lock player research
        public void LockResearch(long characterId, MyDefinitionId itemId)
        {
            if (itemId.TypeId.IsNull || itemId.SubtypeId == null)
            {
                Debug.Fail("Provided typeId is invalid: " + itemId.ToString());
                return;
            }
            SerializableDefinitionId serializableId = itemId;
            MyMultiplayer.RaiseStaticEvent(x => LockResearchSync, characterId, serializableId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void LockResearchSync(long characterId, SerializableDefinitionId itemId)
        {
            MyDefinitionBase definition;
            if (!MyDefinitionManager.Static.TryGetDefinition(itemId, out definition))
                return;
            if (Static.m_unlockedResearch.ContainsKey(characterId))
                Static.m_unlockedResearch[characterId].Remove(definition.Id);
        }

        //unlock player research
        public void UnlockResearchDirect(long characterId, MyDefinitionId itemId)
        {
            if (itemId.TypeId.IsNull || itemId.SubtypeId == null)
            {
                Debug.Fail("Provided typeId is invalid: " + itemId.ToString());
                return;
            }
            SerializableDefinitionId serializableId = itemId;
            MyMultiplayer.RaiseStaticEvent(x => UnlockResearchDirectSync, characterId, serializableId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void UnlockResearchDirectSync(long characterId, SerializableDefinitionId itemId)
        {
            MyDefinitionBase definition;
            if (!MyDefinitionManager.Static.TryGetDefinition(itemId, out definition))
                return;
            if (Static.m_unlockedResearch.ContainsKey(characterId) && Static.m_unlockedResearch[characterId].Contains(definition.Id))
                return;
            if (!Static.m_unlockedResearch.ContainsKey(characterId))
                Static.m_unlockedResearch.Add(characterId, new HashSet<MyDefinitionId>());
            Static.m_unlockedResearch[characterId].Add(definition.Id);
        }

        //unlock player research
        public void SwitchWhitelistMode(bool whitelist)
        {
            MyMultiplayer.RaiseStaticEvent(x => SwitchWhitelistModeSync, whitelist);
        }
        [Event, Reliable, Server, Broadcast]
        private static void SwitchWhitelistModeSync(bool whitelist)
        {
            Static.WhitelistMode = whitelist;
        }

        //reset player research
        [Event, Reliable, Server, Broadcast]
        private static void ResetResearchSync(long identityId)
        {
            if (Static.m_unlockedResearch.ContainsKey(identityId))
                Static.m_unlockedResearch[identityId].Clear();
        }

        #endregion
    }
}
