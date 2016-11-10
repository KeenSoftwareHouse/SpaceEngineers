using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.WorldEnvironment.Definitions;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.World;
using System;
using Havok;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Debris;
using VRageRender;
using VRage.Import;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using VRage.Profiler;
using VRage.Serialization;

namespace Sandbox.Game.WorldEnvironment.Modules
{

    public class MyBreakableEnvironmentProxy : IMyEnvironmentModuleProxy
    {
        private const int BrokenItemLifeSpan = 20 * 1000;

        public void Init(MyEnvironmentSector sector, List<int> items)
        {
            m_sector = sector;

            if (Sync.IsServer)
                m_sector.OnContactPoint += SectorOnContactPoint;
        }

        private void SectorOnContactPoint(int itemId, MyEntity other, ref MyPhysics.MyContactPointEvent e)
        {
            // if item is already disabled: puff
            // We get multiple contact points so this is for that
            if (m_sector.DataView.Items[itemId].ModelIndex < 0) return;

            var vel = Math.Abs(e.ContactPointEvent.SeparatingVelocity);

            if (other == null || other.Physics == null || other is MyFloatingObject) return;

            if (other is IMyHandheldGunObject<MyDeviceBase>) return;

            // Prevent debris from breaking trees.
            // Debris flies in unpredictable ways and this could cause out of sync tree destruction which would is bad.
            if (other.Physics.RigidBody != null && other.Physics.RigidBody.Layer == MyPhysics.CollisionLayers.DebrisCollisionLayer) return;

            // On objects held in manipulation tool, Havok returns high velocities, after this contact is fired by contraint solver.
            // Therefore we disable damage from objects connected by constraint to character
            if (MyManipulationTool.IsEntityManipulated(other))
                return;

            float otherMass = MyDestructionHelper.MassFromHavok(other.Physics.Mass);

            double impactEnergy = vel * vel * otherMass;

            // TODO: per item max impact energy
            if (impactEnergy > ItemResilience(itemId))
            {
                BreakAt(itemId, e.Position, e.ContactPointEvent.ContactPoint.Normal, impactEnergy);
            }

            // Meteor destroy always
            if (other is MyMeteor)
                m_sector.EnableItem(itemId, false);
        }

        /// <summary>
        /// Break item at specified id of Environment Sector
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="hitpos"></param>
        /// <param name="hitnormal"></param>
        /// <param name="impactEnergy"></param>
        public void BreakAt(int itemId, Vector3D hitpos, Vector3D hitnormal, double impactEnergy)
        {
            impactEnergy = MathHelper.Clamp(impactEnergy, 0, ItemResilience(itemId) * 10);

            Impact impact = new Impact(hitpos, hitnormal, impactEnergy);

            m_sector.RaiseItemEvent(this, itemId, impact);
            DisableItemAndCreateDebris(ref impact, itemId);
        }

        [Serializable]
        private struct Impact
        {
            public Vector3D Position;
            public Vector3D Normal;
            public double Energy;

            public Impact(Vector3D position, Vector3D normal, double energy)
            {
                Position = position;
                Normal = normal;
                Energy = energy;
            }
        }

        private void DisableItemAndCreateDebris(ref Impact imp, int itemId)
        {
            if (m_sector.DataView.Items[itemId].ModelIndex < 0) return;

            // Create particle
            MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.DestructionTree, out effect))
            {
                effect.WorldMatrix = MatrixD.CreateTranslation(imp.Position);//, (Vector3D)normal, Vector3D.CalculatePerpendicularVector(normal));
            }

            // Spawn no debris for stuff far away
            if (m_sector.LodLevel <= 1)
            {
                // Spawn debris
                var debris = CreateDebris(itemId);
                if (debris != null)
                {
                    var treeMass = debris.Physics.Mass;

                    const float ENERGY_PRESERVATION = .8f;

                    // Tree final velocity
                    float velTree = (float)Math.Sqrt(imp.Energy / treeMass);
                    float accell = velTree / (MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED) * ENERGY_PRESERVATION;
                    var force = accell * -imp.Normal;

                    var pos = debris.Physics.CenterOfMassWorld + 0.5f * Vector3D.Dot(imp.Position - debris.Physics.CenterOfMassWorld, debris.WorldMatrix.Up) * debris.WorldMatrix.Up;
                    debris.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, force, pos, null);
                    Debug.Assert(debris.GetPhysicsBody().HavokWorld.ActiveRigidBodies.Contains(debris.Physics.RigidBody));
                }
            }

            // Disable item last because debris needs definition and whatnots
            m_sector.EnableItem(itemId, false);
        }

        private MyEntity CreateDebris(int itemId)
        {
            ProfilerShort.Begin("Spawning tree");
            var itemData = m_sector.DataView.Items[itemId];

            Vector3D worldPos = itemData.Position + m_sector.SectorCenter;

            var model = m_sector.Owner.GetModelForId(itemData.ModelIndex);

            var brokenModel = model.Model.Insert(model.Model.Length - 4, "_broken");
            MyEntity debris;
            bool hasBrokenModel = false;

            if (MyModels.GetModelOnlyData(brokenModel) != null)
            {
                hasBrokenModel = true;
                debris = MyDebris.Static.CreateDebris(brokenModel);
            }
            else
                debris = MyDebris.Static.CreateDebris(model.Model);

            var debrisLogic = (MyDebrisBase.MyDebrisBaseLogic)debris.GameLogic;
            debrisLogic.RandomScale = 1;
            debrisLogic.LifespanInMiliseconds = BrokenItemLifeSpan;
            var m = MatrixD.CreateFromQuaternion(itemData.Rotation);
            m.Translation = worldPos + m.Up * (hasBrokenModel ? 0 : 5);
            debrisLogic.Start(m, Vector3.Zero, 1, false);
            ProfilerShort.End();
            return debris;
        }

        private double ItemResilience(int itemId)
        {
            return 200000;
        }

        public void Close()
        {
            m_sector.OnContactPoint -= SectorOnContactPoint;
        }

        public void CommitLodChange(int lodBefore, int lodAfter)
        {
        }

        public void CommitPhysicsChange(bool enabled)
        {
        }

        public void OnItemChange(int item, short newModel)
        {
        }

        public void OnItemChangeBatch(List<int> items, int offset, short newModel)
        {
        }

        public void HandleSyncEvent(int item, object data, bool fromClient)
        {
            Debug.Assert(data is Impact);
            var imp = (Impact)data;

            DisableItemAndCreateDebris(ref imp, item);
        }

        public void DebugDraw()
        {}

        #region Private

        private MyEnvironmentSector m_sector;

        // this is here for debug stuff for now
        public long SectorId
        {
            get { return m_sector.SectorId; }
        }

        #endregion
    }
}
