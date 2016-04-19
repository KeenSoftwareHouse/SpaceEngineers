using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Network;
using VRageMath;
using VRageMath.PackedVector;

namespace Sandbox.Game.Replication
{
    public class MyEntityPositionVerificationStateGroup : IMyStateGroup
    {
        protected MyEntity Entity;

        protected Dictionary<ulong, uint> m_serverClientData;
        Dictionary<ulong, bool> m_clientUpdateFlag;

        protected MyTimestampHelper m_timestamp;
        //protected byte m_previousTimeStamp = 0;
        protected uint m_currentTimeStamp = 0;

        uint m_lastRecievedTimeStamp = 0;

        public StateGroupEnum GroupType
        {
            get { return StateGroupEnum.PositionVerification; }
        }

        public void CreateClientData(MyClientStateBase forClient)
        {
            if(m_serverClientData == null)
            {
                m_serverClientData = new Dictionary<ulong, uint>();
            }
            if (m_clientUpdateFlag == null)
            {
                m_clientUpdateFlag = new Dictionary<ulong, bool>();
            }

            m_clientUpdateFlag[forClient.EndpointId.Value] = false;
            m_serverClientData[forClient.EndpointId.Value] = 0;
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

            if (Sync.IsServer && m_clientUpdateFlag[forClient.EndpointId.Value] == false)
            {
                return 0;
            }

            if (MyEntityPhysicsStateGroup.ResponsibleForUpdate(Entity, forClient.EndpointId))
            {
                return 1;
            }

            return 0;     
        }

        public void Serialize(VRage.Library.Collections.BitStream stream, EndpointId forClient, uint timestamp, byte packetId, int maxBitPosition)
        {
            if (stream.Writing)
            {
                if (Sync.IsServer)
                {
                    ServerWrite(stream, forClient.Value);
                }
                else
                {
                    ClientWrite(stream);
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
        }

        protected virtual void ClientWrite(VRage.Library.Collections.BitStream stream)
        {                         
        }

        void ServerWrite(VRage.Library.Collections.BitStream stream, ulong clientId)
        {
            stream.WriteUInt32(m_serverClientData[clientId]);
            var matrix = Entity.WorldMatrix;
            stream.Write(matrix.Translation);
            stream.WriteQuaternionNorm(Quaternion.CreateFromForwardUp(matrix.Forward, matrix.Up));
            //HalfVector3 linear = Entity.Physics != null ? Vector3.Round(Entity.Physics.LinearVelocity * MyEntityPhysicsStateGroup.EffectiveSimulationRatio, MyEntityPhysicsStateGroup.NUM_DECIMAL_PRECISION) : Vector3.Zero;
            Vector3 linear = Entity.Physics != null ? Entity.Physics.LinearVelocity * MyEntityPhysicsStateGroup.EffectiveSimulationRatio : Vector3.Zero;
            stream.Write(linear);

            Vector3 angular = Entity.Physics != null ? Entity.Physics.AngularVelocity * MyEntityPhysicsStateGroup.EffectiveSimulationRatio : Vector3.Zero;
            stream.Write(angular);

            m_clientUpdateFlag[clientId] = false;
            CustomServerWrite(m_serverClientData[clientId], stream);
        }

        protected virtual void CustomServerWrite(uint timeStamp, VRage.Library.Collections.BitStream stream)
        {

        }

        void ClientRead(VRage.Library.Collections.BitStream stream)
        {    
            uint timeStamp = stream.ReadUInt32();
            if(m_lastRecievedTimeStamp > timeStamp)
            {
                return;
            }
            m_lastRecievedTimeStamp = timeStamp;

            MyTimeStampValues serverPositionAndOrientation = new MyTimeStampValues();
            serverPositionAndOrientation.Transform = new MyTransformD();
            serverPositionAndOrientation.Transform.Position = stream.ReadVector3D();
            serverPositionAndOrientation.Transform.Rotation = stream.ReadQuaternionNorm();
            serverPositionAndOrientation.LinearVelocity = stream.ReadVector3();
            serverPositionAndOrientation.AngularVelocity = stream.ReadVector3();

            serverPositionAndOrientation.LinearVelocity /= MyEntityPhysicsStateGroup.EffectiveSimulationRatio;
            serverPositionAndOrientation.AngularVelocity /= MyEntityPhysicsStateGroup.EffectiveSimulationRatio;
           
            CustomClientRead(timeStamp, ref serverPositionAndOrientation, stream);
        }

        protected virtual void CustomClientRead(uint timeStamp, ref MyTimeStampValues serverPositionAndOrientation, VRage.Library.Collections.BitStream stream)
        {
            if (m_timestamp != null)
            {
                m_timestamp.ServerResponse(timeStamp, ref serverPositionAndOrientation);
            }
        }

        protected virtual void ServerRead(VRage.Library.Collections.BitStream stream, ulong clientId,uint timestamp)
        {
            m_clientUpdateFlag[clientId] = true;
            m_serverClientData[clientId] = timestamp;        
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
    }
}
