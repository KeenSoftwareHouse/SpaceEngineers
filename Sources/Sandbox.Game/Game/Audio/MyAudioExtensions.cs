using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;

namespace VRage.Audio
{
    public static class MyAudioExtensions
    {
        public static readonly MySoundErrorDelegate OnSoundError = (cue, message) =>
        {
            MyAudioDefinition definition = MyDefinitionManager.Static.GetSoundDefinition(cue.SubtypeId);
            MyDefinitionErrors.Add(definition.Context, message, TErrorSeverity.Error);
        };

        public static MyCueId GetCueId(this IMyAudio self, string cueName)
        {
            MyStringHash hash;
            if (self == null || !MyStringHash.TryGet(cueName, out hash))
            {
                hash = MyStringHash.NullOrEmpty;
            }
            return new MyCueId(hash);
        }

        internal static ListReader<MySoundData> GetSoundDataFromDefinitions()
        {
            var allSoundDefinitions = MyDefinitionManager.Static.GetSoundDefinitions();
            //var query = from definition in allSoundDefinitions
            //            where definition.Enabled
            //            select definition.SoundData;

			var query = allSoundDefinitions.Where(x => x.Enabled).Select(x => x.SoundData);

            return query.ToList();
        }

        internal static ListReader<MyAudioEffect> GetEffectData()
        {
            var allEffectDetinitions = MyDefinitionManager.Static.GetAudioEffectDefinitions();
            //var query = from definition in allEffectDetinitions select definition.Effect;
			var query = allEffectDetinitions.Select(x => x.Effect);
            return query.ToList();
        }
    }
}
