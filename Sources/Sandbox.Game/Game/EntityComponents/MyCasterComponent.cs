using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons.Guns;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.Game.WorldEnvironment;
using Sandbox.Game.World;

namespace Sandbox.Game.EntityComponents
{

    /// <summary>
    /// Component used for finding object by casting. It is possible to create this component with different types of casting:
    /// Box, Raycast, Shape
    /// </summary>
    public class MyCasterComponent : MyEntityComponentBase 
    {


        /// <summary>
        /// Indicates block that is hit by raycast.
        /// </summary>
        private MySlimBlock m_hitBlock;

        /// <summary>
        /// Indicates grid that was hit by raycast.
        /// </summary>
        private MyCubeGrid m_hitCubeGrid;

        /// <summary>
        /// Indicates character that was hit by raycast.
        /// </summary>
        private MyCharacter m_hitCharacter;

        /// <summary>
        /// Indicates destroyable object that was hit by raycast
        /// </summary>
        private IMyDestroyableObject m_hitDestroaybleObj;

        /// <summary>
        /// Indicates floating object that was hit by raycast.
        /// </summary>
        private MyFloatingObject m_hitFloatingObject;

        /// <summary>
        /// Indicates Environment Sector hit.
        /// </summary>
        private MyEnvironmentSector m_hitEnvironmentSector;

        /// <summary>
        /// Indicates Specific item of Environment Sector hit.
        /// </summary>
        private int m_environmentItem;

        /// <summary>
        /// Indicates exact hit position of raycast.
        /// </summary>
        private Vector3D m_hitPosition;

        /// <summary>
        /// Indicates distance to block that is hit by raycast.
        /// </summary>
        private double m_distanceToHitSq;

        /// <summary>
        /// Raycaster used for finding hit block.
        /// </summary>
        private MyDrillSensorBase m_caster;

        /// <summary>
        /// Point of reference to which closest object is found.
        /// </summary>
        private Vector3D m_pointOfReference;

        /// <summary>
        /// Indicates if point of reference is set.
        /// </summary>
        private bool m_isPointOfRefSet = false;

        public MyCasterComponent(MyDrillSensorBase caster)
        {
            m_caster = caster;
        }

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);
        }
       
        public void OnWorldPosChanged(ref MatrixD newTransform)
        {

            MatrixD worldPos = newTransform;
            m_caster.OnWorldPositionChanged(ref worldPos);

            var entitiesInRange = this.m_caster.EntitiesInRange;
            float closestDistance = float.MaxValue;
            MyEntity closestEntity = null;
            int itemId = 0;

            if (!m_isPointOfRefSet)
                m_pointOfReference = worldPos.Translation;

            if (entitiesInRange != null && entitiesInRange.Count > 0)
            {
               // int i = 0;
                foreach (var entity in entitiesInRange.Values)
                {
                    float distanceSq = (float)Vector3D.DistanceSquared(entity.DetectionPoint, m_pointOfReference);

                    if (entity.Entity.Physics != null && entity.Entity.Physics.Enabled)
                    {
                        if (distanceSq < closestDistance)
                        {
                            closestEntity = entity.Entity;
                            itemId = entity.ItemId;
                            this.m_distanceToHitSq = distanceSq;
                            this.m_hitPosition = entity.DetectionPoint;

                            closestDistance = distanceSq;
                        }
                    }
                 //   ++i;
                }
            }

            this.m_hitCubeGrid = closestEntity as MyCubeGrid;
            this.m_hitBlock = null;
            this.m_hitDestroaybleObj = closestEntity as IMyDestroyableObject;
            this.m_hitFloatingObject = closestEntity as MyFloatingObject;
            this.m_hitCharacter = closestEntity as MyCharacter;
            this.m_hitEnvironmentSector = closestEntity as MyEnvironmentSector;
            this.m_environmentItem = itemId;

            if (m_hitCubeGrid != null)
            {
                var invWorld = m_hitCubeGrid.PositionComp.WorldMatrixNormalizedInv;
                var gridLocalPos = Vector3D.Transform(this.m_hitPosition, invWorld);
                Vector3I blockPos;
                m_hitCubeGrid.FixTargetCube(out blockPos, gridLocalPos / m_hitCubeGrid.GridSize);
                m_hitBlock = m_hitCubeGrid.GetCubeBlock(blockPos);

            }

            
        }

        public void SetPointOfReference(Vector3D pointOfRef)
        {
            this.m_pointOfReference = pointOfRef;
            this.m_isPointOfRefSet = true;
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            //this.Entity.PositionComp.OnPositionChanged += OnEntityPosChange;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            //this.Entity.PositionComp.OnPositionChanged -= OnEntityPosChange;

            base.OnBeforeRemovedFromContainer();
        }

        public override string ComponentTypeDebugString
        {
            get { return "MyBlockInfoComponent"; }
        }

        /// <summary>
        /// Gets block that is hit by a raycaster.
        /// </summary>
        public MySlimBlock HitBlock { get { return this.m_hitBlock; } }

        public MyCubeGrid HitCubeGrid { get { return this.m_hitCubeGrid; } }

        public Vector3D HitPosition { get { return this.m_hitPosition; } }

        public IMyDestroyableObject HitDestroyableObj { get { return this.m_hitDestroaybleObj; } }

        public MyFloatingObject HitFloatingObject { get { return this.m_hitFloatingObject; } }

        public MyEnvironmentSector HitEnvironmentSector { get { return this.m_hitEnvironmentSector; } }

        public int EnvironmentItem { get { return this.m_environmentItem; } }

        public MyCharacter HitCharacter { get { return this.m_hitCharacter; } }

        public double DistanceToHitSq { get { return this.m_distanceToHitSq; } }

        public Vector3D PointOfReference { get { return this.m_pointOfReference; } }

        public MyDrillSensorBase Caster { get { return this.m_caster; } }

    }
}
