using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using VRage.Utils;
using VRageMath;
using VRage.Game.Entity;
using Sandbox.Game.World;
using Sandbox.Game.GUI;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using VRage.Game.ObjectBuilders.AI;

namespace Sandbox.Game.GameSystems
{
    public class DroneTarget : IComparable<DroneTarget>
    {
        public MyEntity Target;
        public int Priority;

        public DroneTarget(MyEntity target)
        {
            Target = target;
            Priority = 1;
        }

        public DroneTarget(MyEntity target, int priority)
        {
            Target = target;
            Priority = priority;
        }

        public int CompareTo(DroneTarget other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }

    /// <summary>
    /// Improved strafe drone behavior
    /// </summary>
    public class MyDroneStrafeBehaviour : MyRemoteControl.IRemoteControlAutomaticBehaviour
    {
        private MyRemoteControl m_remoteControl;

        public bool NeedUpdate { get; private set; }
        public bool IsActive { get; private set; }

        public bool RotateToTarget { get { return m_canRotateToTarget && m_rotateToTarget; } set { m_rotateToTarget = value; } }
        public bool CollisionAvoidance { get { return m_avoidCollisions; } set { m_avoidCollisions = value; } }
        public Vector3D OriginPoint { get { return m_returnPosition; } set { m_returnPosition = value; } }
        
        public int PlayerPriority { get; set; }
        public TargetPrioritization PrioritizationStyle { get { return m_prioritizationStyle; } set { m_prioritizationStyle = value; } }
        public MyEntity CurrentTarget { get { return m_currentTarget; } }
        public List<DroneTarget> TargetList { get { return m_targetsFiltered; } }
        public List<MyEntity> WaypointList { get { return m_forcedWaypoints; } }
        public bool WaypointActive { get { return !m_canSkipWaypoint; } }

        public float MaxPlayerDistance
        {
            get
            { 
                return m_maxPlayerDistance;
            }
            private set
            {
                m_maxPlayerDistance = value;
                m_maxPlayerDistanceSq = value * value;
            }
        }
        public float m_maxPlayerDistance = 0;
        public float m_maxPlayerDistanceSq = 0;
        private bool m_rotateToTarget = true;
        private bool m_canRotateToTarget = true;
        private float m_rotationLimitSq = 0f;

        public float PlayerYAxisOffset { get; private set; }
        public float WaypointThresholdDistance { get; private set; }
        public bool ResetStuckDetection { get { return IsActive; } }
        public bool CycleWaypoints
        {
            get { return m_cycleWaypoints; }
            set { m_cycleWaypoints = value; }
        }

        private string m_currentPreset;
        private bool m_avoidCollisions;
        private float m_speedLimit;
        private int m_waypointDelayMs;
        private int m_waypointDelayMsRange;
        private int m_waypointMaxTime;
        private int m_lostTimeMs;
        private float m_playerTargetDistance;
        private float m_maxManeuverDistanceSq;
        private float m_width;
        private float m_height;
        private float m_depth;
        private int m_waypointReachedTimeMs;
        private float m_minStrafeDistanceSq;
        private bool m_useStaticWeaponry;
        private float m_staticWeaponryUsageSq;
        private bool m_useTools;
        private float m_toolsUsageSq;
        private bool m_kamikazeBehavior;
        private bool m_canBeDisabled;
        private float m_kamikazeBehaviorDistance;
        private string m_alternativeBehavior;
        private bool m_alternativebehaviorSwitched = false;
        private bool m_useHoverMechanic = false;
        private float m_hoverMin = 2f;
        private float m_hoverMax = 25f;

        private Vector3D m_returnPosition;
        private int m_lostStartTimeMs;
        private int m_waypointStartTimeMs;
        private int m_lastTargetUpdate;
        private int m_lastWeaponUpdate;
        private bool m_farAwayFromTarget = false;
        private MyEntity m_currentTarget = null;
        private List<MyUserControllableGun> m_weapons = new List<MyUserControllableGun>();
        private List<MyFunctionalBlock> m_tools = new List<MyFunctionalBlock>();
        private bool m_shooting = false;
        private bool m_operational = true;
        private bool m_canSkipWaypoint = true;
        private bool m_cycleWaypoints = false;
        private List<MyEntity> m_forcedWaypoints = new List<MyEntity>();
        private List<DroneTarget> m_targetsList = new List<DroneTarget>();
        private List<DroneTarget> m_targetsFiltered = new List<DroneTarget>();
        private TargetPrioritization m_prioritizationStyle = TargetPrioritization.PriorityRandom;
        
