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

namespace Sandbox.Game.Replication
{
    public class MyCharacterPositionVerificationStateGroup : MyEntityPositionVerificationStateGroup
    {
        private MyEntityPhysicsStateGroup m_supportPhysics;
        MyCharacter m_character;
        MyTimestampHelper m_supportTimeStamp;

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

            MyEntity support = MySupportHelper.FindSupportForCharacterAABB(m_character);
            if (support != null)
            {
                if (m_supportTimeStamp == null)
                {
                    m_supportTimeStamp = new MyTimestampHelper(null);
                }

                m_supportTimeStamp.SetEntity(support);
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

        protected override void ClientWrite(VRage.Library.Collections.BitStream stream)
        {
                 
            base.ClientWrite(stream);

            stream.WriteBool(m_character != null);
            if (m_character != null)
            {
                var physGroup = MyExternalReplicable.FindByObject(m_character).FindStateGroup<MyCharacterPhysicsStateGroup>();
                long? supportId = null;
                if (physGroup != null)
                {
                    physGroup.SetSupport(physGroup.FindSupportDelegate());
                    supportId = physGroup.GetSupportId();
                }

                stream.WriteBool(supportId.HasValue);
                if (supportId.HasValue)
                {
                    stream.WriteInt64(supportId.Value);
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

                if ((hasJetpack && m_character.JetpackComp.TurnedOn) == false)
                {
                    MatrixD matrix = m_character.WorldMatrix;
                    stream.WriteQuaternionNorm(Quaternion.CreateFromForwardUp(matrix.Forward, matrix.Up));
                }
            }        
           
        }

        protected override void ServerRead(VRage.Library.Collections.BitStream stream, ulong clientId, uint timestamp)
        {       
            base.ServerRead(stream, clientId, timestamp);
            bool clientHasCharacter = stream.ReadBool();
            if (clientHasCharacter)
            {
                MyEntity support;
                if (stream.ReadBool())
                {
                    bool apply = MyEntities.TryGetEntityById(stream.ReadInt64(), out support);

                    if (m_character != null)
                    {

                        var physGroup = MyExternalReplicable.FindByObject(m_character).FindStateGroup<MyCharacterPhysicsStateGroup>();
                        if (physGroup != null && apply)
                        {
                            physGroup.SetSupport(MySupportHelper.FindPhysics(support));
                        }
                    }
                }
               

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

                Quaternion rotation = Quaternion.Identity;

                if (Jetpack == false)
                {
                    rotation = stream.ReadQuaternionNorm();
                }

                if(m_character == null)
                {
                    return;
                }

                if (m_character.IsDead || m_character.IsUsing != null)
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
                m_character.SetCurrentMovementState(MovementState);
                m_character.HeadLocalXAngle = headXAngle;
                m_character.HeadLocalYAngle = headYAngle;

               
                m_character.CacheMove(ref move, ref rotate, ref roll);

                if (Jetpack == false)
                {
                    m_character.CacheRotation(ref rotation);
                }


            }    
        }

        protected override void CustomServerWrite(uint timeStamp, VRage.Library.Collections.BitStream stream)
        {
            MyEntity support = MySupportHelper.FindSupportForCharacterAABB(m_character);
            stream.WriteBool(support != null);
            if (support != null)
            {
                stream.WriteInt64(support.EntityId);
                Vector3D pos = support.PositionComp.GetPosition() - m_character.PositionComp.GetPosition();
                stream.Write(pos);
                stream.Write(support.PositionComp.GetPosition());
            }
        }

        protected override void CustomClientRead(uint timeStamp, ref MyTimeStampValues serverPositionAndOrientation, VRage.Library.Collections.BitStream stream)
        {
            bool hasSupport = stream.ReadBool();

            if (hasSupport)
            {
                long entityId = stream.ReadInt64();

                Vector3D serverDelta = stream.ReadVector3D();
                Vector3D serverSupportPos = stream.ReadVector3D();

                if (!MyEntities.EntityExists(entityId))
                    return;

                MyEntity support = MyEntities.GetEntityById(entityId);

                MyTimeStampValues? clientTransform = m_timestamp.GetTransform(timeStamp);

                Vector3D clientDelta = Vector3.Zero;
                Vector3D clientVelocity = Vector3D.Zero;
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
                            supportPosition = serverSupportPos;
                            return;
                        }
                    }
   
                    clientDelta = supportPosition - clientTransform.Value.Transform.Position;
                    clientVelocity = clientTransform.Value.LinearVelocity;
                    rotationComp = Quaternion.Inverse(clientTransform.Value.Transform.Rotation);
                }
                else
                {
                    m_character.PositionComp.SetWorldMatrix(serverPositionAndOrientation.Transform.TransformMatrix, null, true);
                    return;
                }

                MyTimeStampValues delta = new MyTimeStampValues();
            
                Quaternion.Multiply(ref serverPositionAndOrientation.Transform.Rotation, ref rotationComp, out delta.Transform.Rotation);

                delta.Transform.Position = clientDelta - serverDelta;
                delta.LinearVelocity = serverPositionAndOrientation.LinearVelocity - clientVelocity;

                double deltaL = delta.Transform.Position.Length();

                //if difference is more than 
                if (deltaL < (MyGridPhysics.ShipMaxLinearVelocity() * Sync.RelativeSimulationRatio))
                {
                    delta.Transform.Position = delta.Transform.Position * 0.2;
                    delta.Transform.Rotation = Quaternion.Slerp(delta.Transform.Rotation,Quaternion.Identity,0.2f);
                }

               
                Quaternion normalized = delta.Transform.Rotation;
                normalized.Normalize();
                delta.Transform.Rotation = normalized;
                normalized = serverPositionAndOrientation.Transform.Rotation;
                normalized.Normalize();
                serverPositionAndOrientation.Transform.Rotation = normalized;

                Quaternion clientNormalized = clientTransform.Value.Transform.Rotation;
                clientNormalized.Normalize();

                double eps = 0.001;
                bool hasJetpack = m_character.JetpackComp != null;
                if (hasJetpack && m_character.JetpackComp.TurnedOn && m_character.IsDead == false)
                {
                    if (Math.Abs(Quaternion.Dot(serverPositionAndOrientation.Transform.Rotation, clientNormalized)) < 1 - eps )
                    {
                        Quaternion currentOrientation = Quaternion.CreateFromForwardUp(m_character.WorldMatrix.Forward, m_character.WorldMatrix.Up);
                        Quaternion.Multiply(ref delta.Transform.Rotation, ref currentOrientation, out currentOrientation);

                        MatrixD matrix = MatrixD.CreateFromQuaternion(currentOrientation);
                        MatrixD currentMatrix = m_character.PositionComp.WorldMatrix;
                        currentMatrix.Translation = Vector3D.Zero;

                        if (currentMatrix.EqualsFast(ref matrix) == false)
                        {
                            if (m_character.Physics.CharacterProxy != null)
                            {
                                m_character.Physics.CharacterProxy.Forward = matrix.Forward;
                                m_character.Physics.CharacterProxy.Up = matrix.Up;
                            }
                        }
                    }
                }

                if (deltaL > (MyGridPhysics.ShipMaxLinearVelocity() * Sync.RelativeSimulationRatio))
                {
                 
                    m_character.PositionComp.SetPosition(serverPositionAndOrientation.Transform.Position);
                    m_character.Physics.LinearVelocity = serverPositionAndOrientation.LinearVelocity;
                    m_timestamp.OverwriteServerPosition(timeStamp, ref serverPositionAndOrientation);
                    return;
                   
                }
                else if (deltaL > 5.0f*MyTimestampHelper.POSITION_TOLERANCE)
                {
                    m_character.CacheMoveDelta(ref delta.Transform.Position);
                }

                m_character.Physics.LinearVelocity += delta.LinearVelocity;

                m_timestamp.UpdateDeltaPosition(timeStamp, ref delta);
            }
            else
            {
                base.CustomClientRead(timeStamp, ref serverPositionAndOrientation, stream);
            }
        }

    }
}
