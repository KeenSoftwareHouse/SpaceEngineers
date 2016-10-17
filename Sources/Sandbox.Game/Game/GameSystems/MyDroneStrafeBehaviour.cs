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

namespace Sandbox.Game.GameSystems
{
    /// <summary>
    /// Strafe drone behavior - drone stays inside box volume.
    /// </summary>
    public class MyDroneStrafeBehaviour : MyRemoteControl.IRemoteControlAutomaticBehaviour
    {
        private MyRemoteControl m_remoteControl;

        public bool NeedUpdate { get; private set; }
        public bool IsActive { get; private set; }

        public bool RotateToPlayer { get; private set; }
        public float PlayerYAxisOffset { get; private set; }
        public float WaypointThresholdDistance { get; private set; }
        public bool ResetStuckDetection { get { return IsActive; } }

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
        private float m_kamikazeBehaviorDistance;
        private string m_alternativeBehavior;
        private bool m_alternativebehaviorSwitched = false;

        private Vector3D ReturnPosition;
        private int m_lostStartTimeMs;
        private int m_waypointStartTimeMs;
        private int m_lastTargetUpdate;
        private int m_lastWeaponUpdate;
        private bool m_farAwayFromTarget = false;
        private MyEntity m_target;
        private List<MyUserControllableGun> m_weapons;
        private List<MyFunctionalBlock> m_tools;
        private bool m_shooting = false;
        private bool m_operational = true;
        private Vector3D m_firstWaypoint = Vector3D.Zero;

        public MyDroneStrafeBehaviour(MyRemoteControl remoteControl, MySpaceStrafeData strafeData, bool activate, List<MyUserControllableGun> weapons, List<MyFunctionalBlock> tools, Vector3D firstWaypoint)
        {
            m_remoteControl = remoteControl;
            ReturnPosition = m_remoteControl.PositionComp.GetPosition();

            LoadStrafeData(strafeData);

            m_weapons = weapons;
            m_tools = tools;
            MyPlayer player = m_remoteControl.GetNearestPlayer();
            m_target = player != null ? player.Character as MyEntity : null;
            m_lastTargetUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_lastWeaponUpdate = m_lastTargetUpdate;
            m_waypointReachedTimeMs = m_lastTargetUpdate;
            m_firstWaypoint = firstWaypoint;

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
            RotateToPlayer = strafeData.RotateToPlayer;
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
            m_staticWeaponryUsageSq = strafeData.StaticWeaponryUsage * strafeData.StaticWeaponryUsage;
            m_useStaticWeaponry = strafeData.UseStaticWeaponry;
            m_kamikazeBehaviorDistance = strafeData.KamikazeBehaviorDistance;
            m_alternativeBehavior = strafeData.AlternativeBehavior;
            m_useTools = strafeData.UseTools;
            m_toolsUsageSq = strafeData.ToolsUsage * strafeData.ToolsUsage;
        }

        public void Update()
        {
            if (!Sync.IsServer)
                return;
            //DebugDraw();

            if (IsActive || NeedUpdate)
                UpdateWaypoint();
        }