        public bool m_loadItems = true;
        private bool m_loadEntities = false;
        private long m_loadCurrentTarget = 0;
        private List<VRage.Game.ObjectBuilders.AI.MyObjectBuilder_AutomaticBehaviour.DroneTargetSerializable> m_loadTargetList = null;
        private List<long> m_loadWaypointList = null;

        #region InitAndLoad

        public MyDroneStrafeBehaviour() { }

        public MyDroneStrafeBehaviour(MyRemoteControl remoteControl, string presetName, bool activate, List<MyEntity> waypoints, List<DroneTarget> targets, int playerPriority, TargetPrioritization prioritizationStyle, float maxPlayerDistance, bool cycleWaypoints)
        {
            m_remoteControl = remoteControl;
            m_returnPosition = m_remoteControl.PositionComp.GetPosition();

            MySpaceStrafeData strafeData = MySpaceStrafeDataStatic.LoadPreset(presetName);
            m_currentPreset = presetName;
            LoadStrafeData(strafeData);

            m_lastTargetUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_lastWeaponUpdate = m_lastTargetUpdate;
            m_waypointReachedTimeMs = m_lastTargetUpdate;
            m_forcedWaypoints = waypoints != null ? waypoints : new List<MyEntity>();
            m_targetsList = targets != null ? targets : new List<DroneTarget>();
            PlayerPriority = playerPriority;
            m_prioritizationStyle = prioritizationStyle;
            MaxPlayerDistance = maxPlayerDistance;
            m_cycleWaypoints = cycleWaypoints;

            NeedUpdate = activate;
        }

        private void LoadStrafeData(MySpaceStrafeData strafeData)
        {
            m_width = strafeData.Width;
            m_height = strafeData.Height;
            m_depth = strafeData.Depth;

            Debug.Assert(m_width >= 0 && m_height >= 0 && m_depth >= 0);

            m_avoidCollisions = strafeData.AvoidCollisions;
            m_speedLimit = strafeData.SpeedLimit;
            m_rotateToTarget = strafeData.RotateToPlayer;
            PlayerYAxisOffset = strafeData.PlayerYAxisOffset;
            WaypointThresholdDistance = strafeData.WaypointThresholdDistance;
            m_waypointDelayMs = strafeData.WaypointDelayMsMin;
            m_waypointDelayMsRange = strafeData.WaypointDelayMsMax - strafeData.WaypointDelayMsMin;
            m_waypointMaxTime = strafeData.WaypointMaxTime;
            m_lostTimeMs = strafeData.LostTimeMs;
            m_playerTargetDistance = strafeData.PlayerTargetDistance;
            m_maxManeuverDistanceSq = strafeData.MaxManeuverDistance * strafeData.MaxManeuverDistance;
            m_minStrafeDistanceSq = strafeData.MinStrafeDistance * strafeData.MinStrafeDistance;
            m_kamikazeBehavior = strafeData.UseKamikazeBehavior;
            m_canBeDisabled = strafeData.CanBeDisabled;
            m_staticWeaponryUsageSq = strafeData.StaticWeaponryUsage * strafeData.StaticWeaponryUsage;
            m_useStaticWeaponry = strafeData.UseStaticWeaponry;
            m_kamikazeBehaviorDistance = strafeData.KamikazeBehaviorDistance;
            m_alternativeBehavior = strafeData.AlternativeBehavior;
            m_useTools = strafeData.UseTools;
            m_toolsUsageSq = strafeData.ToolsUsage * strafeData.ToolsUsage;
            m_rotationLimitSq = Math.Max(m_toolsUsageSq, Math.Max(m_staticWeaponryUsageSq, m_maxManeuverDistanceSq));
            m_useHoverMechanic = strafeData.UsePlanetHover;
            m_hoverMin = strafeData.PlanetHoverMin;
            m_hoverMax = strafeData.PlanetHoverMax;
        }

