using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Audio;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PhysicalMaterialDefinition))]
    public class MyPhysicalMaterialDefinition : MyDefinitionBase
    {
        public float Density;
        public float HorisontalTransmissionMultiplier;
        public float HorisontalFragility;
        public float SupportMultiplier;
        public float CollisionMultiplier;
        public Dictionary<MyStringId, Dictionary<MyStringHash, MySoundPair>> CollisionSounds = new Dictionary<MyStringId, Dictionary<MyStringHash, MySoundPair>>(MyStringId.Comparer);
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
            var soundBuilder = builder as MyObjectBuilder_MaterialSoundsDefinition;
            if(soundBuilder != null)
            {
                InheritSoundsFrom = MyStringHash.GetOrCompute(soundBuilder.InheritFrom);
                

                foreach(var sound in soundBuilder.ContactSounds)
                {
                    var type = MyStringId.GetOrCompute(sound.Type);
                    if (!CollisionSounds.ContainsKey(type))
                        CollisionSounds[type] = new Dictionary<MyStringHash, MySoundPair>(MyStringHash.Comparer);
                    var material = MyStringHash.GetOrCompute(sound.Material);

                    Debug.Assert(!CollisionSounds[type].ContainsKey(material), "Overwriting material sound!");

                    CollisionSounds[type][material] = new MySoundPair(sound.Cue);
                }

                foreach(var sound in soundBuilder.GeneralSounds)
                {
                    GeneralSounds[MyStringId.GetOrCompute(sound.Type)] = new MySoundPair(sound.Cue);
                }
            }
        }   
    }
}
