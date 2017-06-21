using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public enum MyEntityCyclingOrder
    {
        Characters,
        BiggestGrids,
        Fastest,
        BiggestDistanceFromPlayers,
        MostActiveDrills,
        MostActiveReactors,
        MostActiveProductionBuildings,
        MostActiveSensors,
        MostActiveThrusters,
        MostWheels,
        StaticObjects,
        FloatingObjects,   
        Gps,
        Planets,
        OwnerLoginTime,
    }

    public struct CyclingOptions
    {
        public bool Enabled;
        public bool OnlySmallGrids;
        public bool OnlyLargeGrids;
    }
    public static class MyEntityCycling
    {
        public struct Metric
        {
            public static readonly Metric Min = new Metric() { Value = float.MinValue, EntityId = 0 };
            public static readonly Metric Max = new Metric() { Value = float.MaxValue, EntityId = 0 };

            public float Value;
            public long EntityId;

            public Metric(float value, long entityId)
            {
                Value = value;
                EntityId = entityId;
            }

            public static bool operator >(Metric a, Metric b)
            {
                return a.Value > b.Value || (a.Value == b.Value && a.EntityId > b.EntityId);
            }

            public static bool operator <(Metric a, Metric b)
            {
                return b > a;
            }

            public static bool operator >=(Metric a, Metric b)
            {
                return a.Value > b.Value || (a.Value == b.Value && a.EntityId >= b.EntityId);
            }

            public static bool operator <=(Metric a, Metric b)
            {
                return b >= a;
            }

            public static bool operator ==(Metric a, Metric b)
            {
                return a.Value == b.Value && a.EntityId == b.EntityId;
            }

            public static bool operator !=(Metric a, Metric b)
            {
                return !(a == b);
            }
        }

        public static float GetMetric(MyEntityCyclingOrder order, MyEntity entity)
        {
            var grid = entity as MyCubeGrid;
            var phys = entity.Physics;

            switch (order)
            {
                case MyEntityCyclingOrder.Characters: return entity is MyCharacter ? 1 : 0;
                case MyEntityCyclingOrder.BiggestGrids: return grid != null ? grid.GetBlocks().Count : 0;
                case MyEntityCyclingOrder.Fastest: return phys != null ? (float)Math.Round(phys.LinearVelocity.Length(),2) : 0;
                case MyEntityCyclingOrder.BiggestDistanceFromPlayers: return (entity is MyVoxelBase) ? 0 : GetPlayerDistance(entity);
                case MyEntityCyclingOrder.MostActiveDrills: return GetActiveBlockCount<MyShipDrill>(grid);
                case MyEntityCyclingOrder.MostActiveProductionBuildings: return GetActiveBlockCount<MyProductionBlock>(grid);
                case MyEntityCyclingOrder.MostActiveReactors: return GetActiveBlockCount<MyReactor>(grid);
                case MyEntityCyclingOrder.MostActiveSensors: return GetActiveBlockCount<MySensorBlock>(grid);
                case MyEntityCyclingOrder.MostActiveThrusters: return GetActiveBlockCount<MyThrust>(grid);
                case MyEntityCyclingOrder.MostWheels: return GetActiveBlockCount<MyMotorSuspension>(grid, true);
                case MyEntityCyclingOrder.FloatingObjects: return entity is MyFloatingObject ? 1 : 0;
                case MyEntityCyclingOrder.StaticObjects: return  entity.Physics != null && entity.Physics.AngularVelocity.AbsMax() < 0.05f && entity.Physics.LinearVelocity.AbsMax() < 0.05f ? 1:0;
                case MyEntityCyclingOrder.Planets: return entity is MyPlanet ? 1 : 0;
                case MyEntityCyclingOrder.OwnerLoginTime: return GetOwnerLoginTime(grid);
                default: return 0;
            }
        }

        static float GetOwnerLoginTime(MyCubeGrid grid)
        {
            if (grid == null)
                return 0;

            if (grid.BigOwners.Count == 0)
                return 0;

            var identity = MySession.Static.Players.TryGetIdentity(grid.BigOwners[0]);
            if (identity == null)
                return 0;

            return (float)Math.Round((DateTime.Now - identity.LastLoginTime).TotalDays, 2);
        }

        static float GetActiveBlockCount<T>(MyCubeGrid grid, bool includePassive = false)
            where T : MyFunctionalBlock
        {
            if (grid == null)
                return 0;

            int num = 0;
            foreach (var block in grid.GetBlocks())
            {
                var b = block.FatBlock as T;
                if (b != null && (includePassive || b.IsWorking))
                    num++;
            }
            return num;
        }

        static float GetPlayerDistance(MyEntity entity)
        {
            var pos = entity.WorldMatrix.Translation;
            float minDistSq = float.MaxValue;

            foreach (var player in Sync.Players.GetOnlinePlayers())
            {
                var controlledEntity = player.Controller.ControlledEntity;
                if (controlledEntity != null)
                {
                    var distSq = Vector3.DistanceSquared(controlledEntity.Entity.WorldMatrix.Translation, pos);
                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                    }
                }
            }
            return (float)Math.Sqrt(minDistSq);
        }

        public static void FindNext(MyEntityCyclingOrder order, ref float metric, ref long entityId, bool findLarger, CyclingOptions options)
        {
            Metric current = new Metric() { Value = metric, EntityId = entityId };
            Metric start = findLarger ? Metric.Max : Metric.Min;
            Metric result = start;
            Metric extreme = start;

            foreach (var entity in MyEntities.GetEntities())
            {
                if (options.Enabled)
                {
                    MyCubeGrid grid = entity as MyCubeGrid;
                    if (options.OnlyLargeGrids && (grid == null || grid.GridSizeEnum != MyCubeSize.Large))
                    {
                        continue;
                    }

                    if (options.OnlySmallGrids && (grid == null || grid.GridSizeEnum != MyCubeSize.Small))
                    {
                        continue;
                    }
                }

                var newMetric = new Metric(GetMetric(order, entity), entity.EntityId);
                if (newMetric.Value == 0)
                    continue;

                if (findLarger)
                {
                    if (newMetric > current && newMetric < result)
                        result = newMetric;
                    if (newMetric < extreme)
                        extreme = newMetric;
                }
                else
                {
                    if (newMetric < current && newMetric > result)
                        result = newMetric;
                    if (newMetric > extreme)
                        extreme = newMetric;
                }
            }

            if (result == start)
                result = extreme;

            metric = result.Value;
            entityId = result.EntityId;
        }
    }
}