        public void Load(MyObjectBuilder_AutomaticBehaviour objectBuilder, MyRemoteControl remoteControl)
        {
            MyObjectBuilder_DroneStrafeBehaviour builder = objectBuilder as MyObjectBuilder_DroneStrafeBehaviour;
            if (builder != null)
            {
                m_remoteControl = remoteControl;

                MySpaceStrafeData strafeData = MySpaceStrafeDataStatic.LoadPreset(builder.CurrentPreset);
                m_currentPreset = builder.CurrentPreset;
                LoadStrafeData(strafeData);

                m_lastTargetUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                m_lastWeaponUpdate = m_lastTargetUpdate;
                m_waypointReachedTimeMs = m_lastTargetUpdate;

                m_forcedWaypoints = new List<MyEntity>();
                m_loadWaypointList = builder.WaypointList;
                m_targetsList = new List<DroneTarget>();
                m_loadTargetList = builder.TargetList;
                m_currentTarget = null;
                m_loadCurrentTarget = builder.CurrentTarget;

                m_returnPosition = builder.ReturnPosition;
                PlayerPriority = builder.PlayerPriority;
                m_prioritizationStyle = builder.PrioritizationStyle;
                MaxPlayerDistance = builder.MaxPlayerDistance;
                m_cycleWaypoints = builder.CycleWaypoints;
                m_alternativebehaviorSwitched = builder.AlternativebehaviorSwitched;
                CollisionAvoidance = builder.CollisionAvoidance;
                m_canSkipWaypoint = builder.CanSkipWaypoint;

                NeedUpdate = builder.NeedUpdate;
                IsActive = builder.IsActive;
                m_loadEntities = true;
            }
        }

        public MyObjectBuilder_AutomaticBehaviour GetObjectBuilder()
        {
            MyObjectBuilder_DroneStrafeBehaviour builder = new MyObjectBuilder_DroneStrafeBehaviour();
            builder.CollisionAvoidance = CollisionAvoidance;
            builder.CurrentTarget = m_currentTarget != null ? m_currentTarget.EntityId : 0;
            builder.CycleWaypoints = m_cycleWaypoints;
            builder.IsActive = IsActive;
            builder.MaxPlayerDistance = m_maxPlayerDistance;
            builder.NeedUpdate = NeedUpdate;
            builder.PlayerPriority = PlayerPriority;
            builder.PrioritizationStyle = m_prioritizationStyle;
            builder.TargetList = new List<VRage.Game.ObjectBuilders.AI.MyObjectBuilder_AutomaticBehaviour.DroneTargetSerializable>();
            foreach (var target in m_targetsList)
            {
                if(target.Target != null)
                    builder.TargetList.Add(new VRage.Game.ObjectBuilders.AI.MyObjectBuilder_AutomaticBehaviour.DroneTargetSerializable(target.Target.EntityId, target.Priority));
            }
            builder.WaypointList = new List<long>();
            foreach (var waypoint in m_forcedWaypoints)
            {
                if (waypoint != null)
                    builder.WaypointList.Add(waypoint.EntityId);
            }

            builder.CurrentPreset = m_currentPreset;
            builder.AlternativebehaviorSwitched = m_alternativebehaviorSwitched;
            builder.ReturnPosition = m_returnPosition;
            builder.CanSkipWaypoint = m_canSkipWaypoint;

            return builder;
        }

        public void LoadShipGear()
        {
            m_loadItems = false;
            var blocks = m_remoteControl.CubeGrid.GetBlocks();
            m_weapons = new List<MyUserControllableGun>();
            m_tools = new List<MyFunctionalBlock>();
            foreach (var block in blocks)
            {
                if (block.FatBlock is MyUserControllableGun)
                    m_weapons.Add(block.FatBlock as MyUserControllableGun);
                if (block.FatBlock is MyShipToolBase)
                    m_tools.Add(block.FatBlock as MyFunctionalBlock);
                if (block.FatBlock is MyShipDrill)
                    m_tools.Add(block.FatBlock as MyFunctionalBlock);
            }
        }

