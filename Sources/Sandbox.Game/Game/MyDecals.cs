using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Sandbox.Game
{
    public static class MyDecals
    {
        private const string DEFAULT = "Default";

        private static Func<IMyDecalProxy, bool> FilterProxy = DefaultFilterProxy;

        /// <param name="damage">Not used for now but could be used as a multiplier instead of random decal size</param>
        public static void HandleAddDecal(IMyEntity entity, MyHitInfo hitInfo, MyStringHash source = default(MyStringHash), float damage = -1)
        {
            IMyDecalProxy proxy = entity as IMyDecalProxy;
            if (proxy != null)
            {
                AddDecal(proxy, ref hitInfo, damage, source);
                return;
            }

            MyCubeGrid grid = entity.GetTopMostParent() as MyCubeGrid;
            if (grid != null)
            {
                var block = grid.GetTargetedBlock(hitInfo.Position);
                if (block != null)
                {
                    var compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                    if (compoundBlock == null)
                        proxy = block;
                    else
                        proxy = compoundBlock;
                }
            }

            if (proxy != null)
                AddDecal(proxy, ref hitInfo, damage, source);
        }

        private static void AddDecal(IMyDecalProxy proxy, ref MyHitInfo hitInfo, float damage, MyStringHash source)
        {
            bool skip = DefaultFilterProxy(proxy);
            if (skip)
                return;

            MyDecalRenderData data;
            proxy.GetDecalRenderData(hitInfo, out data);
            if (data.Skip)
                return;

            MyDecalMaterial material;
            bool found = MyDecalMaterials.TryGetDecalMaterial(data.Material.String, source.String, out material);
            if (!found)
            {
                if (MyFakes.ENABLE_USE_DEFAULT_DAMAGE_DECAL)
                    found = MyDecalMaterials.TryGetDecalMaterial(DEFAULT, DEFAULT, out material);

                if (!found)
                    return;
            }

            var perp = Vector3.CalculatePerpendicularVector(data.Normal);

            float rotation = material.Rotation;
            if (material.Rotation == float.PositiveInfinity)
                rotation = MyRandom.Instance.NextFloat() * MathHelper.TwoPi;

            if (rotation != 0)
            {
                // Rotate around normal
                Quaternion q = Quaternion.CreateFromAxisAngle(data.Normal, rotation);
                perp = new Vector3((new Quaternion(perp, 0) * q).ToVector4());
            }

            var pos = MatrixD.CreateWorld(data.Position, data.Normal, perp);

            var size = material.MinSize;
            if (material.MaxSize > material.MinSize)
                size += MyRandom.Instance.NextFloat() * (material.MaxSize - material.MinSize);

            pos = Matrix.CreateScale(new Vector3(size, size, material.Depth)) * pos;

            var decalId = MyRenderProxy.CreateDecal(data.RenderObjectId, pos, material.GetStringId());
            proxy.OnAddDecal(decalId, ref data);
        }

        public static void RemoveDecal(uint decalId)
        {
            MyRenderProxy.RemoveDecal(decalId);
        }

        /// <returns>True to skip the decal</returns>
        private static bool DefaultFilterProxy(IMyDecalProxy proxy)
        {
            // TODO: Evaluate it better and add the ability to customize the filter on the game.
            // For example support filtering basing genericallt on the entity, hithinfo, damage...

            // Cannon ball in ME
            if (proxy.ToString().StartsWith("MyRopeHookBlock"))
                return true;

            return false;
        }
    }
}
