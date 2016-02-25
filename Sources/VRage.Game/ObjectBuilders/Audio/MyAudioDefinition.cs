using VRage.Utils;
using VRage.Data.Audio;
using VRage.Game.Definitions;


namespace VRage.Game
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

            if (this.SoundData.Loopable)
            {
                bool hasLoop = true;
                for (int i = 0; i < this.SoundData.Waves.Count; i++) {
                    hasLoop &= this.SoundData.Waves[i].Loop != null;
                }
                //MyDebug.AssertDebug(hasLoop, String.Format("Sound '{0}' has <Loopable> tag set to TRUE, but is missing a <Loop> in <Wave>, please fix the .sbc", this.SoundData.SubtypeId));
            }
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_AudioDefinition)base.GetObjectBuilder();
            ob.SoundData = this.SoundData;
            return ob;
        }
    }
}
