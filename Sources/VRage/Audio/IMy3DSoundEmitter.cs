using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace VRage.Audio
{
    public interface IMy3DSoundEmitter
    {
        MyCueId SoundId { get; }
        IMySourceVoice Sound { get; set; }
        Vector3 SourcePosition { get; }
        Vector3 Velocity { get; }

        float? CustomMaxDistance { get; }
        float? CustomVolume { get; }

        bool Force3D { get; }

        bool Plays2D { get; }
        int SourceChannels { get; set; }
    }    
}
