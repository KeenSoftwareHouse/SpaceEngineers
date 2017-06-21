using Sandbox.Common;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.ModAPI;
using VRageMath;
using Sandbox.Game.Entities.Cube;
using VRage.Profiler;

namespace Sandbox.Game.EntityComponents
{
    public class MyGridTargeting : MyEntityComponentBase
    {
        MyCubeGrid m_grid;
        BoundingSphere m_queryLocal;
        HashSet<MyLargeTurretBase> m_turrets = new HashSet<MyLargeTurretBase>();
        List<MyEntity> m_targetRoots = new List<MyEntity>();
        MyListDictionary<MyCubeGrid, MyEntity> m_targetBlocks = new MyListDictionary<MyCubeGrid, MyEntity>();

        List<long> m_ownersB = new List<long>();
        List<long> m_ownersA = new List<long>();
        private int m_lastScan;

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_grid = Entity as MyCubeGrid;
            m_grid.OnBlockAdded += m_grid_OnBlockAdded;
        }

        void m_grid_OnBlockAdded(Entities.Cube.MySlimBlock obj)
        {
            var turret = obj.FatBlock as MyLargeTurretBase;
            if (turret != null)
            {
                if (m_turrets.Count == 0)
                    m_queryLocal = new BoundingSphere(obj.FatBlock.PositionComp.LocalMatrix.Translation, turret.ShootingRange);
                else
                    m_queryLocal.Include(new BoundingSphere(obj.FatBlock.PositionComp.LocalMatrix.Translation, turret.ShootingRange));
                m_turrets.Add(turret);
            }
        }

        public List<MyEntity> TargetRoots
        {
            get
            {
                if (MySession.Static.GameplayFrameCounter - m_lastScan > 100)
                    Scan();
                return m_targetRoots;
            }
        }

        public MyListDictionary<MyCubeGrid, MyEntity> TargetBlocks
        {
            get
            {
                if (MySession.Static.GameplayFrameCounter - m_lastScan > 100)
                    Scan();
                return m_targetBlocks;
            }
        }

        void Scan()
        {
            m_lastScan = MySession.Static.GameplayFrameCounter;
            ProfilerShort.Begin("QueryTargets");
            BoundingSphereD bs = new BoundingSphereD(Vector3D.Transform(m_queryLocal.Center, m_grid.WorldMatrix), m_queryLocal.Radius);
            m_targetRoots.Clear();
            m_targetBlocks.Clear();

            ProfilerShort.Begin("MyGamePruningStructure.GetAllTop...");
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref bs, m_targetRoots);
            ProfilerShort.End();

            int targetCount = m_targetRoots.Count;
            m_ownersA.AddList(m_grid.SmallOwners);
            m_ownersA.AddList(m_grid.BigOwners);
            for (int i = 0; i < targetCount; i++)
            {
                var grid = m_targetRoots[i] as MyCubeGrid; //perf: using grid owners to not querry friendly ships for blocks
                if (grid != null)
                {
                    if (grid.Physics != null && !grid.Physics.Enabled)
                        continue;

                    ProfilerShort.Begin("Friend checks");
                    bool enemy = false;
                    if (grid.BigOwners.Count == 0 && grid.SmallOwners.Count == 0)
                    {
                        foreach (var owner in m_ownersA)
                        {
                            if (MyIDModule.GetRelation(owner, 0) == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies)
                            {
                                enemy = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        m_ownersB.AddList(grid.BigOwners);
                        m_ownersB.AddList(grid.SmallOwners);


                        foreach (var owner in m_ownersA)
                        {
                            foreach (var other in m_ownersB)
                            {
                                if (MyIDModule.GetRelation(owner, other) == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies)
                                {
                                    enemy = true;
                                    break;
                                }
                            }
                        }
                        m_ownersB.Clear();
                    }
                    ProfilerShort.End();
                    if (enemy)
                    {
                        ProfilerShort.Begin("grid.Hierarchy.QuerySphere");
                        var list = m_targetBlocks.GetOrAddList(grid);
                        grid.Hierarchy.QuerySphere(ref bs, list);
                        ProfilerShort.End();
                    }
                    else
                    {
                        MyIDModule module;
                        //performance unfriendly case when grid is not clearly neutral or enemy
                        foreach (var block in grid.GetFatBlocks())
                        {
                            var moduleOwner = ((MyEntity)block) as IMyComponentOwner<MyIDModule>;
                            if (!(moduleOwner != null && moduleOwner.GetComponent(out module)))
                                continue;

                            var myID = block.OwnerId;
                            foreach (var owner in m_ownersA)
                            {
                                if (MyIDModule.GetRelation(owner, myID) == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies)
                                {
                                    enemy = true;
                                    break;
                                }
                            }
                            if (enemy)
                                break;
                        }

                        if (enemy)
                        {
                            ProfilerShort.Begin("grid.Hierarchy.QuerySphere");
                            var list = m_targetBlocks.GetOrAddList(grid);
                            grid.Hierarchy.QuerySphere(ref bs, list);
                            ProfilerShort.End();
                        }
                    }
                }
            }
            m_ownersA.Clear();

            ProfilerShort.Begin("Filter small objects");
            for (int i = m_targetRoots.Count - 1; i >= 0; i--)
            {
                var target = m_targetRoots[i];
                if (target is Sandbox.Game.Entities.Debris.MyDebrisBase ||
                    target is MyFloatingObject ||
                    (target.Physics != null && !target.Physics.Enabled) ||
                    target.GetTopMostParent().Physics == null || !target.GetTopMostParent().Physics.Enabled)
                {
                    m_targetRoots.RemoveAtFast(i);
                }
            }
            ProfilerShort.End();
            ProfilerShort.End();
        }

        private static bool IsSameOrSubclass(Type potentialBase, Type potentialDescendant)
        {
            return potentialDescendant.IsSubclassOf(potentialBase)
                   || potentialDescendant == potentialBase;
        }


        public override string ComponentTypeDebugString
        {
            get { return "MyGridTargeting"; }
        }
    }
}
