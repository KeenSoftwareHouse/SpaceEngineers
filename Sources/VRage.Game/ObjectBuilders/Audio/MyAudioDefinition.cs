using Sandbox.Common.ObjectBuilders.Audio;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRage.Data;
using VRage.Data.Audio;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AudioDefinition))]
    public class MyAudioDefinition : MyDefinitionBase
    {
        public MySoundData SoundData;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_AudioDefinition;
            MyDebug.AssertDebug(ob != null);

            this.SoundData = ob.SoundData;
            this.SoundData.SubtypeId = base.Id.SubtypeId;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_AudioDefinition)base.GetObjectBuilder();
            ob.SoundData = this.SoundData;
            return ob;
        }
    }
}
