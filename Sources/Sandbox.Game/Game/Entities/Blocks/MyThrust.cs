#region Using

using System;
using System.Diagnostics;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Lights;
using Sandbox.Game.World;

using VRage.Utils;
using VRageMath;
using Sandbox.Engine.Utils;
using System.Collections.Generic;
using Sandbox.Game.Gui;
using VRageRender;
using Sandbox.Game.Entities.Character;
using Sandbox.Engine.Physics;
using Havok;
using Sandbox.Game.Multiplayer;
using Sandbox.Graphics;
using System.Linq;
using Sandbox.Game.Components;
#endregion

namespace Sandbox.Game.Entities
{
    using Sandbox.Game.Weapons;
    using Sandbox.Common;
    using VRage;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.ModAPI.Interfaces;
    using Sandbox.Game.Localization;
    using Sandbox.ModAPI;
    using VRage.Audio;
    using VRage.ModAPI;

    [MyCubeBlockType(typeof(MyObjectBuilder_Thrust))]
    public class MyThrust : MyFunctionalBlock, IMyThrust
    {
        public struct FlameInfo
        {
            public float Radius;
            public Vector3 Direction;
            public Vector3 Position;
        }

        #region Fields

        private MyThrustDefinition m_thrustDefinition;
        private MyGridThrustSystem m_thrustSystem;

        MyLight m_light;

        Vector4 m_thrustColor;

        private new MySyncThruster SyncObject;

        // values for consistency between frames when game is paused
        public float ThrustRadiusRand;
        public float ThrustLengthRand;
        public float ThrustThicknessRand;
        private float m_glareSize;
        private float m_maxBillboardDistanceSquared;
        private float m_maxLightDistanceSquared;

        // This should be stored per-model, not per thruster
        private List<FlameInfo> m_flames = new List<FlameInfo>();

        #endregion

        #region Properties
        /// <summary>
        /// Thrust force direction is opposite to thrust forward vector orientation
        /// </summary>
        public Vector3 ThrustForce
        {
            get
            {
                return -ThrustForwardVector * (m_thrustDefinition.ForceMagnitude * m_thrustMultiplier);
            }
        }

        public Vector3I ThrustForwardVector
        {
            get
            {
                return Base6Directions.GetIntVector(Orientation.Forward);
            }
        }

        private List<MyPhysics.HitInfo> m_gridRayCastLst;
        private List<HkRigidBody> m_flameCollisionsList;
        private List<IMyEntity> m_damagedEntities;

        public bool IsPowered
        {
            get { return CubeGrid.GridSystems.ThrustSystem.IsPowered; }
        }

        public float MaxPowerConsumption
        {
            get { return m_thrustDefinition.MaxPowerConsumption * m_powerConsumptionMultiplier; }
        }

        public float MinPowerConsumption
        {
            get { return m_thrustDefinition.MinPowerConsumption * m_powerConsumptionMultiplier; }
        }

        public float CurrentStrength { get; set; }

        /// <summary>
        /// Overridden thrust in Newtons
        /// </summary>
        private float m_thrustOverride;
        public float ThrustOverride 
        {
            get
            {
                return m_thrustOverride * m_thrustMultiplier;
            }
            private set
            {
                m_thrustOverride = value;
            }
        }

        protected override bool CheckIsWorking()
        {
            return IsPowered && base.CheckIsWorking();
        }
        public MyLight Light { get { return m_light; } }

        public void UpdateThrustFlame()
        {
            ThrustRadiusRand = MyUtils.GetRandomFloat(0.9f, 1.1f);
            ThrustLengthRand = CurrentStrength * 10 * MyUtils.GetRandomFloat(0.6f, 1.0f) * m_thrustDefinition.FlameLengthScale;
            ThrustThicknessRand = MyUtils.GetRandomFloat(ThrustRadiusRand * 0.90f, ThrustRadiusRand);
        }

        public void UpdateThrustColor()
        {
            m_thrustColor = Vector4.Lerp(m_thrustDefinition.FlameIdleColor, m_thrustDefinition.FlameFullColor, CurrentStrength / MyConstants.MAX_THRUST);
            Light.Color = m_thrustColor;
        }
        public Vector4 ThrustColor { get { return m_thrustColor; } }
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

        public string FlameLengthMaterial { get { return m_thrustDefinition.FlameLengthMaterial; } }
        public string FlamePointMaterial { get { return m_thrustDefinition.FlamePointMaterial; } }
        public float FlameDamageLengthScale { get { return m_thrustDefinition.FlameDamageLengthScale; } }

        #endregion

