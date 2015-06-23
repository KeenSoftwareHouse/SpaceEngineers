using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using VRage.Collections;
using VRage;
using VRage.Audio;
using VRage.Utils;
using VRage.Utils;
using VRageMath;
using VRage.Library.Utils;
using VRage.ModAPI;


namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SoundBlock))]
    class MySoundBlock : MyFunctionalBlock, IMyPowerConsumer, IMySoundBlock
    {
        private const float MAXIMUM_LOOP_PERIOD = 30 * 60f; // seconds
        private const int EMITTERS_NUMBER = 5;
        private const int LOOP_UPDATE_THRESHOLD = 5; // seconds
        private static StringBuilder m_helperSB = new StringBuilder();

        #region Fields

        private float m_soundRadius;
        private float m_volume;
        private MyCueId m_cueId;
        private float m_loopPeriod;
        private MySoundPair m_soundPair;
        private bool m_isLoopable;
        private MyEntity3DSoundEmitter[] m_soundEmitters;
        private int m_soundEmitterIndex;
        private long m_startLoopTimeMs;

        #endregion

        #region Properties

        public float Range
        {
            get { return m_soundRadius; }
            set
            {
                if (m_soundRadius != value)
                {
                    m_soundRadius = value;
                    for (int i = 0; i < EMITTERS_NUMBER; i++)
                        m_soundEmitters[i].CustomMaxDistance = m_soundRadius;
                    RaisePropertiesChanged();
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
                    m_volume = value;
                    for (int i = 0; i < EMITTERS_NUMBER; i++)
                        m_soundEmitters[i].CustomVolume = m_volume;
                    RaisePropertiesChanged();
                }
            }
        }

        public MyCueId CueId
        {
            get { return m_cueId; }
            set { m_cueId = value; }
        }

        public float LoopPeriod
        {
            get { return m_loopPeriod; }
            set 
            {
                if (m_loopPeriod != value)
                {
                    m_loopPeriod = value;

                    RaisePropertiesChanged();
                }
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

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }

        public new MySyncSoundBlock SyncObject
        {
            get { return (MySyncSoundBlock)base.SyncObject; }
        }

        #endregion

        #region Init & object builder

        static MySoundBlock()
        {      
            var volumeSlider = new MyTerminalControlSlider<MySoundBlock>("VolumeSlider", MySpaceTexts.BlockPropertyTitle_SoundBlockVolume, MySpaceTexts.BlockPropertyDescription_SoundBlockVolume);
            volumeSlider.SetLimits(0, 1.0f);
            volumeSlider.DefaultValue = 1;
            volumeSlider.Getter = (x) => x.Volume;
            volumeSlider.Setter = (x, v) => x.SyncObject.SendChangeSoundVolumeRequest(v);
            volumeSlider.Writer = (x, result) => result.AppendInt32((int)(x.Volume * 100.0)).Append(" %");
            volumeSlider.EnableActions();
            MyTerminalControlFactory.AddControl(volumeSlider);

            var rangeSlider = new MyTerminalControlSlider<MySoundBlock>("RangeSlider", MySpaceTexts.BlockPropertyTitle_SoundBlockRange, MySpaceTexts.BlockPropertyDescription_SoundBlockRange);
            rangeSlider.SetLimits(0, 500);
            rangeSlider.DefaultValue = 50;
            rangeSlider.Getter = (x) => x.Range;
            rangeSlider.Setter = (x, v) => x.SyncObject.SendChangeSoundRangeRequest(v);
            rangeSlider.Writer = (x, result) => result.AppendInt32((int)x.Range).Append(" m");
            rangeSlider.EnableActions();
            MyTerminalControlFactory.AddControl(rangeSlider);
            
            var playButton = new MyTerminalControlButton<MySoundBlock>("PlaySound", MySpaceTexts.BlockPropertyTitle_SoundBlockPlay, MySpaceTexts.Blank, (x) => x.SyncObject.SendPlaySoundRequest());
            playButton.Enabled = (x) => x.IsSoundSelected;
            playButton.EnableAction();
            MyTerminalControlFactory.AddControl(playButton);

            var stopButton = new MyTerminalControlButton<MySoundBlock>("StopSound", MySpaceTexts.BlockPropertyTitle_SoundBlockStop, MySpaceTexts.Blank, (x) => x.SyncObject.SendStopSoundRequest());
            stopButton.Enabled = (x) => x.IsSoundSelected;
            stopButton.EnableAction();
            MyTerminalControlFactory.AddControl(stopButton);

            var loopableTimeSlider = new MyTerminalControlSlider<MySoundBlock>("LoopableSlider", MySpaceTexts.BlockPropertyTitle_SoundBlockLoopTime, MySpaceTexts.Blank);
            loopableTimeSlider.DefaultValue = 1f;
            loopableTimeSlider.Getter = (x) => x.LoopPeriod;
            loopableTimeSlider.Setter = (x, f) => x.SyncObject.SendChangeLoopPeriodRequest(f);
            loopableTimeSlider.Writer = (x, result) => MyValueFormatter.AppendTimeInBestUnit(x.LoopPeriod, result);
            loopableTimeSlider.Enabled = (x) => x.IsLoopable;
            loopableTimeSlider.Normalizer = (x, f) => x.NormalizeLoopPeriod(f);
            loopableTimeSlider.Denormalizer = (x, f) => x.DenormalizeLoopPeriod(f);
            loopableTimeSlider.EnableActions();
            MyTerminalControlFactory.AddControl(loopableTimeSlider);

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
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);

            var builder = (MyObjectBuilder_SoundBlock)objectBuilder;

            Volume = builder.Volume;
            Range = builder.Range;
            LoopPeriod = builder.LoopPeriod;
            InitCue(builder.CueName);

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                MyEnergyConstants.MAX_REQUIRED_POWER_SOUNDBLOCK,
                UpdateRequiredPowerInput);
            PowerReceiver.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            PowerReceiver.Update();

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

            return ob;
        }

        #endregion

        #region Methods - sounds operations

        public void PlaySound()
        {
            if (!m_soundPair.SoundId.IsNull)
            {
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
            if (sync)
            {
                SyncObject.SendSelectSoundRequest(cueId);
            }
            else
            {
                CueId = cueId;

                if (!MySandboxGame.IsDedicated)
                {
                    m_soundPair.Init(cueId);
                    var soundData = MyAudio.Static.GetCue(cueId);
                    if (soundData != null)
                        IsLoopable = soundData.Loopable;
                }

                RaisePropertiesChanged();
            }
        }

        //public void SelectSound(int cueId, bool sync)
        //{
        //    SelectSound(MyStringId.TryGet(cueId), sync);
        //}

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
                if (!m_soundEmitters[i].Loop)
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

        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncSoundBlock(this);
        }

        void PowerReceiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
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
            return base.CheckIsWorking() && PowerReceiver.IsPowered;
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();

            PowerReceiver.Update();
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
