using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.Sync;
using VRage.Utils;

namespace Sandbox.Game.Entities.Cube
{
    public partial class MyFunctionalBlock : MyTerminalBlock
    {
        protected MySoundPair m_baseIdleSound = new MySoundPair();
        protected MySoundPair m_actionSound = new MySoundPair();
        public MyEntity3DSoundEmitter m_soundEmitter = null;
		internal MyEntity3DSoundEmitter SoundEmitter { get { return m_soundEmitter; } }

        private readonly Sync<bool> m_enabled;

        public override void OnRemovedFromScene(object source)
        {
            if(m_soundEmitter != null)
                m_soundEmitter.StopSound(true, true);
                
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
#if XB1 // XB1_SYNC_NOREFLECTION
            m_enabled = SyncType.CreateAndAddProp<bool>();
#endif // BX1
            CreateTerminalControls();
          
            m_enabled.ValueChanged += (x)=> EnabledSyncChanged();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyFunctionalBlock>())
                return;
            base.CreateTerminalControls();

            var onOffSwitch = new MyTerminalControlOnOffSwitch<MyFunctionalBlock>("OnOff", MySpaceTexts.BlockAction_Toggle);
            onOffSwitch.Getter = (x) => x.Enabled;
            onOffSwitch.Setter = (x, v) => x.Enabled = v;
            onOffSwitch.EnableToggleAction();
            onOffSwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(0, onOffSwitch);

            MyTerminalControlFactory.AddControl(1, new MyTerminalControlSeparator<MyFunctionalBlock>());
        }

        protected override bool CheckIsWorking()
        {
            return Enabled && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            var ob = (MyObjectBuilder_FunctionalBlock)objectBuilder;
            m_soundEmitter = new MyEntity3DSoundEmitter(this, true);

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

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            if (m_soundEmitter != null && SilenceInChange)
            {
                SilenceInChange = m_soundEmitter.FastUpdate(IsSilenced);
                if (!SilenceInChange && !UsedUpdateEveryFrame)
                    NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        public virtual void UpdateSoundEmitters()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.Update();
        }

        protected virtual void OnStartWorking()
        {
            if (InScene && CubeGrid.Physics != null && m_soundEmitter != null && m_baseIdleSound != null && m_baseIdleSound != MySoundPair.Empty)
                m_soundEmitter.PlaySound(m_baseIdleSound, true);
        }

        protected virtual void OnStopWorking()
        {
            if (m_soundEmitter != null && (BlockDefinition.DamagedSound == null || m_soundEmitter.SoundId != BlockDefinition.DamagedSound.SoundId))
                m_soundEmitter.StopSound(false);
            //m_soundEmitter.PlaySingleSound(m_baseOffSound, false, true);
        }

        protected override void Closing()
        {
            if (m_soundEmitter != null) m_soundEmitter.StopSound(true);
            base.Closing();
        }

        public override void SetDamageEffect(bool show)
        {
            base.SetDamageEffect(show);
            if (m_soundEmitter == null)
                return;
            if (BlockDefinition.DamagedSound != null)
                if (show)
                    m_soundEmitter.PlaySound(BlockDefinition.DamagedSound, true);
                else
                    if (m_soundEmitter.SoundId == BlockDefinition.DamagedSound.SoundId)
                        m_soundEmitter.StopSound(false);
        }

        public override void StopDamageEffect()
        {
            base.StopDamageEffect();
            if (m_soundEmitter != null && BlockDefinition.DamagedSound != null && (m_soundEmitter.SoundId == BlockDefinition.DamagedSound.Arcade || m_soundEmitter.SoundId != BlockDefinition.DamagedSound.Realistic))
                m_soundEmitter.StopSound(true);
        }

        public override void OnDestroy()
        {
            if (MyFakes.SHOW_DAMAGE_EFFECTS)
            {
                //particle effect
                if (BlockDefinition.DestroyEffect.Length > 0)
                {
                    if (BlockDefinition.RatioEnoughForDamageEffect(SlimBlock.BuildIntegrity / SlimBlock.MaxIntegrity))//grinded down
                        return;
                    MyParticleEffect effect;
                    if (MyParticlesManager.TryCreateParticleEffect(BlockDefinition.DestroyEffect, out effect))
                    {
                        effect.Loop = false;
                        effect.WorldMatrix = WorldMatrix;
                    }
                }

                //sound effect
                if (!BlockDefinition.DestroySound.Equals(MySoundPair.Empty))
                {
                    MyEntity3DSoundEmitter emitter = MyAudioComponent.TryGetSoundEmitter();
                    if (emitter != null)
                    {
                        emitter.Entity = this;
                        emitter.PlaySound(BlockDefinition.DestroySound);
                    }
                }
            }
            base.OnDestroy();
        }

        public virtual int GetBlockSpecificState()
        {
            return -1;
        }
    }
}
