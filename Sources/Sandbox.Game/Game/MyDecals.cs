using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class MyDecals : IMyDecalHandler
    {
        private const string DEFAULT = "Default";

        private static MyDecals m_handler = new MyDecals();
        private static Func<IMyDecalProxy, bool> FilterProxy = DefaultFilterProxy;

        private bool Enabled;
        private MyStringHash Source;

        private MyDecals() { }

        /// <param name="damage">Not used for now but could be used as a multiplier instead of random decal size</param>
        public static void HandleAddDecal(IMyEntity entity, MyHitInfo hitInfo, MyStringHash source = default(MyStringHash), object customdata = null, float damage = -1)
        {
            IMyDecalProxy proxy = entity as IMyDecalProxy;
            if (proxy != null)
            {
                AddDecal(proxy, ref hitInfo, damage, source, customdata);
                return;
            }

            MyCubeGrid grid = entity as MyCubeGrid;
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

            if (proxy == null)
                return;

            AddDecal(proxy, ref hitInfo, damage, source, customdata);
        }

        public static void UpdateDecals(List<MyDecalPositionUpdate> decals)
        {
            MyRenderProxy.UpdateDecals(decals);
        }

        public static void RemoveDecal(uint decalId)
        {
            MyRenderProxy.RemoveDecal(decalId);
        }

        private static void AddDecal(IMyDecalProxy proxy, ref MyHitInfo hitInfo, float damage, MyStringHash source, object customdata)
        {
            bool skip = DefaultFilterProxy(proxy);
            if (skip)
                return;

            m_handler.Source = source;
            m_handler.Enabled = true;
            proxy.AddDecals(hitInfo, source, customdata, m_handler);
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

            int index = (int)Math.Round(MyRandom.Instance.NextFloat() * (materials.Count - 1));
            MyDecalMaterial material = materials[index];

            float rotation = material.Rotation;
            if (material.Rotation == float.PositiveInfinity)
                rotation = MyRandom.Instance.NextFloat() * MathHelper.TwoPi;

            var size = material.MinSize;
            if (material.MaxSize > material.MinSize)
                size += MyRandom.Instance.NextFloat() * (material.MaxSize - material.MinSize);

            MyDecalTopoData topoData = new MyDecalTopoData();
            topoData.Position = data.Position;
            topoData.Normal = data.Normal;
            topoData.Scale = new Vector3(size, size, material.Depth);
            topoData.Rotation = rotation;

            string sourceTarget = MyDecalMaterials.GetStringId(Source, data.Material);
            return MyRenderProxy.CreateDecal(data.RenderObjectId, ref topoData, data.Flags, sourceTarget, material.StringId, index);
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
