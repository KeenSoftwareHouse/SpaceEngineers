using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Replication
{
    class MyGridPositionVerificationStateGroup : MyEntityPositionVerificationStateGroup
    {
        MyCubeGrid m_grid;

        public MyGridPositionVerificationStateGroup(MyCubeGrid grid) :
            base(grid)
        {
            m_grid = grid;
        }

        protected override void ClientWrite(VRage.Library.Collections.BitStream stream)
        {
            base.ClientWrite(stream);

            MyShipController controller = MySession.Static.ControlledEntity as MyShipController;
            stream.WriteBool(m_grid != null && controller != null);
            if (m_grid != null && controller != null)
            {
                stream.WriteBool(m_grid.IsStatic);
                if (m_grid.IsStatic == false)
                {
                  
                    stream.WriteBool(controller != null);

                        stream.WriteInt64(controller.EntityId);

                        Vector2 rotation = controller.RotationIndicator;
                        stream.WriteFloat(rotation.X);
                        stream.WriteFloat(rotation.Y);

                        stream.WriteHalf(controller.RollIndicator);

                        Vector3 position = controller.MoveIndicator;
                        stream.WriteHalf(position.X);
                        stream.WriteHalf(position.Y);
                        stream.WriteHalf(position.Z);
                }
            }
        }

        protected override void ServerRead(VRage.Library.Collections.BitStream stream, ulong clientId,uint timestamp)
        {
            base.ServerRead(stream, clientId, timestamp);
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

                            MyShipController controller;
                            if (MyEntities.TryGetEntityById<MyShipController>(entityId, out controller))
                            {
                                controller.CacheMoveAndRotate(ref move, ref rotation, roll);
                            }
                        }
                    }
                }
            }
        }
    }
}
