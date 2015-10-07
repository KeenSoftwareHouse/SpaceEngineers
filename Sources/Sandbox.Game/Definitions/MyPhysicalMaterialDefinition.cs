using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PhysicalMaterialDefinition))]
    public class MyPhysicalMaterialDefinition : MyDefinitionBase
    {
        public struct CollisionProperty
        {
            public MySoundPair Sound;
            public string ParticleEffect;
            public ContactPropertyParticleProperties ParticleEffectProperties;

            public CollisionProperty(string soundCue, string particleEffectName, ContactPropertyParticleProperties effectProperties)
            {
                Sound = new MySoundPair(soundCue);
                ParticleEffect = particleEffectName;
	            ParticleEffectProperties = effectProperties;
            }
        }

        public float Density;
        public float HorisontalTransmissionMultiplier;
        public float HorisontalFragility;
        public float SupportMultiplier;
        public float CollisionMultiplier;
        public Dictionary<MyStringId, Dictionary<MyStringHash, CollisionProperty>> CollisionProperties = new Dictionary<MyStringId, Dictionary<MyStringHash, CollisionProperty>>(MyStringId.Comparer);
        public Dictionary<MyStringId, MySoundPair> GeneralSounds = new Dictionary<MyStringId, MySoundPair>(MyStringId.Comparer);
        public MyStringHash InheritSoundsFrom = MyStringHash.NullOrEmpty;
        public string DamageDecal;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var materialBuilder = builder as MyObjectBuilder_PhysicalMaterialDefinition;
            if (materialBuilder != null)
            {
                //MyDebug.AssertDebug(materialBuilder != null, "Initializing physical material definition using wrong object builder.");
                Density = materialBuilder.Density;
                HorisontalTransmissionMultiplier = materialBuilder.HorisontalTransmissionMultiplier;
                HorisontalFragility = materialBuilder.HorisontalFragility;
                SupportMultiplier = materialBuilder.SupportMultiplier;
                CollisionMultiplier = materialBuilder.CollisionMultiplier;
                DamageDecal = materialBuilder.DamageDecal;
            }
            var soundBuilder = builder as MyObjectBuilder_MaterialPropertiesDefinition;
            if(soundBuilder != null)
            {
                InheritSoundsFrom = MyStringHash.GetOrCompute(soundBuilder.InheritFrom);
                

                foreach(var sound in soundBuilder.ContactProperties)
                {
                    var type = MyStringId.GetOrCompute(sound.Type);
                    if (!CollisionProperties.ContainsKey(type))
                        CollisionProperties[type] = new Dictionary<MyStringHash, CollisionProperty>(MyStringHash.Comparer);
                    var material = MyStringHash.GetOrCompute(sound.Material);

                    Debug.Assert(!CollisionProperties[type].ContainsKey(material), "Overwriting material sound!");

                    CollisionProperties[type][material] = new CollisionProperty(sound.SoundCue, sound.ParticleEffect, sound.ParticleProperties);
                }

                foreach(var sound in soundBuilder.GeneralProperties)
                {
                    GeneralSounds[MyStringId.GetOrCompute(sound.Type)] = new MySoundPair(sound.SoundCue);
                }
            }
        }   
    }
}
