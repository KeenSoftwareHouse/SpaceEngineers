using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRageMath;

namespace Sandbox.Game.Components
{
    public static class MyRenderComponentExtensions
    {
        public unsafe static void CalculateBlockDepthBias(this MyRenderComponent renderComponent, MyCubeBlock block)
        {
            if (block.Hierarchy != null)
            {
                var parentCompound = block.Hierarchy.Parent.Entity as MyCompoundCubeBlock;
                if (parentCompound != null)
                {
                    const int offsetCount = 64;
                    bool* offsets = stackalloc bool[offsetCount];

                    foreach (var block2 in parentCompound.GetBlocks())
                    {
                        if (block2.FatBlock != null && block2.FatBlock != block)
                        {
                            var cubeBlockRender = block2.FatBlock.Render as MyRenderComponentBase;
                            if (cubeBlockRender != null)
                                offsets[cubeBlockRender.DepthBias] = true;
                        }
                    }

                    int preferedOffset = 0;
                    var modelStorage = renderComponent.ModelStorage as VRage.Game.Models.MyModel;
                    if (modelStorage != null)
                    {
                        Vector3 blockCenterLocal = modelStorage.BoundingSphere.Center;
                        MatrixI blockOrientation = new MatrixI(block.SlimBlock.Orientation);
                        Vector3 blockCenter = new Vector3();
                        Vector3.Transform(ref blockCenterLocal, ref blockOrientation, out blockCenter);
                        if (blockCenter.LengthSquared() > 0.5f)
                        {
                            if (Math.Abs(blockCenter.X) > Math.Abs(blockCenter.Y))
                            {
                                if (Math.Abs(blockCenter.X) > Math.Abs(blockCenter.Z))
                                {
                                    preferedOffset = blockCenter.X > 0 ? 2 : 4;
                                }
                                else
                                {
                                    preferedOffset = blockCenter.Z > 0 ? 10 : 12;
                                }
                            }
                            else
                            {
                                if (Math.Abs(blockCenter.Z) > Math.Abs(blockCenter.Y))
                                {
                                    preferedOffset = blockCenter.Z > 0 ? 10 : 12;
                                }
                                else
                                {
                                    preferedOffset = blockCenter.Y > 0 ? 6 : 8;
                                }
                            }
                        }
                    }

                    for (int offsetIndex = preferedOffset; offsetIndex < offsetCount; ++offsetIndex)
                    {
                        if (!offsets[offsetIndex])
                        {
                            renderComponent.DepthBias = (byte)offsetIndex;
                            break;
                        }
                    }
                }
            }
        }
    }
}