        public void LoadEntities()
        {
            m_loadEntities = false;

            foreach (long id in m_loadWaypointList)
            {
                MyEntity waypoint;
                if (id > 0 && MyEntities.TryGetEntityById(id, out waypoint))
                    m_forcedWaypoints.Add(waypoint);
            }

            foreach (var target in m_loadTargetList)
            {
                MyEntity targetEntity;
                if (target.TargetId > 0 && MyEntities.TryGetEntityById(target.TargetId, out targetEntity))
                    m_targetsList.Add(new DroneTarget(targetEntity, target.Priority));
            }

            if (m_loadCurrentTarget > 0)
            {
                MyEntity target;
                MyEntities.TryGetEntityById(m_loadCurrentTarget, out target);
                m_currentTarget = target;
            }
            m_loadWaypointList.Clear();
            m_targetsList.Clear();
        }

        #endregion

        public void Update()
        {
            if (!Sync.IsServer)
                return;

            if (m_loadItems)
                LoadShipGear();

            if (m_loadEntities)
                LoadEntities();

            //DebugDraw();

            if (IsActive || NeedUpdate)
                UpdateWaypoint();
        }

        private void UpdateWaypoint()
        {
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (m_currentTarget != null && currentTime - m_lastTargetUpdate >= 1000)
            {
                m_lastTargetUpdate = currentTime;
                if (!IsValidTarget(m_currentTarget))
                {
                    m_currentTarget = null;
                }
            }

            //find new target player
            if ((m_currentTarget == null && currentTime - m_lastTargetUpdate >= 1000) || currentTime - m_lostStartTimeMs >= m_lostTimeMs)
            {
                FindNewTarget();
                m_lastTargetUpdate = currentTime;
                if (m_currentTarget != null)
                {
                    m_lostStartTimeMs = currentTime;
                    m_farAwayFromTarget = true;
                }
                else
                {
                    m_lostStartTimeMs += 5000;
                }
            }

            if (m_farAwayFromTarget && currentTime - m_lastTargetUpdate >= 5000 && m_canSkipWaypoint)
            {
                m_lastTargetUpdate = currentTime;
                NeedUpdate = true;
            }

            //weapon/suicide update
            float distSq = -1f;
            if (m_operational && currentTime - m_lastWeaponUpdate >= 300)
            {
                m_lastWeaponUpdate = currentTime;
                distSq = m_currentTarget != null ? Vector3.DistanceSquared((Vector3)m_currentTarget.PositionComp.GetPosition(), (Vector3)m_remoteControl.PositionComp.GetPosition()) : -1f;
                WeaponsUpdate(distSq);
                m_canRotateToTarget = distSq < m_rotationLimitSq && distSq >= 0;
            }

            if (currentTime - m_waypointReachedTimeMs >= m_waypointMaxTime && m_canSkipWaypoint)
                NeedUpdate = true;
            if (m_remoteControl.CurrentWaypoint == null && WaypointList.Count > 0)
                NeedUpdate = true;

            if (!NeedUpdate)
                return;

            //active and prepare waypoints
            IsActive = true;
            if (distSq < 0 && m_currentTarget != null)
                distSq = Vector3.DistanceSquared((Vector3)m_currentTarget.PositionComp.GetPosition(), (Vector3)m_remoteControl.PositionComp.GetPosition());
            m_farAwayFromTarget = distSq > m_maxManeuverDistanceSq;
            m_canRotateToTarget = distSq < m_rotationLimitSq && distSq >= 0;
            bool origUpdate = NeedUpdate;
            if (m_remoteControl.HasWaypoints())
            {
                m_remoteControl.ClearWaypoints();
                //MyGuiAudio.PlaySound(VRage.Audio.MyGuiSounds.PlayTakeItem); //debug
            }
            m_remoteControl.SetAutoPilotEnabled(true);
            NeedUpdate = origUpdate;
            
            Vector3D newWaypoint;
            m_canSkipWaypoint = true;
            if (m_forcedWaypoints.Count > 0)
            {
                if (m_cycleWaypoints)
                    m_forcedWaypoints.Add(m_forcedWaypoints[0]);
                newWaypoint = m_forcedWaypoints[0].PositionComp.GetPosition();
                m_forcedWaypoints.RemoveAt(0);
                m_canSkipWaypoint = false;
            }
            else if (m_currentTarget == null)
            {
                newWaypoint = m_remoteControl.WorldMatrix.Translation + Vector3.One * 0.01f;
            }
            else if (!m_operational && m_kamikazeBehavior)
            {
                //no functional weapons -> ram the player
                if (m_remoteControl.TargettingAimDelta > 0.02f)
                    return;
                newWaypoint = m_currentTarget.PositionComp.GetPosition() + m_currentTarget.WorldMatrix.Up * PlayerYAxisOffset * 2 - Vector3D.Normalize(m_remoteControl.PositionComp.GetPosition() - m_currentTarget.PositionComp.GetPosition()) * m_kamikazeBehaviorDistance;
            }
            else if (!m_operational && !m_kamikazeBehavior)
            {
                //no functional weapons -> try to escape
                newWaypoint = m_returnPosition + Vector3.One * 0.01f;
            }
            else if (m_farAwayFromTarget)
            {
                //too far away from target
                newWaypoint = m_currentTarget.PositionComp.GetPosition() + Vector3D.Normalize(m_remoteControl.PositionComp.GetPosition() - m_currentTarget.PositionComp.GetPosition()) * m_playerTargetDistance;
                if(m_useHoverMechanic)
                    HoverMechanic(ref newWaypoint);
            }
            else
            {
                //in proximity to target
                m_lostStartTimeMs = currentTime;
                if (currentTime - m_waypointReachedTimeMs <= m_waypointDelayMs)
                    return;
                newWaypoint = GetRandomPoint();
                if (m_useHoverMechanic)
                    HoverMechanic(ref newWaypoint);
            }

            //Add new point to waypoints
            Vector3D currentToWaypoint = newWaypoint - m_remoteControl.WorldMatrix.Translation;
            currentToWaypoint.Normalize();
            m_waypointReachedTimeMs = currentTime;
            bool commitKamikadze = m_kamikazeBehavior && !m_operational;
            m_remoteControl.ChangeFlightMode(MyRemoteControl.FlightMode.OneWay);
            m_remoteControl.SetAutoPilotSpeedLimit(commitKamikadze ? 100f : m_speedLimit);
            m_remoteControl.SetCollisionAvoidance((commitKamikadze || !m_canSkipWaypoint) ? false : m_avoidCollisions);
            m_remoteControl.ChangeDirection(Base6Directions.Direction.Forward);
            m_remoteControl.AddWaypoint(newWaypoint, m_farAwayFromTarget || commitKamikadze ? "Player Vicinity" : "Strafe");

            NeedUpdate = false;
            IsActive = true;
        }

