using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Audio;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Game.GameSystems;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Network;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Physics;

namespace Sandbox.Game.Entities
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    [StaticEventOwner]
    class MyMeteorShower : MySessionComponentBase
    {
        private static readonly int WAVES_IN_SHOWER = 1;

        /// <summary>
        /// Angle above horizon on which meteor direction leaves sun direction.
        /// value = Sin(angle_in_radians)
        /// </summary>
        private static readonly double HORIZON_ANGLE_FROM_ZENITH_RATIO = Math.Sin(0.35);

        /// <summary>
        /// Can change size of hit area.
        /// 0 - meteorits aim to center of structure
        /// 1 - aim to circle which is cut from sphere BB and plane perpendicular to hit vector (from sun to center of BB)
        /// </summary>
        private static readonly double METEOR_BLUR_KOEF = 2.5;

        public static BoundingSphereD? CurrentTarget { get { return m_currentTarget; } set { m_currentTarget = value; } }

        private static Vector3D m_tgtPos, m_normalSun, m_pltTgtDir, m_mirrorDir;

        private static int m_waveCounter;
        static List<MyEntity> m_meteorList = new List<MyEntity>();
        static List<MyEntity> m_tmpEntityList = new List<MyEntity>();
        static BoundingSphereD? m_currentTarget;
        static List<BoundingSphereD> m_targetList = new List<BoundingSphereD>();
        static int m_lastTargetCount;
        private static Vector3 m_downVector;
        static Vector3 m_rightVector = Vector3.Zero;
        private static int m_meteorcount;
        private static List<MyCubeGrid> m_tmpHitGroup = new List<MyCubeGrid>();

        private static string[] m_enviromentHostilityName = new string[] {"Safe", "MeteorWave", "MeteorWaveCataclysm", "MeteorWaveCataclysmUnreal" };

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.Game == GameEnum.SE_GAME;
            }
        }

        public override void LoadData()
        {
            Debug.Assert(m_currentTarget == null && m_meteorList.Count == 0 && m_targetList.Count == 0);
            m_waveCounter = -1;
            m_lastTargetCount = 0;
            base.LoadData();
        }

        protected override void UnloadData()
        {
            foreach (var meteor in m_meteorList)
            {
                if (!meteor.MarkedForClose)
                    meteor.Close();
            }
            m_meteorList.Clear();
            m_currentTarget = null;
            m_targetList.Clear();
            base.UnloadData();
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            if (!Sync.IsServer) return;

            var meteorEvent = MyGlobalEvents.GetEventById(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), "MeteorWave"));
            if (meteorEvent == null) meteorEvent = MyGlobalEvents.GetEventById(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), "MeteorWaveCataclysm"));
            if (meteorEvent == null) meteorEvent = MyGlobalEvents.GetEventById(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), "MeteorWaveCataclysmUnreal"));

            if (meteorEvent == null && MySession.Static.EnvironmentHostility != MyEnvironmentHostilityEnum.SAFE && MyFakes.ENABLE_METEOR_SHOWERS)
            {
                var globalEvent = MyGlobalEventFactory.CreateEvent(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), "MeteorWave"));
                globalEvent.SetActivationTime(MyMeteorShower.CalculateShowerTime(MySession.Static.EnvironmentHostility));
                MyGlobalEvents.AddGlobalEvent(globalEvent);
            }
            else if (meteorEvent != null)
            {
                if (MySession.Static.EnvironmentHostility == MyEnvironmentHostilityEnum.SAFE || !MyFakes.ENABLE_METEOR_SHOWERS)
                {
                    meteorEvent.Enabled = false;
                }
                else
                {
                    meteorEvent.Enabled = true;
                    if (MySession.Static.PreviousEnvironmentHostility.HasValue)
                    {
                        if (MySession.Static.EnvironmentHostility != MySession.Static.PreviousEnvironmentHostility.Value)
                        {
                            meteorEvent.SetActivationTime(
                                MyMeteorShower.CalculateShowerTime(
                                    MySession.Static.EnvironmentHostility,
                                    MySession.Static.PreviousEnvironmentHostility.Value,
                                    meteorEvent.ActivationTime));
                            MySession.Static.PreviousEnvironmentHostility = null;
                        }
                    }
                }
            }
        }

        private static void MeteorWaveInternal(object senderEvent)
        {
            if (MySession.Static.EnvironmentHostility == MyEnvironmentHostilityEnum.SAFE)
            {
                Debug.Assert(false,"Meteor shower shouldnt be enabled in safe enviroment");
                ((MyGlobalEventBase)senderEvent).Enabled = false;
                return;
            }
            if(Sync.IsServer == false)
            {
                return;
            }

            m_waveCounter++;
            if (m_waveCounter == 0)
            {
                ClearMeteorList();
                if (m_targetList.Count == 0)
                {
                    GetTargets();
                    if (m_targetList.Count == 0)
                    {
                        m_waveCounter = WAVES_IN_SHOWER + 1;
                        RescheduleEvent(senderEvent);
                        return;
                    }
                }
                m_currentTarget = m_targetList.ElementAt(MyUtils.GetRandomInt(m_targetList.Count - 1));
                MyMultiplayer.RaiseStaticEvent(x => UpdateShowerTarget, m_currentTarget);
                m_targetList.Remove(m_currentTarget.Value);
                m_meteorcount = (int)(Math.Pow(m_currentTarget.Value.Radius, 2) * Math.PI / 6000);
                m_meteorcount /= (MySession.Static.EnvironmentHostility == MyEnvironmentHostilityEnum.CATACLYSM || MySession.Static.EnvironmentHostility == MyEnvironmentHostilityEnum.CATACLYSM_UNREAL) ? 1 : 8;
                m_meteorcount = MathHelper.Clamp(m_meteorcount, 1, 30);
                
            }

            RescheduleEvent(senderEvent);
            CheckTargetValid();
            if ( m_waveCounter < 0 )
                return;

            StartWave();
        }

        private static void StartWave()
        {
            if (!m_currentTarget.HasValue)
                return;

            var sunDir = GetCorrectedDirection(MySector.DirectionToSunNormalized);
            SetupDirVectors(sunDir);
            var waveMeteorCount = MyUtils.GetRandomFloat(Math.Min(2, m_meteorcount - 3), m_meteorcount + 3);

            var randCircle = MyUtils.GetRandomVector3CircleNormalized();
            var rand = MyUtils.GetRandomFloat(0, 1);
            Vector3D randomCircleInPlain = randCircle.X * m_rightVector + randCircle.Z * m_downVector;
            var hitPosition = m_currentTarget.Value.Center + Math.Pow(rand, 0.7f) * m_currentTarget.Value.Radius * randomCircleInPlain * METEOR_BLUR_KOEF;
            //Cast Ray for sure

            var antigravityDir = -Vector3D.Normalize(MyGravityProviderSystem.CalculateNaturalGravityInPoint(hitPosition));
            if (antigravityDir != Vector3D.Zero)
            {
                var hi = MyPhysics.CastRay(hitPosition + antigravityDir * (3000), hitPosition, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                if (hi != null)
                {
                    hitPosition = hi.Value.Position;
                }
            }
            m_meteorHitPos = hitPosition;
            for (int i = 0; i < waveMeteorCount; i++)
            {
                // hit
                randCircle = MyUtils.GetRandomVector3CircleNormalized();
                rand = MyUtils.GetRandomFloat(0, 1);
                randomCircleInPlain = randCircle.X * m_rightVector + randCircle.Z * m_downVector;
                hitPosition = hitPosition + Math.Pow(rand, 0.7f) * m_currentTarget.Value.Radius * randomCircleInPlain;


                // start
                var toSun = sunDir * (2000 + 100 * i);
                randCircle = MyUtils.GetRandomVector3CircleNormalized();
                randomCircleInPlain = randCircle.X * m_rightVector + randCircle.Z * m_downVector;
                var realPosition = hitPosition + toSun + (float)Math.Tan(MyUtils.GetRandomFloat(0, (float)Math.PI / 18)) * randomCircleInPlain;

                m_meteorList.Add(MyMeteor.SpawnRandom(realPosition, Vector3.Normalize(hitPosition - realPosition)));
            }
            m_rightVector = Vector3.Zero;
        }

        /// <summary>
        /// Calculate propper direction for meteorits. Everytime above horizon.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static Vector3 GetCorrectedDirection(Vector3 direction)
        {
            var currDir = direction;

            if (m_currentTarget == null)
                return currDir;

            var tgtPos = m_currentTarget.Value.Center;
            MyMeteorShower.m_tgtPos = tgtPos;

            if (!MyGravityProviderSystem.IsPositionInNaturalGravity(tgtPos))
                return currDir;

            var pltTgtDir = -Vector3D.Normalize(MyGravityProviderSystem.CalculateNaturalGravityInPoint(tgtPos));
            var tmpVec = Vector3D.Normalize(Vector3D.Cross(pltTgtDir, currDir));
            var mirror = Vector3D.Normalize(Vector3D.Cross(tmpVec, pltTgtDir));

            MyMeteorShower.m_mirrorDir = mirror;
            MyMeteorShower.m_pltTgtDir = pltTgtDir;
            MyMeteorShower.m_normalSun = tmpVec;

            double horizonRatio = pltTgtDir.Dot(currDir);
            //below down horizon
            if (horizonRatio < -HORIZON_ANGLE_FROM_ZENITH_RATIO)
            {
                return Vector3D.Reflect(-currDir, mirror);
            }

            // between below and above horizon (prohi
            if (horizonRatio < HORIZON_ANGLE_FROM_ZENITH_RATIO)
            {
                MatrixD tmpMat = MatrixD.CreateFromAxisAngle(tmpVec, -Math.Asin(HORIZON_ANGLE_FROM_ZENITH_RATIO));
                return Vector3D.Transform(mirror, tmpMat);
            }

            // above 20 Degree above horizon
            return currDir;
        }

        public static void StartDebugWave(Vector3 pos){
            m_currentTarget = new BoundingSphereD(pos, 100);
            m_meteorcount = (int)(Math.Pow(m_currentTarget.Value.Radius, 2) * Math.PI / 3000);
            m_meteorcount /= (MySession.Static.EnvironmentHostility == MyEnvironmentHostilityEnum.CATACLYSM || MySession.Static.EnvironmentHostility == MyEnvironmentHostilityEnum.CATACLYSM_UNREAL) ? 1 : 8;
            m_meteorcount = MathHelper.Clamp(m_meteorcount, 1, 40);
            StartWave();
        }

        private static Vector3D m_meteorHitPos;

        public override void Draw()
        {
            base.Draw();
            if (MyDebugDrawSettings.DEBUG_DRAW_METEORITS_DIRECTIONS)
            {
                Vector3D m_currDir = GetCorrectedDirection(MySector.DirectionToSunNormalized);
                MyRenderProxy.DebugDrawPoint(m_meteorHitPos, Color.White, false);
                MyRenderProxy.DebugDrawText3D(m_meteorHitPos, "Hit position", Color.White, 0.5f, false);
                MyRenderProxy.DebugDrawLine3D(m_tgtPos, m_tgtPos + 10 * MySector.DirectionToSunNormalized, Color.Yellow, Color.Yellow, false);
                MyRenderProxy.DebugDrawText3D(m_tgtPos + 10 * MySector.DirectionToSunNormalized, "Sun direction (sd)", Color.Yellow, 0.5F, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM);
                MyRenderProxy.DebugDrawLine3D(m_tgtPos, m_tgtPos + 10 * m_currDir, Color.Red, Color.Red, false);
                MyRenderProxy.DebugDrawText3D(m_tgtPos + 10 * m_currDir, "Current meteorits direction (cd)", Color.Red, 0.5F, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
                if (MyGravityProviderSystem.IsPositionInNaturalGravity(m_tgtPos))
                {
                    MyRenderProxy.DebugDrawLine3D(m_tgtPos, m_tgtPos + 10 * m_normalSun, Color.Blue, Color.Blue, false);
                    MyRenderProxy.DebugDrawText3D(m_tgtPos + 10 * m_normalSun, "Perpendicular to sd and n0 ", Color.Blue, 0.5F, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    MyRenderProxy.DebugDrawLine3D(m_tgtPos, m_tgtPos + 10 * m_pltTgtDir, Color.Green, Color.Green, false);
                    MyRenderProxy.DebugDrawText3D(m_tgtPos + 10 * m_pltTgtDir, "Dir from center of planet to target (n0)", Color.Green, 0.5F, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    MyRenderProxy.DebugDrawLine3D(m_tgtPos, m_tgtPos + 10 * m_mirrorDir, Color.Purple, Color.Purple, false);
                    MyRenderProxy.DebugDrawText3D(m_tgtPos + 10 * m_mirrorDir, "Horizon in plane n0 and sd (ho)", Color.Purple, 0.5F, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                }
            }
        }

        private static void CheckTargetValid()
        {
            if ( !m_currentTarget.HasValue )
                return;

            m_tmpEntityList.Clear();
            var bs = m_currentTarget.Value;
            m_tmpEntityList = MyEntities.GetEntitiesInSphere(ref bs);
            if (m_tmpEntityList.OfType<MyCubeGrid>().ToList().Count == 0)
                m_waveCounter = -1;
            if (m_waveCounter >= 0 && MyMusicController.Static != null)
            {
                foreach (var entity in m_tmpEntityList)
                {
                    if((entity is MyCharacter) && MySession.Static != null && (entity as MyCharacter) == MySession.Static.LocalCharacter)
                    {
                        MyMusicController.Static.MeteorShowerIncoming();
                        break;
                    }
                }
            }
            m_tmpEntityList.Clear();
        }

        private static void RescheduleEvent(object senderEvent)
        {
            if (m_waveCounter > WAVES_IN_SHOWER)
            {
                TimeSpan time = CalculateShowerTime(MySession.Static.EnvironmentHostility);
                MyGlobalEvents.RescheduleEvent((MyGlobalEventBase)senderEvent, time);
                m_waveCounter = -1;
                m_currentTarget = null;
                MyMultiplayer.RaiseStaticEvent(x => UpdateShowerTarget,m_currentTarget);
            }
            else
            {
                TimeSpan nextWave = TimeSpan.FromSeconds(m_meteorcount / 5f + MyUtils.GetRandomFloat(2, 5));
                MyGlobalEvents.RescheduleEvent((MyGlobalEventBase)senderEvent, nextWave);
            }
        }

        public static double GetActivationTime(MyEnvironmentHostilityEnum hostility, double defaultMinMinutes, double defaultMaxMinutes)
        {
            MyGlobalEventDefinition definition = MyDefinitionManager.Static.GetEventDefinition(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), m_enviromentHostilityName[(int)hostility]));
           if (definition != null)
           {
               if (definition.MinActivationTime.HasValue)
               {
                   defaultMinMinutes = definition.MinActivationTime.Value.TotalMinutes;
               }
               if (definition.MaxActivationTime.HasValue)
               {
                   defaultMaxMinutes = definition.MaxActivationTime.Value.TotalMinutes;
               }
           }
           return MyUtils.GetRandomDouble(defaultMinMinutes, defaultMaxMinutes);
        }

        public static TimeSpan CalculateShowerTime(MyEnvironmentHostilityEnum hostility)
        {
            double timeInMinutes = 5;
            switch (hostility)
            {
                case MyEnvironmentHostilityEnum.SAFE:
                    Debug.Assert(false, "Meteor shower shouldnt be enabled in safe enviroment");
                    break;
                case MyEnvironmentHostilityEnum.NORMAL:
                    timeInMinutes = GetActivationTime(hostility, MyMeteorShowerEventConstants.NORMAL_HOSTILITY_MIN_TIME, MyMeteorShowerEventConstants.NORMAL_HOSTILITY_MAX_TIME) / MathHelper.Max(1, m_lastTargetCount);
                    timeInMinutes = MathHelper.Max(0.4, timeInMinutes);
                    break;
                case MyEnvironmentHostilityEnum.CATACLYSM:
                    timeInMinutes = GetActivationTime(hostility, MyMeteorShowerEventConstants.CATACLYSM_HOSTILITY_MIN_TIME, MyMeteorShowerEventConstants.CATACLYSM_HOSTILITY_MAX_TIME) / MathHelper.Max(1, m_lastTargetCount);
                    timeInMinutes = MathHelper.Max(0.4, timeInMinutes);
                    break;
                case MyEnvironmentHostilityEnum.CATACLYSM_UNREAL:
                    timeInMinutes = GetActivationTime(hostility, MyMeteorShowerEventConstants.CATACLYSM_UNREAL_HOSTILITY_MIN_TIME, MyMeteorShowerEventConstants.CATACLYSM_UNREAL_HOSTILITY_MAX_TIME);
                    break;
                default:
                    Debug.Assert(false, "Invalid branch");
                    break;
            }
            return TimeSpan.FromMinutes(timeInMinutes);
        }

        private static double GetMaxActivationTime(MyEnvironmentHostilityEnum enviroment)
        {
            double defaultMaxMinutes = 0.0f;
            switch (enviroment)
            {            
                case MyEnvironmentHostilityEnum.NORMAL:
                    defaultMaxMinutes = MyMeteorShowerEventConstants.NORMAL_HOSTILITY_MAX_TIME;
                    break;
                case MyEnvironmentHostilityEnum.CATACLYSM:
                    defaultMaxMinutes = MyMeteorShowerEventConstants.CATACLYSM_HOSTILITY_MAX_TIME;
                    break;
                case MyEnvironmentHostilityEnum.CATACLYSM_UNREAL:
                    defaultMaxMinutes = MyMeteorShowerEventConstants.CATACLYSM_UNREAL_HOSTILITY_MAX_TIME;
                    break;
                default:
                    Debug.Assert(false, "Invalid branch");
                    break;
            }
            MyGlobalEventDefinition definition = MyDefinitionManager.Static.GetEventDefinition(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), m_enviromentHostilityName[(int)enviroment]));
            if (definition != null && definition.MaxActivationTime.HasValue)
            {
              defaultMaxMinutes = definition.MaxActivationTime.Value.TotalMinutes;           
            }
            return defaultMaxMinutes;
        }

        public static TimeSpan CalculateShowerTime(MyEnvironmentHostilityEnum newHostility, MyEnvironmentHostilityEnum oldHostility, TimeSpan oldTime)
        {
            double timeInMinutes = oldTime.TotalMinutes;
            double normalizedTime = 1.0;
            if (oldHostility != MyEnvironmentHostilityEnum.SAFE)
            {
                normalizedTime = timeInMinutes / GetMaxActivationTime(oldHostility);
            }
            timeInMinutes = normalizedTime * GetMaxActivationTime(newHostility);         
            return TimeSpan.FromMinutes(timeInMinutes);
        }

        private static void GetTargets()
        {
            List<MyCubeGrid> cg = MyEntities.GetEntities().OfType<MyCubeGrid>().ToList();

            //Optimize - remove grids smaller than 16 cubes (assume its debris)
            for (int i = 0; i < cg.Count; i++)
            {
                int size = (cg[i].Max - cg[i].Min + Vector3I.One).Size;

                if (size < 16 || (MySessionComponentTriggerSystem.Static.IsAnyTriggerActive(cg[i]) == false))
                {
                    cg.RemoveAt(i);
                    i--;
                }
            }

            while (cg.Count > 0)
            {
                MyCubeGrid hitGrid = cg[MyUtils.GetRandomInt(cg.Count - 1)];

                m_tmpHitGroup.Add(hitGrid);
                cg.Remove(hitGrid);
                var tmpTarget = hitGrid.PositionComp.WorldVolume;
                bool added = true;
                while (added)
                {
                    added = false;
                    foreach (MyCubeGrid grid in m_tmpHitGroup)
                    {
                        tmpTarget.Include(grid.PositionComp.WorldVolume);
                    }
                    m_tmpHitGroup.Clear();
                    tmpTarget.Radius += 10;
                    for (int i = 0; i < cg.Count; i++)
                    {
                        if (cg[i].PositionComp.WorldVolume.Intersects(tmpTarget))
                        {
                            added = true;
                            m_tmpHitGroup.Add(cg[i]);
                            cg.RemoveAt(i);
                            i--;
                        }
                    }
                }
                tmpTarget.Radius += 150;
                m_targetList.Add(tmpTarget);
            }
            m_lastTargetCount = m_targetList.Count;
        }

        private static void ClearMeteorList()
        {
            m_meteorList.Clear();
            //for (int i = 0; i < m_meteorList.Count; i++)
            //{
            //    //Debug.Assert(m_meteorList[i].MarkedForClose, "Meteor survived in scene until next shower!");
            //    if (!m_meteorList[i].MarkedForClose)
            //    {
            //        m_meteorList[i].MarkForClose();
            //    }
            //    m_meteorList.RemoveAt(i);
            //    i--;
            //}
        }

        private static void SetupDirVectors(Vector3 direction)
        {
            if (m_rightVector == Vector3.Zero)
            {
                direction.CalculatePerpendicularVector(out m_rightVector);
                m_downVector = MyUtils.Normalize(Vector3.Cross(direction, m_rightVector));
            }
        }

        [MyGlobalEventHandler(typeof(MyObjectBuilder_GlobalEventBase), "MeteorWave")]
        public static void MeteorWave(object senderEvent)
        {
            MeteorWaveInternal(senderEvent);
        }

        [Event,Reliable,Broadcast]
        static void UpdateShowerTarget([Serialize(MyObjectFlags.Nullable)] BoundingSphereD? target)
        {
            if (target.HasValue)
                MyMeteorShower.CurrentTarget = new BoundingSphereD(target.Value.Center, target.Value.Radius);
            else
                MyMeteorShower.CurrentTarget = null;
        }
    }
}
