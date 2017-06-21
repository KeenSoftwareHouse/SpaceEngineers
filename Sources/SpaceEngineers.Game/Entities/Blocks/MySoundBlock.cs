using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.ModAPI;
using VRage.Network;
using VRage.Sync;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SoundBlock))]
    public class MySoundBlock : MyFunctionalBlock, IMySoundBlock
    {
        private const float MAXIMUM_LOOP_PERIOD = 30 * 60f; // seconds
        private const int EMITTERS_NUMBER = 5;
        private const int LOOP_UPDATE_THRESHOLD = 5; // seconds
        private static StringBuilder m_helperSB = new StringBuilder();

        #region Fields

        private readonly Sync<float> m_soundRadius;
        private readonly Sync<float> m_volume;
        private readonly Sync<string> m_cueIdString;
        private readonly Sync<float> m_loopPeriod;
        private bool m_isLoopable;
        private MyEntity3DSoundEmitter[] m_soundEmitters;
        private int m_soundEmitterIndex;
        private long m_startLoopTimeMs;
        private bool m_willStartSound; // will start sound in updateaftersimulation

        static MyTerminalControlButton<MySoundBlock> m_playButton, m_stopButton;
        static MyTerminalControlSlider<MySoundBlock> m_loopableTimeSlider;
        #endregion

        #region Properties

        public float Range
        {
            get { return m_soundRadius; }
            set
            {
                if (m_soundRadius != value)
                {
                    m_soundRadius.Value = value;
                }
            }
        }

        public float Volume
        {
            get { return m_volume; }
            set 
            {
                if (m_volume != value)
                {
                    m_volume.Value = value;
                }
            }
        }

        private MySoundPair m_cueId = MySoundPair.Empty;
        public MySoundPair CueId
        {
            get { return m_cueId; }
            set { m_cueId = value; }
        }

        public float LoopPeriod
        {
            get { return m_loopPeriod; }
            set
            {
                m_loopPeriod.Value = value;
            }
        }

        public bool IsLoopable
        {
            get { return m_isLoopable; }
            private set { m_isLoopable = value; }
        }

        public bool IsLoopablePlaying
        {
            get { return m_soundEmitters[m_soundEmitterIndex].Loop; }
        }

        public bool IsLoopPeriodUnderThreshold
        {
            get { return m_loopPeriod < LOOP_UPDATE_THRESHOLD; }
        }

        public bool IsSoundSelected
        {
            get { return !CueId.Arcade.IsNull || !CueId.Realistic.IsNull; }
        }

        #endregion

        #region Init & object builder

        public MySoundBlock() : base()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_soundRadius = SyncType.CreateAndAddProp<float>();
            m_volume = SyncType.CreateAndAddProp<float>();
            m_cueId = SyncType.CreateAndAddProp<MyCueId>();
            m_loopPeriod = SyncType.CreateAndAddProp<float>();
#endif // XB1
            CreateTerminalControls();

            m_soundEmitterIndex = 0;
            m_soundEmitters = new MyEntity3DSoundEmitter[EMITTERS_NUMBER];
            for (int i = 0; i < EMITTERS_NUMBER; i++)
            {
                m_soundEmitters[i] = new MyEntity3DSoundEmitter(this);
                m_soundEmitters[i].Force3D = true;
            }

            m_volume.ValueChanged += (x) => VolumeChanged();
            m_soundRadius.ValueChanged += (x) => RadiusChanged();
            m_cueIdString.ValueChanged += (x) => SelectionChanged();

        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MySoundBlock>())
                return;
            base.CreateTerminalControls();
            var volumeSlider = new MyTerminalControlSlider<MySoundBlock>("VolumeSlider", MySpaceTexts.BlockPropertyTitle_SoundBlockVolume, MySpaceTexts.BlockPropertyDescription_SoundBlockVolume);
            volumeSlider.SetLimits(0, 1.0f);
            volumeSlider.DefaultValue = 1;
            volumeSlider.Getter = (x) => x.Volume;
            volumeSlider.Setter = (x, v) => x.Volume = v;
            volumeSlider.Writer = (x, result) => result.AppendInt32((int)(x.Volume * 100.0)).Append(" %");
            volumeSlider.EnableActions();
            MyTerminalControlFactory.AddControl(volumeSlider);

            var rangeSlider = new MyTerminalControlSlider<MySoundBlock>("RangeSlider", MySpaceTexts.BlockPropertyTitle_SoundBlockRange, MySpaceTexts.BlockPropertyDescription_SoundBlockRange);
            rangeSlider.SetLimits(0, 500);
            rangeSlider.DefaultValue = 50;
            rangeSlider.Getter = (x) => x.Range;
            rangeSlider.Setter = (x, v) => x.Range = v;
            rangeSlider.Writer = (x, result) => result.AppendInt32((int)x.Range).Append(" m");
            rangeSlider.EnableActions();
            MyTerminalControlFactory.AddControl(rangeSlider);
            
            m_playButton = new MyTerminalControlButton<MySoundBlock>("PlaySound", MySpaceTexts.BlockPropertyTitle_SoundBlockPlay, MySpaceTexts.Blank, (x) => MyMultiplayer.RaiseEvent(x, y => y.PlaySound));
            m_playButton.Enabled = (x) => x.IsSoundSelected;
            m_playButton.EnableAction();
            MyTerminalControlFactory.AddControl(m_playButton);

            m_stopButton = new MyTerminalControlButton<MySoundBlock>("StopSound", MySpaceTexts.BlockPropertyTitle_SoundBlockStop, MySpaceTexts.Blank,
                (x) => { MyMultiplayer.RaiseEvent(x, y => y.StopSound); x.m_willStartSound = false; });
            m_stopButton.Enabled = (x) => x.IsSoundSelected;
            m_stopButton.EnableAction();
            MyTerminalControlFactory.AddControl(m_stopButton);

            m_loopableTimeSlider = new MyTerminalControlSlider<MySoundBlock>("LoopableSlider", MySpaceTexts.BlockPropertyTitle_SoundBlockLoopTime, MySpaceTexts.Blank);
            m_loopableTimeSlider.DefaultValue = 1f;
            m_loopableTimeSlider.Getter = (x) => x.LoopPeriod;
            m_loopableTimeSlider.Setter = (x, f) => x.LoopPeriod = f;
            m_loopableTimeSlider.Writer = (x, result) => MyValueFormatter.AppendTimeInBestUnit(x.LoopPeriod, result);
            m_loopableTimeSlider.Enabled = (x) => x.IsLoopable;
            m_loopableTimeSlider.Normalizer = (x, f) => x.NormalizeLoopPeriod(f);
            m_loopableTimeSlider.Denormalizer = (x, f) => x.DenormalizeLoopPeriod(f);
            m_loopableTimeSlider.EnableActions();
            MyTerminalControlFactory.AddControl(m_loopableTimeSlider);

            var soundsList = new MyTerminalControlListbox<MySoundBlock>("SoundsList", MySpaceTexts.BlockPropertyTitle_SoundBlockSoundList, MySpaceTexts.Blank);
            soundsList.ListContent = (x, list1, list2) => x.FillListContent(list1, list2);
            soundsList.ItemSelected = (x, y) => x.SelectSound(y, true);
            MyTerminalControlFactory.AddControl(soundsList);
        }

        void SelectionChanged()
        {
            if (!MySandboxGame.IsDedicated)
            {
                if (m_cueIdString.Value.Length > 0)
                    m_cueId = new MySoundPair(m_cueIdString.Value);
                else
                    m_cueId = MySoundPair.Empty;
                var soundData = MyAudio.Static.GetCue(m_cueId.Arcade);
                if (soundData != null)
                    IsLoopable = soundData.Loopable;
            }
            m_loopableTimeSlider.UpdateVisual();
            m_playButton.UpdateVisual();
            m_stopButton.UpdateVisual();
        }

        void RadiusChanged()
        {
            for (int i = 0; i < EMITTERS_NUMBER; i++)
                m_soundEmitters[i].CustomMaxDistance = m_soundRadius;
        }

        void VolumeChanged()
        {
            for (int i = 0; i < EMITTERS_NUMBER; i++)
                m_soundEmitters[i].CustomVolume = m_volume;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var soundBlockDefinition = BlockDefinition as MySoundBlockDefinition;
            Debug.Assert(soundBlockDefinition != null);

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                soundBlockDefinition.ResourceSinkGroup,
                MyEnergyConstants.MAX_REQUIRED_POWER_SOUNDBLOCK,
                UpdateRequiredPowerInput);
            sinkComp.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            var builder = (MyObjectBuilder_SoundBlock)objectBuilder;

            Volume = builder.Volume;
            Range = builder.Range;
            LoopPeriod = builder.LoopPeriod;
            if (builder.IsPlaying)
            {
                m_willStartSound = true;
            }
            InitCue(builder.CueName);
	 
            ResourceSink.Update();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            for (int i = 0; i < EMITTERS_NUMBER; i++)
            {
                m_soundEmitters[i] = new MyEntity3DSoundEmitter(this);
                m_soundEmitters[i].Force3D = true;
            }
        }

        private void InitCue(string cueName)
        {
            if (string.IsNullOrEmpty(cueName))
            {
                m_cueIdString.Value = "";
            }
            else
            {
                var cueId = new MySoundPair(cueName);
                MySoundCategoryDefinition.SoundDescription soundDesc = null;
                var soundCategories = MyDefinitionManager.Static.GetSoundCategoryDefinitions();

                // check whether saved cue is in some category
                foreach (var category in soundCategories)
                {
                    foreach (var soundDescTmp in category.Sounds)
                    {
                        if (cueId.SoundId.ToString().EndsWith(soundDescTmp.SoundId.ToString()))    //GK: EndsWith instead of Equals to catch both Realistic and Arcade sounds
                            soundDesc = soundDescTmp;
                    }     
                }

                if (soundDesc != null)
                    SelectSound(soundDesc.SoundId, false);
                else
                    m_cueIdString.Value = "";
            }
        }
    
        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_SoundBlock;

            ob.Volume = Volume;
            ob.Range = Range;
            ob.CueName = CueId.ToString();
            ob.LoopPeriod = LoopPeriod;

            bool isAnyEmitterPlaying = false;
            for (int i = 0; i < EMITTERS_NUMBER; i++)
                if (m_soundEmitters[i] != null && m_soundEmitters[i].IsPlaying)
                {
                    isAnyEmitterPlaying = true;
                    break;
                }
            ob.IsPlaying = isAnyEmitterPlaying || m_willStartSound;

            return ob;
        }

        #endregion

        #region Methods - sounds operations

        [Event, Reliable, Server, Broadcast]
        public void PlaySound()
        {
            if (CueId != MySoundPair.Empty && Enabled && IsWorking)
            {
                StopSound();
                if (IsLoopable)
                {
                    PlayLoopableSound();
                }
                else
                {
                    PlaySingleSound();
                }
            }
        }

        private void PlayLoopableSound()
        {  
            if (!IsLoopablePlaying)
            {
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

                m_startLoopTimeMs = Stopwatch.GetTimestamp();
                m_soundEmitters[m_soundEmitterIndex].PlaySingleSound(CueId, true);

            }
            else if (!IsSameLoopCue(CueId))
            {
                m_startLoopTimeMs = Stopwatch.GetTimestamp();
                m_soundEmitters[m_soundEmitterIndex].StopSound(true);
                m_soundEmitters[m_soundEmitterIndex].PlaySingleSound(CueId, true);  
            }
        }

        private void PlaySingleSound()
        {
            if (IsLoopablePlaying)
                StopLoopableSound();
            else
                m_soundEmitters[m_soundEmitterIndex].StopSound(true);

            m_soundEmitters[m_soundEmitterIndex].PlaySingleSound(CueId, true);

            m_soundEmitterIndex++;
            if (m_soundEmitterIndex == EMITTERS_NUMBER)
                m_soundEmitterIndex = 0;
        }

        public void SelectSound(List<MyGuiControlListbox.Item> cuesId, bool sync)
        {
            SelectSound(cuesId[0].UserData.ToString(), sync);
        }

        public void SelectSound(string cueId, bool sync)
        {
            m_cueIdString.Value = cueId;      
        }

        //public void SelectSound(int cueId, bool sync)
        //{
        //    SelectSound(MyStringId.TryGet(cueId), sync);
        //}

        [Event,Reliable,Server,Broadcast]
        public void StopSound()
        {
            for (int i = 0; i < EMITTERS_NUMBER; i++)
            {
                m_soundEmitters[i].StopSound(true);
            }

            NeedsUpdate &= ~(MyEntityUpdateEnum.EACH_FRAME);
            DetailedInfo.Clear();

            RaisePropertiesChanged();
        }

        private void StopLoopableSound()
        {
            m_soundEmitters[m_soundEmitterIndex].StopSound(true);

            NeedsUpdate &= ~(MyEntityUpdateEnum.EACH_FRAME);
            DetailedInfo.Clear();

            RaisePropertiesChanged();
        }

        #endregion

        #region Methods - terminal helpers

        private void FillListContent(ICollection<MyGuiControlListbox.Item> listBoxContent, ICollection<MyGuiControlListbox.Item> listBoxSelectedItems)
        {
            foreach (var soundCategory in MyDefinitionManager.Static.GetSoundCategoryDefinitions())
            {
                foreach (var sound in soundCategory.Sounds)
                {
                    m_helperSB.Clear().Append(sound.SoundText);

                    var item = new MyGuiControlListbox.Item(text: m_helperSB, userData: sound.SoundId);

                    listBoxContent.Add(item);
                    if (sound.SoundId.Equals(m_cueIdString))
                        listBoxSelectedItems.Add(item);
                }
            }
        }

        private float NormalizeLoopPeriod(float value)
        {
            if (value == 0)
                return 0;
            else
                return MathHelper.InterpLogInv(value, 1f, MAXIMUM_LOOP_PERIOD);
        }

        private float DenormalizeLoopPeriod(float value)
        {
            if (value == 0)
                return 0;
            else
                return MathHelper.InterpLog(value, 1f, MAXIMUM_LOOP_PERIOD);
        }

        #endregion

        #region Methods - update

        public override void UpdateBeforeSimulation()
        {
            if (!IsWorking)
                return;

 	        base.UpdateBeforeSimulation();

            if (IsLoopablePlaying)
            {
                UpdateLoopableSoundEmitter();
            }
        }

        public override void UpdateSoundEmitters()
        {
            if (!IsWorking)
                return;
            for (int i = 0; i < EMITTERS_NUMBER; i++)
                if(m_soundEmitters[i] != null)
                    m_soundEmitters[i].Update();
        }

        private void UpdateLoopableSoundEmitter()
        {
            double elapsedSeconds = (Stopwatch.GetTimestamp() - m_startLoopTimeMs) / (double)Stopwatch.Frequency;
            if (elapsedSeconds > m_loopPeriod)
            {
                StopLoopableSound();
            }
            else
            {
                DetailedInfo.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_LoopTimer));
                MyValueFormatter.AppendTimeInBestUnit(Math.Max(0, (float)(m_loopPeriod - elapsedSeconds)), DetailedInfo);
                RaisePropertiesChanged();
            }
        }

        #endregion


        void PowerReceiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
        }

        private float UpdateRequiredPowerInput()
        {
            if (Enabled && IsFunctional)
                return MyEnergyConstants.MAX_REQUIRED_POWER_SOUNDBLOCK;
            else
                return 0.0f;
        }

        protected override void Closing()
        {
            base.Closing();

            if (m_soundEmitters != null)
            {
                for (int i = 0; i < EMITTERS_NUMBER; i++)
                {
                    m_soundEmitters[i].StopSound(true);
                }
            }
        }

        protected override void OnStartWorking()
        {
            base.OnStartWorking();

            for (int i = 0; i < EMITTERS_NUMBER; i++)
            {
                var sound = m_soundEmitters[i].Sound;
                if (sound != null)
                    sound.Resume();
            }

            if (m_willStartSound && CubeGrid.Physics != null)
            {
                // postponed sound start - here we should know if this.IsWorking == true
                // otherwise sound request is cancelled inside SendPlaySoundRequest call
                MyMultiplayer.RaiseEvent(this, x => x.PlaySound); 
                m_willStartSound = false;
            }
        }

        protected override void OnStopWorking()
        {
            base.OnStopWorking();

            for (int i = 0; i < EMITTERS_NUMBER; i++)
            {
                var sound = m_soundEmitters[i].Sound;
                if (sound != null)
                    sound.Pause();
            }
        }

        protected override bool CheckIsWorking()
        {
            return base.CheckIsWorking() && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();

            ResourceSink.Update();
        }

        private bool IsSameLoopCue(MySoundPair newCue)
        {
            return newCue.Equals(m_soundEmitters[m_soundEmitterIndex].SoundPair);
        }

        float ModAPI.Ingame.IMySoundBlock.Volume { get {return Volume;} }
        float ModAPI.Ingame.IMySoundBlock.Range { get { return Range; } }
        bool ModAPI.Ingame.IMySoundBlock.IsSoundSelected { get { return IsSoundSelected; } }
        float ModAPI.Ingame.IMySoundBlock.LoopPeriod { get { return LoopPeriod; } }
    }
}
