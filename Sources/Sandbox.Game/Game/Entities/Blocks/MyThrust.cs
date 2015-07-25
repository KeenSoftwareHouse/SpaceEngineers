#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Lights;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage;
using VRage.Audio;
using VRage.Import;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender.Lights;
using IMyThrust = Sandbox.ModAPI.IMyThrust;

#endregion

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Thrust))]
    public class MyThrust : MyFunctionalBlock, IMyThrust, IMyLightingBlock
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
        public MyLight Light { get; private set; }

        public void UpdateThrustFlame()
        {
            ThrustRadiusRand = MyUtils.GetRandomFloat(0.9f, 1.1f);
            ThrustLengthRand = CurrentStrength * 10 * MyUtils.GetRandomFloat(0.6f, 1.0f) * m_thrustDefinition.FlameLengthScale;
            ThrustThicknessRand = MyUtils.GetRandomFloat(ThrustRadiusRand * 0.90f, ThrustRadiusRand);
        }

        public void UpdateThrustColor(Vector4 colorVector4)
        {
            m_thrustDefinition.FlameIdleColor = colorVector4;
            m_thrustDefinition.FlameFullColor = colorVector4;

            ThrustColor = Vector4.Lerp(m_thrustDefinition.FlameIdleColor, m_thrustDefinition.FlameFullColor, CurrentStrength / MyConstants.MAX_THRUST);
            Light.Color = ThrustColor;
        }
        public Vector4 ThrustColor { get; private set; }

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

                Light.GlareType = MyGlareTypeEnum.Normal;
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
            MyTerminalControlSlider<MyThrust> thrustOverride = new MyTerminalControlSlider<MyThrust>("Override",
                MySpaceTexts.BlockPropertyTitle_ThrustOverride, MySpaceTexts.BlockPropertyDescription_ThrustOverride)
            {
                Getter = (x) => x.ThrustOverride,
                Setter = (x, v) =>
                {
                    float val = v;
                    float limit = x.m_thrustDefinition.ForceMagnitude*threshold;

                    x.SetThrustOverride(val <= limit ? 0 : v);
                    x.SyncObject.SendChangeThrustOverrideRequest(x.ThrustOverride);
                },
                DefaultValue = 0
            };
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

            MyTerminalControlColor<MyThrust> thrustColor = new MyTerminalControlColor<MyThrust>("Color",
               MySpaceTexts.BlockPropertyTitle_LightColor)
            {
                Getter = (x) => x.ThrustColor,
                Setter = (x, v) =>
                {
                    x.SetFlameColor(v);
                    x.SyncObject.SendChangeThrustColorRequest(x.ThrustColor);
                }
            };
            
            MyTerminalControlFactory.AddControl(thrustColor);
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

        public void SetFlameColor(Color color)
        {
            ThrustColor = new Vector4(color.ToVector3(), 0.75f);
            UpdateThrustColor(ThrustColor);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_Thrust builder = (MyObjectBuilder_Thrust)base.GetObjectBuilderCubeBlock(copy);
            builder.ThrustOverride = ThrustOverride;
            builder.FlameColorRed = ThrustColor.X;
            builder.FlameColorGreen = ThrustColor.Y;
            builder.FlameColorBlue = ThrustColor.Z;
            return builder;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_thrustDefinition = (MyThrustDefinition)BlockDefinition;

            MyObjectBuilder_Thrust builder = (MyObjectBuilder_Thrust)objectBuilder;

            ThrustColor = new Vector4(new Vector3(builder.FlameColorRed, builder.FlameColorGreen, builder.FlameColorBlue), builder.FlameColorAlpha);

            ThrustOverride = builder.ThrustOverride;

            LoadDummies();

            Light = MyLights.AddLight();
            Light.ReflectorDirection = WorldMatrix.Forward;
            Light.ReflectorUp = WorldMatrix.Up;
            Light.ReflectorRange = 1;
            Light.Color = ThrustColor;
            Light.GlareMaterial = m_thrustDefinition.FlameGlareMaterial;
            Light.GlareQuerySize = m_thrustDefinition.FlameGlareQuerySize;

            m_glareSize = m_thrustDefinition.FlameGlareSize;
            m_maxBillboardDistanceSquared = m_thrustDefinition.FlameVisibilityDistance * m_thrustDefinition.FlameVisibilityDistance;
            m_maxLightDistanceSquared = m_maxBillboardDistanceSquared / 100;

            Light.Start(MyLight.LightTypeEnum.PointLight, 1);
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
            foreach (FlameInfo f in from d in Model.Dummies.OrderBy(s => s.Key) where d.Key.StartsWith("thruster_flame", StringComparison.InvariantCultureIgnoreCase) select new FlameInfo
            {
                Direction = Vector3.Normalize(d.Value.Matrix.Forward),
                Position = d.Value.Matrix.Translation,
                Radius = Math.Max(d.Value.Matrix.Scale.X, d.Value.Matrix.Scale.Y)*0.5f
            })
            {
                m_flames.Add(f);
            }
        }

        protected override void Closing()
        {
            MyLights.RemoveLight(Light);
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

                    foreach (HkRigidBody obj in m_flameCollisionsList)
                    {
                        IMyEntity entity = obj.GetEntity();
                        if (entity == null)
                            continue;
                        if (entity.Equals(this))
                            continue;
                        if(!(entity is MyCharacter))
                            entity = entity.GetTopMostParent();
                        if (m_damagedEntities.Contains(entity))
                            continue;
                        m_damagedEntities.Add(entity);

                        if (entity is IMyDestroyableObject)
                            (entity as IMyDestroyableObject).DoDamage(flameInfo.Radius * m_thrustDefinition.FlameDamage * 10, MyDamageType.Environment, true, attackerId: EntityId);
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
            MatrixD transform = MatrixD.CreateWorld(l.From, Vector3.Forward, Vector3.Up);
            Vector3D? hit = MyPhysics.CastShapeReturnPoint(l.To, sph, ref transform, MyPhysics.DefaultCollisionLayer, 0.05f);

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
                MySlimBlock block = grid.GetCubeBlock(grid.WorldToGridInteger(hit.Value));
                //if (block != this.SlimBlock)
                {
                    //MyRenderProxy.DebugDrawSphere(hit.Value, 0.1f, Color.Green.ToVector3(), 1, true);
                    MatrixD invWorld = grid.PositionComp.GetWorldMatrixNormalizedInv();
                    Vector3D gridPos = Vector3D.Transform(hit.Value,invWorld);
                    Vector3D gridDir = Vector3D.TransformNormal(l.Direction, invWorld);
                    if (block != null)
                        if (block.FatBlock != this && (CubeGrid.GridSizeEnum == MyCubeSize.Large || block.BlockDefinition.DeformationRatio > 0.25))
                        {
                            block.DoDamage(30 * m_thrustDefinition.FlameDamage, MyDamageType.Environment, attackerId: EntityId);
                        }
                    float areaPlanar = 0.5f * flameInfo.Radius * CubeGrid.GridSize;
                    float areaVertical = 0.5f * CubeGrid.GridSize;

                    grid.Physics.ApplyDeformation(m_thrustDefinition.FlameDamage, areaPlanar, areaVertical, gridPos, gridDir, MyDamageType.Environment, CubeGrid.GridSizeEnum == MyCubeSize.Small ? 0.1f : 0, attackerId: EntityId);
                }
            }
        }

        public LineD GetDamageCapsuleLine(FlameInfo info)
        {
            Matrix world = Matrix.CreateFromDir(Vector3.TransformNormal(info.Direction, WorldMatrix));

            float halfLenght = ThrustLengthRand * info.Radius / 2;
            halfLenght *= m_thrustDefinition.FlameDamageLengthScale;
            Vector3D position = Vector3D.Transform(info.Position, WorldMatrix);

            if (halfLenght > info.Radius)
                return new LineD(position - world.Forward * (info.Radius), position + world.Forward * (2 * halfLenght - info.Radius));
            
            LineD l = new LineD(position + world.Forward*halfLenght, position + world.Forward*halfLenght)
            {
                Direction = world.Forward
            };
            return l;
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
                switch (thrustDir.X)
                {
                    case 1:
                        return MyTexts.GetString(MySpaceTexts.Thrust_Left);
                    case -1:
                        return MyTexts.GetString(MySpaceTexts.Thrust_Right);
                }
                switch (thrustDir.Y)
                {
                    case 1:
                        return MyTexts.GetString(MySpaceTexts.Thrust_Down);
                    case -1:
                        return MyTexts.GetString(MySpaceTexts.Thrust_Up);
                }
                switch (thrustDir.Z)
                {
                    case 1:
                        return MyTexts.GetString(MySpaceTexts.Thrust_Forward);
                    case -1:
                        return MyTexts.GetString(MySpaceTexts.Thrust_Back);
                }
            }
            return null;
        }
        float ModAPI.Ingame.IMyThrust.ThrustOverride { get { return ThrustOverride; } }

        private float m_thrustMultiplier = 1f;
        float IMyThrust.ThrustMultiplier 
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
        float IMyThrust.PowerConsumptionMultiplier
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

        public float Radius { get; private set; }
        public float Intensity { get; private set; }
        public float BlinkIntervalSeconds { get; private set; }
        public float BlinkLenght { get; private set; }
        public float BlinkOffset { get; private set; }
    }
}

