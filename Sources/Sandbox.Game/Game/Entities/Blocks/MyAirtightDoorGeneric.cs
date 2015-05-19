using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    public abstract class MyAirtightDoorGeneric : MyFunctionalBlock, IMyPowerConsumer, ModAPI.IMyDoor
    {

        private MySoundPair m_sound;

        protected float m_currOpening;//  0=closed, 1=fully open
        protected float m_subpartMovementDistance=2.5f;

        protected float m_openingSpeed = 0.3f;
        protected float m_currSpeed=0;

        private int m_lastUpdateTime;

        private new MySyncAirtightDoorGeneric m_sync;
        private bool m_open;

        private static readonly float EPSILON = 0.000000001f;

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public MyAirtightDoorGeneric()
        {
            m_open = false;
            m_currOpening = 0f;
            m_currSpeed = 0f;
            m_sync = new MySyncAirtightDoorGeneric(this);
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        protected void UpdateEmissivity()
        {
        }

        public bool Open
        {
            get
            {
                return m_open;
            }
        }

        public float OpenRatio
        {
            get { return m_currOpening; }
        }

        public bool IsFullyClosed //closed and airtight
        {
            get
            {
                return (m_currOpening < EPSILON);
            }
        }

        private new MyAirtightDoorGenericDefinition BlockDefinition
        {
            get { return (MyAirtightDoorGenericDefinition)base.BlockDefinition; }
        }

        static MyAirtightDoorGeneric()
        {
            var open = new MyTerminalControlOnOffSwitch<MyAirtightDoorGeneric>("Open", MySpaceTexts.Blank, on: MySpaceTexts.BlockAction_DoorOpen, off: MySpaceTexts.BlockAction_DoorClosed);
            open.Getter = (x) => x.Open;
            open.Setter = (x, v) => x.ChangeOpenClose(v);
            open.EnableToggleAction();
            open.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(open);
        }

        public void ChangeOpenClose(bool open)
        {
            if (open == m_open)
                return;
            if (!Sync.MultiplayerActive)
                DoChangeOpenClose(open);
            else
                m_sync.ChangeOpenClose(open);
        }
        
        internal bool DoChangeOpenClose(bool value)
        {
            if (!Enabled || !PowerReceiver.IsPowered)
                return false;//inoperative
            if (m_open != value)
            {
                m_open = value;
                OnStateChange();
                RaisePropertiesChanged();
                return true;//door will move
            }
            return false;//already as we want
        }

        private void OnStateChange()
        {//BEGIN OF MOVEMENT
            if (m_open)
                m_currSpeed = m_openingSpeed;
            else
                m_currSpeed = -m_openingSpeed;
            PowerReceiver.Update();

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds-1;//-1 because we need to have doors already moving at time of DoorStateChanged
            UpdateCurrentOpening();
            UpdateDoorPosition();
            if (m_open)
            {   //starting to open, not airtight any more
                var handle = DoorStateChanged;
                if (handle != null) handle(m_open);
            }
            m_stateChange = true;
        }


        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();
            base.OnEnabledChanged();
        }

        public override void OnBuildSuccess(long builtBy)
        {
            PowerReceiver.Update();
            base.OnBuildSuccess(builtBy);
        }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            base.Init(builder, cubeGrid);

            var ob = (MyObjectBuilder_AirtightDoorGeneric)builder;
            m_open = ob.Open;
            m_currOpening = ob.CurrOpening;

            m_openingSpeed = BlockDefinition.OpeningSpeed;
            m_sound = new MySoundPair(BlockDefinition.Sound);
            m_subpartMovementDistance = BlockDefinition.SubpartMovementDistance;

            PowerReceiver = new MyPowerReceiver(MyConsumerGroupEnum.Doors,
                false,
                BlockDefinition.PowerConsumptionMoving,
                () => UpdatePowerInput());
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();

            if (!Enabled || !PowerReceiver.IsPowered)
                UpdateDoorPosition();

            OnStateChange();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            PowerReceiver.Update();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_AirtightDoorGeneric)base.GetObjectBuilderCubeBlock(copy);
            ob.Open = m_open;
            ob.CurrOpening = m_currOpening;
            return ob;
        }

        protected float UpdatePowerInput()
        {
            if (!(Enabled && IsFunctional))
                return 0;
            if (m_currSpeed == 0)
                return BlockDefinition.PowerConsumptionIdle;
            return BlockDefinition.PowerConsumptionMoving;
        }

        private void StartSound(MySoundPair cuePair)
        {
            if ((m_soundEmitter.Sound != null) && (m_soundEmitter.Sound.IsPlaying) && (m_soundEmitter.SoundId == cuePair.SoundId))
                return;

            m_soundEmitter.StopSound(true);
            m_soundEmitter.PlaySingleSound(cuePair, true);
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            m_soundEmitter.Update();
        }

        bool m_stateChange = false;
        public override void UpdateBeforeSimulation()
        {
            if (m_stateChange && ((m_open && 1f-m_currOpening < EPSILON) || (!m_open && m_currOpening < EPSILON)))
            {
                //END OF MOVEMENT
                if (m_soundEmitter.IsPlaying && m_soundEmitter.Loop)
                    m_soundEmitter.StopSound(false);
                m_currSpeed = 0;
                PowerReceiver.Update();
                RaisePropertiesChanged();
                if (!m_open)
                {   //finished closing - they are airtight now
                    var handle = DoorStateChanged;
                    if (handle != null) handle(m_open);
                }
                m_stateChange = false;
            }
            if (Enabled && PowerReceiver.IsPowered && m_currSpeed != 0)
            {
                StartSound(m_sound);
            }
            else
            {
                m_soundEmitter.StopSound(false);
            }

            base.UpdateBeforeSimulation();
            UpdateCurrentOpening();

            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (CubeGrid.Physics == null)
                return;
            //Update door position because of inaccuracies in high velocities
            UpdateDoorPosition();
        }


        private void UpdateCurrentOpening()
        {
            if (Enabled && PowerReceiver.IsPowered)
            {
                float timeDelta = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime) / 1000f;
                float deltaPos = m_currSpeed * timeDelta;
                m_currOpening = MathHelper.Clamp(m_currOpening + deltaPos, 0f, 1f);
            }
        }

        protected abstract void UpdateDoorPosition();

        protected override void Closing()
        {
            m_soundEmitter.StopSound(true);
            base.Closing();
        }

        /*public override void OnModelChange()
        {
            base.OnModelChange();
            InitSubparts();
        }*/

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
        }

        public event Action<bool> DoorStateChanged;
        event Action<bool> Sandbox.ModAPI.IMyDoor.DoorStateChanged
        {
            add { DoorStateChanged += value; }
            remove { DoorStateChanged -= value; }
        }
    }
}
