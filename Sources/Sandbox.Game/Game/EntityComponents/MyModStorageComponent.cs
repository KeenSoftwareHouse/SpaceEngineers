using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Game.EntityComponents
{
    /// <summary>
    /// Component that stores custom mod data for an entity. Shared by all mods.
    /// NOTE: Create an EntityComponents.sbc with your mod's GUIDs to make sure data is saved.
    /// This allows data to remain in the world file until the user removes the mod.
    /// </summary>
    [MyComponentType(typeof(MyModStorageComponent))]
    [MyComponentBuilder(typeof(MyObjectBuilder_ModStorageComponent))]
    public class MyModStorageComponent : MyModStorageComponentBase
    {
        /// <summary>
        /// Store custom mod data here. Use a GUID unique to your mod. Use only system types, not custom types in mod script or game.
        /// </summary>
        /// <remarks>This is not synced.
        /// Caution, this contains data for <i>all</i> mods.
        /// It is recommended to use the appropriate methods instead (GetValue, TryGetValue, SetValue, RemoveValue).
        /// </remarks>
        public IReadOnlyDictionary<Guid, string> Storage
        {
            get { return (IReadOnlyDictionary<Guid, string>)m_storageData; }
        }

        public override bool IsSerialized()
        {
            // Don't try to save if there's nothing to try to save
            return m_storageData.Count > 0;
        }

        public override string GetValue(Guid guid)
        {
            return m_storageData[guid];
        }

        public override bool TryGetValue(Guid guid, out string value)
        {
            if (m_storageData.ContainsKey(guid))
            {
                value = m_storageData[guid];
                return true;
            }
            value = null;
            return false;
        }

        public override void SetValue(Guid guid, string value)
        {
            m_storageData[guid] = value;
        }

        public override bool RemoveValue(Guid guid)
        {
            return m_storageData.Remove(guid);
        }

        private HashSet<Guid> m_cachedGuids = new HashSet<Guid>();

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = (MyObjectBuilder_ModStorageComponent)base.Serialize(copy);
            ob.Storage = new SerializableDictionary<Guid, string>();

            // Get a list of known guids.
            // This lets mod data persist if scripts break,
            // but still allow data to be automatically removed when the user removes a mod from the world.
            var storageDefs = MyDefinitionManager.Static.GetEntityComponentDefinitions<MyModStorageComponentDefinition>();
            foreach (var def in storageDefs)
            {
                foreach (var guid in def.RegisteredStorageGuids)
                {
                    if (!m_cachedGuids.Add(guid))
                        VRage.Utils.MyLog.Default.Log(VRage.Utils.MyLogSeverity.Warning, "Duplicate ModStorageComponent GUID: {0}, in {1}: {2}", guid.ToString(), def.Context.ModId, def.Id.ToString());
                }
            }

            // Now that we have a list of known GUIDs, only save items that still exist.
            foreach (var guid in Storage.Keys)
            {
                if(m_cachedGuids.Contains(guid))
                    ob.Storage[guid] = Storage[guid];
                else
                    VRage.Utils.MyLog.Default.Log(VRage.Utils.MyLogSeverity.Warning, "Not saving ModStorageComponent GUID: {0}, not claimed", guid.ToString());
            }

            m_cachedGuids.Clear();

            // If there's no storage, return null so this component isn't saved at all.
            // This saves space in the save file.
            if (ob.Storage.Dictionary.Count == 0)
                return null;

            return ob;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            var serdict = ((MyObjectBuilder_ModStorageComponent)builder).Storage;

            if (serdict != null && serdict.Dictionary != null)
                m_storageData = new Dictionary<Guid, string>(serdict.Dictionary);
        }
    }
}
