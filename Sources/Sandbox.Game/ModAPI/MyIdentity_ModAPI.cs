using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;

namespace Sandbox.Game.World
{
    public partial class MyIdentity : IMyIdentity
    {
        // Warning: this is obsolete!
        long IMyIdentity.PlayerId
        {
            get { return IdentityId; }
        }

        long IMyIdentity.IdentityId
        {
            get { return IdentityId; }
        }

        string IMyIdentity.DisplayName
        {
            get { return DisplayName; }
        }

        string IMyIdentity.Model
        {
            get { return Model; }
        }

        VRageMath.Vector3? IMyIdentity.ColorMask
        {
            get { return ColorMask; }
        }

        bool IMyIdentity.IsDead
        {
            get { return IsDead; }
        }
    }
}