        static MyThrust()
        {          
            float threshold = 0.01f;
            var thrustOverride = new MyTerminalControlSlider<MyThrust>("Override", MySpaceTexts.BlockPropertyTitle_ThrustOverride, MySpaceTexts.BlockPropertyDescription_ThrustOverride);
            thrustOverride.Getter = (x) => x.ThrustOverride;
            thrustOverride.Setter = (x, v) => 
            {
                float val = v;
                float limit = x.m_thrustDefinition.ForceMagnitude * threshold;

                x.SetThrustOverride(val <= limit ? 0 : v); 
                x.SyncObject.SendChangeThrustOverrideRequest(x.ThrustOverride); 
            };
            thrustOverride.DefaultValue = 0;
            thrustOverride.SetLogLimits((x) => x.m_thrustDefinition.ForceMagnitude * 0.01f, (x) => x.m_thrustDefinition.ForceMagnitude);
            thrustOverride.EnableActions();
            thrustOverride.Writer = (x, result) =>
                {
                    if (x.ThrustOverride <= x.m_thrustDefinition.ForceMagnitude * 0.01f)
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
            m_flameCollisionsList = new List<HkRigidBody>();
            m_damagedEntities = new List<IMyEntity>();
            m_gridRayCastLst = new List<MyPhysics.HitInfo>();
            Render = new MyRenderComponentThrust();
            AddDebugRenderComponent(new MyDebugRenderComponentThrust(this));
        }

        public void SetThrustOverride(float force)
        {
            ThrustOverride = force;
            if (m_thrustSystem != null)
                m_thrustSystem.MarkDirty();
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

            m_thrustDefinition = (MyThrustDefinition)BlockDefinition;

            var builder = (MyObjectBuilder_Thrust)objectBuilder;

            m_thrustColor = m_thrustDefinition.FlameIdleColor;

            ThrustOverride = builder.ThrustOverride;

            LoadDummies();

            m_light = MyLights.AddLight();
            m_light.ReflectorDirection = WorldMatrix.Forward;
            m_light.ReflectorUp = WorldMatrix.Up;
            m_light.ReflectorRange = 1;
            m_light.Color = m_thrustColor;
            m_light.GlareMaterial = m_thrustDefinition.FlameGlareMaterial;
            m_light.GlareQuerySize = m_thrustDefinition.FlameGlareQuerySize;

            m_glareSize = m_thrustDefinition.FlameGlareSize;
            m_maxBillboardDistanceSquared = m_thrustDefinition.FlameVisibilityDistance * m_thrustDefinition.FlameVisibilityDistance;
            m_maxLightDistanceSquared = m_maxBillboardDistanceSquared / 100;

            m_light.Start(MyLight.LightTypeEnum.PointLight, 1);
            SyncObject = new MySyncThruster(this);

            UpdateDetailedInfo();
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            m_thrustSystem = CubeGrid.GridSystems.ThrustSystem;
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            LoadDummies();
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
                        shape = new HkCapsuleShape(Vector3.Zero, l.To - l.From, flameInfo.Radius * m_thrustDefinition.FlameDamageLengthScale);
                    else
                        shape = new HkSphereShape(flameInfo.Radius * m_thrustDefinition.FlameDamageLengthScale);
                    MyPhysics.GetPenetrationsShape(shape, ref l.From, ref Quaternion.Identity, m_flameCollisionsList, 0);
                    shape.RemoveReference();

                    foreach (var obj in m_flameCollisionsList)
                    {
                        var entity = obj.GetEntity();
                        if (entity == null)
                            continue;
                        if (entity.Equals(this))
                            continue;
                        if(!(entity is MyCharacter))
                            entity = entity.GetTopMostParent();
                        if (m_damagedEntities.Contains(entity))
                            continue;
                        else
                            m_damagedEntities.Add(entity);

                        if (entity is IMyDestroyableObject)
                            (entity as IMyDestroyableObject).DoDamage(flameInfo.Radius * m_thrustDefinition.FlameDamage * 10, MyDamageType.Environment, true);
                        else if (entity is MyCubeGrid)
                        {
                            var grid = entity as MyCubeGrid;
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
            HkSphereShape sph = new HkSphereShape(flameInfo.Radius * m_thrustDefinition.FlameDamageLengthScale);
            var transform = MatrixD.CreateWorld(l.From, Vector3.Forward, Vector3.Up);
            var hit = MyPhysics.CastShapeReturnPoint(l.To, sph, ref transform, (int)MyPhysics.DefaultCollisionLayer, 0.05f);

            sph.Base.RemoveReference();

            if (hit.HasValue)
            {
                //MyRenderProxy.DebugDrawSphere(hit.Value, 0.1f, Color.Green.ToVector3(), 1, true);
                MyPhysics.CastRay(hit.Value - l.Direction * 0.1f, hit.Value + l.Direction * 0.1f, m_gridRayCastLst, MyPhysics.ObjectDetectionCollisionLayer);
                if ((m_gridRayCastLst.Count == 0 || m_gridRayCastLst[0].HkHitInfo.Body.GetEntity() != grid) && grid == CubeGrid)
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
                    var gridPos = Vector3D.Transform(hit.Value,invWorld);
                    var gridDir = Vector3D.TransformNormal(l.Direction, invWorld);
                    if (block != null)
                        if (block.FatBlock != this && (CubeGrid.GridSizeEnum == MyCubeSize.Large || block.BlockDefinition.DeformationRatio > 0.25))
                            block.DoDamage(30 * m_thrustDefinition.FlameDamage, MyDamageType.Environment);
                    var areaPlanar = 0.5f * flameInfo.Radius * CubeGrid.GridSize;
                    var areaVertical = 0.5f * CubeGrid.GridSize;
                    grid.Physics.ApplyDeformation(m_thrustDefinition.FlameDamage, areaPlanar, areaVertical, gridPos, gridDir, MyDamageType.Environment, CubeGrid.GridSizeEnum == MyCubeSize.Small ? 0.1f : 0);
                }
            }
        }

        public LineD GetDamageCapsuleLine(FlameInfo info)
        {
            var world = Matrix.CreateFromDir(Vector3.TransformNormal(info.Direction, WorldMatrix));

            var halfLenght = ThrustLengthRand * info.Radius / 2;
            halfLenght *= m_thrustDefinition.FlameDamageLengthScale;
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
                // TODO: use primary sound
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
            get
            {
                return m_thrustMultiplier;
            }
            set
            {
                m_thrustMultiplier = value;

                if (m_thrustMultiplier < 0.01f)
                {
                    m_thrustMultiplier = 0.01f;
                }

                if (m_thrustSystem != null)
                {
                    m_thrustSystem.MarkDirty();
                }
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

                if (m_thrustSystem != null)
                {
                    m_thrustSystem.MarkDirty();
                }

                UpdateDetailedInfo();
            }
        }
    }
}

