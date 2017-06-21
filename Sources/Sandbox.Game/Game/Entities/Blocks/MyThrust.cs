#region Using

using System;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Lights;
using Sandbox.Game.World;
using Sandbox.Common;

using VRage.Utils;
using VRageMath;
using Sandbox.Engine.Utils;
using System.Collections.Generic;
using Sandbox.Game.Gui;
using Sandbox.Game.Entities.Character;
using Sandbox.Engine.Physics;
using Havok;
using Sandbox.Game.Multiplayer;
using System.Linq;
using Sandbox.Game.Components;
using VRage;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using VRage.Audio;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Sync;

#endregion

namespace Sandbox.Game.Entities
{

    [MyCubeBlockType(typeof(MyObjectBuilder_Thrust))]
    public class MyThrust : MyFunctionalBlock, IMyThrust, IMyConveyorEndpointBlock
    {
        public struct FlameInfo
        {
            public float Radius;
            public Vector3 Direction;
            public Vector3 Position;
        }

        public new MyThrustDefinition BlockDefinition { get; private set; }
        public MyFuelConverterInfo FuelConverterDefinition { get; private set; }

        #region Fields

        private MyEntityThrustComponent m_thrustComponent;

        MyLight m_light;

        // values for consistency between frames when game is paused
        public float ThrustRadiusRand;
        public float ThrustLengthRand;
        public float ThrustThicknessRand;
        private float m_glareSize;
        private float m_maxBillboardDistanceSquared;
        private float m_maxLightDistanceSquared;

        // values for propeller engines
        private bool m_propellerActive = false;
        private MyEntity m_propellerEntity;
        private float m_propellerSpeed = 0f;// range 0-1
        private float m_propellerIdleRatio = 0f;
        private bool m_propellerCalculate = true;
        private float m_propellerMaxDistance = 0f;
        private float m_propellerAcceleration = 0f;
        private float m_propellerDeceleration = 0f;

        // This should be stored per-model, not per thruster
        private readonly List<FlameInfo> m_flames = new List<FlameInfo>();

        #endregion

        #region Properties
        public MyGasProperties FuelDefinition { get; private set; }

        /// <summary>
        /// Thrust force direction is opposite to thrust forward vector orientation
        /// </summary>
        public Vector3 ThrustForce { get { return -ThrustForwardVector * (BlockDefinition.ForceMagnitude * m_thrustMultiplier); } }

        public Vector3I ThrustForwardVector { get { return Base6Directions.GetIntVector(Orientation.Forward); } }
        public Vector4 ThrustColor { get; private set; }

        private readonly List<MyPhysics.HitInfo> m_gridRayCastLst;
        private readonly List<HkBodyCollision> m_flameCollisionsList;
        private readonly List<IMyEntity> m_damagedEntities;

        public bool IsPowered { get { return m_thrustComponent.IsThrustPoweredByType(this, ref FuelDefinition.Id); } }

        public float MaxPowerConsumption { get { return BlockDefinition.MaxPowerConsumption * m_powerConsumptionMultiplier; } }
        public float MinPowerConsumption { get { return BlockDefinition.MinPowerConsumption * m_powerConsumptionMultiplier; } }

        public float CurrentStrength { get; set; }

        /// <summary>
        /// Overridden thrust in Newtons
        /// </summary>
        private readonly Sync<float> m_thrustOverride;

        public float ThrustOverride
        {
            get { return m_thrustOverride * m_thrustMultiplier * BlockDefinition.ForceMagnitude * 0.01f; }
        }

        public event Action<float> ThrustOverrideChanged;

        protected override bool CheckIsWorking()
        {
            return IsPowered && base.CheckIsWorking();
        }
        public MyLight Light { get { return m_light; } }

        public void UpdateThrustFlame()
        {
            ThrustRadiusRand = MyUtils.GetRandomFloat(0.9f, 1.1f);
            ThrustLengthRand = CurrentStrength * 10 * MyUtils.GetRandomFloat(0.6f, 1.0f) * BlockDefinition.FlameLengthScale;
            ThrustThicknessRand = MyUtils.GetRandomFloat(ThrustRadiusRand * 0.90f, ThrustRadiusRand);
        }

        public void UpdateThrustColor()
        {
            ThrustColor = Vector4.Lerp(BlockDefinition.FlameIdleColor, BlockDefinition.FlameFullColor, CurrentStrength / MyConstants.MAX_THRUST);
            Light.Color = ThrustColor;
        }

