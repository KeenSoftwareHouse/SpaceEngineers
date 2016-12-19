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
using VRage;
using VRageMath;
using VRage.Game.Components;
using VRage.Game;

namespace Sandbox.Game.Utils
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)] //disable
    public class MyMaterialPropertiesHelper : MySessionComponentBase
    {
        public static class CollisionType
        {
            public static MyStringId Start = MyStringId.GetOrCompute("Start");
            public static MyStringId Hit = MyStringId.GetOrCompute("Hit");
	        public static MyStringId Walk = MyStringId.GetOrCompute("Walk");
	        public static MyStringId Run = MyStringId.GetOrCompute("Run");
	        public static MyStringId Sprint = MyStringId.GetOrCompute("Sprint");
        }

        private struct MaterialProperties
        {
            public MySoundPair Sound;
            public string ParticleEffectName;
            public List<MyPhysicalMaterialDefinition.ImpactSounds> ImpactSoundCues;

            public MaterialProperties(MySoundPair soundCue, string particleEffectName, List<MyPhysicalMaterialDefinition.ImpactSounds> impactSounds)
            {
                Sound = soundCue;
                ParticleEffectName = particleEffectName;
                ImpactSoundCues = impactSounds;
            }
        }

        public static MyMaterialPropertiesHelper Static;
        private Dictionary<MyStringId, Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>>> MaterialDictionary =
            new Dictionary<MyStringId, Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>>>(MyStringId.Comparer);
        private HashSet<MyStringHash> m_loaded = new HashSet<MyStringHash>(MyStringHash.Comparer);
        public override void LoadData()
        {
            base.LoadData();
            Static = this;
            Debug.Assert(m_loaded.Count == 0);

            foreach (var material in MyDefinitionManager.Static.GetPhysicalMaterialDefinitions())
            {
                LoadMaterialProperties(material);
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
            if (material.InheritFrom != MyStringHash.NullOrEmpty)
            {
                MyPhysicalMaterialDefinition def;
                if (MyDefinitionManager.Static.TryGetDefinition<MyPhysicalMaterialDefinition>(new MyDefinitionId(typeof(MyObjectBuilder_PhysicalMaterialDefinition), material.InheritFrom), out def))
                {
                    if (!m_loaded.Contains(material.InheritFrom))
                        LoadMaterialSoundsInheritance(def);
                    foreach (var generalSound in def.GeneralSounds)
                    {
                        material.GeneralSounds[generalSound.Key] = generalSound.Value;
                    }
                }
                foreach (var type in MaterialDictionary.Keys)
                {
                    if (!MaterialDictionary[type].ContainsKey(thisMaterial))
                        MaterialDictionary[type][thisMaterial] = new Dictionary<MyStringHash, MaterialProperties>(MyStringHash.Comparer);
                    MaterialProperties? selfProps =  null;
                    if (!MaterialDictionary[type].ContainsKey(material.InheritFrom))
                        continue;
                    foreach (var pair in MaterialDictionary[type][material.InheritFrom])
                    {
                        if(pair.Key == material.InheritFrom)
                        {
                            selfProps = pair.Value;
                            continue;
                        }
                        // parent should no longer override
                        if (MaterialDictionary[type][thisMaterial].ContainsKey(pair.Key))
                        {
                            if (MaterialDictionary[type][pair.Key].ContainsKey(thisMaterial))
                            {
                                continue;
                            }
                            MaterialDictionary[type][pair.Key][thisMaterial] = pair.Value;
                        }
                        else
                        {
                            MaterialDictionary[type][thisMaterial][pair.Key] = pair.Value;
                            Debug.Assert(MaterialDictionary[type].ContainsKey(pair.Key));
                            MaterialDictionary[type][pair.Key][thisMaterial] = pair.Value;
                        }
                    }

                    if (selfProps != null)
                    {
                        MaterialDictionary[type][thisMaterial][thisMaterial] = selfProps.Value;
                        MaterialDictionary[type][thisMaterial][material.InheritFrom] = selfProps.Value;
                        MaterialDictionary[type][material.InheritFrom][thisMaterial] = selfProps.Value;
                    }
                }
            }
        }

        private void LoadMaterialProperties(MyPhysicalMaterialDefinition material)
        {
            var thisMaterial = material.Id.SubtypeId;
            foreach (var materialSounds in material.CollisionProperties)
            {
                var type = materialSounds.Key;
                if (!MaterialDictionary.ContainsKey(type))
                    MaterialDictionary[type] = new Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>>(MyStringHash.Comparer);
                if (!MaterialDictionary[type].ContainsKey(thisMaterial))
                    MaterialDictionary[type][thisMaterial] = new Dictionary<MyStringHash, MaterialProperties>(MyStringHash.Comparer);
                foreach (var otherMaterial in materialSounds.Value)
                {
                    MaterialDictionary[type][thisMaterial][otherMaterial.Key] = new MaterialProperties(otherMaterial.Value.Sound, otherMaterial.Value.ParticleEffect, otherMaterial.Value.ImpactSoundCues);

                    //add the sound in oposite direction if not defined
                    if (!MaterialDictionary[type].ContainsKey(otherMaterial.Key))
                        MaterialDictionary[type][otherMaterial.Key] = new Dictionary<MyStringHash, MaterialProperties>(MyStringHash.Comparer);
                    if (!MaterialDictionary[type][otherMaterial.Key].ContainsKey(thisMaterial))
                        MaterialDictionary[type][otherMaterial.Key][thisMaterial] = new MaterialProperties(otherMaterial.Value.Sound, otherMaterial.Value.ParticleEffect, otherMaterial.Value.ImpactSoundCues);
                }
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Static = null;
        }

		public bool TryCreateCollisionEffect(MyStringId type, Vector3D position, Vector3 normal, MyStringHash material1, MyStringHash material2)
		{
            var effectName = GetCollisionEffect(type, material1, material2);
            if (effectName != null)
			{
				MyParticleEffect effect;
                if (MyParticlesManager.TryCreateParticleEffect(effectName, out effect))
				{
                    effect.WorldMatrix = MatrixD.CreateWorld(position, normal, Vector3.CalculatePerpendicularVector(normal));
				    return true;
				}
			}

			return false;
		}

	    public string GetCollisionEffect(MyStringId type, MyStringHash materialType1, MyStringHash materialType2)
	    {
		    string foundEffect = null;
			Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>> typeDic;
			if (MaterialDictionary.TryGetValue(type, out typeDic))
			{
				Dictionary<MyStringHash, MaterialProperties> materialDic;
				if (typeDic.TryGetValue(materialType1, out materialDic))
				{
					MaterialProperties result;
					if (materialDic.TryGetValue(materialType2, out result))
					{
                        foundEffect = result.ParticleEffectName;
					}
				}
			}
            return foundEffect;
	    }


        public MySoundPair GetCollisionCue(MyStringId type, MyStringHash materialType1, MyStringHash materialType2)
        {
            Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>> typeDic;
            if(MaterialDictionary.TryGetValue(type, out typeDic))
            {
                Dictionary<MyStringHash, MaterialProperties> materialDic;
                if(typeDic.TryGetValue(materialType1, out materialDic))
                {
                    MaterialProperties result;
                    if (materialDic.TryGetValue(materialType2, out result))
                        return result.Sound;
                }
            }
            return MySoundPair.Empty;
        }

        public MySoundPair GetCollisionCueWithMass(MyStringId type, MyStringHash materialType1, MyStringHash materialType2, ref float volume, float? mass = null, float velocity = 0f)
        {
            Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>> typeDic;
            if (MaterialDictionary.TryGetValue(type, out typeDic))
            {
                Dictionary<MyStringHash, MaterialProperties> materialDic;
                if (typeDic.TryGetValue(materialType1, out materialDic))
                {
                    MaterialProperties result;
                    if (materialDic.TryGetValue(materialType2, out result))
                    {
                        if (mass == null || result.ImpactSoundCues == null || result.ImpactSoundCues.Count == 0)
                            return result.Sound;
                        else
                        {
                            int bestSoundIndex = -1;
                            float bestSoundMass = -1;
                            for (int i = 0; i < result.ImpactSoundCues.Count; i++)
                            {
                                if (mass >= result.ImpactSoundCues[i].Mass && result.ImpactSoundCues[i].Mass > bestSoundMass && velocity >= result.ImpactSoundCues[i].minVelocity)
                                {
                                    bestSoundIndex = i;
                                    bestSoundMass = result.ImpactSoundCues[i].Mass;
                                }
                            }
                            if (bestSoundIndex >= 0)
                            {
                                volume = 0.25f + 0.75f * MyMath.Clamp((velocity - result.ImpactSoundCues[bestSoundIndex].minVelocity) / (result.ImpactSoundCues[bestSoundIndex].maxVolumeVelocity - result.ImpactSoundCues[bestSoundIndex].minVelocity), 0f, 1f);
                                return result.ImpactSoundCues[bestSoundIndex].SoundCue;
                            }
                            else
                            {
                                return result.Sound;
                            }
                        }
                    }
                }
            }
            return MySoundPair.Empty;
        }
    }
}