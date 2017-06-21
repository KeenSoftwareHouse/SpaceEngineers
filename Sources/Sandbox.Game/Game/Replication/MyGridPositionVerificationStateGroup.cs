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
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Replication
{
    class MyGridPositionVerificationStateGroup : MyEntityPositionVerificationStateGroup
    {
        MyCubeGrid m_grid;

        bool m_lowPositionOrientation = false;
        int m_currentSentPosition = 0;
        protected Dictionary<ulong, Vector3D> m_additionalServerClientData;

        public MyGridPositionVerificationStateGroup(MyCubeGrid grid) :
            base(grid)
        {
            m_grid = grid;
        }

        protected override void ClientWrite(VRage.Library.Collections.BitStream stream,EndpointId forClient, uint timestamp, int maxBitPosition)
        {
            base.ClientWrite(stream,forClient,timestamp,maxBitPosition);

            stream.Write(Entity.WorldMatrix.Translation);

            MyShipController controller = MySession.Static.ControlledEntity as MyShipController;
            stream.WriteBool(m_grid != null && controller != null);
            if (m_grid != null && controller != null)
            {
                stream.WriteBool(m_grid.IsStatic);
                if (m_grid.IsStatic == false)
                {              
                    stream.WriteBool(controller != null);
                    if (controller != null)
                    {
                        stream.WriteInt64(controller.EntityId);

                        Vector2 rotation = controller.RotationIndicator;
                        stream.WriteFloat(rotation.X);
                        stream.WriteFloat(rotation.Y);

                        stream.WriteHalf(controller.RollIndicator);

                        Vector3 position = controller.MoveIndicator;
                        stream.WriteHalf(position.X);
                        stream.WriteHalf(position.Y);
                        stream.WriteHalf(position.Z);

                        Vector3D gridPosition = m_grid.PositionComp.GetPosition();
                        MyGridPhysicsStateGroup.WriteSubgrids(m_grid, stream, ref forClient, timestamp, maxBitPosition, m_lowPositionOrientation, ref gridPosition, ref m_currentSentPosition);
                    }
                }
            }

        }

        protected override void ServerRead(VRage.Library.Collections.BitStream stream, ulong clientId,uint timestamp)
        {
            base.ServerRead(stream, clientId, timestamp);

            if (m_additionalServerClientData == null)
            {
                m_additionalServerClientData = new Dictionary<ulong, Vector3D>();
            }

            m_additionalServerClientData[clientId] = stream.ReadVector3D();

            if (stream.ReadBool())
            {
                if (m_grid != null)
                {
                    bool isStatic = stream.ReadBool();
                    if (isStatic == false)
                    {
                        if (stream.ReadBool())
                        {
                            long entityId = stream.ReadInt64();
                            Vector2 rotation = new Vector2();
                            rotation.X = stream.ReadFloat();
                            rotation.Y = stream.ReadFloat();

                            float roll = stream.ReadHalf();

                            Vector3 move = new Vector3();
                            move.X = stream.ReadHalf();
                            move.Y = stream.ReadHalf();
                            move.Z = stream.ReadHalf();

                            Vector3D gridPos = Vector3D.Zero;
                            MyShipController controller;
                            if (MyEntities.TryGetEntityById<MyShipController>(entityId, out controller))
                            {
                                controller.CacheMoveAndRotate(move, rotation, roll);
                                gridPos = controller.CubeGrid.PositionComp.GetPosition();
                            }
   
                            MyGridPhysicsStateGroup.ReadSubGrids(stream, timestamp,true,m_lowPositionOrientation,ref gridPos);
                        }
                    }
                }

             
            }
        }

        protected override void CalculatePositionDifference(ulong clientId, out bool positionValid, out bool correctServer, out Vector3D delta)
        {
            positionValid = true;
            correctServer = true;

            Vector3D clientData = m_additionalServerClientData[clientId];
            MatrixD worldMatrix = Entity.PositionComp.WorldMatrix;

            delta = m_additionalServerClientData[clientId] - worldMatrix.Translation;
               
        }
                    
    }
}
