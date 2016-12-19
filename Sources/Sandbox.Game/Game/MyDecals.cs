using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace Sandbox.Game
{
    public class MyDecals : IMyDecalHandler
    {
        private const string DEFAULT = "Default";

        private static MyCubeGridHitInfo m_gridHitInfo = new MyCubeGridHitInfo();
        private static MyDecals m_handler = new MyDecals();
        private static Func<IMyDecalProxy, bool> FilterProxy = DefaultFilterProxy;

        private bool Enabled;
        private MyStringHash Source;

        private MyDecals() { }

        /// <param name="damage">Not used for now but could be used as a multiplier instead of random decal size</param>
        public static void HandleAddDecal(IMyEntity entity, MyHitInfo hitInfo, MyStringHash material = default(MyStringHash), MyStringHash source = default(MyStringHash), object customdata = null, float damage = -1)
        {
            IMyDecalProxy proxy = entity as IMyDecalProxy;
            if (proxy != null)
            {
                AddDecal(proxy, ref hitInfo, damage, source, customdata, material);
                return;
            }

            MyCubeGrid grid = entity as MyCubeGrid;
            if (grid != null)
            {
                MyCubeGridHitInfo info = customdata as MyCubeGridHitInfo;
                MySlimBlock block;
                if (info == null)
                {
                    block = grid.GetTargetedBlock(hitInfo.Position);
                    if (block == null)
                        return;

                    // If info is not provided, provide info with just block position
                    m_gridHitInfo.Position = block.Position;
                    customdata = m_gridHitInfo;
                }
                else
                {
                    // If info is provided, lookup for the cube using provided position
                    MyCube cube;
                    bool found = grid.TryGetCube(info.Position, out cube);
                    if (!found)
                        return;

                    block = cube.CubeBlock;
                }

                var compoundBlock = block != null ? block.FatBlock as MyCompoundCubeBlock : null;
                if (compoundBlock == null)
                    proxy = block;
                else
                    proxy = compoundBlock;
            }

            if (proxy == null)
                return;

            AddDecal(proxy, ref hitInfo, damage, source, customdata, material);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateDecals(List<MyDecalPositionUpdate> decals)
        {
            MyRenderProxy.UpdateDecals(decals);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveDecal(uint decalId)
        {
            MyRenderProxy.RemoveDecal(decalId);
        }

        private static void AddDecal(IMyDecalProxy proxy, ref MyHitInfo hitInfo, float damage, MyStringHash source, object customdata, MyStringHash material)
        {
            bool skip = DefaultFilterProxy(proxy);
            if (skip)
                return;

            m_handler.Source = source;
            m_handler.Enabled = true;
            proxy.AddDecals(hitInfo, source, customdata, m_handler, material);
            m_handler.Enabled = false;
            m_handler.Source = MyStringHash.NullOrEmpty;
        }

        uint? IMyDecalHandler.AddDecal(ref MyDecalRenderInfo data)
        {
            CheckEnabled();

            IReadOnlyList<MyDecalMaterial> materials;
            bool found = MyDecalMaterials.TryGetDecalMaterial(Source.String, data.Material.String, out materials);
            if (!found)
            {
                if (MyFakes.ENABLE_USE_DEFAULT_DAMAGE_DECAL)
                    found = MyDecalMaterials.TryGetDecalMaterial(DEFAULT, DEFAULT, out materials);

                if (!found)
                    return null;
            }

            MyDecalBindingInfo binding;
            if (data.Binding == null)
            {
                binding = new MyDecalBindingInfo();
                binding.Position = data.Position;
                binding.Normal = data.Normal;
                binding.Transformation = Matrix.Identity;
            }
            else
            {
                binding = data.Binding.Value;
            }

            int index = (int)Math.Round(MyRandom.Instance.NextFloat() * (materials.Count - 1));
            MyDecalMaterial material = materials[index];

            float rotation = material.Rotation;
            if (material.Rotation == float.PositiveInfinity)
                rotation = MyRandom.Instance.NextFloat() * MathHelper.TwoPi;

            var bindingPerp = Vector3.CalculatePerpendicularVector(binding.Normal);
            if (rotation != 0)
            {
                // Rotate around normal
                Quaternion q = Quaternion.CreateFromAxisAngle(binding.Normal, rotation);
                bindingPerp = new Vector3((new Quaternion(bindingPerp, 0) * q).ToVector4());
            }
            bindingPerp = Vector3.Normalize(bindingPerp);

            var size = material.MinSize;
            if (material.MaxSize > material.MinSize)
                size += MyRandom.Instance.NextFloat() * (material.MaxSize - material.MinSize);

            Vector3 scale = new Vector3(size, size, material.Depth);
            MyDecalTopoData topoData = new MyDecalTopoData();

            Matrix pos;
            Vector3 worldPosition;
            if (data.Flags.HasFlag(MyDecalFlags.World))
            {
                // Using tre translation component here would loose accuracy
                pos = Matrix.CreateWorld(Vector3.Zero, binding.Normal, bindingPerp);
                worldPosition = data.Position;
            }
            else
            {
                pos = Matrix.CreateWorld(binding.Position, binding.Normal, bindingPerp);
                worldPosition = Vector3.Invalid; // Set in the render thread
            }

            topoData.MatrixBinding = Matrix.CreateScale(scale) * pos;
            topoData.WorldPosition = worldPosition;
            topoData.MatrixCurrent = binding.Transformation * topoData.MatrixBinding;
            topoData.BoneIndices = data.BoneIndices;
            topoData.BoneWeights = data.BoneWeights;

            MyDecalFlags flags = material.Transparent ? MyDecalFlags.Transparent : MyDecalFlags.None;

            string sourceTarget = MyDecalMaterials.GetStringId(Source, data.Material);
            return MyRenderProxy.CreateDecal(data.RenderObjectId, ref topoData, data.Flags | flags, sourceTarget, material.StringId, index);
        }

        [Conditional("DEBUG")]
        private void CheckEnabled()
        {
            if (!Enabled)
                throw new Exception("Decals must be added only on the IMyDecalProxy.AddDecals method override");
        }

        /// <returns>True to skip the decal</returns>
        private static bool DefaultFilterProxy(IMyDecalProxy proxy)
        {
            // TODO: Evaluate it better and add the ability to customize the filter on the game.
            // For example support filtering basing genericallt on the entity, hithinfo, damage...

            // Cannon ball in ME
            string tostring = proxy.ToString();
            if (tostring != null && tostring.StartsWith("MyRopeHookBlock"))
                return true;

            return false;
        }
    }
}
