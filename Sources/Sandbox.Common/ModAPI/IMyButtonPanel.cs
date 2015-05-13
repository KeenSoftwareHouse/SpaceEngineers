using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyButtonPanel : ModAPI.Ingame.IMyButtonPanel
    {
        event Action<int> ButtonPressed;
    }
}