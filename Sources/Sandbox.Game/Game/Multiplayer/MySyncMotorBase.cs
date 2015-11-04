using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncMotorBase : MySyncCubeBlock
    {
        private MyMotorBase myMotorBase;

        [MessageId(223, P2PMessageEnum.Reliable)]
        struct AttachMsg : IEntityMessage
        {
            public long EntityId;
            public long AttachableEntityId;
            public long GetEntityId() { return EntityId; }
            public long GetAttachableId() { return AttachableEntityId; }
        }

        static MySyncMotorBase()
        {
            MySyncLayer.RegisterEntityMessage<MySyncMotorBase, AttachMsg>(OnAttach, MyMessagePermissions.FromServer);
        }

        public MySyncMotorBase(MyMotorBase stator)
            : base(stator)
        {
        }

        public void AttachRotor(MyMotorRotor rotor)
        {
            AttachMsg msg = new AttachMsg();
            msg.EntityId = this.Entity.EntityId;
            msg.AttachableEntityId = rotor.EntityId;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnAttach(MySyncMotorBase block, ref AttachMsg msg, MyNetworkClient sender)
        {
            MyMotorBase stator = (MyMotorBase)block.Entity;
            MyEntity rotorEntity = null;
            if (!MyEntities.TryGetEntityById(msg.AttachableEntityId, out rotorEntity))
            {
                Debug.Assert(false, "Could not find rotor entity to attach to stator");
                stator.RetryAttach(msg.AttachableEntityId);
                return;
            }
            MyMotorRotor rotor = (MyMotorRotor)rotorEntity;

            Debug.Assert(stator.CubeGrid != rotor.CubeGrid, "Trying to attach rotor to stator on the same grid");

            stator.Attach(rotor);
        }
    }
}
