#region Using

using System.Diagnostics;
using Sandbox.Engine.Physics;
using VRageMath;

using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using VRage.Utils;
using System.Linq;
using System.Collections.Generic;

using VRageRender;
using Sandbox.AppCode.Game;
using Sandbox.Game.Utils;
using Sandbox.Engine.Models;
using Havok;
using Sandbox.Graphics;
using Sandbox.Common;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.Game.Entities.Character;
using VRage;
using Sandbox.Common.Components;
using VRageMath.Spatial;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Components;

#endregion

namespace Sandbox.Engine.Physics
{
    public class MyControlledPhysicsBody : MyPhysicsBody
    {
        public MyControlledPhysicsBody(IMyEntity entity, RigidBodyFlag flags) 
            : base(entity, flags) 
        {
        }

        public override void OnWorldPositionChanged(object source)
        {
            // Do nothing
        }

        public override void Activate(object world, ulong clusterObjectID)
        {
            System.Diagnostics.Debug.Assert(m_world == null, "Cannot activate already active object!");
            System.Diagnostics.Debug.Assert(!IsInWorld, "Cannot activate already active object!");

            m_world = (HkWorld)world;
            ClusterObjectID = clusterObjectID;

            IsInWorld = true;

            Matrix rigidBodyMatrix = GetRigidBodyTransform();

            if (BreakableBody != null)
            {
                RigidBody.SetWorldMatrix(rigidBodyMatrix);
                m_world.DestructionWorld.AddBreakableBody(BreakableBody);
            }
            else if (RigidBody != null)
            {
                RigidBody.SetWorldMatrix(rigidBodyMatrix);
                m_world.AddRigidBody(RigidBody);
            }
            if (RigidBody2 != null)
            {
                RigidBody2.SetWorldMatrix(rigidBodyMatrix);
                m_world.AddRigidBody(RigidBody2);
            }

            if (CharacterProxy != null)
            {
                CharacterProxy.SetRigidBodyTransform(rigidBodyMatrix);
                CharacterProxy.Activate(m_world);
            }

            foreach (var constraint in m_constraints)
            {
                m_world.AddConstraint(constraint);
            }
        }

        public override void ActivateBatch(object world, ulong clusterObjectID)
        {
            System.Diagnostics.Debug.Assert(m_world == null, "Cannot activate already active object!");

            m_world = (HkWorld)world;
            ClusterObjectID = clusterObjectID;
            IsInWorld = true;

            Matrix rigidBodyMatrix = GetRigidBodyTransform();

            if (RigidBody != null)
            {
                RigidBody.SetWorldMatrix(rigidBodyMatrix);
                m_world.AddRigidBodyBatch(RigidBody);
            }
            if (RigidBody2 != null)
            {
                RigidBody2.SetWorldMatrix(rigidBodyMatrix);
                m_world.AddRigidBodyBatch(RigidBody2);
            }

            if (CharacterProxy != null)
            {
                CharacterProxy.SetRigidBodyTransform(rigidBodyMatrix);
                CharacterProxy.Activate(m_world);
            }

            foreach (var constraint in m_constraints)
            {
                m_constraintsAddBatch.Add(constraint);
            }
        }


        internal void SetRigidBodyTransform(MatrixD worldMatrix)
        {
            Matrix rigidBodyMatrix = GetRigidBodyMatrix(worldMatrix);

            if (RigidBody != null)
            {
                RigidBody.SetWorldMatrix(rigidBodyMatrix);
            }

            Matrix m = RigidBody.GetRigidBodyMatrix();
            Matrix wM = GetWorldMatrix();

            if (RigidBody2 != null)
            {
                RigidBody2.SetWorldMatrix(rigidBodyMatrix);
            }

            if (CharacterProxy != null)
            {
                CharacterProxy.Position = rigidBodyMatrix.Translation;
                CharacterProxy.Forward = rigidBodyMatrix.Forward;
                CharacterProxy.Up = rigidBodyMatrix.Up;
                CharacterProxy.Speed = 0;

                if (CharacterProxy.ImmediateSetWorldTransform)
                {
                    CharacterProxy.SetRigidBodyTransform(rigidBodyMatrix);
                }
            }
        }

        internal Matrix GetRigidBodyTransform()
        {
            Matrix transform = Matrix.Identity;
            if (RigidBody != null)
            {
                transform = RigidBody.GetRigidBodyMatrix();
            }

            return transform;
        }


    }
}
