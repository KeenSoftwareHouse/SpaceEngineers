using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public static class MyTrashRemoval
    {
        static Dictionary<MyTrashRemovalFlags, MyStringId> m_names = new Dictionary<MyTrashRemovalFlags, MyStringId>()
        {
            { MyTrashRemovalFlags.Fixed,          MySpaceTexts.ScreenDebugAdminMenu_Stations         },
            { MyTrashRemovalFlags.Stationary,     MySpaceTexts.ScreenDebugAdminMenu_Stationary       },
            { MyTrashRemovalFlags.Linear,         MyCommonTexts.ScreenDebugAdminMenu_Linear           },
            { MyTrashRemovalFlags.Accelerating,   MyCommonTexts.ScreenDebugAdminMenu_Accelerating     },
            { MyTrashRemovalFlags.Powered,        MySpaceTexts.ScreenDebugAdminMenu_Powered          },
            { MyTrashRemovalFlags.Controlled,     MySpaceTexts.ScreenDebugAdminMenu_Controlled       },
            { MyTrashRemovalFlags.WithProduction, MySpaceTexts.ScreenDebugAdminMenu_WithProduction   },
            { MyTrashRemovalFlags.WithMedBay,     MySpaceTexts.ScreenDebugAdminMenu_WithMedBay       },
            { MyTrashRemovalFlags.WithBlockCount, MyCommonTexts.ScreenDebugAdminMenu_WithBlockCount},
            { MyTrashRemovalFlags.DistanceFromPlayer,  MyCommonTexts.ScreenDebugAdminMenu_DistanceFromPlayer},
        };

        // Enable trash preview
        public static bool PreviewEnabled = false;

        //Trash settings
        public static MyTrashRemovalSettings PreviewSettings= MyTrashRemovalSettings.Default;

        public static int REMOVAL_INTERVAL_MINIMUM_S = 10;
        public static int REMOVAL_INTERVAL_MAXIMUM_S = 3600;

        //Trash action interval
        public static int CurrentRemovalInterval = 10;

        //Is trash paused?
        public static bool RemovalPaused = false;

        public static MyTrashRemovalOperation TrashOperation = MyTrashRemovalOperation.Remove;

        public static string GetName(MyTrashRemovalFlags flag)
        {
            MyStringId id;
            if (m_names.TryGetValue(flag, out id))
                return MyTexts.GetString(id);
            else
                return MyEnum<MyTrashRemovalFlags>.GetName(flag);
        }

        public static void Apply(MyTrashRemovalSettings settings, MyTrashRemovalOperation operation)
        {
            foreach (var entity in MyEntities.GetEntities())
            {
                // So far removal is applied only to grids
                var grid = entity as MyCubeGrid;
                if (grid != null)
                {
                    var keepReason = GetTrashState(grid, settings);
                    if (keepReason == MyTrashRemovalFlags.None)
                    {
                        ApplyOperation(grid, operation);
                    }
                }
            }
        }

        public static int Calculate(MyTrashRemovalSettings settings)
        {
            int num = 0;
            foreach (var entity in MyEntities.GetEntities())
            {
                if (entity != null)
                {
                    num += CalculateTrash(ref settings, entity);
                }
            }
            return num;
        }

        public static int CalculateTrash(ref MyTrashRemovalSettings settings, MyEntity entity)
        {
            // So far removal is applied only to grids
            var grid = entity as MyCubeGrid;
            if (grid != null)
            {
                var keepReason = GetTrashState(grid, settings);             
                if (keepReason == MyTrashRemovalFlags.None)
                {
                    return 1;
                }
            }
            return 0;
        }

        public static void ApplyOperation(MyEntity entity, MyTrashRemovalOperation operation)
        {
            if ((operation & MyTrashRemovalOperation.Remove) == MyTrashRemovalOperation.Remove)
            {
                entity.Close();
                return;
            }
            if ((operation & MyTrashRemovalOperation.Stop) == MyTrashRemovalOperation.Stop && entity.Physics != null)
            {
                entity.Physics.LinearVelocity = Vector3.Zero;
                entity.Physics.AngularVelocity = Vector3.Zero;
            }
            if ((operation & MyTrashRemovalOperation.Depower) == MyTrashRemovalOperation.Depower)
            {
                var grid = entity as MyCubeGrid;
                if (grid != null)
                {
                    grid.ChangePowerProducerState(MyMultipleEnabledEnum.AllDisabled, 0);
                }
            }
        }

        public static MyTrashRemovalFlags GetTrashState(MyCubeGrid grid, MyTrashRemovalSettings settings)
        {
            float metric;
            return GetTrashState(grid, settings, out metric,true);
        }

        static MyTrashRemovalFlags GetTrashState(MyCubeGrid grid, MyTrashRemovalSettings settings, out float metric,bool checkGroup = false)
        {
            metric = -1;

            HashSet<MySlimBlock> blocks = grid.GetBlocks();

            if (blocks != null  && blocks.Count > settings.BlockCountThreshold)
            {
                metric = settings.BlockCountThreshold;
                return MyTrashRemovalFlags.WithBlockCount;
            }

            if (!settings.HasFlag(MyTrashRemovalFlags.Fixed) && grid.IsStatic)
                return MyTrashRemovalFlags.Fixed;

            bool isAccelerating = false;
            bool isLinearMoving = false;
            bool isStationary = true;

            if (grid.Physics != null)
            {
                isStationary = grid.Physics.AngularVelocity.AbsMax() < 0.05f && grid.Physics.LinearVelocity.AbsMax() < 0.05f;
                isAccelerating = !isStationary && (grid.Physics.AngularAcceleration.AbsMax() > 0.05f || grid.Physics.LinearAcceleration.AbsMax() > 0.05f);
                isLinearMoving = !isAccelerating && !isStationary;
            }
            else
            {
                return MyTrashRemovalFlags.Default;
            }

            if (!settings.HasFlag(MyTrashRemovalFlags.Stationary) && isStationary)
                return MyTrashRemovalFlags.Stationary;

            if (!settings.HasFlag(MyTrashRemovalFlags.Linear) && isLinearMoving)
                return MyTrashRemovalFlags.Linear;

            if (!settings.HasFlag(MyTrashRemovalFlags.Accelerating) && isAccelerating)
                return MyTrashRemovalFlags.Accelerating;


            if (grid.GridSystems != null)
            {
                bool isPowered = grid.GridSystems.ResourceDistributor.ResourceStateByType(MyResourceDistributorComponent.ElectricityId) != MyResourceStateEnum.NoPower;
                if (!settings.HasFlag(MyTrashRemovalFlags.Powered) && isPowered)
                    return MyTrashRemovalFlags.Powered;

                if (!settings.HasFlag(MyTrashRemovalFlags.Controlled) && grid.GridSystems.ControlSystem.IsControlled)
                    return MyTrashRemovalFlags.Controlled;

                if(!settings.HasFlag(MyTrashRemovalFlags.WithProduction) &&
                    (grid.BlocksCounters.GetValueOrDefault(typeof(MyObjectBuilder_ProductionBlock)) > 0 ||
                    grid.BlocksCounters.GetValueOrDefault(typeof(MyObjectBuilder_Assembler)) > 0 ||
                    grid.BlocksCounters.GetValueOrDefault(typeof(MyObjectBuilder_Refinery)) > 0))
                    return MyTrashRemovalFlags.WithProduction;
            }

            if (!settings.HasFlag(MyTrashRemovalFlags.WithMedBay) && grid.BlocksCounters.GetValueOrDefault(typeof(MyObjectBuilder_MedicalRoom)) > 0)
                return MyTrashRemovalFlags.WithMedBay;

            if (IsCloseToPlayerOrCamera(grid, settings.PlayerDistanceThreshold))
            {
                metric = settings.PlayerDistanceThreshold;
                return MyTrashRemovalFlags.DistanceFromPlayer;
            }

            if (checkGroup && MyCubeGridGroups.Static.Physical != null)
            {
                var physicalGroup = MyCubeGridGroups.Static.Physical.GetGroup(grid);
                if (physicalGroup != null)
                {
                    foreach (var cubeGrid in physicalGroup.Nodes)
                    {
                        if (cubeGrid.NodeData == null || cubeGrid.NodeData.Physics == null || cubeGrid.NodeData.Physics.Shape == null || cubeGrid.NodeData == grid)
                            continue;


                        var subGridReason = GetTrashState(cubeGrid.NodeData, settings, out metric, false);
                        if (subGridReason != MyTrashRemovalFlags.None)
                        {
                            return subGridReason;
                        }
                    }
                }
            }

            return MyTrashRemovalFlags.None;
        }

        public static bool IsCloseToPlayerOrCamera(MyCubeGrid grid, float distanceThreshold)
        {
            var pos = grid.WorldMatrix.Translation;
            var thresholdSq = distanceThreshold * distanceThreshold;

            foreach (var player in Sync.Players.GetOnlinePlayers())
            {
                var controlledEntity = player.Controller.ControlledEntity;
                if (controlledEntity != null)
                {
                    var distSq = Vector3D.DistanceSquared(controlledEntity.Entity.WorldMatrix.Translation, pos);
                    if (distSq < thresholdSq)
                    {
                        return true;
                    }
                }
            }

            if(MySector.MainCamera != null)
            {
                var distSq = Vector3D.DistanceSquared(MySector.MainCamera.Position, pos);
                if (distSq < thresholdSq)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