        public bool CanDraw()
        {
            return IsWorking && Vector3.DistanceSquared(MySector.MainCamera.Position, PositionComp.GetPosition()) < m_maxBillboardDistanceSquared;
        }

        public static float LIGHT_INTENSITY_BASE = 4.5f;
        public static float LIGHT_INTENSITY_LENGTH = 1.7f;
        public static float LIGHT_RANGE_RADIUS = 2.0f;
        public static float LIGHT_RANGE_LENGTH = 0.1f;
        public static float GLARE_INTENSITY_BASE = 0.24f;
        public static float GLARE_INTENSITY_LENGTH = 0.2f;
        public static float GLARE_SIZE_RADIUS = 0.8f;
        public static float GLARE_SIZE_LENGTH = 0.05f;

        public void UpdateLight()
        {
            bool shouldLit = (float)Vector3D.DistanceSquared(MySector.MainCamera.Position, PositionComp.GetPosition()) < m_maxLightDistanceSquared; ;

            if (ThrustRadiusRand > 0 && shouldLit && m_flames.Count > 0)
            {
                var f = m_flames[0];
                var position = Vector3D.Transform(f.Position, PositionComp.WorldMatrix);

                float radius = ThrustRadiusRand * f.Radius * CubeGrid.GridScale;
                float length = ThrustLengthRand * f.Radius * CubeGrid.GridScale;

                Light.LightOn = true;
                Light.Intensity = LIGHT_INTENSITY_BASE + length * LIGHT_INTENSITY_LENGTH;

                Light.Range = radius * LIGHT_RANGE_RADIUS + length * LIGHT_RANGE_LENGTH;

                Light.Position = Vector3D.Transform(position, MatrixD.Invert(CubeGrid.PositionComp.WorldMatrix));
                Light.ParentID = CubeGrid.Render.GetRenderObjectID();

                Light.GlareOn = true;

                Light.GlareIntensity = GLARE_INTENSITY_BASE + length * GLARE_INTENSITY_LENGTH;

                Light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;
                Light.GlareSize = (radius * GLARE_SIZE_RADIUS + length * GLARE_SIZE_LENGTH) * m_glareSize * CubeGrid.GridScale;

                Light.UpdateLight();
            }
            else
            {
                if (Light.GlareOn || Light.LightOn)
                {
                    Light.GlareOn = false;
                    Light.LightOn = false;
                    Light.UpdateLight();
                }
            }
        }
        public List<FlameInfo> Flames { get { return m_flames; } }

        public string FlameLengthMaterial { get { return BlockDefinition.FlameLengthMaterial; } }
        public string FlamePointMaterial { get { return BlockDefinition.FlamePointMaterial; } }
        public float FlameDamageLengthScale { get { return BlockDefinition.FlameDamageLengthScale; } }

        #endregion

        public MyThrust()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_thrustOverride = SyncType.CreateAndAddProp<float>();
#endif // XB1
            CreateTerminalControls();

