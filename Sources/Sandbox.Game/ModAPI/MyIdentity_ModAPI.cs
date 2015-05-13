using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.World
{
    public partial class MyIdentity : Sandbox.ModAPI.IMyIdentity
    {
        // Warning: this is obsolete!
        long ModAPI.IMyIdentity.PlayerId
        {
            get { return IdentityId; }
        }

        long ModAPI.IMyIdentity.IdentityId
        {
            get { return IdentityId; }
        }

        string ModAPI.IMyIdentity.DisplayName
        {
            get { return DisplayName; }
        }

        string ModAPI.IMyIdentity.Model
        {
            get { return Model; }
        }

        VRageMath.Vector3? ModAPI.IMyIdentity.ColorMask
        {
            get { return ColorMask; }
        }

        bool ModAPI.IMyIdentity.IsDead
        {
            get { return IsDead; }
        }
    }
}
