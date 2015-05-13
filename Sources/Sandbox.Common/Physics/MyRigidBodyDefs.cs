#region Using Statements
using System;
#endregion

namespace Sandbox.Engine.Physics
{
    //////////////////////////////////////////////////////////////////////////
    [Flags]
    public enum RigidBodyFlag
    {
        RBF_DEFAULT                             = (0),      // Default flag
	    RBF_KINEMATIC		                    = (1 << 1), // Rigid body is kinematic (has to be updated (matrix) per frame, velocity etc is then computed..)
	    RBF_STATIC                              = (1 << 2), // Rigid body is static
	    RBF_DISABLE_COLLISION_RESPONSE          = (1 << 6), // Rigid body has no collision response        
        RBF_DOUBLED_KINEMATIC                   = (1 << 7),
        RBF_BULLET                              = (1 << 8),
        RBF_DEBRIS                              = (1 << 9),
        RBF_KEYFRAMED_REPORTING                 = (1 << 10),
    }

    public enum IntersectionFlags
    {
        DIRECT_TRIANGLES = 0x01,
        FLIPPED_TRIANGLES = 0x02,

        ALL_TRIANGLES = DIRECT_TRIANGLES | FLIPPED_TRIANGLES
    }
}
