﻿using Sandbox.Common.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public interface IMyVoxelDrawable
    {
        IMyStorage Storage
        {
            get;
        }

        Vector3I Size
        {
            get;
        }

        Vector3D PositionLeftBottomCorner
        {
            get;
        }

        Matrix Orientation
        {
            get;
        }

        Vector3I StorageMin
        {
            get;
        }

        MyRenderComponentBase Render
        {
            get;
        }

        MyClipmapScaleEnum ScaleGroup
        {
            get;
        }
    }
}