        //alter position based on altitude
        private void HoverMechanic(ref Vector3D pos)
        {
            m_hoverMin = 5;
            m_hoverMax = 25;

            Vector3 gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(pos);
            if (gravity.LengthSquared() > 0f)
            {
                MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(pos);
                if (planet != null)
                {
                    Vector3D closestPoint = planet.GetClosestSurfacePointGlobal(ref pos);
                    float altitude = (float)Vector3D.Distance(closestPoint, pos);
                    if (Vector3D.DistanceSquared(planet.PositionComp.GetPosition(), closestPoint) > Vector3D.DistanceSquared(planet.PositionComp.GetPosition(), pos))
                        altitude *= -1;
                    if (altitude < m_hoverMin)
                        pos = closestPoint - Vector3D.Normalize(gravity) * m_hoverMin;
                    else if (altitude > m_hoverMax)
                        pos = closestPoint - Vector3D.Normalize(gravity) * m_hoverMax;
                }
            }
        }

        //check if target is alive/functional
        private bool IsValidTarget(MyEntity target)
        {
            if (target is MyCharacter && !((MyCharacter)target).IsDead)
                return true;

            if (target is MyFunctionalBlock)
                return ((MyFunctionalBlock)target).IsFunctional;
            else if (target is MyCubeBlock)
                return !((MyCubeBlock)target).Closed;

            return false;
        }

