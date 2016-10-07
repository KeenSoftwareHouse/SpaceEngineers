using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;
using Sandbox.Game.World;
using VRage.Network;
using Sandbox.Engine.Physics;
using Havok;

namespace Sandbox.Game.Replication
{
    public class MyCharacterPositionVerificationStateGroup : MyEntityPositionVerificationStateGroup
    {
        new struct ClientData
        {
            public bool HasSupport;
            public Vector3D SupportPosition;
        }

        MyEntity m_support = null;
         static List<HkBodyCollision> m_results = new List<HkBodyCollision>();
        private MyEntityPhysicsStateGroup m_supportPhysics;
        MyCharacter m_character;
        MyTimestampHelper m_supportTimeStamp;
        Dictionary<ulong, ClientData> m_additionalServerClientData;
        Dictionary<ulong, bool> m_commandsApplied;

        public override long? GetSupportID()
        {
            if(m_supportPhysics != null)
            {
                return m_supportPhysics.Entity.EntityId;
            }
            return null;
        }

        public MyCharacterPositionVerificationStateGroup(MyCharacter character):
            base(character)
        {
            m_character = character;
        }

        public override void ClientUpdate(uint timestamp)
        {
            base.ClientUpdate(timestamp);

            if (MySession.Static.ControlledEntity != m_character)
            {
                if(m_supportTimeStamp != null)
                {
                    m_supportTimeStamp.Clear();
                }
                return;
            }

            var physGroup = MyExternalReplicable.FindByObject(m_character).FindStateGroup<MyCharacterPhysicsStateGroup>();
            if (physGroup == null)
            {
                if (m_supportTimeStamp != null)
                {
                    m_supportTimeStamp.Clear();
                }
                return;
            }

            m_supportPhysics = physGroup.FindSupportDelegate();
            physGroup.SetSupport(m_supportPhysics);
            if (m_supportPhysics != null)
            {
                if (m_supportTimeStamp == null)
                {
                    m_supportTimeStamp = new MyTimestampHelper(null);
                }

                m_supportTimeStamp.SetEntity(m_supportPhysics.Entity);
                m_supportTimeStamp.Update(timestamp);
            }
            else
            {
                if (m_supportTimeStamp != null)
                {
                    m_supportTimeStamp.Clear();
                }
            }

        }

        protected override void ClientWrite(VRage.Library.Collections.BitStream stream, EndpointId forClient, uint timestamp, int maxBitPosition)
        {
            base.ClientWrite(stream, forClient,timestamp,maxBitPosition);

            stream.WriteBool(m_character != null);
            if (m_character != null)
            {

                stream.WriteBool(m_supportPhysics != null);
                if (m_supportPhysics != null)
                {
                    stream.WriteInt64(m_supportPhysics.Entity.EntityId);
                    stream.Write(m_supportPhysics.Entity.PositionComp.GetPosition());
                }
                
                Vector3 position = m_character.MoveIndicator;
                stream.WriteHalf(position.X);
                stream.WriteHalf(position.Y);
                stream.WriteHalf(position.Z);

                Vector2 rotate = m_character.RotationIndicator;
                stream.WriteFloat(rotate.X);
                stream.WriteFloat(rotate.Y);

                stream.WriteFloat(m_character.RollIndicator);

                // Movement state, 2B
                stream.WriteUInt16((ushort)m_character.GetNetworkMovementState());
                // Movement flag.
                stream.WriteUInt16((ushort)m_character.PreviousMovementFlags);

                // Flags, 6 bits
                bool hasJetpack = m_character.JetpackComp != null;
                stream.WriteBool(hasJetpack ? m_character.JetpackComp.TurnedOn : false);
                stream.WriteBool(hasJetpack ? m_character.JetpackComp.DampenersTurnedOn : false);
                stream.WriteBool(m_character.LightEnabled); // TODO: Remove
                stream.WriteBool(m_character.ZoomMode == MyZoomModeEnum.IronSight);
                stream.WriteBool(m_character.RadioBroadcaster.WantsToBeEnabled); // TODO: Remove
                stream.WriteBool(m_character.TargetFromCamera);
                stream.WriteFloat(m_character.HeadLocalXAngle);
                stream.WriteFloat(m_character.HeadLocalYAngle);
            }        
           
        }

