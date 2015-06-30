using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

using Havok;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;

using VRage.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_AdvancedDoor))]
    public class MyAdvancedDoor : MyFunctionalBlock, IMyPowerConsumer, ModAPI.IMyDoor
    {
        private const float CLOSED_DISSASEMBLE_RATIO = 3.3f;

        private int m_lastUpdateTime;
        private float m_time; // timer for opening sequenz
        private float m_totalTime = 99999f; // holds the total time a Door needs to fully open, this will be set to m_time after the Door reaches FullyOpen the first time
        private bool m_stateChange = false;
        private bool m_open;

        // Auto Close
        private bool m_autoClose;
        private float m_autoCloseInterval = 2f;
        private float m_autoCloseTimer = 0f;

        private MySyncAdvancedDoor m_sync;

        private List<MyEntitySubpart> m_subparts = new List<MyEntitySubpart>();
        private List<int> m_subpartIDs = new List<int>();
        private List<float> m_currentOpening = new List<float>();
        private List<float> m_currentSpeed = new List<float>();
        private List<MyEntity3DSoundEmitter> m_emitter = new List<MyEntity3DSoundEmitter>();
        private List<Vector3> m_hingePosition = new List<Vector3>();
        private List<MyObjectBuilder_AdvancedDoorDefinition.Opening> m_openingSequence = new List<MyObjectBuilder_AdvancedDoorDefinition.Opening>();

        // temp matrices
        private Matrix[] transMat = new Matrix[1];
        private Matrix[] rotMat = new Matrix[1];

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

        public MyAdvancedDoor()
        {
            m_subparts.Clear();
            m_subpartIDs.Clear();
            m_currentOpening.Clear();
            m_currentSpeed.Clear();
            m_emitter.Clear();
            m_hingePosition.Clear();
            m_openingSequence.Clear();
            m_open = false;
            m_autoClose = false;
            m_sync = new MySyncAdvancedDoor(this);
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (Enabled && PowerReceiver != null && PowerReceiver.IsPowered)
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

        public bool AutoClose
        {
            get
            {
                return m_autoClose;
            }
            set
            {
                m_autoCloseTimer = 0f;
                m_autoClose = value;
                RaisePropertiesChanged();
            }
        }

        public MyBounds AutoCloseIntervalBounds
        {
            get
            {
                return BlockDefinition.AutocloseInterval;
            }
        }

        public float AutoCloseInterval
        {
            get
            {
                return m_autoCloseInterval;
            }
            set
            {
                if (m_autoCloseInterval != value)
                {
                    m_autoCloseInterval = (float)Math.Round(value, 1);
                    RaisePropertiesChanged();
                }
            }
        }

        public bool FullyClosed
        {
            get
            {
                return m_currentOpening.Sum() == 0f;
            }
        }

        public bool FullyOpen
        {
            get
            {
                return m_currentOpening.Sum() == m_openingSequence.Sum(x => x.MaxOpen);
            }
        }

        public float OpenRatio
        {
            get
            {
                return m_currentOpening.Sum() / m_openingSequence.Sum(x => x.MaxOpen);
            }
        }

        public float OpeningSpeed
        {
            get
            {
                for (int i = 0; i < m_currentSpeed.Count; i++)
                {
                    if (m_currentSpeed[i] > 0f)
                        return m_currentSpeed[i];
                }
                return 0f;
            }
        }

        private new MyAdvancedDoorDefinition BlockDefinition
        {
            get
            {
                return (MyAdvancedDoorDefinition)base.BlockDefinition;
            }
        }

        static MyAdvancedDoor()
        {
            var autoClose = new MyTerminalControlCheckbox<MyAdvancedDoor>("Autoclose", MySpaceTexts.BlockPropertiesText_DoorAutoclose, MySpaceTexts.Blank, on: MySpaceTexts.BlockAction_DoorAutocloseEnabled, off: MySpaceTexts.BlockAction_DoorAutocloseDisabled);
            autoClose.Getter = (x) => x.AutoClose;
            autoClose.Setter = (x, v) => x.m_sync.SendChangeAutocloseRequest(v, x.OwnerId);
            autoClose.EnableAction();
            MyTerminalControlFactory.AddControl(autoClose);

            var autoCloseInterval = new MyTerminalControlSlider<MyAdvancedDoor>("Autoclose Interval", MySpaceTexts.BlockPropertiesText_DoorAutocloseInterval, MySpaceTexts.Blank);
            autoCloseInterval.SetLimits((x) => x.AutoCloseIntervalBounds.Min, (x) => x.AutoCloseIntervalBounds.Max);
            autoCloseInterval.DefaultValueGetter = (x) => x.AutoCloseIntervalBounds.Default;
            autoCloseInterval.Getter = (x) => x.AutoCloseInterval;
            autoCloseInterval.Setter = (x, v) => x.m_sync.SendChangeAutocloseIntervalRequest(v);
            autoCloseInterval.Writer = (x, result) => result.Append(MyValueFormatter.GetFormatedFloat(x.AutoCloseInterval, 1));
            autoCloseInterval.EnableActions();
            MyTerminalControlFactory.AddControl(autoCloseInterval);

            var open = new MyTerminalControlOnOffSwitch<MyAdvancedDoor>("Open", MySpaceTexts.Blank, on: MySpaceTexts.BlockAction_DoorOpen, off: MySpaceTexts.BlockAction_DoorClosed);
            open.Getter = (x) => x.Open;
            open.Setter = (x, v) => x.m_sync.SendChangeDoorRequest(v, x.OwnerId);
            open.EnableToggleAction();
            open.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(open);
        }

        public void SetOpenRequest(bool open, long identityId)
        {
            m_sync.SendChangeDoorRequest(open, identityId);
        }

        private void OnStateChange()
        {
            for (int i = 0; i < m_openingSequence.Count; i++)
            {
                float speed = m_openingSequence[i].Speed;
                m_currentSpeed[i] = m_open ? speed : -speed;
            }

            PowerReceiver.Update();

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds - 1;

            UpdateCurrentOpening();
            UpdateDoorPosition();

            if (m_open)
            {
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
            UpdateHavokCollisionSystemID(CubeGrid.Physics.HavokCollisionSystemID);
            base.OnBuildSuccess(builtBy);
        }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            base.Init(builder, cubeGrid);

            var ob = (MyObjectBuilder_AdvancedDoor)builder;
            m_open = ob.Open;
            AutoClose = ob.Autoclose;
            AutoCloseInterval = ob.AutocloseInterval;

            PowerReceiver = new MyPowerReceiver(MyConsumerGroupEnum.Doors,
                false,
                BlockDefinition.PowerConsumptionMoving,
                () => UpdatePowerInput());
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();

            if (!Enabled || !PowerReceiver.IsPowered)
                UpdateDoorPosition();

            OnStateChange();

            if (m_open)
            {
                UpdateDoorPosition();
            }

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            PowerReceiver.Update();
        }

        private MyEntitySubpart LoadSubpartFromName(string name)
        {
            MyEntitySubpart subpart;

            Subparts.TryGetValue(name, out subpart);

            // simply return the subpart if it exists in the dictionary
            if (subpart != null)
                return subpart;

            // otherwise load it now and add to dictionary
            subpart = new MyEntitySubpart();

            string fileName = Path.Combine(Path.GetDirectoryName(Model.AssetName), name) + ".mwm";

            subpart.Render.EnableColorMaskHsv = Render.EnableColorMaskHsv;
            subpart.Render.ColorMaskHsv = Render.ColorMaskHsv;
            subpart.Init(null, fileName, this, null);

            // add to dictionary
            Subparts[name] = subpart;

            if (InScene)
                subpart.OnAddedToScene(this);

            return subpart;
        }

        private void InitSubparts()
        {
            if (!CubeGrid.CreatePhysics)
                return;

            m_subparts.Clear();
            m_subpartIDs.Clear();
            m_hingePosition.Clear();
            m_openingSequence.Clear();

            for (int i = 0; i < ((MyAdvancedDoorDefinition)BlockDefinition).Subparts.Length; i++)
            {
                MyEntitySubpart foundPart = LoadSubpartFromName(((MyAdvancedDoorDefinition)BlockDefinition).Subparts[i].Name);

                if (foundPart != null)
                {
                    m_subparts.Add(foundPart);

                    // save the Subparts hinge (pivot)
                    // if not defined...
                    if (((MyAdvancedDoorDefinition)BlockDefinition).Subparts[i].PivotPosition == null)
                    {
                        // ...try to get pivot from Model...
                        VRage.Import.MyModelBone bone = foundPart.Model.Bones.First(b => !b.Name.Contains("Root"));

                        if (bone != null)
                        {
                            m_hingePosition.Add(bone.Transform.Translation);
                        }
                    }
                    else // ...otherwise get from definition
                    {
                        m_hingePosition.Add((Vector3)((MyAdvancedDoorDefinition)BlockDefinition).Subparts[i].PivotPosition);
                    }
                }
            }

            // get the sequence count from definition
            int openSequenzCount = ((MyAdvancedDoorDefinition)BlockDefinition).OpeningSequence.Length;

            for (int i = 0; i < openSequenzCount; i++)
            {
                if (!String.IsNullOrEmpty(((MyAdvancedDoorDefinition)BlockDefinition).OpeningSequence[i].IDs))
                {
                    // if one sequence should be applied for multiple subparts (i.e. <IDs>1-3,4,6,8,9</IDs>
                    // add copies to m_openingSequence List

                    // split by comma
                    string[] tmp1 = ((MyAdvancedDoorDefinition)BlockDefinition).OpeningSequence[i].IDs.Split(',');

                    for (int j = 0; j < tmp1.Length; j++)
                    {
                        // split by minus
                        string[] tmp2 = tmp1[j].Split('-');

                        if (tmp2.Length == 2)
                        {
                            for (int k = Convert.ToInt32(tmp2[0]); k <= Convert.ToInt32(tmp2[1]); k++)
                            {
                                m_openingSequence.Add(((MyAdvancedDoorDefinition)BlockDefinition).OpeningSequence[i]);
                                m_subpartIDs.Add(k);
                            }
                        }
                        else
                        {
                            m_openingSequence.Add(((MyAdvancedDoorDefinition)BlockDefinition).OpeningSequence[i]);
                            m_subpartIDs.Add(Convert.ToInt32(tmp1[j]));
                        }
                    }
                }
                else
                {
                    Debug.Assert(false, "IDs cannot be null or empty");
                }
            }

            for (int i = 0; i < m_openingSequence.Count; i++)
            {
                if (m_currentOpening.Count < m_openingSequence.Count)
                    m_currentOpening.Add(0f);

                if (m_currentSpeed.Count < m_openingSequence.Count)
                    m_currentSpeed.Add(0f);

                if (m_emitter.Count < m_openingSequence.Count)
                    m_emitter.Add(new MyEntity3DSoundEmitter(this));

                // make sure maxOpen is always positive and invert accordingly
                if (m_openingSequence[i].MaxOpen < 0f)
                {
                    m_openingSequence[i].MaxOpen *= -1;
                    m_openingSequence[i].InvertRotation = !m_openingSequence[i].InvertRotation;
                }
            }

            Array.Resize(ref transMat, m_subparts.Count);
            Array.Resize(ref rotMat, m_subparts.Count);

            UpdateDoorPosition();

            if (CubeGrid.Projector != null)
            {
                //This is a projected grid, don't add collisions for subparts
                return;
            }

            for (int i = 0; i < m_subparts.Count; i++ )
            {
                m_subparts[i].Physics = null;
                if (m_subparts[i] != null && m_subparts[i].Physics == null && ((MyAdvancedDoorDefinition)BlockDefinition).Subparts[i].HasPhysics)
                {
                    if ((m_subparts[i].ModelCollision.HavokCollisionShapes != null) && (m_subparts[i].ModelCollision.HavokCollisionShapes.Length > 0))
                    {
                        List<HkShape> shapes = m_subparts[i].ModelCollision.HavokCollisionShapes.ToList();
                        var listShape = new HkListShape(shapes.GetInternalArray(), shapes.Count, HkReferencePolicy.None);
                        m_subparts[i].Physics = new Engine.Physics.MyPhysicsBody(m_subparts[i], RigidBodyFlag.RBF_DOUBLED_KINEMATIC | RigidBodyFlag.RBF_KINEMATIC);
                        m_subparts[i].Physics.IsPhantom = false;
                        m_subparts[i].Physics.CreateFromCollisionObject((HkShape)listShape, Vector3.Zero, WorldMatrix, null, MyPhysics.KinematicDoubledCollisionLayer);
                        m_subparts[i].Physics.Enabled = true;
                        listShape.Base.RemoveReference();
                    }
                }
            }

            CubeGrid.OnHavokSystemIDChanged -= CubeGrid_HavokSystemIDChanged;
            CubeGrid.OnHavokSystemIDChanged += CubeGrid_HavokSystemIDChanged;
            if (CubeGrid.Physics != null)
                UpdateHavokCollisionSystemID(CubeGrid.Physics.HavokCollisionSystemID);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_AdvancedDoor)base.GetObjectBuilderCubeBlock(copy);
            ob.Open = m_open;
            ob.Autoclose = AutoClose;
            ob.AutocloseInterval = AutoCloseInterval;
            return ob;
        }

        protected float UpdatePowerInput()
        {
            if (!(Enabled && IsFunctional))
                return 0;

            if (OpeningSpeed == 0f)
                return BlockDefinition.PowerConsumptionIdle;

            return BlockDefinition.PowerConsumptionMoving;
        }

        private void StartSound(int emitterId, MySoundPair cuePair)
        {
            if ((m_emitter[emitterId].Sound != null) && (m_emitter[emitterId].Sound.IsPlaying) && (m_emitter[emitterId].SoundId == cuePair.SoundId))
                return;

            m_emitter[emitterId].StopSound(true);
            m_emitter[emitterId].PlaySingleSound(cuePair);
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            for (int i = 0; i < m_emitter.Count; i++)
                m_emitter[i].Update();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (CubeGrid.Physics == null)
                return;

            UpdateDoorPosition();
        }

        public override void UpdateBeforeSimulation()
        {
            if (FullyClosed)
            {
                m_time = 0f;
                m_autoCloseTimer = 0f;
            }
            else if (FullyOpen)
            {
                if (m_totalTime != m_time)
                    m_totalTime = m_time;

                m_time = m_totalTime;

                if(AutoClose)
                    m_autoCloseTimer += ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime) / 1000f);
            }

            if(AutoClose && FullyOpen)
            {
                if (m_autoCloseTimer >= m_autoCloseInterval)
                {
                    m_autoCloseTimer = 0f;
                    SetOpenRequest(!Open, this.OwnerId);
                }
            }

            for (int i = 0; i < m_openingSequence.Count; i++)
            {
                float maxOpen = m_openingSequence[i].MaxOpen;

                if ((Open && (m_currentOpening[i] == maxOpen)) || (!Open && (m_currentOpening[i] == 0f)))
                {
                    if (m_emitter[i] != null && m_emitter[i].IsPlaying && m_emitter[i].Loop)
                        m_emitter[i].StopSound(false);

                    m_currentSpeed[i] = 0f;
                }

                if (Enabled && PowerReceiver != null && PowerReceiver.IsPowered && m_currentSpeed[i] != 0)
                {
                    string soundName = "";
                    if (Open)
                    {
                        soundName = m_openingSequence[i].OpenSound;
                    }
                    else
                    {
                        soundName = m_openingSequence[i].CloseSound;
                    }

                    if (!String.IsNullOrEmpty(soundName))
                        StartSound(i, new MySoundPair(soundName));
                }
                else
                {
                    if (m_emitter[i] != null)
                        m_emitter[i].StopSound(false);
                }
            }

            if (m_stateChange && ((m_open && FullyOpen) || (!m_open && FullyClosed)))
            {
                PowerReceiver.Update();
                RaisePropertiesChanged();
                if (!m_open)
                {
                    var handle = DoorStateChanged;
                    if (handle != null) handle(m_open);
                }
                m_stateChange = false;
            }

            base.UpdateBeforeSimulation();
            UpdateCurrentOpening();

            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            // Draw Physical primitives for Subparts
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_COLLISION_PRIMITIVES)
            {
                for (int i = 0; i < m_subparts.Count; i++)
                {
                    m_subparts[i].DebugDraw();
                    m_subparts[i].DebugDrawPhysics();
                }
            }
        }

        private void UpdateCurrentOpening()
        {
            if (Enabled && PowerReceiver != null && PowerReceiver.IsPowered)
            {
                float timeDelta = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime) / 1000f;

                // keep track of time since the last state changed
                m_time += ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime) / 1000f) * (m_open ? 1f : -1f);
                m_time = MathHelper.Clamp(m_time, 0f, m_totalTime);

                for (int i = 0; i < m_openingSequence.Count; i++)
                {
                    float delay = m_open ? m_openingSequence[i].OpenDelay : m_openingSequence[i].CloseDelay;

                    if ((m_open && m_time > delay) || (!m_open && m_time < m_totalTime - delay))
                    {
                        float deltaPos = m_currentSpeed[i] * timeDelta;
                        float maxOpen = m_openingSequence[i].MaxOpen;

                        m_currentOpening[i] = MathHelper.Clamp(m_currentOpening[i] + deltaPos, 0f, maxOpen);
                    }
                }
            }
        }

        private void UpdateDoorPosition()
        {
            if (this.CubeGrid.Physics == null)
                return;

            for (int i = 0; i < m_subparts.Count; i++)
            {
                transMat[i] = Matrix.Identity;
                rotMat[i] = Matrix.Identity;
            }

            for (int i = 0; i < m_openingSequence.Count; i++)
            {
                MyObjectBuilder_AdvancedDoorDefinition.Opening.MoveType moveType = m_openingSequence[i].Move;
                float opening = m_currentOpening[i];

                // get id of the subpart this opening sequenz should be applied to
                int id = m_subpartIDs[i];

                if (m_subparts.Count == 0 || id < 0)
                    break;

                if (m_subparts[id] != null && m_subparts[id].Physics != null)
                {
                    if (moveType == MyObjectBuilder_AdvancedDoorDefinition.Opening.MoveType.Slide)
                    {
                        transMat[id] *= Matrix.CreateTranslation(m_openingSequence[i].SlideDirection * new Vector3(opening));
                    }
                    else if (moveType == MyObjectBuilder_AdvancedDoorDefinition.Opening.MoveType.Rotate)
                    {
                        float invert = m_openingSequence[i].InvertRotation ? -1f : 1f;

                        float rotX = 0f;
                        float rotY = 0f;
                        float rotZ = 0f;

                        if (m_openingSequence[i].RotationAxis == MyObjectBuilder_AdvancedDoorDefinition.Opening.Rotation.X)
                        {
                            rotX = MathHelper.ToRadians(opening * invert);
                        }
                        else if (m_openingSequence[i].RotationAxis == MyObjectBuilder_AdvancedDoorDefinition.Opening.Rotation.Y)
                        {
                            rotY = MathHelper.ToRadians(opening * invert);
                        }
                        else if (m_openingSequence[i].RotationAxis == MyObjectBuilder_AdvancedDoorDefinition.Opening.Rotation.Z)
                        {
                            rotZ = MathHelper.ToRadians(opening * invert);
                        }

                        Vector3 hingePos = (m_openingSequence[i].PivotPosition == null) ? m_hingePosition[id] : (Vector3)m_openingSequence[i].PivotPosition;

                        rotMat[id] *=
                                (Matrix.CreateTranslation(-(hingePos)) *
                                (Matrix.CreateRotationX(rotX) *
                                Matrix.CreateRotationY(rotY) *
                                Matrix.CreateRotationZ(rotZ)) *
                                Matrix.CreateTranslation(hingePos));
                    }

                    if (m_subparts[id].Physics != null)
                    {
                        if (m_subparts[id].Physics.LinearVelocity != this.CubeGrid.Physics.LinearVelocity)
                        {
                            m_subparts[id].Physics.LinearVelocity = this.CubeGrid.Physics.LinearVelocity;
                        }

                        if (m_subparts[id].Physics.AngularVelocity != this.CubeGrid.Physics.AngularVelocity)
                        {
                            m_subparts[id].Physics.AngularVelocity = this.CubeGrid.Physics.AngularVelocity;
                        }
                    }
                }
            }

            // combine matrices and apply to subparts
            for (int i = 0; i < m_subparts.Count; i++)
            {
                m_subparts[i].PositionComp.LocalMatrix = rotMat[i] * transMat[i];
            }
        }

        public override void OnCubeGridChanged(MyCubeGrid oldGrid)
        {
            oldGrid.OnHavokSystemIDChanged -= CubeGrid_HavokSystemIDChanged;
            CubeGrid.OnHavokSystemIDChanged += CubeGrid_HavokSystemIDChanged;
            if (CubeGrid.Physics != null)
                UpdateHavokCollisionSystemID(CubeGrid.Physics.HavokCollisionSystemID);
            base.OnCubeGridChanged(oldGrid);
        }

        void CubeGrid_HavokSystemIDChanged(int id)
        {
            UpdateHavokCollisionSystemID(id);
        }

        internal void UpdateHavokCollisionSystemID(int HavokCollisionSystemID)
        {
            foreach (var subpart in m_subparts)
            {
                if (subpart != null && subpart.Physics != null)
                {
                    if ((subpart.ModelCollision.HavokCollisionShapes != null) && (subpart.ModelCollision.HavokCollisionShapes.Length > 0))
                    {
                        var info = HkGroupFilter.CalcFilterInfo(MyPhysics.KinematicDoubledCollisionLayer, HavokCollisionSystemID, 1, 1);
                        subpart.Physics.RigidBody.SetCollisionFilterInfo(info);

                        info = HkGroupFilter.CalcFilterInfo(MyPhysics.DynamicDoubledCollisionLayer, HavokCollisionSystemID, 1, 1);
                        subpart.Physics.RigidBody2.SetCollisionFilterInfo(info);
                    }
                }
            }
        }

        protected override void Closing()
        {
            for (int i = 0; i < m_emitter.Count; i++)
            {
                if (m_emitter[i] != null)
                    m_emitter[i].StopSound(true);
            }

            CubeGrid.OnHavokSystemIDChanged -= CubeGrid_HavokSystemIDChanged;

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
    }
}

