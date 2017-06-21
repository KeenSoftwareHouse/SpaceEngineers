using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Audio;
using VRage.Data.Audio;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Audio
{
    class MyMusicController
    {
        #region Structs

        private struct MusicOption
        {
            public MyStringId Category;
            public float Frequency;

            public MusicOption(string category, float frequency){
                this.Category = MyStringId.GetOrCompute(category);
                this.Frequency = frequency;
            }
        }
        private enum MusicCategory
        {
            location,
            building,
            danger,
            lightFight,
            heavyFight,
            custom
        }

        #endregion

        #region Constants

        private const int METEOR_SHOWER_MUSIC_FREQUENCY = 60 * 60 * 12;//in frames
        private const int METEOR_SHOWER_CROSSFADE_LENGTH = 8000;//in miliseconds

        private const int DEFAULT_NO_MUSIC_TIME_MIN = 2;//in seconds
        private const int DEFAULT_NO_MUSIC_TIME_MAX = 8;//in seconds
        private const int FAST_NO_MUSIC_TIME_MIN = 1;//in seconds
        private const int FAST_NO_MUSIC_TIME_MAX = 4;//in seconds

        private const int BUILDING_NEED = 7 * 1000;
        private const int BUILDING_COOLDOWN = 45 * 1000;
        private const int BUILDING_CROSSFADE_LENGTH = 14000;//in miliseconds

        private const int FIGHTING_NEED = 100;
        private const int FIGHTING_COOLDOWN_LIGHT = 15;
        private const int FIGHTING_COOLDOWN_HEAVY = 20;
        private const int FIGHTING_CROSSFADE_LENGTH = 6000;//in miliseconds

        #endregion

        #region StaticFields

        public static MyMusicController Static { get; set; }
        private static List<MusicOption> m_defaultSpaceCategories = new List<MusicOption>()
        {
            new MusicOption("Space", 0.7f),
            new MusicOption("Calm", 0.25f),
            new MusicOption("Mystery", 0.05f)
        };
        private static List<MusicOption> m_defaultPlanetCategory = new List<MusicOption>()
        {
            new MusicOption("Planet", 0.8f),
            new MusicOption("Calm", 0.1f),
            new MusicOption("Danger", 0.1f)
        };

        private static MyStringHash m_hashCrossfade = MyStringHash.GetOrCompute("CrossFade");
        private static MyStringHash m_hashFadeIn = MyStringHash.GetOrCompute("FadeIn");
        private static MyStringHash m_hashFadeOut = MyStringHash.GetOrCompute("FadeOut");
        private static MyStringId m_stringIdDanger = MyStringId.GetOrCompute("Danger");
        private static MyStringId m_stringIdBuilding = MyStringId.GetOrCompute("Building");
        private static MyStringId m_stringIdLightFight = MyStringId.GetOrCompute("LightFight");
        private static MyStringId m_stringIdHeavyFight = MyStringId.GetOrCompute("HeavyFight");
        private static MyCueId m_cueEmpty = new MyCueId();

        #endregion

        #region Properties

        public MyStringId CategoryPlaying { get; private set; }
        public MyStringId CategoryLast { get; private set; }
        public MyCueId CueIdPlaying { get; private set; }
        public bool Active = false;
        public float NextMusicTrackIn { get { return m_noMusicTimer; } }
        public bool CanChangeCategoryGlobal = true;
        private bool CanChangeCategoryLocal = true;
        public bool CanChangeCategory { get { return CanChangeCategoryGlobal && CanChangeCategoryLocal; } }

        #endregion

        #region Fields

        private Dictionary<MyStringId, List<MyCueId>> m_musicCuesAll;
        private Dictionary<MyStringId, List<MyCueId>> m_musicCuesRemaining;
        private List<MusicOption> m_actualMusicOptions = new List<MusicOption>();
        private MyPlanet m_lastVisitedPlanet = null;
        private MySoundData m_lastMusicData = null;

        private int m_frameCounter = 0;
        private float m_noMusicTimer = 0f;
        public bool MusicIsPlaying { get { return m_musicSourceVoice != null && m_musicSourceVoice.IsPlaying; } }

        private Random m_random = new Random();
        private IMySourceVoice m_musicSourceVoice = null;

        private int m_lastMeteorShower = int.MinValue;
        private MusicCategory m_currentMusicCategory = MusicCategory.location;

        private int m_meteorShower = 0;
        private int m_building = 0;
        private int m_buildingCooldown = 0;
        private int m_fightLight = 0;
        private int m_fightLightCooldown = 0;
        private int m_fightHeavy = 0;
        private int m_fightHeavyCooldown = 0;

        #endregion

        #region Initialization

        public MyMusicController(Dictionary<MyStringId, List<MyCueId>> musicCues = null)
        {
            CategoryPlaying = MyStringId.NullOrEmpty;
            CategoryLast = MyStringId.NullOrEmpty;
            Active = false;
            if (musicCues == null)
                m_musicCuesAll = new Dictionary<MyStringId, List<MyCueId>>(MyStringId.Comparer);
            else
                m_musicCuesAll = musicCues;
            m_musicCuesRemaining = new Dictionary<MyStringId, List<MyCueId>>(MyStringId.Comparer);
        }

        #endregion

        #region Update

        private void Update_1s()
        {
            if (m_meteorShower > 0)
                m_meteorShower--;

            if (m_buildingCooldown > 0)
                m_buildingCooldown = Math.Max(0, m_buildingCooldown - 1000);//miliseconds
            else if (m_building > 0)
                m_building = Math.Max(0, m_building - 1000);//miliseconds

            if (m_fightHeavyCooldown > 0)
                m_fightHeavyCooldown = Math.Max(0, m_fightHeavyCooldown - 1);//seconds
            else if (m_fightHeavy > 0)
                m_fightHeavy = Math.Max(0, m_fightHeavy - 10);//seconds

            if (m_fightLightCooldown > 0)
                m_fightLightCooldown = Math.Max(0, m_fightLightCooldown - 1);//seconds
            else if (m_fightLight > 0)
                m_fightLight = Math.Max(0, m_fightLight - 10);//seconds
        }

        public void Update()
        {
            if (m_frameCounter % 60 == 0)
                Update_1s();

            if (MusicIsPlaying)
                m_musicSourceVoice.SetVolume(m_lastMusicData != null ? MyAudio.Static.VolumeMusic * m_lastMusicData.Volume: MyAudio.Static.VolumeMusic);
            else
            {
                if (m_noMusicTimer > 0f)
                    m_noMusicTimer -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                else
                {
                    if (CanChangeCategory)
                    {
                        if (m_fightHeavy >= FIGHTING_NEED)
                            m_currentMusicCategory = MusicCategory.heavyFight;
                        else if (m_fightLight >= FIGHTING_NEED)
                            m_currentMusicCategory = MusicCategory.lightFight;
                        else if (m_meteorShower > 0)
                            m_currentMusicCategory = MusicCategory.danger;
                        else if (m_building >= BUILDING_NEED)
                            m_currentMusicCategory = MusicCategory.building;
                        else
                            m_currentMusicCategory = MusicCategory.location;
                    }

                    switch (m_currentMusicCategory)
                    {
                        case MusicCategory.building:
                            PlayBuildingMusic();
                            break;

                        case MusicCategory.danger:
                            PlayDangerMusic();
                            break;

                        case MusicCategory.lightFight:
                            PlayFightingMusic(true);
                            break;

                        case MusicCategory.heavyFight:
                            PlayFightingMusic(false);
                            break;

                        case MusicCategory.custom:
                            PlaySpecificMusicCategory(CategoryLast, false);
                            break;

                        default:
                            CalculateNextCue();
                            break;
                    }
                }
            }

            m_frameCounter++;
        }

        #endregion

        #region DynemicEvents

        public void Building(int amount)
        {
            m_building = Math.Min(BUILDING_NEED, m_building + amount);
            m_buildingCooldown = Math.Min(BUILDING_COOLDOWN, m_buildingCooldown + amount * 5);
            if (CanChangeCategory && m_building >= BUILDING_NEED)
            {
                m_noMusicTimer = m_random.Next(FAST_NO_MUSIC_TIME_MIN, FAST_NO_MUSIC_TIME_MAX);
                if ((int)m_currentMusicCategory < (int)MusicCategory.building)
                    PlayBuildingMusic();
            }
        }

        public void MeteorShowerIncoming()
        {
            int now = MyFpsManager.GetSessionTotalFrames();
            if (!CanChangeCategory || Math.Abs(m_lastMeteorShower - now) < METEOR_SHOWER_MUSIC_FREQUENCY)
                return;
            m_meteorShower = 10;
            m_lastMeteorShower = now;
            m_noMusicTimer = m_random.Next(FAST_NO_MUSIC_TIME_MIN, FAST_NO_MUSIC_TIME_MAX);
            if ((int)m_currentMusicCategory < (int)MusicCategory.danger)
                PlayDangerMusic();
        }

        public void Fighting(bool heavy, int amount)
        {
            m_fightLight = Math.Min(m_fightLight + amount, FIGHTING_NEED);
            m_fightLightCooldown = FIGHTING_COOLDOWN_LIGHT;
            if (heavy)
            {
                m_fightHeavy = Math.Min(m_fightHeavy + amount, FIGHTING_NEED);
                m_fightHeavyCooldown = FIGHTING_COOLDOWN_HEAVY;
            }
            if (!CanChangeCategory)
                return;
            if (m_fightHeavy >= FIGHTING_NEED && (int)m_currentMusicCategory < (int)MusicCategory.heavyFight)
                PlayFightingMusic(false);
            else if (m_fightLight >= FIGHTING_NEED && (int)m_currentMusicCategory < (int)MusicCategory.lightFight)
                PlayFightingMusic(true);
        }

        public void IncreaseCategory(MyStringId category, int amount)
        {
            if(category == m_stringIdLightFight)
                Fighting(false, amount);
            else if(category == m_stringIdHeavyFight)
                Fighting(true, amount);
            else if (category == m_stringIdBuilding)
                Building(amount);
            else if (category == m_stringIdDanger)
                MeteorShowerIncoming();
        }

        #endregion

        #region DynamicMusic

        private void PlayDangerMusic()
        {
            CategoryPlaying = m_stringIdDanger;
            m_currentMusicCategory = MusicCategory.danger;
            if (m_musicSourceVoice != null && m_musicSourceVoice.IsPlaying)
                PlayMusic(CueIdPlaying, m_hashCrossfade, METEOR_SHOWER_CROSSFADE_LENGTH, new MyCueId[] { SelectCueFromCategory(m_stringIdDanger) }, false);
            else
                PlayMusic(SelectCueFromCategory(CategoryPlaying), m_hashFadeIn, METEOR_SHOWER_CROSSFADE_LENGTH / 2, new MyCueId[] { });

            m_noMusicTimer = m_random.Next(DEFAULT_NO_MUSIC_TIME_MIN, DEFAULT_NO_MUSIC_TIME_MAX);
        }

        private void PlayBuildingMusic()
        {
            CategoryPlaying = m_stringIdBuilding;
            m_currentMusicCategory = MusicCategory.building;
            if (m_musicSourceVoice != null && m_musicSourceVoice.IsPlaying)
                PlayMusic(CueIdPlaying, m_hashCrossfade, BUILDING_CROSSFADE_LENGTH, new MyCueId[] { SelectCueFromCategory(m_stringIdBuilding) }, false);
            else
                PlayMusic(SelectCueFromCategory(CategoryPlaying), m_hashFadeIn, BUILDING_CROSSFADE_LENGTH / 2, new MyCueId[] { });

            m_noMusicTimer = m_random.Next(DEFAULT_NO_MUSIC_TIME_MIN, DEFAULT_NO_MUSIC_TIME_MAX);
        }

        private void PlayFightingMusic(bool light)
        {
            CategoryPlaying = light ? m_stringIdLightFight : m_stringIdHeavyFight;
            m_currentMusicCategory = light ? MusicCategory.lightFight : MusicCategory.heavyFight;
            if (m_musicSourceVoice != null && m_musicSourceVoice.IsPlaying)
                PlayMusic(CueIdPlaying, m_hashCrossfade, FIGHTING_CROSSFADE_LENGTH, new MyCueId[] { SelectCueFromCategory(CategoryPlaying) }, false);
            else
                PlayMusic(SelectCueFromCategory(CategoryPlaying), m_hashFadeIn, FIGHTING_CROSSFADE_LENGTH / 2, new MyCueId[] { });

            m_noMusicTimer = m_random.Next(FAST_NO_MUSIC_TIME_MIN, FAST_NO_MUSIC_TIME_MAX);
        }

        #endregion

        #region LocationMusicCalculation

        private void CalculateNextCue()
        {
            if (MySession.Static == null || MySession.Static.LocalCharacter == null)
                return;
            m_noMusicTimer = m_random.Next(DEFAULT_NO_MUSIC_TIME_MIN, DEFAULT_NO_MUSIC_TIME_MAX);

            //planet or space
            Vector3D pos = MySession.Static.LocalCharacter.PositionComp.GetPosition();
            MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(pos);
            var grav = planet!= null ? planet.Components.Get<MyGravityProviderComponent>() as MySphericalNaturalGravityComponent : null;
            if (planet != null && (grav != null) && Vector3D.Distance(pos, planet.PositionComp.GetPosition()) <= grav.GravityLimit * 0.65f)
            {
                if (planet != m_lastVisitedPlanet)
                {
                    m_lastVisitedPlanet = planet;
                    if (planet.Generator.MusicCategories != null && planet.Generator.MusicCategories.Count > 0)
                    {
                        m_actualMusicOptions.Clear();
                        foreach (var option in planet.Generator.MusicCategories)
                            m_actualMusicOptions.Add(new MusicOption(option.Category, option.Frequency));
                    }
                    else
                        m_actualMusicOptions = m_defaultPlanetCategory;
                }
            }
            else
            {
                m_lastVisitedPlanet = null;
                m_actualMusicOptions = m_defaultSpaceCategories;
            }

            //choose category based on frequency
            float sum = 0f;
            foreach (var option in m_actualMusicOptions)
            {
                sum += Math.Max(option.Frequency, 0f);
            }
            float r = (float)m_random.NextDouble() * sum;
            MyStringId selected = m_actualMusicOptions[0].Category;
            for (int i = 0; i < m_actualMusicOptions.Count; i++)
            {
                if (r <= m_actualMusicOptions[i].Frequency)
                {
                    selected = m_actualMusicOptions[i].Category;
                    break;
                }
                else
                    r -= m_actualMusicOptions[i].Frequency;
            }

            //pick from cue list and play
            CueIdPlaying = SelectCueFromCategory(selected);
            CategoryPlaying = selected;

            if (CueIdPlaying == m_cueEmpty)
                return;

            PlayMusic(CueIdPlaying, MyStringHash.NullOrEmpty);
            m_currentMusicCategory = MusicCategory.location;
        }

        #endregion

        #region Utility

        public void PlaySpecificMusicTrack(MyCueId cue, bool playAtLeastOnce)
        {
            if (!cue.IsNull)
            {
                if (m_musicSourceVoice != null && m_musicSourceVoice.IsPlaying)
                    PlayMusic(CueIdPlaying, m_hashCrossfade, BUILDING_CROSSFADE_LENGTH, new MyCueId[] { cue }, false);
                else
                    PlayMusic(cue, m_hashFadeIn, BUILDING_CROSSFADE_LENGTH / 2, new MyCueId[] { });

                m_noMusicTimer = m_random.Next(DEFAULT_NO_MUSIC_TIME_MIN, DEFAULT_NO_MUSIC_TIME_MAX);
                CanChangeCategoryLocal = !playAtLeastOnce;
                m_currentMusicCategory = MusicCategory.location;
            }
        }

        public void PlaySpecificMusicCategory(MyStringId category, bool playAtLeastOnce)
        {
            if (category.Id != 0)
            {
                CategoryPlaying = category;
                if (m_musicSourceVoice != null && m_musicSourceVoice.IsPlaying)
                    PlayMusic(CueIdPlaying, m_hashCrossfade, BUILDING_CROSSFADE_LENGTH, new MyCueId[] { SelectCueFromCategory(CategoryPlaying) }, false);
                else
                    PlayMusic(SelectCueFromCategory(CategoryPlaying), m_hashFadeIn, BUILDING_CROSSFADE_LENGTH / 2, new MyCueId[] { });

                m_noMusicTimer = m_random.Next(DEFAULT_NO_MUSIC_TIME_MIN, DEFAULT_NO_MUSIC_TIME_MAX);
                CanChangeCategoryLocal = !playAtLeastOnce;
                m_currentMusicCategory = MusicCategory.custom;
            }
        }

        public void SetSpecificMusicCategory(MyStringId category)
        {
            if (category.Id != 0)
            {
                CategoryPlaying = category;
                m_currentMusicCategory = MusicCategory.custom;
            }
        }

        private void PlayMusic(MyCueId cue, MyStringHash effect, int effectDuration = 2000, MyCueId[] cueIds = null, bool play = true)
        {
            if (MyAudio.Static == null)
                return;
            if(play)
                m_musicSourceVoice = MyAudio.Static.PlayMusicCue(cue, true);
            if (m_musicSourceVoice != null)
            {
                if (effect != MyStringHash.NullOrEmpty)
                {
                    m_musicSourceVoice = MyAudio.Static.ApplyEffect(m_musicSourceVoice, effect, cueIds, effectDuration, true).OutputSound;
                }
                if (m_musicSourceVoice != null)
                    m_musicSourceVoice.StoppedPlaying += MusicStopped;
            }
            m_lastMusicData = MyAudio.Static.GetCue(cue);
        }

        private MyCueId SelectCueFromCategory(MyStringId category)
        {
            //check list of cues for selected category - if empty copy cues from storage
            if (m_musicCuesRemaining.ContainsKey(category) == false)
                m_musicCuesRemaining.Add(category, new List<MyCueId>());
            if (m_musicCuesRemaining[category].Count == 0)
            {
                if (m_musicCuesAll.ContainsKey(category) == false || m_musicCuesAll[category] == null || m_musicCuesAll[category].Count == 0)
                    return m_cueEmpty;
                foreach (var option in m_musicCuesAll[category])
                    m_musicCuesRemaining[category].Add(option);
                m_musicCuesRemaining[category].ShuffleList();
            }

            //pick from cue list
            MyCueId result = m_musicCuesRemaining[category][0];
            m_musicCuesRemaining[category].RemoveAt(0);
            return result;
        }

        public void MusicStopped()
        {
            if (m_musicSourceVoice == null || m_musicSourceVoice.IsPlaying == false)
            {
                CategoryLast = CategoryPlaying;
                CategoryPlaying = MyStringId.NullOrEmpty;
                CanChangeCategoryLocal = true;
            }
        }

        #endregion

        #region MusicCuesLoading

        public void ClearMusicCues()
        {
            m_musicCuesAll.Clear();
            m_musicCuesRemaining.Clear();
        }

        public void AddMusicCue(MyStringId category, MyCueId cueId)
        {
            if (m_musicCuesAll.ContainsKey(category) == false)
                m_musicCuesAll.Add(category, new List<MyCueId>());
            m_musicCuesAll[category].Add(cueId);
        }

        public void SetMusicCues(Dictionary<MyStringId, List<MyCueId>> musicCues)
        {
            ClearMusicCues();
            m_musicCuesAll = musicCues;
        }

        public void Unload()
        {
            if(m_musicSourceVoice != null)
            {
                m_musicSourceVoice.Stop();
                m_musicSourceVoice = null;
            }
            Active = false;
            ClearMusicCues();
        }

        #endregion
    }
}
