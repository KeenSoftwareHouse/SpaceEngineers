using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRage.Utils;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Library.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_SoundCategoryDefinition))]
    public class MySoundCategoryDefinition : MyDefinitionBase
    {
        public class SoundDescription
        {
            public string SoundId;
            public string SoundName;
            public MyStringId? SoundNameEnum;

            public SoundDescription(string soundId, string soundName, MyStringId? soundNameEnum)
            {
                SoundId = soundId;
                SoundName = soundName;
                SoundNameEnum = soundNameEnum;
            }

            public string SoundText
            {
                get
                {
                    return (SoundNameEnum.HasValue)
                        ? MyTexts.GetString(SoundNameEnum.Value)
                        : SoundName;
                }
            }
        }

        public List<SoundDescription> Sounds;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var soundCategoryBuilder = builder as MyObjectBuilder_SoundCategoryDefinition;

            Sounds = new List<SoundDescription>();

            if (soundCategoryBuilder.Sounds != null)
            {
                foreach (var soundDesc in soundCategoryBuilder.Sounds)
                {
                    MyStringId tmp = MyStringId.GetOrCompute(soundDesc.SoundName);
                    if (MyTexts.Exists(tmp))
                        Sounds.Add(new SoundDescription(soundDesc.Id, soundDesc.SoundName, tmp));
                    else
                        Sounds.Add(new SoundDescription(soundDesc.Id, soundDesc.SoundName, null));
                }
            }
        }
    }
}
