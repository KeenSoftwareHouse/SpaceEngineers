using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Graphics.GUI
{
    public interface IMyGuiControlsParent : IMyGuiControlsOwner
    {
        MyGuiControls Controls { get; }
    }
}
