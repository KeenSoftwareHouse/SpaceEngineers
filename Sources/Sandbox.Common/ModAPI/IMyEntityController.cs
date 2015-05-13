using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Interfaces;

namespace Sandbox.ModAPI
{
    public interface IMyEntityController
    {
        IMyControllableEntity ControlledEntity { get; }
        void TakeControl(IMyControllableEntity entity);
        event Action<IMyControllableEntity, IMyControllableEntity> ControlledEntityChanged;
    }
}
