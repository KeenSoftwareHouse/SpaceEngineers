using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Network;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;

namespace Sandbox.Game.Replication
{
    public abstract class MyEntityPositionVerificationStateGroup : IMyStateGroup
    {
        protected MyEntity Entity;

        protected struct ClientData
        {
            public uint TimeStamp;
            public MyTransformD Transform;
        }

        protected Dictionary<ulong, ClientData> m_serverClientData;
        Dictionary<ulong, bool> m_clientUpdateFlag;

        protected MyTimestampHelper m_timestamp;
        //protected byte m_previousTimeStamp = 0;
        protected uint m_currentTimeStamp = 0;

        uint m_lastRecievedTimeStamp = 0;

        public virtual long? GetSupportID()
        {
            return null;
        }

        public StateGroupEnum GroupType
        {
            get { return StateGroupEnum.PositionVerification; }
        }

        public void CreateClientData(MyClientStateBase forClient)
        {
            if(m_serverClientData == null)
            {
                m_serverClientData = new Dictionary<ulong, ClientData>();
            }
            if (m_clientUpdateFlag == null)
            {
                m_clientUpdateFlag = new Dictionary<ulong, bool>();
            }

            m_clientUpdateFlag[forClient.EndpointId.Value] = false;
        }

        public void DestroyClientData(MyClientStateBase forClient)
        {
            if (m_serverClientData != null)
            {
                m_serverClientData.Remove(forClient.EndpointId.Value);
            }

            if (m_clientUpdateFlag != null)
            {
                m_clientUpdateFlag.Remove(forClient.EndpointId.Value);
            }
        }

        public virtual void ClientUpdate(uint timestamp)
        {
            MyEntity entity = MySession.Static.ControlledEntity as MyEntity;
            if (entity == null || entity.GetTopMostParent() != Entity)
            {
                if(m_timestamp != null)
                {
                    m_timestamp.Clear();
                }
                return;
            }
            if(m_timestamp == null)
            {
                m_timestamp = new MyTimestampHelper(Entity);
            }

            m_timestamp.Update(timestamp);
            m_currentTimeStamp = timestamp;  
        }

        public void Destroy()
        {
            
        }

        public float GetGroupPriority(int frameCountWithoutSync, MyClientInfo forClient)
        {
            // Called only on server
            if (Entity.Physics == null || Entity.Physics.IsStatic)
                return 0;

            if (MyEntityPhysicsStateGroup.ResponsibleForUpdate(Entity, forClient.EndpointId))
            {
                return 1000;
            }

            return 0;     
        }

        public bool Serialize(VRage.Library.Collections.BitStream stream, EndpointId forClient, uint timestamp, byte packetId, int maxBitPosition)
        {
            if (stream.Writing)
            {
                if (Sync.IsServer)
                {
                    ServerWrite(stream, forClient.Value);
                }
                else
                {
                    ClientWrite(stream, forClient, timestamp,maxBitPosition);
                }
            }
            else
            {
                if (Sync.IsServer)
                {
                    ServerRead(stream, forClient.Value,timestamp);
                }
                else
                {
                    ClientRead(stream);
                }
            }

            return true;
        }

        protected virtual void ClientWrite(VRage.Library.Collections.BitStream stream, EndpointId forClient, uint timestamp, int maxBitPosition)
        {
            MatrixD matrix = Entity.WorldMatrix;
            stream.WriteQuaternionNorm(Quaternion.CreateFromForwardUp(matrix.Forward, matrix.Up));
            stream.Write(matrix.Translation);
        }

        protected virtual void ServerRead(VRage.Library.Collections.BitStream stream, ulong clientId,uint timestamp)
        {
            m_clientUpdateFlag[clientId] = true;

            ClientData data = new ClientData();
            data.TimeStamp = timestamp;
            data.Transform = new MyTransformD();

            data.Transform.Rotation = stream.ReadQuaternionNorm();
            data.Transform.Position = stream.ReadVector3D();

            m_serverClientData[clientId] = data;        
        }