        //find new target
        private bool FindNewTarget()
        {
            //create new target list with players
            List<DroneTarget> possibleTargets = new List<DroneTarget>();
            if (PlayerPriority > 0)
            {
                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    var controlled = player.Controller.ControlledEntity;
                    if (controlled == null)
                        continue;
                    if (controlled is MyCharacter && ((MyCharacter)controlled).IsDead)
                        continue;

                    Vector3D position = controlled.Entity.WorldMatrix.Translation;
                    double distSq = Vector3D.DistanceSquared(m_remoteControl.PositionComp.GetPosition(), position);
                    if (distSq < m_maxPlayerDistanceSq)
                        possibleTargets.Add(new DroneTarget((MyEntity)controlled, PlayerPriority));
                }
            }
            for (int i = 0; i < m_targetsList.Count;i++ )
            {
                if (IsValidTarget(m_targetsList[i].Target))
                    possibleTargets.Add(m_targetsList[i]);
            }
            m_targetsFiltered.Clear();
            m_targetsFiltered = possibleTargets;
            if (possibleTargets.Count == 0)
                return false;

            bool completeRandom = m_prioritizationStyle == TargetPrioritization.Random;
            switch (m_prioritizationStyle)
            {
                default:
                case TargetPrioritization.HightestPriorityFirst:
                    possibleTargets.Sort();
                    m_currentTarget = possibleTargets[0].Target;
                    return true;

                case TargetPrioritization.ClosestFirst:
                    double bestDist = double.MaxValue;
                    foreach (DroneTarget target in possibleTargets)
                    {
                        double distSq = Vector3D.DistanceSquared(m_remoteControl.PositionComp.GetPosition(), target.Target.PositionComp.GetPosition());
                        if (distSq < bestDist)
                        {
                            bestDist = distSq;
                            m_currentTarget = target.Target;
                        }
                    }
                    return true;

                case TargetPrioritization.Random:
                case TargetPrioritization.PriorityRandom:
                    int sum = 0;
                    foreach (var pair in possibleTargets)
                    {
                        sum += completeRandom ? 1 : Math.Max(0, pair.Priority);
                    }
                    int randomPick = MyUtils.GetRandomInt(0, sum+1);
                    foreach (var pair in possibleTargets)
                    {
                        int p = completeRandom ? 1 : Math.Max(0, pair.Priority);
                        if (randomPick <= p)
                        {
                            m_currentTarget = pair.Target;
                            break;
                        }
                        else
                        {
                            randomPick -= p;
                        }
                    }
                    return true;
            }

            return false;
        }

        public void TargetAdd(DroneTarget target)
        {
            if (!m_targetsList.Contains(target))
                m_targetsList.Add(target);
        }

        public void TargetClear()
        {
            m_targetsList.Clear();
        }

        public void TargetLoseCurrent()
        {
            m_currentTarget = null;
        }

        public void TargetRemove(MyEntity target)
        {
            for (int i = 0; i < m_targetsList.Count; i++)
            {
                if (m_targetsList[i].Target == target)
                {
                    m_targetsList.RemoveAt(i);
                    i--;
                }
            }
        }

        public void WaypointAdd(MyEntity target)
        {
            if (target != null && !m_forcedWaypoints.Contains(target))
                m_forcedWaypoints.Add(target);
        }

        public void WaypointClear()
        {
            m_forcedWaypoints.Clear();
        }

