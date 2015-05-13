using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyIdentity
    {
        long PlayerId { get; }
        long IdentityId { get; }
        string DisplayName { get; }
        string Model { get; }
        Vector3? ColorMask { get; }
        bool IsDead { get; }
    }
}
