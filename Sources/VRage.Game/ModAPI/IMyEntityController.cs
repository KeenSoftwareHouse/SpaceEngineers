using System;
using VRage.Game.ModAPI.Interfaces;

namespace VRage.Game.ModAPI
{
    public interface IMyEntityController
    {
        IMyControllableEntity ControlledEntity { get; }
        void TakeControl(IMyControllableEntity entity);
        event Action<IMyControllableEntity, IMyControllableEntity> ControlledEntityChanged;
    }
}
