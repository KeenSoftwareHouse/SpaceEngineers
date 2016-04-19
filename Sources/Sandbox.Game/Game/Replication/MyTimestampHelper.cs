using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.Replication
{
    public struct MyTimeStampValues
    {
        public long EntityId;
        public MyTransformD Transform;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
    }

    public class MyTimestampHelper
    {
        public const double POSITION_TOLERANCE = 0.02;
        public const uint MAX_POSHISTORY = 255;

        SortedDictionary<uint, MyTimeStampValues> m_timeStampData;
        MyEntity m_entity;
        uint m_lastTSFromServer = 0;
        uint m_currentTimestamp = 0;

        public void SetEntity(MyEntity entity)
        {
            m_entity = entity;
        }

        public MyTimestampHelper(MyEntity entity)
        {
           SetEntity(entity);
        }

        public void Update(uint timeStamp)
        {
            if (m_timeStampData == null)
            {
                m_timeStampData = new SortedDictionary<uint, MyTimeStampValues>();
            }

            if(m_entity == null|| m_entity.Physics == null)
            {
                m_timeStampData.Remove(timeStamp);
                return;
            }
    
            var matrix = m_entity.WorldMatrix;

            m_timeStampData[timeStamp] = new MyTimeStampValues()
            {
                EntityId = m_entity.EntityId,
                Transform = new MyTransformD(matrix),
                LinearVelocity = m_entity.Physics.LinearVelocity,
                AngularVelocity = m_entity.Physics.AngularVelocity,
            };

            if (m_timeStampData.Count >= MAX_POSHISTORY)
            {
                m_timeStampData.Remove(m_timeStampData.First().Key);
            }

            m_currentTimestamp = timeStamp;
        }

        public MyTimeStampValues? GetTransform(uint timeStamp)
        {
            MyTimeStampValues transform;
            if (m_timeStampData.TryGetValue(timeStamp, out transform) == false)
            {
                return null;
            }
            return transform;          
        }

        public void ServerResponse(uint timeStamp, ref MyTimeStampValues serverPositionAndOrientation)
        {
            if (timeStamp < m_lastTSFromServer)
                return;      

            if (m_timeStampData.ContainsKey(timeStamp) == false)
            {
                m_entity.PositionComp.SetWorldMatrix(serverPositionAndOrientation.Transform.TransformMatrix, null, true);
                return;
            }

            MyTimeStampValues cachedData = m_timeStampData[timeStamp];
      
            MatrixD worldMatrix = m_entity.PositionComp.WorldMatrix;

            MyTimeStampValues delta = new MyTimeStampValues();

            delta.Transform.Position = serverPositionAndOrientation.Transform.Position - cachedData.Transform.Position;

            double deltaL = delta.Transform.Position.Length();

            MyCharacter character = (m_entity as MyCharacter);

            cachedData.Transform.Rotation = Quaternion.Inverse(cachedData.Transform.Rotation);
            Quaternion.Multiply(ref serverPositionAndOrientation.Transform.Rotation, ref cachedData.Transform.Rotation, out delta.Transform.Rotation);
            if (deltaL < (MyGridPhysics.ShipMaxLinearVelocity()/ (60f * Sync.RelativeSimulationRatio)))
            {
                delta.Transform.Position = delta.Transform.Position * 0.2;
               
            }
            delta.Transform.Rotation = Quaternion.Slerp(delta.Transform.Rotation, Quaternion.Identity, 0.2f);
            Vector3D position = worldMatrix.Translation;

            position += delta.Transform.Position;

            delta.LinearVelocity = serverPositionAndOrientation.LinearVelocity -cachedData.LinearVelocity;

            delta.AngularVelocity = serverPositionAndOrientation.AngularVelocity - cachedData.AngularVelocity;

            double deltaVelocity = delta.LinearVelocity.LengthSquared();

            if (deltaVelocity > 0.1 * 0.1)
            {
                m_entity.Physics.LinearVelocity += delta.LinearVelocity;
            }

            deltaVelocity = delta.AngularVelocity.LengthSquared();

            if (deltaVelocity > 0.01 * 0.01)
            {
                m_entity.Physics.AngularVelocity += delta.AngularVelocity;
            } 

            Quaternion orientation = Quaternion.CreateFromForwardUp(worldMatrix.Forward, worldMatrix.Up);


            Quaternion normalized = cachedData.Transform.Rotation;
            normalized.Normalize();
            cachedData.Transform.Rotation = normalized;
            normalized = serverPositionAndOrientation.Transform.Rotation;
            normalized.Normalize();
            serverPositionAndOrientation.Transform.Rotation = normalized;

            double eps = 0.001;
            if (Math.Abs(Quaternion.Dot(serverPositionAndOrientation.Transform.Rotation, cachedData.Transform.Rotation)) < 1 - eps)
            {
                Quaternion.Multiply(ref delta.Transform.Rotation, ref orientation, out orientation);
                MatrixD matrix = MatrixD.CreateFromQuaternion(orientation);
                MatrixD currentMatrix = m_entity.PositionComp.WorldMatrix;
                Vector3D translation = currentMatrix.Translation;
                currentMatrix.Translation = Vector3D.Zero;
                if (currentMatrix.EqualsFast(ref matrix, 0.01) == false)
                {
                    matrix.Translation = translation;
                    m_entity.PositionComp.SetWorldMatrix(matrix, null, true);
                }
            }


            if (deltaL > (MyGridPhysics.ShipMaxLinearVelocity()/ (60f * Sync.RelativeSimulationRatio)))
            {
                m_entity.PositionComp.SetPosition(serverPositionAndOrientation.Transform.Position);
            }
            else if (deltaL > POSITION_TOLERANCE)
            {
                if (m_entity is MyCharacter)
                {
                    (m_entity as MyCharacter).CacheMoveDelta(ref delta.Transform.Position);
                }
                else
                {
                    m_entity.PositionComp.SetPosition(position);
                }
            }
           

            UpdateDeltaPosition(timeStamp, ref delta);

            m_lastTSFromServer = timeStamp;    
        }

        public void UpdateDeltaPosition(uint timestamp, ref MyTimeStampValues data)
        {
            if (m_timeStampData.Count <= 0)
                return;

            for (uint i = timestamp; i <= m_currentTimestamp; ++i)
            {
                UpdateData(i, ref data);
            }
        }

        public void OverwriteServerPosition(uint timestamp, ref MyTimeStampValues data)
        {
            if (m_timeStampData.Count <= 0)
                return;

            for (uint i = timestamp; i <= m_currentTimestamp; ++i)
            {
                OverWriteData(i, ref data);
            }
        }

        void OverWriteData(uint i, ref MyTimeStampValues delta)
        {
            if (m_timeStampData.ContainsKey(i))
            {
                MyTimeStampValues cachedData = m_timeStampData[i];

                cachedData.LinearVelocity = delta.LinearVelocity;
                cachedData.Transform.Position = delta.Transform.Position;
                m_timeStampData[i] = cachedData;
            }
        }

        void UpdateData(uint i, ref MyTimeStampValues delta)
        {
            if (m_timeStampData.ContainsKey(i))
            {
                MyTimeStampValues cachedData = m_timeStampData[i];

                cachedData.AngularVelocity += delta.AngularVelocity;
                cachedData.LinearVelocity += delta.LinearVelocity;
                cachedData.Transform.Position += delta.Transform.Position;
                cachedData.Transform.Position += delta.LinearVelocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                Quaternion.Multiply(ref delta.Transform.Rotation, ref cachedData.Transform.Rotation, out cachedData.Transform.Rotation);
                m_timeStampData[i] = cachedData;
            }
        }

        public void Clear()
        {
            m_timeStampData.Clear();
            m_currentTimestamp = 0;
        }

    }
}
