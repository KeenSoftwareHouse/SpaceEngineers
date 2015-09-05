using Havok;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Physics;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    public struct MyGridContactInfo
    {
        [Flags]
        enum ContactFlags
        {
            Known = 0x01,
            //HandleBlockA = 0x02, // BlockA wants handling in future
            //HandleBlockB = 0x04, // BlockB wants handling in future
            Deformation = 0x08, // Won't do deformations
            Particles = 0x10, // Won't do particle effects (sparks, smoke...)
            RubberDeformation = 0x20, // Will do rubber deformation (higher threshold, lower damage)
        }

        public readonly HkContactPointEvent Event;
        public readonly Vector3D ContactPosition;
        public MyCubeGrid m_currentEntity;
        public MyEntity m_collidingEntity;
        private MySlimBlock m_currentBlock;
        private MySlimBlock m_otherBlock;

        public MyCubeGrid CurrentEntity { get { return m_currentEntity; } }
        public MyEntity CollidingEntity { get { return m_collidingEntity; } }
        public MySlimBlock OtherBlock { get { return m_otherBlock; } }

        ContactFlags Flags
        {
            get { return (ContactFlags)Event.ContactProperties.UserData.AsUint; }
            set
            {
                var properties = Event.ContactProperties;
                properties.UserData = HkContactUserData.UInt((uint)value);
            }
        }

        public bool EnableDeformation
        {
            get { return (Flags & ContactFlags.Deformation) != 0; }
            set { SetFlag(ContactFlags.Deformation, value); }
        }

        public bool RubberDeformation
        {
            get { return (Flags & ContactFlags.RubberDeformation) != 0; }
            set { SetFlag(ContactFlags.RubberDeformation, value); }
        }

        public bool EnableParticles
        {
            get { return (Flags & ContactFlags.Particles) != 0; }
            set { SetFlag(ContactFlags.Particles, value); }
        }

        public float ImpulseMultiplier;

        public MyGridContactInfo(ref HkContactPointEvent evnt, MyCubeGrid grid)
        {
            Event = evnt;
            ContactPosition = grid.Physics.ClusterToWorld(evnt.ContactPoint.Position); 
            m_currentEntity = grid;
            m_collidingEntity = Event.GetOtherEntity(grid) as MyEntity;
            m_currentBlock = null;
            m_otherBlock = null;
            ImpulseMultiplier = 1;
        }

        //public void RegisterForSubsequentCallbacks()
        //{
        //    SetFlag(ThisEntity == Event.Base.BodyA.GetEntity() ? ContactFlags.HandleBlockA : ContactFlags.HandleBlockB, true);
        //}

        //public void UnregisterForSubsequentCallbacks()
        //{
        //    SetFlag(ThisEntity == Event.Base.BodyA.GetEntity() ? ContactFlags.HandleBlockA : ContactFlags.HandleBlockB, false);
        //}

        public bool IsKnown
        {
            get
            {
                var flags = Flags;
                return ((flags & ContactFlags.Known) != 0);
            }
        }

        public void HandleEvents()
        {
            var flags = Flags;
            if ((flags & ContactFlags.Known) == 0)
            {
                Flags |= ContactFlags.Particles | ContactFlags.Deformation | ContactFlags.Known;

                m_currentBlock = GetContactBlock(CurrentEntity, ContactPosition, Event.ContactPoint.NormalAndDistance.W);
                var collidingGrid = CollidingEntity as MyCubeGrid;
                if (collidingGrid != null)
                {
                    m_otherBlock = GetContactBlock(collidingGrid, ContactPosition, Event.ContactPoint.NormalAndDistance.W);
                }

                if (m_currentBlock != null && m_currentBlock.FatBlock != null)
                {
                    m_currentBlock.FatBlock.ContactPointCallback(ref this);
                    ImpulseMultiplier *= m_currentBlock.BlockDefinition.PhysicalMaterial.CollisionMultiplier;
                }

                if (m_otherBlock != null && m_otherBlock.FatBlock != null)
                {
                    SwapEntities();
                    ImpulseMultiplier *= m_currentBlock.BlockDefinition.PhysicalMaterial.CollisionMultiplier;
                    m_currentBlock.FatBlock.ContactPointCallback(ref this);
                    SwapEntities();
                }
            }
        }

        void SetFlag(ContactFlags flag, bool value)
        {
            Flags = value ? (Flags | flag) : (Flags & ~flag);
        }

        void SwapEntities()
        {
            var x = m_currentEntity;
            m_currentEntity = (MyCubeGrid)m_collidingEntity;
            m_collidingEntity = x;
            var y = m_currentBlock;
            m_currentBlock = m_otherBlock;
            m_otherBlock = y;
        }

        static MySlimBlock GetContactBlock(MyCubeGrid grid, Vector3D worldPosition, float graceDistance)
        {
            graceDistance = Math.Max(Math.Abs(graceDistance), grid.GridSize * 0.2f);
            graceDistance += 1f;
            MatrixD invWorld = grid.PositionComp.GetWorldMatrixNormalizedInv();
            Vector3D localVelocity = Vector3D.TransformNormal(grid.Physics.LinearVelocity * Sandbox.Common.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, invWorld);
            Vector3D localPos;
            Vector3D.Transform(ref worldPosition, ref invWorld, out localPos);

            var min1 = Vector3I.Round((localPos - graceDistance - localVelocity) / grid.GridSize);
            var max1 = Vector3I.Round((localPos + graceDistance + localVelocity) / grid.GridSize);
            var min2 = Vector3I.Round((localPos + graceDistance - localVelocity) / grid.GridSize);
            var max2 = Vector3I.Round((localPos - graceDistance + localVelocity) / grid.GridSize);

            Vector3I min = Vector3I.Min(Vector3I.Min(Vector3I.Min(min1, max1), min2), max2);
            Vector3I max = Vector3I.Max(Vector3I.Max(Vector3I.Max(min1, max1), min2), max2);

            MySlimBlock resultBlock = null;
            float distSq = float.MaxValue;

            // TODO: optimize this, it should be possible using normal from contact
            Vector3I pos;
            for (pos.X = min.X; pos.X <= max.X; pos.X++)
            {
                for (pos.Y = min.Y; pos.Y <= max.Y; pos.Y++)
                {
                    for (pos.Z = min.Z; pos.Z <= max.Z; pos.Z++)
                    {
                        var block = grid.GetCubeBlock(pos);
                        if (block != null)
                        {
                            var testDistSq = (float)(pos * grid.GridSize - localPos).LengthSquared();
                            if (testDistSq < distSq)
                            {
                                distSq = testDistSq;
                                resultBlock = block;
                            }
                        }
                    }
                }
            }

            return resultBlock;
        }
    }
}
