
namespace VRage.Audio
{
    public static class MyAudioConstants
    {
        public const float MUSIC_MASTER_VOLUME_MIN = 0;
        public const float MUSIC_MASTER_VOLUME_MAX = 1;
        public const float GAME_MASTER_VOLUME_MIN = 0;
        public const float GAME_MASTER_VOLUME_MAX = 1;
        public const float VOICE_CHAT_VOLUME_MIN = 0;
        public const float VOICE_CHAT_VOLUME_MAX = 1;

        public const float REVERB_MAX = 100;

        // Multiple explosions sounds weird when set to 2
        // Multiple explosions sounds ok when set to 7
        public const int MAX_SAME_CUES_PLAYED = 7;

        public const int PREALLOCATED_UNITED_SOUNDS_PER_PHYS_OBJECT = 100;

        public const bool LIMIT_MAX_SAME_CUES = false;

        public const int MAX_COLLISION_SOUNDS = 3; // per contact
        public const int MAX_COLLISION_SOUNDS_PER_SECOND = 5; // per second

        //  How many cues of same type can be played simultaneously. E.g. if 10 bullet hit cues should be played at once, only this number will be really played.
        //public const int MAX_SAME_CUES_PLAYED = 3;

        //  It doesn't seem to be good to limit cues... so I disabled it for a while.
        //public const bool LIMIT_MAX_SAME_CUES = false;

        //  Constants for calculating collision sound pitch
        public const float MIN_DECELERATION_FOR_COLLISION_SOUND = 0;//-0.1f;
        public const float MAX_DECELERATION = -1f;
        public const float DECELERATION_MIN_VOLUME = 0.95f;//0.85f;
        public const float DECELERATION_MAX_VOLUME = 1.0f;

        public const float OCCLUSION_INTERVAL = 200.0f;
        public const float MAIN_MENU_DECREASE_VOLUME_LEVEL = 0.5f;


        public const int FAREST_TIME_IN_PAST = -60 * 1000;
    }
}
