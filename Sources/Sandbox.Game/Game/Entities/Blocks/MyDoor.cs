using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Game.GameSystems.Electricity;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Havok;
using System.Reflection;
using System.Threading;
using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens;
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Game.Screens.Terminal.Controls;
using VRage.Utils;
using Sandbox.Definitions;
using Sandbox.Game.Localization;
using VRage.Components;
using VRage.ModAPI;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Door))]
    public class MyDoor : MyFunctionalBlock, IMyPowerConsumer, ModAPI.IMyDoor
    {
        private const float CLOSED_DISSASEMBLE_RATIO = 3.3f;

        private MySoundPair m_openSound;
        private MySoundPair m_closeSound;

        private float m_currOpening;
        private float m_currSpeed;
        private int m_lastUpdateTime;

        private long? m_interlockTargetId;
        public long? InterlockTargetId
        {
            get { return m_interlockTargetId; }
            set
            {
                if (m_interlockTargetId == value) return;
                if (value == 0) return;
                m_interlockTargetId = value == -1 ? null : value;
                if (m_interlockTargetId == EntityId)
                {
                    m_interlockTargetId = null;
                }
                if (!m_interlockTargetId.HasValue)
                {
                    UpdateVisual();
                    return;
                }
                MyDoor targetDoor = GetDoorById(m_interlockTargetId.Value);
                if (targetDoor == null) return;
                UpdateVisual();
            }
        }

        StringBuilder m_termTargetName = new StringBuilder();

        private MyEntitySubpart m_leftSubpart = null;
        private MyEntitySubpart m_rightSubpart = null;

        private new MySyncDoor SyncObject;
        private bool m_open;
        public bool IsDelayedOpen { get; set; }
        public float MaxOpen = 1.2f;

        private static readonly MyTerminalControlTextbox<MyDoor> TargetCoords;
        private static readonly MyTerminalControlButton<MyDoor> ClearTarget;
        private static readonly MyTerminalControlButton<MyDoor> CopyTargetCoordsButton;
        private static readonly MyTerminalControlCheckbox<MyDoor> DelayedOpen;
        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public override float DisassembleRatio
        {
            get
            {
                return base.DisassembleRatio * (Open ? 1.0f : CLOSED_DISSASEMBLE_RATIO);
            }
        }

        public MyDoor()
        {
            InterlockTargetId = null;
            m_open = false;
            m_currOpening = 0f;
            m_currSpeed = 0f;
            SyncObject = new MySyncDoor(this);
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            TargetCoords.UpdateVisual();
            ClearTarget.UpdateVisual();
            CopyTargetCoordsButton.UpdateVisual();
            DelayedOpen.UpdateVisual();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (Enabled && PowerReceiver.IsPowered)
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
                OnStateChange();
            }
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
        }

        public bool Open
        {
            get
            {
                return m_open;
            }
            set
            {
                if (m_open != value && Enabled && PowerReceiver.IsPowered)
                {
                    m_open = value;
                    OnStateChange();
                    RaisePropertiesChanged();
                }
            }
        }

        public float OpenRatio
        {
            get { return m_currOpening/MaxOpen; }
        }

        static MyDoor()
        {
            var open = new MyTerminalControlOnOffSwitch<MyDoor>("Open", MySpaceTexts.Blank, on: MySpaceTexts.BlockAction_DoorOpen, off: MySpaceTexts.BlockAction_DoorClosed);
            open.Getter = (x) => x.Open;
            open.Setter = (x, v) => x.SyncObject.SendChangeDoorRequest(v, x.OwnerId);
            open.EnableToggleAction();
            open.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(open);


            //--------------------------------------------------------------------------------------
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyDoor>());

            var copyCoordsButton = new MyTerminalControlButton<MyDoor>("CopyCoords", MySpaceTexts.LaserAntennaCopyCoords, MySpaceTexts.LaserAntennaCopyCoordsHelp,
                delegate(MyDoor self)
                {
                    StringBuilder sanitizedName = new StringBuilder(self.DisplayNameText);
                    sanitizedName.Replace(':', ' ');
                    StringBuilder sb = new StringBuilder("GPS:", 256);
                    sb.Append(sanitizedName); sb.Append(":");
                    sb.Append(self.Position.X.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    sb.Append(self.Position.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    sb.Append(self.Position.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    Thread thread = new Thread(() => System.Windows.Forms.Clipboard.SetText(sb.ToString()));
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                });
            MyTerminalControlFactory.AddControl(copyCoordsButton);

            CopyTargetCoordsButton = new MyTerminalControlButton<MyDoor>("CopyTargetCoords", MySpaceTexts.LaserAntennaCopyTargetCoords, MySpaceTexts.LaserAntennaCopyTargetCoordsHelp,
                delegate(MyDoor self)
                {
                    if (self.InterlockTargetId == null)
                        return;
                    MyDoor targetDoor = GetDoorById(self.InterlockTargetId.Value);
                    if(targetDoor==null)
                        return;
                    StringBuilder sanitizedName = new StringBuilder(targetDoor.DisplayName.ToString());
                    sanitizedName.Replace(':', ' ');
                    StringBuilder sb = new StringBuilder("GPS:", 256);
                    sb.Append(sanitizedName); sb.Append(":");
                    sb.Append(targetDoor.Position.X.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    sb.Append(targetDoor.Position.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    sb.Append(targetDoor.Position.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    Thread thread = new Thread(() => System.Windows.Forms.Clipboard.SetText(sb.ToString()));
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();

                });
            CopyTargetCoordsButton.Enabled = (x) => x.InterlockTargetId != null;
            MyTerminalControlFactory.AddControl(CopyTargetCoordsButton);

            var pasteTargetCoords = new MyTerminalControlButton<MyDoor>("PasteTargetCoords", MySpaceTexts.LaserAntennaPasteGPS, MySpaceTexts.Blank,
                 delegate(MyDoor self)
                 {
                     string clipboardText = null;
                     Thread thread = new Thread(() => clipboardText = System.Windows.Forms.Clipboard.GetText());
                     thread.SetApartmentState(ApartmentState.STA);
                     thread.Start();
                     thread.Join();
                     self.SyncObject.PasteCoordinates(clipboardText, self.OwnerId);
                 });
            //PasteGpsCoords.Enabled = (x) => x.P2PTargetCoords;
            pasteTargetCoords.EnableAction();
            MyTerminalControlFactory.AddControl(pasteTargetCoords);

            TargetCoords = new MyTerminalControlTextbox<MyDoor>("TargetCoords", MySpaceTexts.MyDoorSelectedCoords, MySpaceTexts.Blank);
            TargetCoords.Getter = (x) =>
            {
                x.m_termTargetName.Clear();
                if (x.m_interlockTargetId.HasValue)
                {
                    MyDoor targetDoor = GetDoorById(x.m_interlockTargetId.Value);
                    if (targetDoor != null)
                    {
                        x.m_termTargetName.Append(targetDoor.DisplayNameText);
                    }
                }
                return x.m_termTargetName;
            };
            TargetCoords.Enabled = (x) => false;
            MyTerminalControlFactory.AddControl(TargetCoords);

            ClearTarget = new MyTerminalControlButton<MyDoor>("ClearTarget", MySpaceTexts.MyDoorClearInterlockTarget, MySpaceTexts.Blank,
                delegate(MyDoor self)
                {
                    if (self.InterlockTargetId == null)
                        return;
                    self.SyncObject.ClearTarget(self.OwnerId);
                });
            ClearTarget.Enabled = (x) => x.InterlockTargetId != null;
            ClearTarget.EnableAction();
            MyTerminalControlFactory.AddControl(ClearTarget);


            DelayedOpen = new MyTerminalControlCheckbox<MyDoor>("DelayedOpen", MySpaceTexts.MyDoorDelayedOpen, MySpaceTexts.Blank);
            DelayedOpen.Enabled = (x) => x.InterlockTargetId != null;
            DelayedOpen.Getter = (x) => x.IsDelayedOpen;
            DelayedOpen.Setter = (x, v) =>
            {
                x.SyncObject.IsDelayedOpenChange(v,x.OwnerId);

            };
            DelayedOpen.EnableAction();
            MyTerminalControlFactory.AddControl(DelayedOpen);

        }



        public void SetOpenRequest(bool open, long identityId)
        {
            SyncObject.SendChangeDoorRequest(open, identityId);
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

            //m_subpartsSize = 0.5f * (0.5f * SlimBlock.CubeGrid.GridSize - 0.3f);

            if (BlockDefinition is MyDoorDefinition)
            {
                var doorDefinition = (MyDoorDefinition)BlockDefinition;
                MaxOpen = doorDefinition.MaxOpen;
                m_openSound = new MySoundPair(doorDefinition.OpenSound);
                m_closeSound = new MySoundPair(doorDefinition.CloseSound);
            }
            else
            {
                MaxOpen = 1.2f;
                m_openSound = new MySoundPair("BlockDoorSmallOpen");
                m_closeSound = new MySoundPair("BlockDoorSmallClose");
            }

            var ob = (MyObjectBuilder_Door)builder;
            m_open = ob.State;
            m_currOpening = ob.Opening;
            m_interlockTargetId = ob.InterlockTargetId == -1 || ob.InterlockTargetId == 0 ? (long?)null : ob.InterlockTargetId;
            IsDelayedOpen = ob.IsDelayedOpen;

            PowerReceiver = new MyPowerReceiver(MyConsumerGroupEnum.Doors,
                false,
                MyEnergyConstants.MAX_REQUIRED_POWER_DOOR,
                () => (Enabled && IsFunctional) ? PowerReceiver.MaxRequiredInput : 0f);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();

            if (!Enabled || !PowerReceiver.IsPowered)
                UpdateSlidingDoorsPosition(true);

            OnStateChange();

            if (m_open)
            {
                // required when reinitializing a door after the armor beneath it is destroyed
                if (Open && (m_currOpening == MaxOpen))
                    UpdateSlidingDoorsPosition(true);
            }

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        private void InitSubparts()
        {
            if (!CubeGrid.CreatePhysics)
                return;

            Subparts.TryGetValue("DoorLeft", out m_leftSubpart);
            Subparts.TryGetValue("DoorRight", out m_rightSubpart);

            UpdateSlidingDoorsPosition();

            if (CubeGrid.Projector != null)
            {
                //This is a projected grid, don't add collisions for subparts
                return;
            }

            if (m_leftSubpart != null && m_leftSubpart.Physics == null)
            {
                if ((m_leftSubpart.ModelCollision.HavokCollisionShapes != null) && (m_leftSubpart.ModelCollision.HavokCollisionShapes.Length > 0))
                {
                    var shape = m_leftSubpart.ModelCollision.HavokCollisionShapes[0];
                    m_leftSubpart.Physics = new Engine.Physics.MyPhysicsBody(m_leftSubpart, RigidBodyFlag.RBF_KINEMATIC);
                    m_leftSubpart.Physics.IsPhantom = false;
                    Vector3 center = new Vector3(0.35f, 0f, 0f) + m_leftSubpart.PositionComp.LocalVolume.Center;
                    m_leftSubpart.Physics.CreateFromCollisionObject(shape, center, WorldMatrix, null, MyPhysics.KinematicDoubledCollisionLayer);
                    m_leftSubpart.Physics.Enabled = true;
                }
            }

            if (m_rightSubpart != null && m_rightSubpart.Physics == null)
            {
                if ((m_rightSubpart.ModelCollision.HavokCollisionShapes != null) && (m_rightSubpart.ModelCollision.HavokCollisionShapes.Length > 0))
                {
                    var shape = m_rightSubpart.ModelCollision.HavokCollisionShapes[0];
                    m_rightSubpart.Physics = new Engine.Physics.MyPhysicsBody(m_rightSubpart, RigidBodyFlag.RBF_KINEMATIC);
                    m_rightSubpart.Physics.IsPhantom = false;
                    Vector3 center = new Vector3(-0.35f, 0f, 0f) + m_rightSubpart.PositionComp.LocalVolume.Center;
                    m_rightSubpart.Physics.CreateFromCollisionObject(shape, center, WorldMatrix, null, MyPhysics.KinematicDoubledCollisionLayer);
                    m_rightSubpart.Physics.Enabled = true;
                }
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_Door)base.GetObjectBuilderCubeBlock(copy);
            ob.State = Open;
            ob.Opening = m_currOpening;
            ob.InterlockTargetId = InterlockTargetId ?? -1;
            ob.IsDelayedOpen = IsDelayedOpen;
            ob.OpenSound = m_openSound.ToString();
            ob.CloseSound = m_closeSound.ToString();
            return ob;
        }

        private void OnStateChange()
        {
            float speed = ((MyDoorDefinition)BlockDefinition).OpeningSpeed;
            m_currSpeed = m_open ? speed : -speed;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            UpdateCurrentOpening();
            UpdateSlidingDoorsPosition();
            var handle = DoorStateChanged;
            if (handle != null) handle(m_open);
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

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (CubeGrid.Physics == null)
                return;
            //Update door position because of inaccuracies in high velocities
            UpdateSlidingDoorsPosition(this.CubeGrid.Physics.LinearVelocity.LengthSquared() > 10f);
        }

        public override void UpdateBeforeSimulation()
        {
            if ((Open && (m_currOpening == MaxOpen)) || (!Open && (m_currOpening == 0f)))
            {
                if (m_soundEmitter.IsPlaying && m_soundEmitter.Loop)
                    m_soundEmitter.StopSound(false);
                return;
            }

            if (Enabled && PowerReceiver.IsPowered)
            {
                if (Open)
                    StartSound(m_openSound);
                else
                    StartSound(m_closeSound);
            }

            base.UpdateBeforeSimulation();
            UpdateCurrentOpening();

            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        private void UpdateCurrentOpening()
        {
            if (Enabled && PowerReceiver.IsPowered)
            {
                bool flag = true;
                if (this.InterlockTargetId.HasValue && m_currSpeed > 0.0)
                {
                    MyDoor interlockTargetDoor = GetDoorById(this.InterlockTargetId.Value);
                    if (interlockTargetDoor != null)
                    {
                        if (interlockTargetDoor.Open)
                        {
                            interlockTargetDoor.SyncObject.SendChangeDoorRequest(false, interlockTargetDoor.OwnerId);
                            flag = false;
                        }
                        else
                        {
                            if (interlockTargetDoor.OpenRatio > double.Epsilon)
                            {
                                flag = false;
                            }
                        }
                    }
                }
                if(! IsDelayedOpen)
                {
                    flag = true;
                }
                if(flag)
                {
                    float timeDelta = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime) / 1000f;
                    float deltaPos = m_currSpeed * timeDelta;
                    m_currOpening = MathHelper.Clamp(m_currOpening + deltaPos, 0f, MaxOpen);
                }
            }
        }

        private void UpdateSlidingDoorsPosition(bool forceUpdate = false)
        {
            if (this.CubeGrid.Physics == null)
                return;

            float opening = m_currOpening * 0.475f; // enrico's magic numbers

            if (m_leftSubpart != null && m_leftSubpart.Physics != null)
            {
                m_leftSubpart.PositionComp.LocalMatrix = Matrix.CreateTranslation(new Vector3(-opening, 0f, 0f));
                if (m_leftSubpart.Physics.LinearVelocity != this.CubeGrid.Physics.LinearVelocity)
                {
                    m_leftSubpart.Physics.LinearVelocity = this.CubeGrid.Physics.LinearVelocity;
                }

                if (m_leftSubpart.Physics.AngularVelocity != this.CubeGrid.Physics.AngularVelocity)
                {
                    m_leftSubpart.Physics.AngularVelocity = this.CubeGrid.Physics.AngularVelocity;
                }
            }

            if (m_rightSubpart != null && m_rightSubpart.Physics != null)
            {
                m_rightSubpart.PositionComp.LocalMatrix = Matrix.CreateTranslation(new Vector3(opening, 0f, 0f));
                if (m_rightSubpart.Physics.LinearVelocity != this.CubeGrid.Physics.LinearVelocity)
                {
                    m_rightSubpart.Physics.LinearVelocity = this.CubeGrid.Physics.LinearVelocity;
                }

                if (m_rightSubpart.Physics.AngularVelocity != this.CubeGrid.Physics.AngularVelocity)
                {
                    m_rightSubpart.Physics.AngularVelocity = this.CubeGrid.Physics.AngularVelocity;
                }
            }
        }

        protected override void Closing()
        {
            m_soundEmitter.StopSound(true);
            base.Closing();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            InitSubparts();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
        }

        event Action<bool> DoorStateChanged;

 
   
        event Action<bool> Sandbox.ModAPI.IMyDoor.DoorStateChanged
        {
            add { DoorStateChanged += value; }
            remove { DoorStateChanged -= value; }
        }

        protected static MyDoor GetDoorById(long id)
        {

            MyEntity entity = null;
            MyEntities.TryGetEntityById(id, out entity);
            MyDoor laser = entity as MyDoor;
            //System.Diagnostics.Debug.Assert(laser != null, "Laser is null");
            return laser;
        }
    }
}