        private void UpdateWaypoint()
        {
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            //find new target player
            if ((m_target == null && currentTime - m_lastTargetUpdate >= 5000) || currentTime - m_lostStartTimeMs >= m_lostTimeMs)
            {
                MyPlayer player = m_remoteControl.GetNearestPlayer();
                m_target = player != null ? player.Character as MyEntity : null;
                m_lastTargetUpdate = currentTime;
                if (m_target != null)
                {
                    m_lostStartTimeMs = currentTime;
                    m_farAwayFromTarget = true;
                }
                else
                {
                    m_lostStartTimeMs += 5000;
                }
            }

            if (m_farAwayFromTarget && currentTime - m_lastTargetUpdate >= 5000)
            {
                m_lastTargetUpdate = currentTime;
                NeedUpdate = true;
            }

            //weapon/suicide update
            float distSq = -1f;
            if (m_operational && currentTime - m_lastWeaponUpdate >= 300)
            {
                m_lastWeaponUpdate = currentTime;
                distSq = Vector3.DistanceSquared((Vector3)m_target.PositionComp.GetPosition(), (Vector3)m_remoteControl.PositionComp.GetPosition());
                WeaponsUpdate(distSq);
            }

            if (currentTime - m_waypointReachedTimeMs >= m_waypointMaxTime)
                NeedUpdate = true;

            if (!NeedUpdate || m_target == null)
                return;

            //active and prepare waypoints
            IsActive = true;
            if (distSq < 0)
                distSq = Vector3.DistanceSquared((Vector3)m_target.PositionComp.GetPosition(), (Vector3)m_remoteControl.PositionComp.GetPosition());
            m_farAwayFromTarget = distSq > m_maxManeuverDistanceSq;
            bool origUpdate = NeedUpdate;
            if (m_remoteControl.HasWaypoints())
            {
                m_remoteControl.ClearWaypoints();
                //MyGuiAudio.PlaySound(MyGuiSounds.PlayTakeItem); //debug
            }
            m_remoteControl.SetAutoPilotEnabled(true);
            NeedUpdate = origUpdate;
            
            Vector3D newWaypoint;
            if (m_firstWaypoint != Vector3D.Zero)
            {
                newWaypoint = m_firstWaypoint;
                m_firstWaypoint = Vector3D.Zero;
            }
            else if (!m_operational && m_kamikazeBehavior)
            {
                //no functional weapons -> ram the player
                if (m_remoteControl.TargettingAimDelta > 0.02f)
                    return;
                newWaypoint = m_target.PositionComp.GetPosition() + m_target.WorldMatrix.Up * PlayerYAxisOffset * 2 - Vector3D.Normalize(m_remoteControl.PositionComp.GetPosition() - m_target.PositionComp.GetPosition()) * m_kamikazeBehaviorDistance;
            }
            else if (!m_operational && !m_kamikazeBehavior)
            {
                //no functional weapons -> try to escape
                newWaypoint = ReturnPosition + Vector3.One * 0.01f;
            }
            else if (m_farAwayFromTarget)
            {
                //too far away from target
                newWaypoint = m_target.PositionComp.GetPosition() + Vector3D.Normalize(m_remoteControl.PositionComp.GetPosition() - m_target.PositionComp.GetPosition()) * m_playerTargetDistance;
            }
            else
            {
                //in proximity to target
                m_lostStartTimeMs = currentTime;
                if (currentTime - m_waypointReachedTimeMs <= m_waypointDelayMs)
                    return;
                newWaypoint = GetRandomPoint();
            }

            //Add new point to waypoints
            Vector3D currentToWaypoint = newWaypoint - m_remoteControl.WorldMatrix.Translation;
            currentToWaypoint.Normalize();
            m_waypointReachedTimeMs = currentTime;
            var strafeLocalDir = Vector3.TransformNormal((Vector3)currentToWaypoint, m_remoteControl.PositionComp.WorldMatrixNormalizedInv);
            Base6Directions.Direction strafeDir = Base6Directions.GetClosestDirection(ref strafeLocalDir);
            bool commitKamikadze = m_kamikazeBehavior && !m_operational;
            m_remoteControl.ChangeFlightMode(MyRemoteControl.FlightMode.OneWay);
            m_remoteControl.SetAutoPilotSpeedLimit(commitKamikadze ? 100f : m_speedLimit);
            m_remoteControl.SetCollisionAvoidance(commitKamikadze ? false : m_avoidCollisions);
            m_remoteControl.ChangeDirection(strafeDir);
            m_remoteControl.AddWaypoint(newWaypoint, m_farAwayFromTarget || commitKamikadze ? "Player Vicinity" : "Strafe");

            NeedUpdate = false;
            IsActive = true;
        }

        //checks if it has usable weapons and use them if necessary or ram player if weapons are unusable
        private void WeaponsUpdate(float distSq)
        {
            bool suicide = true;
            m_shooting = false;
            bool hasNonStationaryWeapons = false;
            if (m_weapons != null && m_weapons.Count > 0)
            {
                foreach (var weapon in m_weapons)
                {
                    MyGunStatusEnum status;
                    if (weapon.CanOperate() && weapon.CanShoot(out status) && status == MyGunStatusEnum.OK)
                    {
                        suicide = false;
                        if (m_useStaticWeaponry && weapon.IsStationary())
                        {
                            if (m_remoteControl.TargettingAimDelta <= 0.03f && distSq < m_staticWeaponryUsageSq)
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
                            if (distSq < m_toolsUsageSq)
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
                RotateToPlayer = true;
                m_weapons.Clear();
                m_tools.Clear();
                if(m_remoteControl.HasWaypoints())
                    m_remoteControl.ClearWaypoints();
                NeedUpdate = true;
            }
            if (!hasNonStationaryWeapons && !m_alternativebehaviorSwitched)
            {
                RotateToPlayer = true;
                if(m_alternativeBehavior.Length > 0)
                {
                    MySpaceStrafeData strafeData = MySpaceStrafeDataStatic.LoadPreset(m_alternativeBehavior);
                    LoadStrafeData(strafeData);
                }
                m_alternativebehaviorSwitched = true;
            }
        }

        public void WaypointAdvanced()
        {
            if (!Sync.IsServer)
                return;

            if (IsActive && m_remoteControl.CurrentWaypoint != null)
            {
                NeedUpdate = true;
            }
        }

        public void DebugDraw()
        {
            if (m_remoteControl.CurrentWaypoint != null)
                VRageRender.MyRenderProxy.DebugDrawSphere((Vector3)m_remoteControl.CurrentWaypoint.Coords, 0.5f, Color.Aquamarine, 1f, true);
        }

        private Vector3D GetRandomPoint()
        {
            Vector3D right, up, forward, result;
            int counter = 0;
            float lenSqr;
            MatrixD matrix = MatrixD.CreateFromDir(Vector3D.Normalize(m_target.PositionComp.GetPosition() - m_remoteControl.PositionComp.GetPosition()));

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
