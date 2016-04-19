using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public partial class MyShape : IMyVoxelShape
    {
        MatrixD IMyVoxelShape.Transform
        {
            get
            {
                return Transformation;
            }
            set
            {
                Transformation = value;
            }
        }

        BoundingBoxD IMyVoxelShape.GetWorldBoundary()
        {
            return GetWorldBoundaries();
        }

        BoundingBoxD IMyVoxelShape.PeekWorldBoundary(ref Vector3D targetPosition)
        {
            return PeekWorldBoundaries(ref targetPosition);
        }

        float IMyVoxelShape.GetIntersectionVolume(ref Vector3D voxelPosition)
        {
            return GetVolume(ref voxelPosition);
        }
    }

    public partial class MyShapeBox : IMyVoxelShapeBox
    {
        BoundingBoxD IMyVoxelShapeBox.Boundaries
        {
            get
            {
                return Boundaries;
            }
            set
            {
                Boundaries = value;
            }
        }
    }

    public partial class MyShapeSphere : IMyVoxelShapeSphere
    {
        Vector3D IMyVoxelShapeSphere.Center
        {
            get 
            {
                return Center;
            }
            set
            {
                Center = value;
            }
        }

        float IMyVoxelShapeSphere.Radius
        {
            get
            {
                return Radius;
            }
            set
            {
                Radius = value;
            }
        }
    }

    public partial class MyShapeCapsule : IMyVoxelShapeCapsule
    {
        Vector3D IMyVoxelShapeCapsule.A
        {
            get
            {
                return A;
            }
            set
            {
                A = value;
            }
        }

        Vector3D IMyVoxelShapeCapsule.B
        {
            get
            {
                return B;
            }
            set
            {
                B = value;
            }
        }

        float IMyVoxelShapeCapsule.Radius
        {
            get
            {
                return Radius;
            }
            set
            {
                Radius = value;
            }
        }
    }

    public partial class MyShapeRamp : IMyVoxelShapeRamp
    {
        BoundingBoxD IMyVoxelShapeRamp.Boundaries
        {
            get
            {
                return Boundaries;
            }
            set
            {
                Boundaries = value;
            }
        }

        Vector3D IMyVoxelShapeRamp.RampNormal
        {
            get
            {
                return RampNormal;
            }
            set
            {
                RampNormal = value;
            }
        }

        double IMyVoxelShapeRamp.RampNormalW
        {
            get
            {
                return RampNormalW;
            }
            set
            {
                RampNormalW = value;
            }
        }
    }
}
