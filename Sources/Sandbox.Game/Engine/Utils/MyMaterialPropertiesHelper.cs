using System;
using System.Collections.Generic;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Common;
using Sandbox.Graphics.TransparentGeometry.Particles;
using VRage.Utils;
using VRage.Library.Utils;
using System.Diagnostics;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage;
using VRageMath;

namespace Sandbox.Game.Utils
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)] //disable
    public class MyMaterialPropertiesHelper : MySessionComponentBase
    {
        public static class CollisionType
        {
            public static MyStringId Start = MyStringId.GetOrCompute("Start");
	        public static MyStringId Walk = MyStringId.GetOrCompute("Walk");
	        public static MyStringId Run = MyStringId.GetOrCompute("Run");
	        public static MyStringId Sprint = MyStringId.GetOrCompute("Sprint");
        }

        private struct MaterialProperties
        {
            public MySoundPair Sound;
            public int ParticleEffectID;
            public ContactPropertyParticleProperties ParticleEffectProperties;

            public MaterialProperties(MySoundPair soundCue, string particleEffectName, ContactPropertyParticleProperties effectProperties)
            {
                Sound = soundCue;
                ParticleEffectProperties = effectProperties;
                if (particleEffectName != null)
                    MyParticlesLibrary.GetParticleEffectsID(
                        particleEffectName, out ParticleEffectID);
                else
                    ParticleEffectID = -1;
            }
        }

        public static MyMaterialPropertiesHelper Static;
        private Dictionary<MyStringId, Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>>> MaterialCues =
            new Dictionary<MyStringId, Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>>>(MyStringId.Comparer);
        private HashSet<MyStringHash> m_loaded = new HashSet<MyStringHash>(MyStringHash.Comparer);
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
            if (material.InheritSoundsFrom != MyStringHash.NullOrEmpty)
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
                        MaterialCues[type][thisMaterial] = new Dictionary<MyStringHash, MaterialProperties>(MyStringHash.Comparer);
                    MaterialProperties? selfProps =  null;
                    if (!MaterialCues[type].ContainsKey(material.InheritSoundsFrom))
                        continue;
                    foreach (var pair in MaterialCues[type][material.InheritSoundsFrom])
                    {
                        if(pair.Key == material.InheritSoundsFrom)
                        {
                            selfProps = pair.Value;
                            continue;
                        }
                        MaterialCues[type][thisMaterial][pair.Key] = pair.Value;
                        Debug.Assert(MaterialCues[type].ContainsKey(pair.Key));
                        MaterialCues[type][pair.Key][thisMaterial] = pair.Value;
                    }

                    if (selfProps != null)
                    {
                        MaterialCues[type][thisMaterial][thisMaterial] = selfProps.Value;
                        MaterialCues[type][thisMaterial][material.InheritSoundsFrom] = selfProps.Value;
                        MaterialCues[type][material.InheritSoundsFrom][thisMaterial] = selfProps.Value;
                    }
                }
            }
        }

        private void LoadMaterialSounds(MyPhysicalMaterialDefinition material)
        {
            var thisMaterial = material.Id.SubtypeId;
            foreach (var materialSounds in material.CollisionProperties)
            {
                var type = materialSounds.Key;
                if (!MaterialCues.ContainsKey(type))
                    MaterialCues[type] = new Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>>(MyStringHash.Comparer);
                if (!MaterialCues[type].ContainsKey(thisMaterial))
                    MaterialCues[type][thisMaterial] = new Dictionary<MyStringHash, MaterialProperties>(MyStringHash.Comparer);
                foreach (var otherMaterial in materialSounds.Value)
                {
                    MaterialCues[type][thisMaterial][otherMaterial.Key] = new MaterialProperties(otherMaterial.Value.Sound, otherMaterial.Value.ParticleEffect, otherMaterial.Value.ParticleEffectProperties);

                    //add the sound in oposite direction if not defined
                    if (!MaterialCues[type].ContainsKey(otherMaterial.Key))
                        MaterialCues[type][otherMaterial.Key] = new Dictionary<MyStringHash, MaterialProperties>(MyStringHash.Comparer);
                    if (!MaterialCues[type][otherMaterial.Key].ContainsKey(thisMaterial))
                        MaterialCues[type][otherMaterial.Key][thisMaterial] = new MaterialProperties(otherMaterial.Value.Sound, otherMaterial.Value.ParticleEffect, otherMaterial.Value.ParticleEffectProperties);
                }
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Static = null;
        }

        public bool TryCreateCollisionEffect(Vector3 position, Vector3 normal, MyStringHash material1, MyStringHash material2)
        {
            return TryCreateCollisionEffect(CollisionType.Start, position, normal, material1, material2);
        }

		public bool TryCreateCollisionEffect(MyStringId type, Vector3 position, Vector3 normal, MyStringHash material1, MyStringHash material2)
		{
            var effectInfo = GetCollisionEffectAndProperties(type, material1, material2);
            var effectId = effectInfo.Item1;
			if (effectId >= 0)
			{
				MyParticleEffect effect;
				if (MyParticlesManager.TryCreateParticleEffect(effectId, out effect))
				{
					effect.WorldMatrix = MatrixD.CreateWorld(position, Vector3.CalculatePerpendicularVector(normal), normal);
					effect.AutoDelete = true;
				    if (effectInfo.Item2 != null)
				    {
				        effect.UserScale = effectInfo.Item2.SizeMultiplier;
				        effect.UserColorMultiplier = effectInfo.Item2.ColorMultiplier;
				        effect.SetPreload(effectInfo.Item2.Preload);
				    }
				    return true;
				}
			}

			return false;
		}

	    public MyTuple<int, ContactPropertyParticleProperties> GetCollisionEffectAndProperties(MyStringId type, MyStringHash materialType1, MyStringHash materialType2)
	    {
		    int foundId = -1;
		    ContactPropertyParticleProperties foundProperties = null;
			Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>> typeDic;
			if (MaterialCues.TryGetValue(type, out typeDic))
			{
				Dictionary<MyStringHash, MaterialProperties> materialDic;
				if (typeDic.TryGetValue(materialType1, out materialDic))
				{
					MaterialProperties result;
					if (materialDic.TryGetValue(materialType2, out result))
					{
						foundId = result.ParticleEffectID;
						foundProperties = result.ParticleEffectProperties;
					}
				}
			}
			return MyTuple.Create(foundId, foundProperties);
	    }

        public int GetCollisionParticleEffectID(MyStringId type, MyStringHash materialType1, MyStringHash materialType2)
        {
            Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>> typeDic;
            if (MaterialCues.TryGetValue(type, out typeDic))
            {
                Dictionary<MyStringHash, MaterialProperties> materialDic;
                if (typeDic.TryGetValue(materialType1, out materialDic))
                {
                    MaterialProperties result;
                    if (materialDic.TryGetValue(materialType2, out result))
                        return result.ParticleEffectID;
                }
            }
            return -1;
        }

        public MySoundPair GetCollisionCue(MyStringId type, MyStringHash materialType1, MyStringHash materialType2)
        {
            Dictionary<MyStringHash, Dictionary<MyStringHash, MaterialProperties>> typeDic;
            if(MaterialCues.TryGetValue(type, out typeDic))
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
    }
}