        protected override void ServerRead(VRage.Library.Collections.BitStream stream, ulong clientId, uint timestamp)
        {       
            base.ServerRead(stream, clientId, timestamp);
            bool clientHasCharacter = stream.ReadBool();
            if (clientHasCharacter)
            {
                MyEntity support;
                bool hasSupport = stream.ReadBool();
                Vector3D supportPosition = Vector3D.Zero;

                if (m_character != null)
                {
                    var physGroup = MyExternalReplicable.FindByObject(m_character).FindStateGroup<MyCharacterPhysicsStateGroup>();
                    if (physGroup != null)
                    {
                        m_support = MySupportHelper.FindSupportForCharacterAABB(m_character);
                        physGroup.SetSupport(MySupportHelper.FindPhysics(m_support));
                    }
                }

                 if (hasSupport)
                 {
                     long supportId = stream.ReadInt64();
                     bool apply = MyEntities.TryGetEntityById(supportId, out support);
                     supportPosition = stream.ReadVector3D();
                 }


                if(m_additionalServerClientData == null)
                {
                    m_additionalServerClientData = new Dictionary<ulong, ClientData>();
                }

                m_additionalServerClientData[clientId] = new ClientData() { HasSupport = hasSupport, SupportPosition = supportPosition };

                Vector3 move = new Vector3();
                move.X = stream.ReadHalf();
                move.Y = stream.ReadHalf();
                move.Z = stream.ReadHalf();

                Vector2 rotate = new Vector2();
                rotate.X = stream.ReadFloat();
                rotate.Y = stream.ReadFloat();

                float roll = stream.ReadFloat();

                MyCharacterMovementEnum MovementState = (MyCharacterMovementEnum)stream.ReadUInt16();
                MyCharacterMovementFlags MovementFlag = (MyCharacterMovementFlags)stream.ReadUInt16();

                bool Jetpack = stream.ReadBool();
                bool Dampeners = stream.ReadBool();
                bool Lights = stream.ReadBool(); // TODO: Remove
                bool Ironsight = stream.ReadBool();
                bool Broadcast = stream.ReadBool(); // TODO: Remove
                bool TargetFromCamera = stream.ReadBool();
                float headXAngle = stream.ReadFloat();
                float headYAngle = stream.ReadFloat();

                if(m_character == null)
                {
                    return;
                }

                if (m_character.IsUsing != null)
                {
                    return;
                }

                var jetpack = m_character.JetpackComp;
                if (jetpack != null)
                {
                    if (Jetpack != jetpack.TurnedOn)
                    {
                        jetpack.TurnOnJetpack(Jetpack, true);
                    }
                    if (Dampeners != jetpack.DampenersTurnedOn)
                    {
                        jetpack.EnableDampeners(Dampeners, false);
                    }
                }

                if (Lights != m_character.LightEnabled)
                {
                    m_character.EnableLights(Lights);
                }

                if (m_character.RadioBroadcaster != null && Broadcast != m_character.RadioBroadcaster.Enabled)
                {
                    m_character.EnableBroadcasting(Broadcast);
                }

                m_character.TargetFromCamera = TargetFromCamera;

                // Set client movement state directly and don't perform other operations
                // that may have side-effects to let server side Character.UpdateAfterSimulation()
                // perform exactly same operations as on client
                m_character.MovementFlags = MovementFlag;
                if (m_character.IsDead == false)
                {
                    m_character.SetCurrentMovementState(MovementState);
                }
                m_character.HeadLocalXAngle = headXAngle;
                m_character.HeadLocalYAngle = headYAngle;
                if (m_commandsApplied == null)
                {
                    m_commandsApplied = new Dictionary<ulong, bool>();
                }

                if (Vector3.IsZero(move, 0.01f) == false || Vector2.IsZero(ref rotate, 0.01f) == false || Math.Abs(roll - 0.0) > 0.01f)
                {            
                    m_commandsApplied[clientId] = true;
                }

                m_character.CacheMove(ref move, ref rotate, ref roll);
            }    
        }

        protected override void CustomServerWrite(uint timeStamp, ulong clientId, VRage.Library.Collections.BitStream stream)
        {
            stream.WriteBool(m_support != null);
            if (m_support != null)
            {
                stream.WriteInt64(m_support.EntityId);
                stream.Write(m_support.PositionComp.GetPosition());
            }
        }

