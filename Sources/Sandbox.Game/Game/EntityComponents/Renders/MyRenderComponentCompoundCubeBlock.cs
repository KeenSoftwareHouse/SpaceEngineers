using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using VRageRender;
using System.Diagnostics;
using VRageMath;

namespace Sandbox.Game.Components
{
    class MyRenderComponentCompoundCubeBlock : MyRenderComponentCubeBlock
    {
        public override void InvalidateRenderObjects(bool sortIntoCullobjects = false)
        {
            base.InvalidateRenderObjects(sortIntoCullobjects);

            MyCompoundCubeBlock compoundBlock = m_cubeBlock as MyCompoundCubeBlock;

            foreach (var block in compoundBlock.GetBlocks())
            {
                if (block.FatBlock != null)
                {
                    if ((block.FatBlock.Render.Visible || block.FatBlock.Render.CastShadows) && block.FatBlock.InScene && block.FatBlock.InvalidateOnMove)
                    {
                        foreach (uint renderObjectID in block.FatBlock.Render.RenderObjectIDs)
                        {
                            MatrixD worldMatrix = block.FatBlock.WorldMatrix;
                            VRageRender.MyRenderProxy.UpdateRenderObject(renderObjectID, ref worldMatrix, sortIntoCullobjects);
                        }
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        public override void AddRenderObjects()
        {
            // Do not set render obejct instead call Invalidate on children.
            InvalidateRenderObjects();
        }

    }
}
