using System;
using System.Collections.Generic;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Common;
using VRage.Utils;
using VRage.Library.Utils;
using System.Diagnostics;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Game.Utils
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)] //disable
    public class MyMaterialSoundsHelper : MySessionComponentBase
    {
        public static class CollisionType
        {
            public static MyStringId Start = MyStringId.GetOrCompute("Start");
        }
        public static MyMaterialSoundsHelper Static;
        private Dictionary<MyStringId, Dictionary<MyStringId, Dictionary<MyStringId, MySoundPair>>> MaterialCues = new Dictionary<MyStringId, Dictionary<MyStringId, Dictionary<MyStringId, MySoundPair>>>();
        private HashSet<MyStringId> m_loaded = new HashSet<MyStringId>();
        public override void LoadData()
        {
            base.LoadData();
            Static = this;
            Debug.Assert(m_loaded.Count == 0);

            foreach (var material in MyDefinitionManager.Static.GetPhysicalMaterialDefinitions())
            {
                LoadMaterialSounds(material);
            }

            foreach(var material in MyDefinitionManager.Static.GetPhysicalMaterialDefinitions())
            {
                LoadMaterialSoundsInheritance(material);
            }
        }

        private void LoadMaterialSoundsInheritance(MyPhysicalMaterialDefinition material)
        {
            var thisMaterial = material.Id.SubtypeId;
            if (!m_loaded.Add(thisMaterial))
                return;
            if (material.InheritSoundsFrom != MyStringId.NullOrEmpty)
            {
                MyPhysicalMaterialDefinition def;
                if (MyDefinitionManager.Static.TryGetDefinition<MyPhysicalMaterialDefinition>(new MyDefinitionId(typeof(MyObjectBuilder_PhysicalMaterialDefinition), material.InheritSoundsFrom), out def))
                {
                    if (!m_loaded.Contains(material.InheritSoundsFrom))
                        LoadMaterialSoundsInheritance(def);
                    foreach (var generalSound in def.GeneralSounds)
                    {
                        material.GeneralSounds[generalSound.Key] = generalSound.Value;
                    }
                }
                foreach (var type in MaterialCues.Keys)
                {
                    if (!MaterialCues[type].ContainsKey(thisMaterial))
                        MaterialCues[type][thisMaterial] = new Dictionary<MyStringId, MySoundPair>();
                    MySoundPair selfCue =  null;
                    foreach (var pair in MaterialCues[type][material.InheritSoundsFrom])
                    {
                        if(pair.Key == material.InheritSoundsFrom)
                        {
                            selfCue = pair.Value;
                            continue;
                        }
                        MaterialCues[type][thisMaterial][pair.Key] = pair.Value;
                        Debug.Assert(MaterialCues[type].ContainsKey(pair.Key));
                        MaterialCues[type][pair.Key][thisMaterial] = pair.Value;
                    }

                    if (selfCue != null)
                    {
                        MaterialCues[type][thisMaterial][thisMaterial] = selfCue;
                        MaterialCues[type][thisMaterial][material.InheritSoundsFrom] = selfCue;
                        MaterialCues[type][material.InheritSoundsFrom][thisMaterial] = selfCue;
                    }
                }
            }
        }

        private void LoadMaterialSounds(MyPhysicalMaterialDefinition material)
        {
            var thisMaterial = material.Id.SubtypeId;
            foreach (var materialSounds in material.CollisionSounds)
            {
                var type = materialSounds.Key;
                if (!MaterialCues.ContainsKey(type))
                    MaterialCues[type] = new Dictionary<MyStringId, Dictionary<MyStringId, MySoundPair>>();
                if (!MaterialCues[type].ContainsKey(thisMaterial))
                    MaterialCues[type][thisMaterial] = new Dictionary<MyStringId, MySoundPair>();
                foreach (var otherMaterial in materialSounds.Value)
                {
                    MaterialCues[type][thisMaterial][otherMaterial.Key] = otherMaterial.Value;

                    //add the sound in oposite direction if not defined
                    if (!MaterialCues[type].ContainsKey(otherMaterial.Key))
                        MaterialCues[type][otherMaterial.Key] = new Dictionary<MyStringId, MySoundPair>();
                    if (!MaterialCues[type][otherMaterial.Key].ContainsKey(thisMaterial))
                        MaterialCues[type][otherMaterial.Key][thisMaterial] = otherMaterial.Value;
                }
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Static = null;
        }

        public MySoundPair GetCollisionCue(MyStringId type, MyStringId materialType1, MyStringId materialType2)
        {
            Dictionary<MyStringId, Dictionary<MyStringId, MySoundPair>> typeDic;
            if(MaterialCues.TryGetValue(type, out typeDic))
            {
                Dictionary<MyStringId, MySoundPair> materialDic;
                if(typeDic.TryGetValue(materialType1, out materialDic))
                {
                    MySoundPair result;
                    if (materialDic.TryGetValue(materialType2, out result))
                        return result;
                }
            }
            return MySoundPair.Empty;
        }
    }
}