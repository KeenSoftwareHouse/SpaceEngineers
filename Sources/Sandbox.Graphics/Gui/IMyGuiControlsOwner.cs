using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public interface IMyGuiControlsOwner
    {
        Vector2 GetPositionAbsolute();
        Vector2 GetPositionAbsoluteTopLeft();
        Vector2 GetPositionAbsoluteCenter();
        Vector2? GetSize();

        MyGuiControlBase GetNextFocusControl(MyGuiControlBase currentFocusControl, bool forwardMovement);

        string DebugNamePath { get; }

        string Name { get; }
        IMyGuiControlsOwner Owner { get; }
    }
}
