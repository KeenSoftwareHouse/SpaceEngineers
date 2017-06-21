using System.Linq;
using Sandbox.Engine.Voxels;
using Sandbox.Engine.Voxels.Storage;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage.Input;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GUI.DebugInputComponents
{
    public partial class MyVoxelDebugInputComponent
    {
        private class StorageWriteCacheComponent : MyDebugComponent
        {
            private MyVoxelDebugInputComponent m_comp;

            private bool DisplayDetails = false;
            private bool DebugDraw = false;


            public StorageWriteCacheComponent(MyVoxelDebugInputComponent comp)
            {
                m_comp = comp;

                AddShortcut(MyKeys.NumPad1, true, false, false, false, () => "Toggle detailed details.", () => DisplayDetails = !DisplayDetails);
                AddShortcut(MyKeys.NumPad2, true, false, false, false, () => "Toggle debug draw.", () => DebugDraw = !DebugDraw);
                AddShortcut(MyKeys.NumPad3, true, false, false, false, () => "Toggle cache writing.", ToggleWrite);
                AddShortcut(MyKeys.NumPad4, true, false, false, false, () => "Toggle cache flushing.", ToggleFlush);
                AddShortcut(MyKeys.NumPad5, true, false, false, false, () => "Toggle cache.", ToggleCache);
            }

            private bool ToggleWrite()
            {
                var ops = MySession.Static.GetComponent<MyVoxelOperationsSessionComponent>();
                ops.ShouldWrite = !ops.ShouldWrite;

                return true;
            }

            private bool ToggleFlush()
            {
                var ops = MySession.Static.GetComponent<MyVoxelOperationsSessionComponent>();
                ops.ShouldFlush = !ops.ShouldFlush;

                return true;
            }

            private bool ToggleCache()
            {
                MyVoxelOperationsSessionComponent.EnableCache = !MyVoxelOperationsSessionComponent.EnableCache;

                return true;
            }

            public override void Draw()
            {
                base.Draw();

                if (MySession.Static == null) return;

                var ops = MySession.Static.GetComponent<MyVoxelOperationsSessionComponent>();

                if (ops != null)
                {
                    Text("Cache Enabled: {0}", MyVoxelOperationsSessionComponent.EnableCache);

                    Text("Cache Writing: {0}", ops.ShouldWrite ? "Enabled" : "Disabled");
                    Text("Cache Flushing: {0}", ops.ShouldFlush ? "Enabled" : "Disabled");

                    var storages = ops.QueuedStorages.ToArray();

                    if (storages.Length == 0)
                    {
                        Text(Color.Orange, "No queued storages.");
                    }
                    else
                    {
                        Text(Color.Yellow, 1.2f, "{0} Queued storages:", storages.Length);

                        foreach (var storage in storages)
                        {
                            MyStorageBase.WriteCacheStats stats;
                            storage.GetStats(out stats);
                            Text("Voxel storage {0}:", storage.ToString());
                            Text(Color.White, .9f, "Pending Writes: {0}", stats.QueuedWrites);
                            Text(Color.White, .9f, "Cached Chunks: {0}", stats.CachedChunks);

                            if (DisplayDetails)
                            {
                                foreach (var chunk in stats.Chunks)
                                {
                                    var ck = chunk.Value;
                                    Text(Color.Wheat, .9f, "Chunk {0}: {1} hits; pending {2}", chunk.Key, ck.HitCount, ck.Dirty);
                                }
                            }

                            if (DebugDraw)
                            {
                                var vmap = MySession.Static.VoxelMaps.Instances.FirstOrDefault(x => x.Storage == storage);

                                if (vmap == null) continue;

                                foreach (var chunk in stats.Chunks)
                                {
                                    var box = new BoundingBoxD(chunk.Key << MyStorageBase.VoxelChunk.SizeBits, ((chunk.Key + 1) << MyStorageBase.VoxelChunk.SizeBits));

                                    box.Translate(-((Vector3D)storage.Size * .5) - .5);

                                    MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(box, vmap.WorldMatrix), GetColorForDirty(chunk.Value.Dirty), 0.1f, true, true);
                                }
                            }
                        }
                    }
                }
            }

            private static Color GetColorForDirty(MyStorageDataTypeFlags dirty)
            {
                switch (dirty)
                {
                    case MyStorageDataTypeFlags.Content:
                        return Color.Blue;
                    case MyStorageDataTypeFlags.Material:
                        return Color.Red;
                    case MyStorageDataTypeFlags.ContentAndMaterial:
                        return Color.Magenta;
                    case MyStorageDataTypeFlags.None:
                        return Color.Green;
                    default:
                        return Color.White;
                }
            }

            public override string GetName()
            {
                return "Storage Write Cache";
            }
        }
    }
}
