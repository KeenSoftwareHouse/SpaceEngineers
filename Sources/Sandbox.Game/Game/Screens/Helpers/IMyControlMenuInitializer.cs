using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens.Helpers
{
    public interface IMyControlMenuInitializer
    {
        void OpenControlMenu(IMyControllableEntity controlledEntity);
    }
}
