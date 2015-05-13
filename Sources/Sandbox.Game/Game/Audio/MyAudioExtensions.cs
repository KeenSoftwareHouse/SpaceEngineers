using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Library.Utils;
using VRage.Utils;

namespace VRage.Audio
{
    public static class MyAudioExtensions
    {
        public static readonly MySoundErrorDelegate OnSoundError = (cue, message) =>
        {
            MyAudioDefinition definition = MyDefinitionManager.Static.GetSoundDefinition(cue.SubtypeId);
            MyDefinitionErrors.Add(definition.Context, message, ErrorSeverity.Error);
        };

        public static MyStringId GetCueId(this IMyAudio self, string cueName)
        {
            if (self == null)
                return MyStringId.NullOrEmpty;

            MyStringId id;
            if (MyStringId.TryGet(cueName, out id))
                return id;

            return MyStringId.NullOrEmpty;
        }

        internal static ListReader<MySoundData> GetSoundDataFromDefinitions()
        {
            var allSoundDefinitions = MyDefinitionManager.Static.GetSoundDefinitions();
            var query = from definition in allSoundDefinitions
                        where definition.Enabled
                        select definition.SoundData;
            return query.ToList();
        }

        internal static ListReader<MyAudioEffect> GetEffectData()
        {
            var allEffectDetinitions = MyDefinitionManager.Static.GetAudioEffectDefinitions();
            var query = from definition in allEffectDetinitions select definition.Effect;
            return query.ToList();
        }
    }
}
