#region Using

using System;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Lights;
using Sandbox.Game.World;

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
using VRage.ModAPI;
using VRage.Components;
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

        private new MySyncThruster SyncObject;

        // values for consistency between frames when game is paused
        public float ThrustRadiusRand;
        public float ThrustLengthRand;
        public float ThrustThicknessRand;
        private float m_glareSize;
        private float m_maxBillboardDistanceSquared;
        private float m_maxLightDistanceSquared;

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

        public bool IsPowered { get { return m_thrustComponent.ResourceSink.IsPoweredByType(FuelDefinition.Id); } }

        public float MaxPowerConsumption { get { return BlockDefinition.MaxPowerConsumption * m_powerConsumptionMultiplier; } }
        public float MinPowerConsumption { get { return BlockDefinition.MinPowerConsumption * m_powerConsumptionMultiplier; } }

        public float CurrentStrength { get; set; }

        /// <summary>
        /// Overridden thrust in Newtons
        /// </summary>
        private readonly Sync<float> m_thrustOverride;

        public float ThrustOverride  { get { return m_thrustOverride * m_thrustMultiplier; } private set { m_thrustOverride.Value = value; } }

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

        public void UpdateLight()
        {
            bool shouldLit = (float)Vector3D.DistanceSquared(MySector.MainCamera.Position, PositionComp.GetPosition()) < m_maxLightDistanceSquared; ;

            if (ThrustRadiusRand > 0 && shouldLit && m_flames.Count > 0)
            {
                var f = m_flames[0];
                var position = Vector3D.Transform(f.Position, PositionComp.WorldMatrix);

                float radius = ThrustRadiusRand * f.Radius;
                float length = ThrustLengthRand * f.Radius;
                float thickness = ThrustThicknessRand * f.Radius;

                Light.LightOn = true;
                Light.Intensity = 1.3f + length;

                Light.Range = radius * 2 + length / 10;

                Light.Position = Vector3D.Transform(position, MatrixD.Invert(CubeGrid.PositionComp.WorldMatrix));
                Light.ParentID = CubeGrid.Render.GetRenderObjectID();

                Light.GlareOn = true;

                if (((MyCubeGrid)Parent).GridSizeEnum == MyCubeSize.Large)
                    Light.GlareIntensity = 0.5f + length * 2;
                else
                    Light.GlareIntensity = 0.5f + length * 2;

                Light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;
                Light.GlareSize = (radius * 0.8f + length * 0.05f) * m_glareSize;

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

        static MyThrust()
        {
            float threshold = 0.01f;
            var thrustOverride = new MyTerminalControlSlider<MyThrust>("Override", MySpaceTexts.BlockPropertyTitle_ThrustOverride, MySpaceTexts.BlockPropertyDescription_ThrustOverride);
            thrustOverride.Getter = (x) => x.ThrustOverride;
            thrustOverride.Setter = (x, v) =>
            {
                float val = v;
                float limit = x.BlockDefinition.ForceMagnitude * threshold;

                x.SetThrustOverride(val <= limit ? 0 : v);
                x.SyncObject.SendChangeThrustOverrideRequest(x.ThrustOverride);
            };
            thrustOverride.DefaultValue = 0;
            thrustOverride.SetLogLimits((x) => x.BlockDefinition.ForceMagnitude * 0.01f, (x) => x.BlockDefinition.ForceMagnitude);
            thrustOverride.EnableActions();
            thrustOverride.Writer = (x, result) =>
                {
                    if (x.ThrustOverride <= x.BlockDefinition.ForceMagnitude * 0.01f)
                        result.Append(MyTexts.Get(MySpaceTexts.Disabled));
                    else
                        MyValueFormatter.AppendForceInBestUnit(x.ThrustOverride, result);
                };
            MyTerminalControlFactory.AddControl(thrustOverride);
        }

        public MyThrust()
        {
            Render.NeedsDrawFromParent = true;
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_flameCollisionsList = new List<HkBodyCollision>();
            m_damagedEntities = new List<IMyEntity>();
            m_gridRayCastLst = new List<MyPhysics.HitInfo>();
            Render = new MyRenderComponentThrust();
            AddDebugRenderComponent(new MyDebugRenderComponentThrust(this));
        }

        public void SetThrustOverride(float force)
        {
            ThrustOverride = force;
            if (m_thrustComponent != null)
                m_thrustComponent.MarkDirty();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_Thrust)base.GetObjectBuilderCubeBlock(copy);
            builder.ThrustOverride = ThrustOverride;
            return builder;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            BlockDefinition = (MyThrustDefinition)base.BlockDefinition;

            var builder = (MyObjectBuilder_Thrust)objectBuilder;

            ThrustColor = BlockDefinition.FlameIdleColor;

            ThrustOverride = builder.ThrustOverride;

            LoadDummies();

            m_light = MyLights.AddLight();
            m_light.ReflectorDirection = WorldMatrix.Forward;
            m_light.ReflectorUp = WorldMatrix.Up;
            m_light.ReflectorRange = 1;
            m_light.Color = ThrustColor;
            m_light.GlareMaterial = BlockDefinition.FlameGlareMaterial;
            m_light.GlareQuerySize = BlockDefinition.FlameGlareQuerySize;

            m_glareSize = BlockDefinition.FlameGlareSize;
            m_maxBillboardDistanceSquared = BlockDefinition.FlameVisibilityDistance*BlockDefinition.FlameVisibilityDistance;
            m_maxLightDistanceSquared = m_maxBillboardDistanceSquared / 100;

            m_light.Start(MyLight.LightTypeEnum.PointLight, 1);
            SyncObject = new MySyncThruster(this);

            UpdateDetailedInfo();

            FuelConverterDefinition = !MyFakes.ENABLE_HYDROGEN_FUEL ? new MyFuelConverterInfo { Efficiency = 1.0f } : BlockDefinition.FuelConverter;

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

	        MyEntityThrustComponent entityThrustComponent;
	        if (!cubeGrid.Components.TryGet(out entityThrustComponent))
	        {
		        entityThrustComponent = new MyThrusterBlockThrustComponent();

                entityThrustComponent.Init();
		        cubeGrid.Components.Add<MyEntityThrustComponent>(entityThrustComponent);
	        }

            m_thrustComponent = entityThrustComponent;
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
			m_thrustComponent.Register(this, ThrustForwardVector);
            m_thrustComponent.ResourceSink.IsPoweredChanged += Sink_IsPoweredChanged;
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            m_thrustComponent.ResourceSink.IsPoweredChanged -= Sink_IsPoweredChanged;
            m_thrustComponent.Unregister(this, ThrustForwardVector);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            LoadDummies();
        }

        private void Sink_IsPoweredChanged()
        {
            UpdateIsWorking();
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
                    MyPhysics.GetPenetrationsShape(shape, ref l.From, ref Quaternion.Identity, m_flameCollisionsList, 0);
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
            var hit = MyPhysics.CastShapeReturnPoint(l.To, sph, ref transform, (int)MyPhysics.DefaultCollisionLayer, 0.05f);

            sph.Base.RemoveReference();

            if (hit.HasValue)
            {
                //MyRenderProxy.DebugDrawSphere(hit.Value, 0.1f, Color.Green.ToVector3(), 1, true);
                MyPhysics.CastRay(hit.Value - l.Direction * 0.1f, hit.Value + l.Direction * 0.1f, m_gridRayCastLst, MyPhysics.ObjectDetectionCollisionLayer);
                if ((m_gridRayCastLst.Count == 0 || m_gridRayCastLst[0].HkHitInfo.GetHitEntity() != grid) && grid == CubeGrid)
                {
                    m_gridRayCastLst.Clear();
                    return;
                }
                m_gridRayCastLst.Clear();
                var block = grid.GetCubeBlock(grid.WorldToGridInteger(hit.Value));
                //if (block != this.SlimBlock)
                {
                    //MyRenderProxy.DebugDrawSphere(hit.Value, 0.1f, Color.Green.ToVector3(), 1, true);
                    var invWorld = grid.PositionComp.GetWorldMatrixNormalizedInv();
                    var gridPos = Vector3D.Transform(hit.Value, invWorld);
                    var gridDir = Vector3D.TransformNormal(l.Direction, invWorld);
                    if (block != null)
                        if (block.FatBlock != this && (CubeGrid.GridSizeEnum == MyCubeSize.Large || block.BlockDefinition.DeformationRatio > 0.25))
                        {
                            block.DoDamage(30 * BlockDefinition.FlameDamage, MyDamageType.Environment, attackerId: EntityId);
                        }
                    var areaPlanar = 0.5f * flameInfo.Radius * CubeGrid.GridSize;
                    var areaVertical = 0.5f * CubeGrid.GridSize;

                    grid.Physics.ApplyDeformation(BlockDefinition.FlameDamage, areaPlanar, areaVertical, gridPos, gridDir, MyDamageType.Environment, CubeGrid.GridSizeEnum == MyCubeSize.Small ? 0.1f : 0, attackerId: EntityId);
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
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            m_soundEmitter.Update();
        }

        private void UpdateSoundState()
        {
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
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.AppendFormat("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(MaxPowerConsumption, DetailedInfo);
            DetailedInfo.AppendFormat("\n");

            RaisePropertiesChanged();
        }

        private string GetDirectionString()
        {
            var cockpit = MySession.ControlledEntity as MyCockpit;
            if (cockpit != null)
            {
                Quaternion cockpitOrientation;
                cockpit.Orientation.GetQuaternion(out cockpitOrientation);
                var thrustDir = Vector3I.Transform(ThrustForwardVector, Quaternion.Inverse(cockpitOrientation));
                if (thrustDir.X == 1)
                    return MyTexts.GetString(MySpaceTexts.Thrust_Left);
                else if (thrustDir.X == -1)
                    return MyTexts.GetString(MySpaceTexts.Thrust_Right);
                else if (thrustDir.Y == 1)
                    return MyTexts.GetString(MySpaceTexts.Thrust_Down);
                else if (thrustDir.Y == -1)
                    return MyTexts.GetString(MySpaceTexts.Thrust_Up);
                else if (thrustDir.Z == 1)
                    return MyTexts.GetString(MySpaceTexts.Thrust_Forward);
                else if (thrustDir.Z == -1)
                    return MyTexts.GetString(MySpaceTexts.Thrust_Back);
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

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint { get { return m_conveyorEndpoint; } }
        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }
    }
}