        void ServerWrite(VRage.Library.Collections.BitStream stream, ulong clientId)
        {
            bool clientUpdate = m_clientUpdateFlag[clientId];
            stream.WriteBool(clientUpdate);
            if (clientUpdate)
            {
                ClientData clientData = m_serverClientData[clientId];
                m_clientUpdateFlag[clientId] = false;

                stream.WriteUInt32(clientData.TimeStamp);

                MyTransformD serverData = new MyTransformD(Entity.WorldMatrix);


                //rotation is calculated same way for both
                Quaternion serverRotation = serverData.Rotation;
                serverRotation.Normalize();
                clientData.Transform.Rotation.Normalize();

                MyTimeStampValues delta = new MyTimeStampValues();

                serverRotation = Quaternion.Inverse(serverRotation);
                Quaternion.Multiply(ref clientData.Transform.Rotation, ref serverRotation, out delta.Transform.Rotation);

                bool applyRotation = false;
                double eps = 0.001;
                if (Math.Abs(Quaternion.Dot(clientData.Transform.Rotation, serverData.Rotation)) < 1 - eps)
                {
                    applyRotation = true;
                }

                bool isValidPosition = true;
                bool correctServerPosition = false;

                CalculatePositionDifference(clientId, out isValidPosition, out correctServerPosition, out delta.Transform.Position);

                bool isValidClientPosition = isValidPosition;

                if ((correctServerPosition && isValidPosition) || applyRotation)
                {
                    MatrixD matrix = Entity.WorldMatrix;
                    MatrixD correctionMatrix = MatrixD.Multiply(matrix.GetOrientation(), delta.Transform.TransformMatrix);
                    correctionMatrix.Translation += Entity.WorldMatrix.Translation;

                    if (correctServerPosition)
                    {
                        isValidClientPosition = IsPositionValid(new MyTransformD(correctionMatrix));
                    }

                    if (isValidClientPosition)
                    {
                        Entity.PositionComp.SetWorldMatrix(correctionMatrix, null, true);
                        MyEntityPhysicsStateGroup support = MySupportHelper.FindPhysics(Entity);
                        if (support != null && support.MoveHandler != null)
                        {
                            support.MoveHandler(ref matrix, ref correctionMatrix);
                        }
                    }
                    else if (applyRotation)
                    {
                        correctionMatrix.Translation = Entity.WorldMatrix.Translation;
                        Entity.PositionComp.SetWorldMatrix(correctionMatrix, null, true);
                    }

                }

                stream.WriteBool(!isValidClientPosition);

                if (!isValidClientPosition)
                {
                    serverData = new MyTransformD(Entity.WorldMatrix);
                    stream.Write(serverData.Position);

                    CustomServerWrite(m_serverClientData[clientId].TimeStamp, clientId, stream);
                }
            }

            stream.Write(Entity.Physics != null ? Entity.Physics.LinearVelocity * MyEntityPhysicsStateGroup.EffectiveSimulationRatio : Vector3.Zero);
            stream.Write(Entity.Physics != null ? Entity.Physics.AngularVelocity * MyEntityPhysicsStateGroup.EffectiveSimulationRatio : Vector3.Zero);           
        }

        protected abstract void CalculatePositionDifference(ulong clientId, out bool positionValid, out bool correctServer, out Vector3D delta);

        protected virtual void CustomServerWrite(uint timeStamp, ulong clientId, VRage.Library.Collections.BitStream stream)
        {

        }

        void ClientRead(VRage.Library.Collections.BitStream stream)
        {
            bool hasClientData= stream.ReadBool();
            uint? timeStamp = null;
            if (hasClientData)
            {
                timeStamp = stream.ReadUInt32();
                m_lastRecievedTimeStamp = timeStamp.Value;
        
                bool isUpdate = stream.ReadBool();
                if (isUpdate)
                {
                    MyTransformD serverTransform = new MyTransformD();
                    serverTransform.Position = stream.ReadVector3D();
                    serverTransform.Rotation = Quaternion.Identity;

                    CustomClientRead(timeStamp.Value, ref serverTransform, stream);
                }
            }

            Vector3 serverLinearVelocity = stream.ReadVector3();
            Vector3 serverAngularVelocity = stream.ReadVector3();

            MyTimeStampValues? clientData = null;
            if (timeStamp.HasValue)
            {
                clientData = m_timestamp.GetTransform(timeStamp.Value);
            }

            if (clientData.HasValue)
            {
                Vector3 linearDelta = serverLinearVelocity / MyEntityPhysicsStateGroup.EffectiveSimulationRatio - clientData.Value.LinearVelocity;
                Entity.Physics.LinearVelocity += Vector3.Round(linearDelta, 2);
                Vector3 angularDelta = serverAngularVelocity / MyEntityPhysicsStateGroup.EffectiveSimulationRatio - clientData.Value.AngularVelocity;
                Entity.Physics.AngularVelocity += Vector3.Round(angularDelta, 2);

                m_timestamp.UpdateDeltaVelocities(timeStamp.Value, ref linearDelta, ref angularDelta);
            }
            else
            {
                Vector3 linearVelocity = serverLinearVelocity / MyEntityPhysicsStateGroup.EffectiveSimulationRatio;
                Entity.Physics.LinearVelocity = Vector3.Round(linearVelocity, 2);
                Vector3 angularVelocity = serverAngularVelocity / MyEntityPhysicsStateGroup.EffectiveSimulationRatio;
                Entity.Physics.AngularVelocity = Vector3.Round(angularVelocity, 2);

            }
        }

        protected virtual void CustomClientRead(uint timeStamp, ref  MyTransformD serverPositionAndOrientation, VRage.Library.Collections.BitStream stream)
        {
            if (m_timestamp != null)
            {
                m_timestamp.ServerResponse(timeStamp, ref serverPositionAndOrientation);
            }
        }

        public void OnAck(MyClientStateBase forClient, byte packetId, bool delivered)
        {
            
        }

        public void ForceSend(MyClientStateBase clientData)
        {
           
        }

        public MyEntityPositionVerificationStateGroup(MyEntity entity)
        {
            Entity = entity;
        }

        protected virtual bool IsPositionValid(MyTransformD clientPos)
        {
            return true;
        }
    }
}