        protected override void CustomClientRead(uint timeStamp, ref MyTransformD serverPositionAndOrientation, VRage.Library.Collections.BitStream stream)
        {
            bool hasSupport = stream.ReadBool();

            if (hasSupport)
            {
                long entityId = stream.ReadInt64();

                Vector3D serverSupportPos = stream.ReadVector3D();

                if (!MyEntities.EntityExists(entityId))
                    return;

                MyEntity support = MyEntities.GetEntityById(entityId);

                MyTimeStampValues? clientTransform = m_timestamp.GetTransform(timeStamp);

                Vector3D clientPosition = Vector3D.Zero;
                Vector3D clientSupportPosition = Vector3D.Zero;
                Quaternion rotationComp = Quaternion.Identity;

                if (clientTransform != null)
                {
                    if(m_supportTimeStamp == null)
                    {
                        return;
                    }

                    MyTimeStampValues? supportTransform = m_supportTimeStamp.GetTransform(timeStamp);
                    Vector3D supportPosition = support.PositionComp.WorldMatrix.Translation;

                    if (supportTransform.HasValue)
                    {
                        supportPosition = supportTransform.Value.Transform.Position;

                        if(supportTransform.Value.EntityId != entityId)
                        {
                            return;
                        }
                    }

                    clientPosition = clientTransform.Value.Transform.Position;
                    clientSupportPosition = supportPosition;
                    rotationComp = Quaternion.Inverse(clientTransform.Value.Transform.Rotation);
                }
                else
                {
                    m_character.PositionComp.SetWorldMatrix(serverPositionAndOrientation.TransformMatrix, null, true);
                    return;
                }

                MyTransformD delta = new MyTransformD();

                delta.Rotation = Quaternion.Identity;

                Vector3D characterDelta = serverPositionAndOrientation.Position - clientPosition;
                Vector3D supportDelta = serverSupportPos - clientSupportPosition;

                delta.Position = characterDelta - supportDelta;           
                m_character.CacheMoveDelta(ref delta.Position);
                m_timestamp.UpdateDeltaPosition(timeStamp, ref delta);
            }
            else
            {
                base.CustomClientRead(timeStamp, ref serverPositionAndOrientation, stream);
            }
        }

        protected override void CalculatePositionDifference(ulong clientId, out bool positionValid, out bool correctServer, out Vector3D delta)
        {
            positionValid = true;
            correctServer = false;

            delta = Vector3D.Zero;

            ClientData clientData = m_additionalServerClientData[clientId];
            MatrixD worldMatrix = Entity.PositionComp.WorldMatrix;
     
            if (clientData.HasSupport)
            {
                if (m_support != null)
                {
                    Vector3D characterDelta = m_serverClientData[clientId].Transform.Position - m_character.PositionComp.GetPosition();
                    Vector3D supportDelta = clientData.SupportPosition - m_support.PositionComp.GetPosition();
                    delta = characterDelta - supportDelta;
                }
                else
                {
                    positionValid = false;
                    delta = m_serverClientData[clientId].Transform.Position - m_character.PositionComp.GetPosition();
                }
            }
            else
            {
                
                delta = m_serverClientData[clientId].Transform.Position - worldMatrix.Translation;
            }


            if (delta.LengthSquared() > 0.05 * 0.05)
            {
                if (m_commandsApplied.ContainsKey(clientId) == false || m_commandsApplied[clientId] == false)
                {
                    positionValid = false;
                }
                else
                {
                    correctServer = true;
                }
            }

            m_commandsApplied[clientId] = false;

        }

        protected override bool IsPositionValid(MyTransformD clientDelta)
        {
            if (m_character.Physics != null && m_character.Physics.CharacterProxy != null)
            {
                float offset = 2 * MyPerGameSettings.PhysicsConvexRadius + 0.03f;

                float width = (m_character.Definition.CharacterCollisionWidth * m_character.Definition.CharacterCollisionScale) - offset;
                float height = (m_character.Definition.CharacterCollisionHeight - m_character.Definition.CharacterCollisionWidth * m_character.Definition.CharacterCollisionScale) - offset;

                HkCapsuleShape capsule = new HkCapsuleShape(Vector3.Up * height / 2.0f, Vector3.Down * height / 2.0f, width / 2.0f);
                m_results.Clear();

                clientDelta.Position = clientDelta.Position + m_character.WorldMatrix.Up * (m_character.Definition.CharacterCollisionHeight/2 + offset);

                MyPhysics.GetPenetrationsShape(capsule, ref clientDelta.Position, ref clientDelta.Rotation, m_results, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                bool otherFound = false;
                foreach (var colision in m_results)
                {
                    if (colision.Body.Quality == HkCollidableQualityType.Character)
                    {
                        continue;
                    }
                    otherFound = true;
                }
                return !otherFound;
            }
            return false;
        }
    }
}