            Render.NeedsDrawFromParent = true;
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_flameCollisionsList = new List<HkBodyCollision>();
            m_damagedEntities = new List<IMyEntity>();
            m_gridRayCastLst = new List<MyPhysics.HitInfo>();
            Render = new MyRenderComponentThrust();
            AddDebugRenderComponent(new MyDebugRenderComponentThrust(this));
            m_thrustOverride.ValueChanged += (x) => ThrustOverrideValueChanged();
        }


        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyThrust>())
                return;
            base.CreateTerminalControls();
            float threshold = 1f;
            var thrustOverride = new MyTerminalControlSlider<MyThrust>("Override", MySpaceTexts.BlockPropertyTitle_ThrustOverride, MySpaceTexts.BlockPropertyDescription_ThrustOverride);
            thrustOverride.Getter = (x) => x.m_thrustOverride;
            thrustOverride.Setter = (x, v) =>
            {
                x.m_thrustOverride.Value = (v <= threshold ? 0 : v);
                x.RaisePropertiesChanged();
            };

            thrustOverride.DefaultValue = 0;
            thrustOverride.SetLimits((x) => 0f, (x) => 100f);
            thrustOverride.EnableActions();
            thrustOverride.Writer = (x, result) =>
            {
                if (x.ThrustOverride < 1f)
                    result.Append(MyTexts.Get(MyCommonTexts.Disabled));
                else
                    MyValueFormatter.AppendForceInBestUnit(x.ThrustOverride * x.m_thrustComponent.GetLastThrustMultiplier(x), result);
            };
            MyTerminalControlFactory.AddControl(thrustOverride);
        }

        private void ThrustOverrideValueChanged()
        {
            if (ThrustOverrideChanged != null)
                ThrustOverrideChanged(ThrustOverride);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_Thrust)base.GetObjectBuilderCubeBlock(copy);
            builder.ThrustOverride = ThrustOverride;
            return builder;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            MyEntityThrustComponent entityThrustComponent;
            if (!cubeGrid.Components.TryGet(out entityThrustComponent))
            {
                entityThrustComponent = new MyThrusterBlockThrustComponent();

                entityThrustComponent.Init();
                cubeGrid.Components.Add<MyEntityThrustComponent>(entityThrustComponent);
            }

            m_thrustComponent = entityThrustComponent;

            BlockDefinition = (MyThrustDefinition)base.BlockDefinition;

            MyDefinitionId fuelId = new MyDefinitionId();
            if (!BlockDefinition.FuelConverter.FuelId.IsNull())
                fuelId = BlockDefinition.FuelConverter.FuelId;

            MyGasProperties fuelDef = null;
            if (MyFakes.ENABLE_HYDROGEN_FUEL)
                MyDefinitionManager.Static.TryGetDefinition(fuelId, out fuelDef);

            FuelDefinition = fuelDef ?? new MyGasProperties // Use electricity by default
            {
                Id = MyResourceDistributorComponent.ElectricityId,
                EnergyDensity = 1f,
            };

            base.Init(objectBuilder, cubeGrid);



            var builder = (MyObjectBuilder_Thrust)objectBuilder;

            ThrustColor = BlockDefinition.FlameIdleColor;

            m_thrustOverride.Value = (builder.ThrustOverride * 100f) / BlockDefinition.ForceMagnitude;

            LoadDummies();
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            m_light = MyLights.AddLight();
            m_light.ReflectorDirection = WorldMatrix.Forward;
            m_light.ReflectorUp = WorldMatrix.Up;
            m_light.ReflectorRange = CubeGrid.GridScale;
            m_light.Color = ThrustColor;
            m_light.GlareMaterial = BlockDefinition.FlameGlareMaterial;
            m_light.GlareQuerySize = BlockDefinition.FlameGlareQuerySize * CubeGrid.GridScale;

            m_glareSize = BlockDefinition.FlameGlareSize * CubeGrid.GridScale;
            m_maxBillboardDistanceSquared = BlockDefinition.FlameVisibilityDistance * BlockDefinition.FlameVisibilityDistance;
            m_maxLightDistanceSquared = m_maxBillboardDistanceSquared ;

            m_light.Start(MyLight.LightTypeEnum.PointLight, 1);

            UpdateDetailedInfo();

            FuelConverterDefinition = !MyFakes.ENABLE_HYDROGEN_FUEL ? new MyFuelConverterInfo { Efficiency = 1.0f } : BlockDefinition.FuelConverter;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        private bool LoadPropeller()
        {
            if (BlockDefinition.PropellerUse && BlockDefinition.PropellerEntity != null)
            {
                MyEntitySubpart propeller;
                if (Subparts.TryGetValue(BlockDefinition.PropellerEntity, out propeller))
                {
                    m_propellerEntity = propeller;
                    m_propellerIdleRatio = BlockDefinition.PropellerIdleSpeed / BlockDefinition.PropellerFullSpeed;
                    m_propellerMaxDistance = BlockDefinition.PropellerMaxDistance * BlockDefinition.PropellerMaxDistance;
                    m_propellerAcceleration = (1f / BlockDefinition.PropellerAcceleration) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    m_propellerDeceleration = (1f / BlockDefinition.PropellerDeceleration) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
                    return true;
                }
            }
            return false;
        }

        private void PropellerUpdate()
        {
            float targetSpeed = 0f;
            if (IsWorking)
            {
                if (CurrentStrength > 0f)
                {
                    targetSpeed = 1f;
                }
                else
                {
                    targetSpeed = m_propellerIdleRatio;
                }
            }
            if (m_propellerSpeed > targetSpeed)
            {
                m_propellerSpeed = Math.Max(targetSpeed, m_propellerSpeed - m_propellerDeceleration);
            }
            else
            {
                m_propellerSpeed = Math.Min(targetSpeed, m_propellerSpeed + m_propellerAcceleration);
            }

            //normalizedRotationSpeed
            float rotateBy = m_propellerSpeed * BlockDefinition.PropellerFullSpeed * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MathHelper.TwoPi;
            Matrix worldMatrix = this.PositionComp.WorldMatrix;
            m_propellerEntity.PositionComp.LocalMatrix = Matrix.CreateRotationZ(rotateBy) * m_propellerEntity.PositionComp.LocalMatrix;
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            MyEntityThrustComponent entityThrustComponent;
            if (!CubeGrid.Components.TryGet(out entityThrustComponent))
            {
                entityThrustComponent = new MyThrusterBlockThrustComponent();

                entityThrustComponent.Init();
                CubeGrid.Components.Add<MyEntityThrustComponent>(entityThrustComponent);
            }
            m_thrustComponent = entityThrustComponent;
            m_thrustComponent.Register(this, ThrustForwardVector, OnRegisteredToThrustComponent);
        }

        private bool OnRegisteredToThrustComponent()
        {
            var resourceSink = m_thrustComponent.ResourceSink(this);
            resourceSink.IsPoweredChanged += Sink_IsPoweredChanged;
            resourceSink.Update();
            return true;
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            m_thrustComponent.ResourceSink(this).IsPoweredChanged -= Sink_IsPoweredChanged;
            m_thrustComponent.Unregister(this, ThrustForwardVector);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            LoadDummies();
        }

        public void Sink_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            if (CubeGrid.GridSystems.ResourceDistributor != null)
                CubeGrid.GridSystems.ResourceDistributor.ConveyorSystem_OnPoweredChanged(); // Hotfix TODO
        }

        private void LoadDummies()
        {
            m_flames.Clear();
            foreach (var d in this.Model.Dummies.OrderBy(s => s.Key))
            {
                if (d.Key.StartsWith("thruster_flame", StringComparison.InvariantCultureIgnoreCase))
                {
                    var f = new FlameInfo();
                    f.Direction = Vector3.Normalize(d.Value.Matrix.Forward);
                    f.Position = d.Value.Matrix.Translation;
                    f.Radius = Math.Max(d.Value.Matrix.Scale.X, d.Value.Matrix.Scale.Y) * 0.5f;
                    m_flames.Add(f);
                }
            }
            if (BlockDefinition != null)
                m_propellerActive = LoadPropeller();
        }

        protected override void Closing()
        {
            //	m_thrustComponent.Unregister(this, ThrustForwardVector);
            MyLights.RemoveLight(m_light);
            base.Closing();
        }

        public override void GetTerminalName(StringBuilder result)
        {
            var dirString = GetDirectionString();
            if (dirString == null)
            {
                base.GetTerminalName(result);
                return;
            }
            result.Append(DisplayNameText).Append(" (").Append(dirString).Append(") ");
        }

        public override void UpdateBeforeSimulation()
        {
            if (m_propellerActive && m_propellerCalculate)
                PropellerUpdate();

            base.UpdateBeforeSimulation();
        }

        public override void UpdateBeforeSimulation10()
        {
            if (!IsWorking)
            {
                ThrustRadiusRand = 0.0f;
                ThrustLengthRand = 0.0f;
                ThrustThicknessRand = 0.0f;
            }

            ThrustDamage();
            base.UpdateBeforeSimulation10();
        }

        private void ThrustDamage()
        {
            if (m_flames.Count > 0 && MySession.Static.ThrusterDamage && Sync.IsServer && IsWorking && CubeGrid.InScene && CubeGrid.Physics != null && CubeGrid.Physics.Enabled)
            {
                if (CurrentStrength == 0 && !MyFakes.INACTIVE_THRUSTER_DMG)
                    return;

                UpdateThrustFlame();

                foreach (var flameInfo in m_flames)
                {
                    var l = GetDamageCapsuleLine(flameInfo);
                    HkShape shape;
                    if (l.Length != 0)
                        shape = new HkCapsuleShape(Vector3.Zero, l.To - l.From, flameInfo.Radius * BlockDefinition.FlameDamageLengthScale);
                    else
                        shape = new HkSphereShape(flameInfo.Radius * BlockDefinition.FlameDamageLengthScale);
                    MyPhysics.GetPenetrationsShape(shape, ref l.From, ref Quaternion.Identity, m_flameCollisionsList, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                    shape.RemoveReference();

                    foreach (var obj in m_flameCollisionsList)
                    {
                        var ent = obj.GetCollisionEntity();
                        if (ent == null || ent.Equals(this))
                            continue;

                        if (!(ent is MyCharacter))
                            ent = ent.GetTopMostParent();
                        if (m_damagedEntities.Contains(ent))
                            continue;
                        else
                            m_damagedEntities.Add(ent);

                        if (ent is IMyDestroyableObject)
                            (ent as IMyDestroyableObject).DoDamage(flameInfo.Radius * BlockDefinition.FlameDamage * 10, MyDamageType.Environment, true, attackerId: EntityId);
                        else if (ent is MyCubeGrid)  
                        {
                            var grid = ent as MyCubeGrid;
                            if (grid.BlocksDestructionEnabled)
                            {
                                DamageGrid(flameInfo, l, grid);
                            }
                        }
                    }
                    m_damagedEntities.Clear();
                    m_flameCollisionsList.Clear();
                }
            }
        }

        private void DamageGrid(FlameInfo flameInfo, LineD l, MyCubeGrid grid)
        {
            HkSphereShape sph = new HkSphereShape(flameInfo.Radius * BlockDefinition.FlameDamageLengthScale);
            var transform = MatrixD.CreateWorld(l.From, Vector3.Forward, Vector3.Up);
            var hit = MyPhysics.CastShapeReturnPoint(l.To, sph, ref transform, (int)MyPhysics.CollisionLayers.DefaultCollisionLayer, 0.05f);

            sph.Base.RemoveReference();

            if (hit.HasValue)
            {
                //MyRenderProxy.DebugDrawSphere(hit.Value, 0.1f, Color.Green.ToVector3(), 1, true);
                MyPhysics.CastRay(hit.Value - l.Direction * 0.1f, hit.Value + l.Direction * 0.1f, m_gridRayCastLst, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
                if (m_gridRayCastLst.Count == 0 || m_gridRayCastLst[0].HkHitInfo.GetHitEntity() != grid)    //If you found something other than the targeted grid do nothing
                {
                    m_gridRayCastLst.Clear();
                    return;
                }

                Vector3D offsetHit = hit.Value + l.Direction * 0.1;
                m_gridRayCastLst.Clear();
                var block = grid.GetCubeBlock(grid.WorldToGridInteger(offsetHit));
                //if (block != this.SlimBlock)
                {
                    //MyRenderProxy.DebugDrawSphere(hit.Value, 0.1f, Color.Green.ToVector3(), 1, true);
                    var invWorld = grid.PositionComp.WorldMatrixNormalizedInv;
                    var gridPos = Vector3D.Transform(offsetHit, invWorld);
                    var gridDir = Vector3D.TransformNormal(l.Direction, invWorld);
                    if (block != null)
                    {
                        //We dont want to damage thruster itself
                        //We dont want smallship thruster to damage heavy armors because of landing
                        if (block.FatBlock != this && (CubeGrid.GridSizeEnum == MyCubeSize.Large || block.BlockDefinition.DeformationRatio > 0.25))
                        {
                            block.DoDamage(30 * BlockDefinition.FlameDamage, MyDamageType.Environment, attackerId: EntityId);
                        }
                    }

                    if (block == null || block.FatBlock != this)
                    {
                        var areaPlanar = 0.5f * flameInfo.Radius * CubeGrid.GridSize;
                        var areaVertical = 0.5f * CubeGrid.GridSize;

                        grid.Physics.ApplyDeformation(BlockDefinition.FlameDamage, areaPlanar, areaVertical, gridPos, gridDir, MyDamageType.Environment, CubeGrid.GridSizeEnum == MyCubeSize.Small ? 0.1f : 0, attackerId: EntityId);
                    }
                }
            }
        }

        public LineD GetDamageCapsuleLine(FlameInfo info)
        {
            var world = Matrix.CreateFromDir(Vector3.TransformNormal(info.Direction, WorldMatrix));

            var halfLenght = ThrustLengthRand * info.Radius / 2;
            halfLenght *= BlockDefinition.FlameDamageLengthScale;
            var position = Vector3D.Transform(info.Position, WorldMatrix);

            if (halfLenght > info.Radius)
                return new LineD(position - world.Forward * (info.Radius), position + world.Forward * (2 * halfLenght - info.Radius));
            else
            {
                var l = new LineD(position + world.Forward * halfLenght, position + world.Forward * halfLenght);
                l.Direction = world.Forward;
                return l;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            UpdateSoundState();
            if (m_propellerActive)
            {
                m_propellerCalculate = Vector3D.DistanceSquared(this.PositionComp.GetPosition(), MySector.MainCamera.Position) < m_propellerMaxDistance;

                if (m_propellerCalculate)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                else
                    NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            UpdateIsWorking();
        }

        private void UpdateSoundState()
        {
            if (m_soundEmitter == null || !IsWorking)
                return;
            if (CurrentStrength > 0.1f)
            {
                if (!m_soundEmitter.IsPlaying)
                    m_soundEmitter.PlaySound(BlockDefinition.PrimarySound, true);
            }
            else
                m_soundEmitter.StopSound(false);

            if ((m_soundEmitter.Sound != null) && (m_soundEmitter.Sound.IsPlaying))
            {
                float semitones = 8f * (CurrentStrength - 0.5f * MyConstants.MAX_THRUST) / MyConstants.MAX_THRUST;
                m_soundEmitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
            }
        }

        private void UpdateDetailedInfo()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.AppendFormat("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(MaxPowerConsumption, DetailedInfo);
            DetailedInfo.AppendFormat("\n");

            RaisePropertiesChanged();
        }

        private string GetDirectionString()
        {
            var cockpit = MySession.Static.ControlledEntity as MyCockpit;
            if (cockpit != null)
            {
                Quaternion cockpitOrientation;
                cockpit.Orientation.GetQuaternion(out cockpitOrientation);
                var thrustDir = Vector3I.Transform(ThrustForwardVector, Quaternion.Inverse(cockpitOrientation));
                if (thrustDir.X == 1)
                    return MyTexts.GetString(MyCommonTexts.Thrust_Left);
                else if (thrustDir.X == -1)
                    return MyTexts.GetString(MyCommonTexts.Thrust_Right);
                else if (thrustDir.Y == 1)
                    return MyTexts.GetString(MyCommonTexts.Thrust_Down);
                else if (thrustDir.Y == -1)
                    return MyTexts.GetString(MyCommonTexts.Thrust_Up);
                else if (thrustDir.Z == 1)
                    return MyTexts.GetString(MyCommonTexts.Thrust_Forward);
                else if (thrustDir.Z == -1)
                    return MyTexts.GetString(MyCommonTexts.Thrust_Back);
            }
            return null;
        }
        float Sandbox.ModAPI.Ingame.IMyThrust.ThrustOverride { get { return ThrustOverride; } }

        private float m_thrustMultiplier = 1f;
        float Sandbox.ModAPI.IMyThrust.ThrustMultiplier
        {
            get { return m_thrustMultiplier; }
            set
            {
                m_thrustMultiplier = value;

                if (m_thrustMultiplier < 0.01f)
                {
                    m_thrustMultiplier = 0.01f;
                }

                if (m_thrustComponent != null)
                    m_thrustComponent.MarkDirty();
            }
        }

        private float m_powerConsumptionMultiplier = 1f;
        float Sandbox.ModAPI.IMyThrust.PowerConsumptionMultiplier
        {
            get
            {
                return m_powerConsumptionMultiplier;
            }
            set
            {
                m_powerConsumptionMultiplier = value;
                if (m_powerConsumptionMultiplier < 0.01f)
                {
                    m_powerConsumptionMultiplier = 0.01f;
                }

                if (m_thrustComponent != null)
                    m_thrustComponent.MarkDirty();

                UpdateDetailedInfo();
            }
        }

        float Sandbox.ModAPI.Ingame.IMyThrust.MaxThrust
        {
            get
            {
                return BlockDefinition.ForceMagnitude * m_thrustMultiplier;
            }
        }

        float Sandbox.ModAPI.Ingame.IMyThrust.CurrentThrust
        {
            get
            {
                return CurrentStrength * BlockDefinition.ForceMagnitude * m_thrustMultiplier;
            }
        }
        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint { get { return m_conveyorEndpoint; } }
        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }

        #region IMyConveyorEndpointBlock implementation

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            return null;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}