        //checks if it has usable weapons and use them if necessary or ram player if weapons are unusable
        private void WeaponsUpdate(float distSq)
        {
            bool suicide = m_canBeDisabled;
            m_shooting = false;
            bool hasNonStationaryWeapons = false;
            if (m_weapons != null && m_weapons.Count > 0)
            {
                foreach (var weapon in m_weapons)
                {
                    if (!weapon.Enabled && weapon.IsFunctional)
                    {
                        suicide = false;
                        if (!weapon.IsStationary())
                            hasNonStationaryWeapons = true;
                        continue;
                    }

                    MyGunStatusEnum status;
                    if (weapon.CanOperate() && weapon.CanShoot(out status) && status == MyGunStatusEnum.OK)
                    {
                        suicide = false;
                        if (m_useStaticWeaponry && weapon.IsStationary())
                        {
                            if (m_remoteControl.TargettingAimDelta <= 0.05f && distSq < m_staticWeaponryUsageSq && distSq >= 0 && m_canRotateToTarget)
                            {
                                weapon.SetShooting(true);
                                m_shooting = true;
                            }
                            else
                                weapon.SetShooting(false);
                        }
                        if (!weapon.IsStationary())
                            hasNonStationaryWeapons = true;
                    }
                }
            }
            if (m_tools != null && m_tools.Count > 0)
            {
                foreach (var tool in m_tools)
                {
                    if (tool.IsFunctional)
                    {
                        suicide = false;
                        if (m_useTools)
                        {
                            if (distSq < m_toolsUsageSq && distSq >= 0 && m_canRotateToTarget)
                                tool.Enabled = true;
                            else
                                tool.Enabled = false;
                        }
                    }
                }
            }
            m_operational = !suicide;
            if (suicide)
            {
                m_rotateToTarget = true;
                m_weapons.Clear();
                m_tools.Clear();
                if(m_remoteControl.HasWaypoints())
                    m_remoteControl.ClearWaypoints();
                NeedUpdate = true;
                m_forcedWaypoints.Clear();
            }
            if (!hasNonStationaryWeapons && !m_alternativebehaviorSwitched)
            {
                m_rotateToTarget = true;
                if(m_alternativeBehavior.Length > 0)
                {
                    MySpaceStrafeData strafeData = MySpaceStrafeDataStatic.LoadPreset(m_alternativeBehavior);
                    LoadStrafeData(strafeData);
                    m_currentPreset = m_alternativeBehavior;
                }
                m_alternativebehaviorSwitched = true;
            }
        }

        public void WaypointAdvanced()
        {
            if (!Sync.IsServer)
                return;

            m_waypointReachedTimeMs = MySandboxGame.TotalGamePlayTimeInMilliseconds + MyUtils.GetRandomInt(m_waypointDelayMsRange);
            if (IsActive && (m_remoteControl.CurrentWaypoint != null || m_targetsFiltered.Count > 0 || m_forcedWaypoints.Count > 0))
            {
                NeedUpdate = true;
            }
        }

        public void DebugDraw()
        {
            if (m_remoteControl.CurrentWaypoint != null)
                VRageRender.MyRenderProxy.DebugDrawSphere((Vector3)m_remoteControl.CurrentWaypoint.Coords, 0.5f, Color.Aquamarine, 1f, true);
            if (m_currentTarget != null)
                VRageRender.MyRenderProxy.DebugDrawSphere((Vector3)m_currentTarget.PositionComp.GetPosition(), 2f, m_canRotateToTarget ? Color.Green : Color.Red, 1f, true);
        }

        private Vector3D GetRandomPoint()
        {
            Vector3D right, up, forward, result;
            int counter = 0;
            float lenSqr;
            MatrixD matrix = MatrixD.CreateFromDir(Vector3D.Normalize(m_currentTarget.PositionComp.GetPosition() - m_remoteControl.PositionComp.GetPosition()));
            do
            {
                right = matrix.Right * (MyUtils.GetRandomFloat(-m_width, m_width));
                up = matrix.Up * (MyUtils.GetRandomFloat(-m_height, m_height));
                forward = matrix.Forward * (MyUtils.GetRandomFloat(-m_depth, m_depth));
                result = m_remoteControl.PositionComp.GetPosition() + right + up + forward;
                lenSqr = (float)(result - m_remoteControl.PositionComp.GetPosition()).LengthSquared();
                if (lenSqr > m_minStrafeDistanceSq)
                    break;
            } while (++counter < 10);
            return result;
        }
    }
}
