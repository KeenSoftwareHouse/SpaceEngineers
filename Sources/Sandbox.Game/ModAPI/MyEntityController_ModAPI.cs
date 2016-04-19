using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;

namespace Sandbox.Game.World
{
    public partial class  MyEntityController : IMyEntityController
    {
        void IMyEntityController.TakeControl(IMyControllableEntity entity)
        {
            if (entity is Sandbox.Game.Entities.IMyControllableEntity)
            {
                TakeControl(entity as Sandbox.Game.Entities.IMyControllableEntity);
            }
        }

        event Action<IMyControllableEntity, IMyControllableEntity> IMyEntityController.ControlledEntityChanged
        {
            add { ControlledEntityChanged += value; }
            remove { ControlledEntityChanged -= value; }
        }

        IMyControllableEntity IMyEntityController.ControlledEntity
        {
            get { return ControlledEntity; }
        }
    }
}
