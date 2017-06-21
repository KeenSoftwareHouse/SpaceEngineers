#region Using

using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics;
using SharpDX;
using System;
using System.Diagnostics;

using VRageMath;
using VRageRender;

#endregion

namespace Sandbox.AppCode.Game.TransparentGeometry
{
    using Matrix = VRageMath.Matrix;
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using Sandbox.Game.Entities;
    using System.Collections.Generic;
    using VRage.Utils;
    using Sandbox.Graphics;
    using Sandbox.Common;
    using Sandbox.Engine.Physics;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using VRage;
    using VRage.ModAPI;
    using VRage.Game.Components;
    using VRage.Game.Entity;
    using VRage.Game;

//  This class render "sun wind" coming from the sun. It works for sun in any direction (don't have to be parallel with one of the axis) - though I haven't tested it.
//  There are large and small billboards. Large are because I don't want draw a lot of small billboards on edge, where player won't see anything. 
//  Important are small. These are only close to camera. We check on them how far they can reach (at start of sun wind) and they will go only there.
//  Only voxels and large ships can stop small billboards. Other objects are ignored.
//
//  Sound of sun wind is not exactly point sound source, because it lies on a line. So we hear it coming, then we are in the sound and then we hear it comint out.

    

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, Priority = 1000)]
    class MySunWind : MySessionComponentBase
    {
        //  This isn't particle as we know it in MyParticles. It just stores information about one individual sun win billboard.
        class MySunWindBillboard
        {
            public Vector4 Color;
            public float Radius;
            public float InitialAngle;
            public float RotationSpeed;
            public Vector3 InitialAbsolutePosition;
        }

        //  This isn't particle as we know it in MyParticles. It just stores information about one individual sun win billboard.
        class MySunWindBillboardSmall : MySunWindBillboard
        {            
            public float MaxDistance;
            public int TailBillboardsCount;
            public float TailBillboardsDistance;

            public float[] RadiusScales;
        }

        struct MyEntityRayCastPair
        {
            public MyEntity Entity;
            public LineD _Ray;
            public Vector3D Position;
            public MyParticleEffect Particle;
        }

        //  True if sun wind is comming, otherwise false
        public static bool IsActive = false;
        public static bool IsVisible = true;

        // Actual position of sun wind
        public static Vector3D Position;

        //  Center of sun wind particles or wall of particles
        static Vector3D m_initialSunWindPosition;

        //  Direction which sun wind is coming from (from sun to camera)
        static Vector3D m_directionFromSunNormalized;

        //  These parameters are updated each UPDATE
        static PlaneD m_planeMiddle;
        static PlaneD m_planeFront;
        static PlaneD m_planeBack;
        static double m_distanceToSunWind;
        static Vector3D m_positionOnCameraLine;

        static int m_timeLastUpdate;

        //  Speed of sun wind, in meters per second
        static float m_speed;

        //  Vectors that define plane of sun wind
        static Vector3D m_rightVector;
        static Vector3D m_downVector;

        //  Strength of sun wind, in interval <0..1>. This will determine strength of sun color and other values.
        static float m_strength;

        //  When checking if how far small billboard can reach, we will check only objects of these type
        public static Type[] DoNotIgnoreTheseTypes = new Type[] { typeof(MyVoxelMap) };// typeof(MyPrefabBase), typeof(MyPrefab), typeof(MyPrefabLargeShip), typeof(MyStaticAsteroid) };

        static MySunWindBillboard[][] m_largeBillboards;
        
        static MySunWindBillboardSmall[][] m_smallBillboards;
        static bool m_smallBillboardsStarted;

        //static MySoundCuesEnum? m_burningCue;

        static List<IMyEntity> m_sunwindEntities = new List<IMyEntity>();

        static List<Havok.HkBodyCollision> m_intersectionLst;

        static List<MyEntityRayCastPair> m_rayCastQueue = new List<MyEntityRayCastPair>();
        // MaxDistance values for SmallBillboards are computed in more updates
        // Small Billboards are ready when m_computedMaxDistances == SMALL_BILLBOARDS_SIZE.X * SMALL_BILLBOARDS_SIZE.Y
        static int m_computedMaxDistances;

        static MySunWind()
        {
           //MyRender.RegisterRenderModule(MyRenderModuleEnum.SunWind, "Sun wind", Draw, MyRenderStage.PrepareForDraw);
        }

        public override void LoadData()
        {
            MyLog.Default.WriteLine("MySunWind.LoadData() - START");
            MyLog.Default.IncreaseIndent();
            //MyRender.GetRenderProfiler().StartProfilingBlock("MySunwind::LoadContent");
            m_intersectionLst = new List<Havok.HkBodyCollision>();

            //  Large billboards
            m_largeBillboards = new MySunWindBillboard[MySunWindConstants.LARGE_BILLBOARDS_SIZE.X][];
            for (int x = 0; x < MySunWindConstants.LARGE_BILLBOARDS_SIZE.X; x++)
            {
                m_largeBillboards[x] = new MySunWindBillboard[MySunWindConstants.LARGE_BILLBOARDS_SIZE.Y];
                for (int y = 0; y < MySunWindConstants.LARGE_BILLBOARDS_SIZE.Y; y++)
                {
                    m_largeBillboards[x][y] = new MySunWindBillboard();
                    MySunWindBillboard billboard = m_largeBillboards[x][y];

                    billboard.Radius = MyUtils.GetRandomFloat(MySunWindConstants.LARGE_BILLBOARD_RADIUS_MIN, MySunWindConstants.LARGE_BILLBOARD_RADIUS_MAX);
                    billboard.InitialAngle = MyUtils.GetRandomRadian();
                    billboard.RotationSpeed = MyUtils.GetRandomSign() * MyUtils.GetRandomFloat(MySunWindConstants.LARGE_BILLBOARD_ROTATION_SPEED_MIN, MySunWindConstants.LARGE_BILLBOARD_ROTATION_SPEED_MAX);

                    //billboard.Color = MySunWindConstants.BILLBOARD_COLOR;
                    //billboard.Color.X = MyMwcUtils.GetRandomFloat(0.5f, 3);
                    //billboard.Color.Y = MyMwcUtils.GetRandomFloat(0.5f, 2);
                    //billboard.Color.Z = MyMwcUtils.GetRandomFloat(0.5f, 2);
                    //billboard.Color.W = MyMwcUtils.GetRandomFloat(0.5f, 2);
                    billboard.Color.X = MyUtils.GetRandomFloat(0.5f, 3);
                    billboard.Color.Y = MyUtils.GetRandomFloat(0.5f, 1);
                    billboard.Color.Z = MyUtils.GetRandomFloat(0.5f, 1);
                    billboard.Color.W = MyUtils.GetRandomFloat(0.5f, 1);
                }
            }

            //  Small billboards
            m_smallBillboards = new MySunWindBillboardSmall[MySunWindConstants.SMALL_BILLBOARDS_SIZE.X][];
            for (int x = 0; x < MySunWindConstants.SMALL_BILLBOARDS_SIZE.X; x++)
            {
                m_smallBillboards[x] = new MySunWindBillboardSmall[MySunWindConstants.SMALL_BILLBOARDS_SIZE.Y];
                for (int y = 0; y < MySunWindConstants.SMALL_BILLBOARDS_SIZE.Y; y++)
                {
                    m_smallBillboards[x][y] = new MySunWindBillboardSmall();
                    MySunWindBillboardSmall billboard = m_smallBillboards[x][y];

                    billboard.Radius = MyUtils.GetRandomFloat(MySunWindConstants.SMALL_BILLBOARD_RADIUS_MIN, MySunWindConstants.SMALL_BILLBOARD_RADIUS_MAX);
                    billboard.InitialAngle = MyUtils.GetRandomRadian();
                    billboard.RotationSpeed = MyUtils.GetRandomSign() * MyUtils.GetRandomFloat(MySunWindConstants.SMALL_BILLBOARD_ROTATION_SPEED_MIN, MySunWindConstants.SMALL_BILLBOARD_ROTATION_SPEED_MAX);

                    //billboard.Color = MySunWindConstants.BILLBOARD_COLOR;
                    billboard.Color.X = MyUtils.GetRandomFloat(0.5f, 1);
                    billboard.Color.Y = MyUtils.GetRandomFloat(0.2f, 0.5f);
                    billboard.Color.Z = MyUtils.GetRandomFloat(0.2f, 0.5f);
                    billboard.Color.W = MyUtils.GetRandomFloat(0.1f, 0.5f);

                    billboard.TailBillboardsCount = MyUtils.GetRandomInt(MySunWindConstants.SMALL_BILLBOARD_TAIL_COUNT_MIN, MySunWindConstants.SMALL_BILLBOARD_TAIL_COUNT_MAX);
                    billboard.TailBillboardsDistance = MyUtils.GetRandomFloat(MySunWindConstants.SMALL_BILLBOARD_TAIL_DISTANCE_MIN, MySunWindConstants.SMALL_BILLBOARD_TAIL_DISTANCE_MAX);

                    billboard.RadiusScales = new float[billboard.TailBillboardsCount];
                    for (int i = 0; i < billboard.TailBillboardsCount; i++)
                    {
                        billboard.RadiusScales[i] = MyUtils.GetRandomFloat(0.7f, 1.0f);
                    }
                }
            }

            MyLog.Default.DecreaseIndent();
            MyLog.Default.WriteLine("MySunWind.LoadData() - END");
        }

        protected override void UnloadData()
        {
            MyLog.Default.WriteLine("MySunWind.UnloadData - START");
            MyLog.Default.IncreaseIndent();

            IsActive = false;

            MyLog.Default.DecreaseIndent();
            MyLog.Default.WriteLine("MySunWind.UnloadData - END");            
        }

        static float m_deltaTime;
        private int m_rayCastCounter;
        private List<MyPhysics.HitInfo> m_hitLst = new List<MyPhysics.HitInfo>();
        
        
        //  This method will start sun wind. Or if there is one coming, this will reset it so it will start again.
        public static void Start()
        {
            //  Activate sun wind
            IsActive = true;

            m_smallBillboardsStarted = false;

            m_timeLastUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            //  Place sun wind at farest possible negative Z position
            //Vector3 directionToSunNormalized = MyMwcUtils.Normalize(MyGuiScreenGameBase.Static.SunPosition - MyCamera.Position); MyMwcSectorGroups.Get(MyGuiScreenGameBase.Static.Sector.SectorGroup).GetDirectionToSunNormalized();
            Vector3D directionToSunNormalized = MySector.DirectionToSunNormalized;
            m_initialSunWindPosition = /*MySession.Static.Player.PlayerEntity.Entity.WorldMatrix.Translation +*/ directionToSunNormalized * MySunWindConstants.SUN_WIND_LENGTH_HALF / 2;
            m_directionFromSunNormalized = -directionToSunNormalized;

            //  Start the sound of burning (looping)
            StopCue();
            //m_burningCue = MyAudio.Static.AddCue3D(MySoundCuesEnum.SfxSolarWind, m_initialSunWindPosition, m_directionFromSunNormalized, Vector3.Up, Vector3.Zero);
            //MySounds.UpdateCuePitch(m_burningCue, MyMwcUtils.GetRandomFloat(-1, +1));

            m_speed = MyUtils.GetRandomFloat(MySunWindConstants.SPEED_MIN, MySunWindConstants.SPEED_MAX);

            m_strength = MyUtils.GetRandomFloat(0, 1);

            m_directionFromSunNormalized.CalculatePerpendicularVector(out m_rightVector);
            m_downVector = MyUtils.Normalize(Vector3D.Cross(m_directionFromSunNormalized, m_rightVector));

            StartBillboards();
            
            // Reinit computed max distances, they'll be computed in update
            m_computedMaxDistances = 0;

            m_deltaTime = 0;

            // Collect entities
            m_sunwindEntities.Clear();
            //foreach (var entity in MyEntities.GetEntities())
            //{
            //    /*if (!(entity is MySmallShip)) continue;

            //    // Do not move with indestructibles (NPCs etc)
            //    if (!entity.IsDestructible)
            //        continue;*/

            //    m_sunwindEntities.Add(entity);
            //}
        }

        public override void UpdateBeforeSimulation()
        {
            int rayCount = 0;
            if (m_rayCastQueue.Count > 0 && m_rayCastCounter % 20 == 0)
            {
                while (rayCount < 50 && m_rayCastQueue.Count > 0)
                {
                    var rand = MyUtils.GetRandomInt(m_rayCastQueue.Count - 1);
                    var entity = m_rayCastQueue[rand].Entity;
                    var l = m_rayCastQueue[rand]._Ray;
                    var p = m_rayCastQueue[rand].Position;
                    var particle = m_rayCastQueue[rand].Particle;
                    if (entity is MyCubeGrid)
                    {
                        particle.Stop();
                        var grid = entity as MyCubeGrid;
                        var invMat = grid.PositionComp.WorldMatrixNormalizedInv;

                        if (grid.BlocksDestructionEnabled)
                        {
                            grid.Physics.ApplyDeformation(6f, 3f, 3f, Vector3.Transform(p, invMat), Vector3.Normalize(Vector3.Transform(m_directionFromSunNormalized, invMat)), MyDamageType.Environment);
                        }

                        //MyPhysics.HavokWorld.CastRay(l.From, l.To, m_hitLst);
                        //rayCount++;
                        //MyEntity ent = null;
                        //if (m_hitLst.Count != 0)
                        //    ent = m_hitLst[0].GetEntity();
                        //if (ent == grid)
                        //{
                        //    grid.Physics.ApplyDeformation(6f, 3f, 3f, Vector3.Transform(m_hitLst[0].Position, invMat), Vector3.Normalize(Vector3.Transform(m_directionFromSunNormalized, invMat)), Sandbox.Game.Weapons.MyDamageType.Environment);
                        //    //var block = grid.GetBlock(Vector3I.Floor(Vector3.Transform(m_hitLst[0].Position, invMat) / grid.GridSize));
                        //    //if (block != null)
                        //    //    grid.ApplyDestructionDeformation(block);
                        //    m_rayCastQueue.RemoveAt(0);
                        //    m_hitLst.Clear();
                        //    break;
                        //}
                        m_rayCastQueue.RemoveAt(rand);
                        m_hitLst.Clear();
                        break;
                    }
                }
            }
            m_rayCastCounter++;
            //  Update only if sun wind is active
            if (IsActive == false) return;

            //?
            float dT = ((float)MySandboxGame.TotalGamePlayTimeInMilliseconds - (float)m_timeLastUpdate) / 1000.0f;
            m_timeLastUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if(MySandboxGame.IsPaused)
                return;

            m_deltaTime += dT;

            float traveledDistance = m_speed * m_deltaTime;

            //  If sun wind finished its way, we will turn it off
            if (traveledDistance >= MySunWindConstants.SUN_WIND_LENGTH_TOTAL)
            {
                IsActive = false;
                StopCue();
                
                return;
            }

            Vector3D campos = MySession.Static.LocalCharacter == null ? Vector3D.Zero : MySession.Static.LocalCharacter.Entity.WorldMatrix.Translation;

            //  This is plane that goes through sun wind, it's in its middle
            m_planeMiddle = new PlaneD(m_initialSunWindPosition + m_directionFromSunNormalized * traveledDistance, m_directionFromSunNormalized);
            m_distanceToSunWind = m_planeMiddle.DistanceToPoint(ref campos);

            //  We make sure that sound moves always on line that goes through camera. So it's not in the middle of sun wind, more like middle where is camera.
            //  Reason is that I want the sound always go through camera.            
            m_positionOnCameraLine = /*MySession.Static.Player.PlayerEntity.Entity.WorldMatrix.Translation*/ - m_directionFromSunNormalized * m_distanceToSunWind;

            Vector3D positionFront = m_positionOnCameraLine + m_directionFromSunNormalized * 2000;
            Vector3D positionBack = m_positionOnCameraLine + m_directionFromSunNormalized * -2000;

            m_planeFront = new PlaneD(positionFront, m_directionFromSunNormalized);
            m_planeBack = new PlaneD(positionBack, m_directionFromSunNormalized);

            var distanceToFrontPlane = m_planeFront.DistanceToPoint(ref campos);
            var distanceToBackPlane = m_planeBack.DistanceToPoint(ref campos);

            #region commented
            
            //Vector3 positionOfSound;
            //if ((distanceToFrontPlane <= 0) && (distanceToBackPlane >= 0))
            //{
            //    positionOfSound = MySession.Static.Player.PlayerEntity.Entity.WorldMatrix.Translation;
            //}
            //else if (distanceToFrontPlane > 0)
            //{
            //    positionOfSound = positionFront;
            //}
            //else
            //{
            //    positionOfSound = positionBack;
            //}

            //  Update position of sound. It works like this: we hear coming sound, then we are in the sound and then we hear it coming out.

            //MyAudio.Static.UpdateCuePosition(m_burningCue, positionOfSound, m_directionFromSunNormalized, -m_downVector, m_directionFromSunNormalized * m_speed);

            //MySounds.UpdateCuePosition(m_burningCue, positionOfSound, m_directionFromSunNormalized, Vector3.Up, Vector3.Zero);

            //MyLogManager.WriteLine("positionOfSound: " + MyUtils.GetFormatedVector3(positionOfSound, 3));
            //MyLogManager.WriteLine("m_directionFromSunNormalized: " + MyUtils.GetFormatedVector3(m_directionFromSunNormalized, 3));
            //MyLogManager.WriteLine("m_downVector: " + MyUtils.GetFormatedVector3(m_downVector, 3));

            //Position = positionOfSound;

            //  Shake player's head
            //float distanceToSound;
            //Vector3.Distance(ref positionOfSound, ref campos, out distanceToSound);
            //float shake = 1 - MathHelper.Clamp(distanceToSound / 1000, 0, 1);
            /*if (MySession.Static.Player.Controller.ControlledEntity != null)
            {
                MySession.Static.PlayerShip.IncreaseHeadShake(
                    MathHelper.Lerp(MyHeadShakeConstants.HEAD_SHAKE_AMOUNT_DURING_SUN_WIND_MIN,
                                    MyHeadShakeConstants.HEAD_SHAKE_AMOUNT_DURING_SUN_WIND_MAX, shake));
            }*/
            #endregion

            for (int i = 0; i < m_sunwindEntities.Count;)
			{
                if (m_sunwindEntities[i].MarkedForClose)
                {
                    m_sunwindEntities.RemoveAtFast(i);
                }
                else
                {
                    i++;
                }
            }
            var q = VRageMath.Quaternion.CreateFromRotationMatrix(Matrix.CreateFromDir(m_directionFromSunNormalized, m_downVector));
            var v = new Vector3(10000, 10000, 2000);
            MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(positionFront + m_directionFromSunNormalized * 2500, v, q), Color.Red.ToVector3(), 1, false, false);
            if (m_rayCastCounter == 120)
            {
                var pos = positionFront + m_directionFromSunNormalized * 2500;
                MyPhysics.GetPenetrationsBox(ref v, ref pos, ref q, m_intersectionLst, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                
                foreach (var hit in m_intersectionLst)
                {
                    var entity = hit.GetCollisionEntity();
                    if (entity is MyVoxelMap)
                        continue;
                    if (!m_sunwindEntities.Contains(entity))
                        m_sunwindEntities.Add(entity);
                }
                m_intersectionLst.Clear();
                for (int i = 0; i < m_sunwindEntities.Count; i++)
                {
                    var entity = m_sunwindEntities[i];
                    if (entity is MyCubeGrid)
                    {
                        var grid = entity as MyCubeGrid;

                        var aabb = grid.PositionComp.WorldAABB;
                        var halfDiagonal = (aabb.Center - aabb.Min).Length();
                        var rightMax = ((aabb.Center - aabb.Min) / m_rightVector).AbsMin();
                        var downMax = ((aabb.Center - aabb.Min) / m_downVector).AbsMin();

                        var size = (grid.Max - grid.Min);
                        var max = Math.Max(size.X, Math.Max(size.Y, size.Z));
                        var invMat = grid.PositionComp.WorldMatrixNormalizedInv;

                        var start = aabb.Center - rightMax * m_rightVector - downMax * m_downVector;

                        for (int x = 0; x < rightMax * 2; x += grid.GridSizeEnum == MyCubeSize.Large ? 25 : 10)
                        {
                            for (int y = 0; y < downMax * 2; y += grid.GridSizeEnum == MyCubeSize.Large ? 25 : 10)
                            {
                                var pivot = start + x * m_rightVector + y * m_downVector;
                                pivot += (float)halfDiagonal * m_directionFromSunNormalized;
                                var circle = MyUtils.GetRandomVector3CircleNormalized();
                                float rand = MyUtils.GetRandomFloat(0, grid.GridSizeEnum == MyCubeSize.Large ? 10 : 5);
                                pivot += m_rightVector * circle.X * rand + m_downVector * circle.Z * rand;
                                LineD l = new LineD(pivot - m_directionFromSunNormalized * (float)halfDiagonal, pivot);
                                if (grid.RayCastBlocks(l.From, l.To).HasValue)
                                {
                                    l.From = pivot - m_directionFromSunNormalized * 1000;

                                    MyPhysics.CastRay(l.From, l.To, m_hitLst);
                                    m_rayCastCounter++;
                                    if (m_hitLst.Count == 0 || m_hitLst[0].HkHitInfo.GetHitEntity() != grid.Components)
                                    {
                                        m_hitLst.Clear();
                                        continue;
                                    }
                                    MyParticleEffect particle;
                                    if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Prefab_LeakingFire, out particle))
                                    {
                                        particle.WorldMatrix = MatrixD.CreateWorld(m_hitLst[0].Position, Vector3D.Forward, Vector3D.Up);
                                    }
                                    m_rayCastQueue.Add(new MyEntityRayCastPair() { Entity = grid, _Ray = l , Position = m_hitLst[0].Position, Particle = particle});
                                    //grid.Physics.ApplyDeformation(0.2f, 4, 2, Vector3.Transform(m_hitLst[0].Position, invMat), Vector3.Transform(m_directionFromSunNormalized, invMat), Sandbox.Game.Weapons.MyDamageType.Environment);
                                }
                            }
                        }
                        m_sunwindEntities.Remove(grid);
                        i--;
                    }
                    else
                    {
                        m_sunwindEntities.Remove(entity);
                        i--;
                    }

                }
                m_rayCastCounter = 0;
            }
            
            //  Apply force to all objects that aren't static and are hit by sun wind (ignoring voxels and large ships)
            //MyEntities.ApplySunWindForce(m_sunwindEntities, ref m_planeFront, ref m_planeBack, DoNotIgnoreTheseTypes, ref m_directionFromSunNormalized);

            //  Start small billboards
            if (m_distanceToSunWind <= MySunWindConstants.SWITCH_LARGE_AND_SMALL_BILLBOARD_DISTANCE)
            {
                Debug.Assert(m_computedMaxDistances == MySunWindConstants.SMALL_BILLBOARDS_SIZE.X * MySunWindConstants.SMALL_BILLBOARDS_SIZE.Y, "Not all small billboard MaxDistances are computed!");
                m_smallBillboardsStarted = true;
            }

            ComputeMaxDistances();

            base.UpdateBeforeSimulation();
        }

        public static bool IsActiveForHudWarning()
        {            
            //if (!IsActive || MySession.Static.PlayerShip == null)
            //{
            //    return false;
            //}
            return true;
            //Vector3 playerToSunwind = MySession.Static.Player.PlayerEntity.Entity.WorldMatrix.Translation - Position;
            //Vector3 directionToPlayerNormalize = Vector3.Normalize(playerToSunwind);
            //float dot = Vector3.Dot(m_directionFromSunNormalized, directionToPlayerNormalize);
            //// if sun wind before player, always display hud warning
            //if (dot >= 0f)
            //{
            //    return true;
            //}

            //// if sun wind behind player, display hud waring only up to 1000m
            //return playerToSunwind.LengthSquared() <= 1000;//MySmallShipConstants.WARNING_SUN_WIND_MAX_DISTANCE_BEHIND_PLAYER_SQR;
        }

        static void StopCue()
        {
            //if ((m_burningCue != null) && (m_burningCue.Value.IsPlaying == true))
            //{
            //    m_burningCue.Value.Stop(SharpDX.XACT3.StopFlags.Immediate);
            //}            
        }

        //  When sun wind is approaching camera position, we have to make sun color more bright
        public static Vector4 GetSunColor()
        {
            //  Increase sun color only if sun wind is really close
            float multiply = (float)(1 - MathHelper.Clamp(Math.Abs(m_distanceToSunWind) / MySunWindConstants.SUN_COLOR_INCREASE_DISTANCE, 0, 1));
            multiply *= MathHelper.Lerp(MySunWindConstants.SUN_COLOR_INCREASE_STRENGTH_MIN, MySunWindConstants.SUN_COLOR_INCREASE_STRENGTH_MAX, m_strength);

            return new Vector4(MySector.SunProperties.EnvironmentLight.SunColorRaw, 1.0f) * (1 + multiply);
        }

        //  When sun wind is approaching camera position, we have to make particle dust more transparent (or invisible), because it doesn't look when mixed with sun wind billboards
        public static float GetParticleDustFieldAlpha()
        {
            return (float)Math.Pow(MathHelper.Clamp(Math.Abs(m_distanceToSunWind) / MySunWindConstants.PARTICLE_DUST_DECREAS_DISTANCE, 0, 1), 4);
        }

        //  This method doesn't really draw. It just creates billboards that are later drawn in MyParticles.Draw()
        public override void Draw()
        {
            if (IsActive == false) return;
            if (IsVisible == false) return;

            //float deltaTime = ((float)MyMinerGame.TotalGamePlayTimeInMilliseconds - (float)m_timeStarted) / 1000.0f;
            float traveledDistance = m_speed * m_deltaTime;
            Vector3 deltaPosition = m_directionFromSunNormalized * traveledDistance;

            //  Draw LARGE billboards
            //for (int x = 0; x < MySunWindConstants.LARGE_BILLBOARDS_SIZE.X; x++)
            //{
            //    for (int y = 0; y < MySunWindConstants.LARGE_BILLBOARDS_SIZE.Y; y++)
            //    {
            //        MySunWindBillboard billboard = m_largeBillboards[x][y];

            //        Vector3 actualPosition = billboard.InitialAbsolutePosition + deltaPosition;

            //        float distanceToCamera;
            //        Vector3 campos = MySession.Static.Player.PlayerEntity.Entity.WorldMatrix.Translation;
            //        Vector3.Distance(ref actualPosition, ref campos, out distanceToCamera);
            //        float alpha = 0.15f;// -MathHelper.Clamp(distanceToCamera / MySunWindConstants.LARGE_BILLBOARD_DISAPEAR_DISTANCE, 0, 1);

            //        float distanceToCenterOfSunWind;
            //        Vector3.Distance(ref actualPosition, ref campos, out distanceToCenterOfSunWind);

            //        //if (distanceToCenterOfSunWind < MySunWindConstants.SWITCH_LARGE_AND_SMALL_BILLBOARD_RADIUS)
            //        //{
            //        //    alpha *= MathHelper.Clamp(distanceToCamera / MySunWindConstants.SWITCH_LARGE_AND_SMALL_BILLBOARD_DISTANCE, 0, 1);
            //        //}

            //        //billboard.Color *= alpha;

            //        Graphics.TransparentGeometry.MyTransparentGeometry.AddPointBillboard(
            //            MyTransparentMaterials.GetMaterial("Explosion"),
            //            new Vector4(billboard.Color.X * alpha, billboard.Color.Y * alpha, billboard.Color.Z * alpha, alpha),
            //            actualPosition,
            //            billboard.Radius,
            //            billboard.InitialAngle + billboard.RotationSpeed * m_deltaTime);
            //    }
            //}

            //  //Draw SMALL billboards
            //if (m_distanceToSunWind <= MySunWindConstants.SWITCH_LARGE_AND_SMALL_BILLBOARD_DISTANCE)
            //{
            //    if (m_smallBillboardsStarted == false)
            //    {
            //        StartSmallBillboards();
            //        m_smallBillboardsStarted = true;
            //    }
            //}

            //if (m_smallBillboardsStarted == true)
            //{
            //    for (int x = 0; x < MySunWindConstants.SMALL_BILLBOARDS_SIZE.X; x++)
            //    {
            //        for (int y = 0; y < MySunWindConstants.SMALL_BILLBOARDS_SIZE.Y; y++)
            //        {
            //            MySunWindBillboardSmall billboard = m_smallBillboards[x][y];

            //            Vector3 actualPosition = billboard.InitialAbsolutePosition + deltaPosition;

            //            for (int z = 0; z < billboard.TailBillboardsCount; z++)
            //            {
            //                //Vector2 positionRandomDelta = new Vector2(
            //                // MyVRageUtils.GetRandomFloat(MySunWindConstants.SMALL_BILLBOARD_POSITION_DELTA_MIN / 10, MySunWindConstants.SMALL_BILLBOARD_POSITION_DELTA_MAX / 10),
            //                // MyVRageUtils.GetRandomFloat(MySunWindConstants.SMALL_BILLBOARD_POSITION_DELTA_MIN / 10, MySunWindConstants.SMALL_BILLBOARD_POSITION_DELTA_MAX / 10));
            //                Vector3 tempPosition = actualPosition - m_directionFromSunNormalized * (z - billboard.TailBillboardsCount / 2) * billboard.TailBillboardsDistance;
 
            //               //tempPosition += m_rightVector * positionRandomDelta.X + m_downVector * positionRandomDelta.Y; 
            //                float distanceToCamera;
            //                Vector3 campos = MySession.Static.Player.PlayerEntity.Entity.WorldMatrix.Translation;
            //                Vector3.Distance(ref tempPosition, ref campos, out distanceToCamera);

            //                //distanceToCamera = Math.Abs(Vector3.Dot(tempPosition - campos, m_directionFromSunNormalized));

            //                float alpha = 0.2f;// -MathHelper.Clamp((distanceToCamera) / (MySunWindConstants.SWITCH_LARGE_AND_SMALL_BILLBOARD_DISTANCE_HALF), 0, 1);

            //                if (alpha > 0)
            //                {
            //                    float distanceFromOrigin;
            //                    Vector3.Distance(ref tempPosition, ref billboard.InitialAbsolutePosition, out distanceFromOrigin);
            //                    if (distanceFromOrigin < billboard.MaxDistance)
            //                    {
            //                        Graphics.TransparentGeometry.MyTransparentGeometry.AddPointBillboard(
            //                            MyTransparentMaterials.GetMaterial("Explosion"),
            //                            new Vector4(billboard.Color.X * alpha, billboard.Color.Y * alpha, billboard.Color.Z * alpha, billboard.Color.W * alpha),
            //                            tempPosition,
            //                            billboard.Radius * billboard.RadiusScales[z],
            //                            billboard.InitialAngle + billboard.RotationSpeed * m_deltaTime);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
            base.Draw();
        }

        static void StartBillboards()
        {
            //  Initialize LARGE billboards
            for (int x = 0; x < MySunWindConstants.LARGE_BILLBOARDS_SIZE.X; x++)
            {
                for (int y = 0; y < MySunWindConstants.LARGE_BILLBOARDS_SIZE.Y; y++)
                {
                    MySunWindBillboard billboard = m_largeBillboards[x][y];

                    Vector3 positionRandomDelta = new Vector3(
                        MyUtils.GetRandomFloat(MySunWindConstants.LARGE_BILLBOARD_POSITION_DELTA_MIN, MySunWindConstants.LARGE_BILLBOARD_POSITION_DELTA_MAX),
                        MyUtils.GetRandomFloat(MySunWindConstants.LARGE_BILLBOARD_POSITION_DELTA_MIN, MySunWindConstants.LARGE_BILLBOARD_POSITION_DELTA_MAX),
                        MyUtils.GetRandomFloat(MySunWindConstants.LARGE_BILLBOARD_POSITION_DELTA_MIN, MySunWindConstants.LARGE_BILLBOARD_POSITION_DELTA_MAX));

                    Vector3 positionRelative = new Vector3(
                        (x - MySunWindConstants.LARGE_BILLBOARDS_SIZE_HALF.X) * MySunWindConstants.LARGE_BILLBOARD_DISTANCE,
                        (y - MySunWindConstants.LARGE_BILLBOARDS_SIZE_HALF.Y) * MySunWindConstants.LARGE_BILLBOARD_DISTANCE,
                        (x - MySunWindConstants.LARGE_BILLBOARDS_SIZE_HALF.X) * MySunWindConstants.LARGE_BILLBOARD_DISTANCE * 0.2f);

                    billboard.InitialAbsolutePosition =
                        m_initialSunWindPosition +
                        m_rightVector * (positionRandomDelta.X + positionRelative.X) +
                        m_downVector * (positionRandomDelta.Y + positionRelative.Y) +
                        -1 * m_directionFromSunNormalized * (positionRandomDelta.Z + positionRelative.Z);
                }
            }

            Vector3D initialPositionOnCameraLine = MySession.Static.LocalCharacter == null ?
                Vector3D.Zero :
               MySession.Static.LocalCharacter.Entity.WorldMatrix.Translation - m_directionFromSunNormalized * MySunWindConstants.SUN_WIND_LENGTH_HALF / 3;

            for (int x = 0; x < MySunWindConstants.SMALL_BILLBOARDS_SIZE.X; x++)
            {
                for (int y = 0; y < MySunWindConstants.SMALL_BILLBOARDS_SIZE.Y; y++)
                {
                    MySunWindBillboardSmall billboard = m_smallBillboards[x][y];

                    Vector2 positionRandomDelta = new Vector2(
                        MyUtils.GetRandomFloat(MySunWindConstants.SMALL_BILLBOARD_POSITION_DELTA_MIN, MySunWindConstants.SMALL_BILLBOARD_POSITION_DELTA_MAX),
                        MyUtils.GetRandomFloat(MySunWindConstants.SMALL_BILLBOARD_POSITION_DELTA_MIN, MySunWindConstants.SMALL_BILLBOARD_POSITION_DELTA_MAX));

                    Vector2 positionRelative = new Vector2(
                        (x - MySunWindConstants.SMALL_BILLBOARDS_SIZE_HALF.X) * MySunWindConstants.SMALL_BILLBOARD_DISTANCE,
                        (y - MySunWindConstants.SMALL_BILLBOARDS_SIZE_HALF.Y) * MySunWindConstants.SMALL_BILLBOARD_DISTANCE);

                    billboard.InitialAbsolutePosition =
                        initialPositionOnCameraLine +
                        m_rightVector * (positionRandomDelta.X + positionRelative.X) +
                        m_downVector * (positionRandomDelta.Y + positionRelative.Y);
                }
            }
        }

        /// <summary>
        /// Compute MaxDistances for uninitialized SmallBillboards
        /// </summary>
        private static void ComputeMaxDistances()
        {
            int smallBillBoardsCount = MySunWindConstants.SMALL_BILLBOARDS_SIZE.X * MySunWindConstants.SMALL_BILLBOARDS_SIZE.Y;
            if (m_computedMaxDistances < smallBillBoardsCount)
            {
                int cnt = (int)(smallBillBoardsCount / MySunWindConstants.SECONDS_FOR_SMALL_BILLBOARDS_INITIALIZATION / VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                while (m_computedMaxDistances < smallBillBoardsCount && cnt > 0)
                {
                    int x = m_computedMaxDistances % MySunWindConstants.SMALL_BILLBOARDS_SIZE.Y;
                    int y = m_computedMaxDistances / MySunWindConstants.SMALL_BILLBOARDS_SIZE.X;

                    var billBoard = m_smallBillboards[x][y];

                    ComputeMaxDistance(billBoard);

                    ++m_computedMaxDistances;
                    --cnt;
                }
            }
        }

        private static void ComputeMaxDistance(MySunWindBillboardSmall billboard)
        {
            Vector3 sunWindVector = m_directionFromSunNormalized * MySunWindConstants.SUN_WIND_LENGTH_HALF;
            var offset = (-m_directionFromSunNormalized * MySunWindConstants.RAY_CAST_DISTANCE);
            //  This line start where billboard starts and end at place that is farest possible place billboard can reach
            //  If intersection found, we will mark that place as small billboard's destination. It can't go further.
            LineD line = new LineD((sunWindVector + billboard.InitialAbsolutePosition) + offset, billboard.InitialAbsolutePosition + m_directionFromSunNormalized * MySunWindConstants.SUN_WIND_LENGTH_TOTAL);
            VRage.Game.Models.MyIntersectionResultLineTriangleEx? intersection = MyEntities.GetIntersectionWithLine(ref line, null, null);
            if (intersection != null)
                billboard.MaxDistance = (float)(intersection.Value.Triangle.Distance - billboard.Radius);
            else
                billboard.MaxDistance = MySunWindConstants.SUN_WIND_LENGTH_TOTAL;
        }
    }
}
