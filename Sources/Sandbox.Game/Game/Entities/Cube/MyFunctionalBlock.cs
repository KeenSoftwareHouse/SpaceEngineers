using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using System;
using VRage;
using VRage.Game;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Cube
{
    public partial class MyFunctionalBlock : MyTerminalBlock
    {
        protected MySoundPair m_baseIdleSound = new MySoundPair();
        protected MySoundPair m_actionSound = new MySoundPair();
        protected MyEntity3DSoundEmitter m_soundEmitter = null;
		internal MyEntity3DSoundEmitter SoundEmitter { get { return m_soundEmitter; } }

        private readonly Sync<bool> m_enabled;

        static MyFunctionalBlock()
        {
            var onOffSwitch = new MyTerminalControlOnOffSwitch<MyFunctionalBlock>("OnOff", MySpaceTexts.BlockAction_Toggle);
            onOffSwitch.Getter = (x) => x.Enabled;
            onOffSwitch.Setter = (x, v) => x.Enabled = v;
            onOffSwitch.EnableToggleAction();
            onOffSwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(0, onOffSwitch);

            MyTerminalControlFactory.AddControl(1, new MyTerminalControlSeparator<MyTerminalBlock>());
        }


        public override void OnRemovedFromScene(object source)
        {
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true, true);
                
            m_soundEmitter = null;
            base.OnRemovedFromScene(source);
        }

        void EnabledSyncChanged()
        {
            UpdateIsWorking();
            OnEnabledChanged();
        }

        public bool Enabled
        {
            get { return m_enabled; }
            set
            {
                m_enabled.Value = value;
            }
        }

        public event Action<MyTerminalBlock> EnabledChanged;

        public MyFunctionalBlock()
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            m_enabled.ValueChanged += (x)=> EnabledSyncChanged();
        }

        protected override bool CheckIsWorking()
        {
            return Enabled && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            var ob = (MyObjectBuilder_FunctionalBlock)objectBuilder;
            m_soundEmitter = new MyEntity3DSoundEmitter(this);

            m_enabled.Value = ob.Enabled;
            IsWorkingChanged += CubeBlock_IsWorkingChanged;
            m_baseIdleSound = BlockDefinition.PrimarySound;
            m_actionSound = BlockDefinition.ActionSound;
        }

        void CubeBlock_IsWorkingChanged(MyCubeBlock obj)
        {
            if (IsWorking)
                OnStartWorking();
            else
                OnStopWorking();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_FunctionalBlock)base.GetObjectBuilderCubeBlock(copy);

            ob.Enabled = Enabled;
            return ob;
        }

        protected virtual void OnEnabledChanged()
        {
            if (IsWorking)
                OnStartWorking();
            else
                OnStopWorking();
            var handler = EnabledChanged;
            if (handler != null) handler(this);

            // Enabled is also terminal property
            RaisePropertiesChanged();
        }

        public override void UpdateBeforeSimulation100()
        {
            if(m_soundEmitter != null)
                m_soundEmitter.Update();
            base.UpdateBeforeSimulation100();
        }

        protected virtual void OnStartWorking()
        {
            if (this.InScene && this.CubeGrid.Physics != null && m_soundEmitter != null && m_baseIdleSound != null && m_baseIdleSound != MySoundPair.Empty)
                m_soundEmitter.PlaySound(m_baseIdleSound, true);
        }

        protected virtual void OnStopWorking()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(false);
            //m_soundEmitter.PlaySingleSound(m_baseOffSound, false, true);
        }

        protected override void Closing()
        {
            if (m_soundEmitter != null) m_soundEmitter.StopSound(true);
            base.Closing();
        }
        internal override void SetDamageEffect(bool show)
        {
            if (m_soundEmitter == null)
                return;
            if (BlockDefinition.DamagedSound != null)
                if (show)
                    m_soundEmitter.PlaySound(BlockDefinition.DamagedSound, true);
                else
                    if (m_soundEmitter.SoundId == BlockDefinition.DamagedSound.SoundId)
                        m_soundEmitter.StopSound(false);
            base.SetDamageEffect(show);
        }
        internal override void StopDamageEffect()
        {
            if (m_soundEmitter != null && BlockDefinition.DamagedSound != null && m_soundEmitter.SoundId == BlockDefinition.DamagedSound.SoundId)
                m_soundEmitter.StopSound(true);
            base.StopDamageEffect();
        }

    }
}
