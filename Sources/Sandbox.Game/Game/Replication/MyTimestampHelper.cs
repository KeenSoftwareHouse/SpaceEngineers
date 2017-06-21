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
using VRage.Utils;

namespace Sandbox.Game.Replication
{
    public struct MyTimeStampValues
    {
        public uint Timestamp;
        public long EntityId;
        public MyTransformD Transform;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
    }

    public class MyTimestampHelper
    {
        public const double POSITION_TOLERANCE = 1;
        public const uint MAX_POSHISTORY = 255;

        readonly SortedDictionary<uint, MyTimeStampValues> m_timeStampData = new SortedDictionary<uint, MyTimeStampValues>();
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

        public void ServerResponse(uint timeStamp, ref MyTransformD serverPositionAndOrientation)
        {
            if (timeStamp < m_lastTSFromServer)
                return;

            if (m_timeStampData.ContainsKey(timeStamp) == false)
            {
                m_entity.PositionComp.SetWorldMatrix(serverPositionAndOrientation.TransformMatrix, null, true);
                return;
            }

            MyTimeStampValues cachedData = m_timeStampData[timeStamp];

            var mat = m_entity.WorldMatrix;
            MyTransformD delta = UpdateValues(m_entity, ref serverPositionAndOrientation, ref cachedData);
            mat.Translation = serverPositionAndOrientation.Position;
            m_entity.PositionComp.SetWorldMatrix(mat, null, true);

            UpdateDeltaPosition(timeStamp, ref delta);

            m_lastTSFromServer = timeStamp;
        }

        MyTransformD UpdateValues(MyEntity entity, ref MyTransformD serverPositionAndOrientation, ref MyTimeStampValues cachedData)
        {

            MyTransformD delta = new MyTransformD();

            delta.Position = serverPositionAndOrientation.Position - cachedData.Transform.Position;

            cachedData.Transform.Rotation = Quaternion.Inverse(cachedData.Transform.Rotation);
            Quaternion.Multiply(ref serverPositionAndOrientation.Rotation, ref cachedData.Transform.Rotation, out delta.Rotation);
            delta.Rotation = Quaternion.Identity;

            MatrixD matrix = entity.WorldMatrix;
            matrix.Translation = Vector3D.Zero;
            MatrixD correctionMatrix = MatrixD.Multiply(matrix, delta.TransformMatrix);
            correctionMatrix.Translation += entity.WorldMatrix.Translation;
            entity.PositionComp.SetWorldMatrix(correctionMatrix, null, true);

            return delta;
        }

        public void UpdateDeltaVelocities(uint timestamp, ref Vector3 deltaLinear, ref Vector3 deltaAngular)
        {
            if (m_timeStampData.Count <= 0)
                return;

            for (uint i = timestamp; i <= m_currentTimestamp; ++i)
            {
                UpdateData(i, ref deltaLinear, ref deltaAngular);
            }
        }

        public void UpdateDeltaPosition(uint timestamp, ref MyTransformD data)
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

        void UpdateData(uint i, ref MyTransformD delta)
        {
            if (m_timeStampData.ContainsKey(i))
            {
                MyTimeStampValues cachedData = m_timeStampData[i];

                cachedData.Transform.Position += delta.Position;
                Quaternion.Multiply(ref delta.Rotation, ref cachedData.Transform.Rotation, out cachedData.Transform.Rotation);
                m_timeStampData[i] = cachedData;
            }
        }

        void UpdateData(uint i, ref Vector3 deltaLinear,ref Vector3 deltaAngular)
        {
            if (m_timeStampData.ContainsKey(i))
            {
                MyTimeStampValues cachedData = m_timeStampData[i];
                cachedData.LinearVelocity += deltaLinear;
                cachedData.AngularVelocity += deltaAngular;
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
