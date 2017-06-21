using System;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRageMath;

namespace Sandbox.Game.Replication.History
{
    public struct MySnapshot
    {
        public bool Active;
        public Vector3D Position;
        public Quaternion Rotation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;

        public MySnapshot(BitStream stream):this()
        {
            Read(stream);
        }
        public MySnapshot(MyEntity entity)
        {
            Position = entity.WorldMatrix.Translation;
            Rotation = Quaternion.CreateFromRotationMatrix(entity.WorldMatrix);
            if (entity.Physics != null)
            {
                Active = entity.Physics.RigidBody == null || entity.Physics.RigidBody.IsActive;
                LinearVelocity = entity.Physics.LinearVelocity;
                AngularVelocity = entity.Physics.AngularVelocity;
            }
            else
            {
                Active = true;
                LinearVelocity = Vector3.Zero;
                AngularVelocity = Vector3.Zero;
            }
        }

        public MySnapshot Diff(MySnapshot value0)
        {
            return new MySnapshot()
            {
                Active = this.Active,
                Position = this.Position - value0.Position,
                Rotation = Quaternion.Inverse(this.Rotation) * value0.Rotation,
                LinearVelocity = this.LinearVelocity - value0.LinearVelocity,
                AngularVelocity = this.AngularVelocity - value0.AngularVelocity
            };
        }

        public void Scale(float factor)
        {
            ScaleTransform(factor);

            LinearVelocity *= factor;
            AngularVelocity *= factor;
        }

        public void ScaleTransform(float factor)
        {
            Vector3 axis;
            float angle;
            Rotation.GetAxisAngle(out axis, out angle);
            angle *= factor;
            Rotation = Quaternion.CreateFromAxisAngle(axis, angle);

            Position *= factor;
        }

        public bool CheckThresholds(float posSq, float rotSq, float linearSq, float angularSq)
        {
            return Position.LengthSquared() > posSq ||
                   Math.Abs(Rotation.W - 1) > rotSq ||
                   LinearVelocity.LengthSquared() > linearSq ||
                   AngularVelocity.LengthSquared() > angularSq;
        }

        public void Add(MySnapshot value)
        {
            Active = value.Active;
            Position += value.Position;
            Rotation = Rotation * Quaternion.Inverse(value.Rotation); Rotation.Normalize();
            LinearVelocity += value.LinearVelocity;
            AngularVelocity += value.AngularVelocity;
        }

        public void Apply(MyEntity entity, bool applyRotation = true, bool applyPhysics = true, bool reset = false)
        {
            VRage.Profiler.ProfilerShort.Begin("Matrix calc");
            var oldMat = entity.WorldMatrix;
            MatrixD mat = oldMat;
            if (applyRotation)
                mat = MatrixD.CreateFromQuaternion(Rotation);
            else 
                mat = oldMat;
            mat.Translation = Position;
            VRage.Profiler.ProfilerShort.End();

            VRage.Profiler.ProfilerShort.Begin("WorldMatrix update");
            if (Sandbox.Engine.Utils.MyFakes.MULTIPLAYER_CLIENT_PHYSICS)
            {
                entity.SetWorldMatrix(mat, true, reset);
                entity.m_positionResetFromServer = reset;
            }
            else
                entity.Physics.ServerWorldMatrix = mat;
            VRage.Profiler.ProfilerShort.End();

            VRage.Profiler.ProfilerShort.Begin("Physics update");
            if (applyPhysics && entity.Physics != null)
            {
                if (entity.Physics.LinearVelocity != LinearVelocity)
                    entity.Physics.LinearVelocity = LinearVelocity;
                if (applyRotation && entity.Physics.AngularVelocity != AngularVelocity)
                    entity.Physics.AngularVelocity = AngularVelocity;
                var rb = entity.Physics.RigidBody;
                if (rb != null && rb.IsActive != Active)
                {
                    if (Active)
                        rb.Activate();
                    else
                        rb.Deactivate();
                }
            }
            VRage.Profiler.ProfilerShort.End();
        }

        public MySnapshot Lerp(MySnapshot value2, float factor)
        {
            return new MySnapshot()
            {
                Active = this.Active || value2.Active,
                Position = Vector3D.Lerp(this.Position, value2.Position, factor),
                Rotation = Quaternion.Slerp(this.Rotation, value2.Rotation, factor),
                LinearVelocity = Vector3.Lerp(this.LinearVelocity, value2.LinearVelocity, factor),
                AngularVelocity = Vector3.Lerp(this.AngularVelocity, value2.AngularVelocity, factor)
            };
        }
        public void Write(BitStream stream)
        {
            stream.WriteBool(Active);
            stream.Write(Position);
            //stream.WriteQuaternionNormCompressedIdentity(Rotation);
            stream.WriteQuaternionNorm(Rotation);
            if (Active)
            {
                stream.Write(LinearVelocity);
                stream.Write(AngularVelocity);
            }
        }
        public void Read(BitStream stream)
        {
            Active = stream.ReadBool(); // 1b
            Position = stream.ReadVector3D(); // 192b
            //Rotation = stream.ReadQuaternionNormCompressedIdentity(); // 30b
            Rotation = stream.ReadQuaternionNorm(); // 52b
            if (Active)
            {
                LinearVelocity = stream.ReadVector3(); // 96b
                AngularVelocity = stream.ReadVector3(); // 96b
            }
            else
            {
                LinearVelocity = Vector3.Zero;
                AngularVelocity = Vector3.Zero;
            }
        }

        public override string ToString()
        {
            return " pos " + Position.ToString("N3") + " linVel " + LinearVelocity.ToString("N3");
        }
    }
}
