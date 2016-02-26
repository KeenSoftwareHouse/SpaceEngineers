﻿using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Audio;
using VRage.Utils;
using VRageMath;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using VRage.Game;


namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SoundBlock))]
    class MySoundBlock : MyFunctionalBlock, IMySoundBlock
    {
        private const float MAXIMUM_LOOP_PERIOD = 30 * 60f; // seconds
        private const int EMITTERS_NUMBER = 5;
        private const int LOOP_UPDATE_THRESHOLD = 5; // seconds
        private static StringBuilder m_helperSB = new StringBuilder();

        #region Fields

        private readonly Sync<float> m_soundRadius;
        private readonly Sync<float> m_volume;
        private readonly Sync<MyCueId> m_cueId;
        private readonly Sync<float> m_loopPeriod;
        private MySoundPair m_soundPair;
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

        public MyCueId CueId
        {
            get { return m_cueId; }
            set { m_cueId.Value = value; }
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
            get { return !CueId.IsNull; }
        }

        #endregion

        #region Init & object builder

        static MySoundBlock()
        {      
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

        public MySoundBlock() : base()
        {
            m_soundPair = new MySoundPair();

            m_soundEmitterIndex = 0;
            m_soundEmitters = new MyEntity3DSoundEmitter[EMITTERS_NUMBER];
            for (int i = 0; i < EMITTERS_NUMBER; i++)
            {
                m_soundEmitters[i] = new MyEntity3DSoundEmitter(this);
                m_soundEmitters[i].Force3D = true;
            }

            m_volume.ValueChanged += (x) => VolumeChanged();
            m_soundRadius.ValueChanged += (x) => RadiusChanged();
            m_cueId.ValueChanged += (x) => SelectionChanged();

        }

        void SelectionChanged()
        {
            if (!MySandboxGame.IsDedicated)
            {
                m_soundPair.Init(m_cueId);
                var soundData = MyAudio.Static.GetCue(m_cueId);
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
        }

        private void InitCue(string cueName)
        {
            if (string.IsNullOrEmpty(cueName))
            {
                CueId = new MyCueId(MyStringHash.NullOrEmpty);
            }
            else
            {
                var cueId = new MyCueId(MyStringHash.GetOrCompute(cueName));
                MySoundCategoryDefinition.SoundDescription soundDesc = null;
                var soundCategories = MyDefinitionManager.Static.GetSoundCategoryDefinitions();

                // check whether saved cue is in some category
                foreach (var category in soundCategories)
                {
                    foreach (var soundDescTmp in category.Sounds)
                    {
                        if (MySoundPair.GetCueId(soundDescTmp.SoundId) == cueId)
                            soundDesc = soundDescTmp;
                    }     
                }

                if (soundDesc != null)
                    SelectSound(cueId, false);
                else
                    CueId = new MyCueId(MyStringHash.NullOrEmpty);
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
            if (!m_soundPair.SoundId.IsNull)
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
                m_soundEmitters[m_soundEmitterIndex].PlaySingleSound(m_soundPair, true);

            }
            else if (!IsSameLoopCue(m_soundPair.SoundId))
            {
                m_startLoopTimeMs = Stopwatch.GetTimestamp();
                m_soundEmitters[m_soundEmitterIndex].StopSound(true);
                m_soundEmitters[m_soundEmitterIndex].PlaySingleSound(m_soundPair, true);  
            }
        }

        private void PlaySingleSound()
        {
            if (IsLoopablePlaying)
                StopLoopableSound();
            else
                m_soundEmitters[m_soundEmitterIndex].StopSound(true);

            m_soundEmitters[m_soundEmitterIndex].PlaySingleSound(m_soundPair, true);

            m_soundEmitterIndex++;
            if (m_soundEmitterIndex == EMITTERS_NUMBER)
                m_soundEmitterIndex = 0;
        }

        public void SelectSound(List<MyGuiControlListbox.Item> cuesId, bool sync)
        {
            var cueId = (MyCueId)cuesId[0].UserData;
            SelectSound(cueId, sync);
        }

        public void SelectSound(MyCueId cueId, bool sync)
        {
            CueId = cueId;        
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
                    var stringId = MySoundPair.GetCueId(sound.SoundId);
                    
                    var item = new MyGuiControlListbox.Item(text: m_helperSB, userData: stringId);

                    listBoxContent.Add(item);
                    if (stringId == CueId)
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

        public override void UpdateBeforeSimulation100()
        {
            if (!IsWorking)
                return;

            base.UpdateBeforeSimulation100();

            for (int i = 0; i < EMITTERS_NUMBER; i++)
            {
                m_soundEmitters[i].Update();
            }
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
			return base.CheckIsWorking() && ResourceSink.IsPowered;
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();

            ResourceSink.Update();
        }

        private bool IsSameLoopCue(MyCueId newCue)
        {
            return newCue == m_soundEmitters[m_soundEmitterIndex].SoundId;
        }

        float IMySoundBlock.Volume { get {return Volume;} }
        float IMySoundBlock.Range { get {return Range;}  }
        bool IMySoundBlock.IsSoundSelected{ get {return IsSoundSelected;}}
        float IMySoundBlock.LoopPeriod { get { return LoopPeriod; } }
    }
}
