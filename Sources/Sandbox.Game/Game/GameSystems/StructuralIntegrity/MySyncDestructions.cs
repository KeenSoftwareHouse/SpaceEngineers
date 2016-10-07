#region Using

using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Game.Components;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Serialization;

#endregion

namespace Sandbox.Game.Multiplayer
{
    [StaticEventOwner]
    [PreloadRequired]
    public static class MySyncDestructions
    {
        public static void AddDestructionEffect(MyParticleEffectsIDEnum effectId, Vector3D position, Vector3 direction, float scale)
        {
            MyMultiplayer.RaiseStaticEvent(s => MySyncDestructions.OnAddDestructionEffectMessage, effectId, position, direction, scale);
        }
        
        [Event, Server, Broadcast]
        static void OnAddDestructionEffectMessage(MyParticleEffectsIDEnum effectId, Vector3D position, Vector3 direction, float scale)
        {
            MyGridPhysics.CreateDestructionEffect(effectId, position, direction, scale);
        }

        public static void CreateFracturePiece(MyObjectBuilder_FracturedPiece fracturePiece)
        {
            Debug.Assert(Sync.IsServer);
            MyMultiplayer.RaiseStaticEvent(s => MySyncDestructions.OnCreateFracturePieceMessage, fracturePiece);
        }

        [Event, Reliable, Broadcast]
        static void OnCreateFracturePieceMessage(
            [Serialize(MyObjectFlags.Dynamic, DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))]MyObjectBuilder_FracturedPiece fracturePiece)
        {
            var fracturedPiece = MyFracturedPiecesManager.Static.GetPieceFromPool(fracturePiece.EntityId, true);
            Debug.Assert(fracturePiece.EntityId != 0, "Fracture piece without ID");
            try
            {
                fracturedPiece.Init(fracturePiece);
                MyEntities.Add(fracturedPiece);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Cannot add fracture piece: " + e.Message);
                if (fracturedPiece == null)
                    return;

                MyFracturedPiecesManager.Static.RemoveFracturePiece(fracturedPiece, 0, true, false);
                StringBuilder sb = new StringBuilder();
                foreach (var shape in fracturePiece.Shapes)
                {
                    sb.Append(shape.Name).Append(" ");
                }
                Debug.Fail("Recieved fracture piece not added");
                MyLog.Default.WriteLine("Received fracture piece not added, no shape found. Shapes: " + sb.ToString());
            }
        }

        public static void RemoveFracturePiece(long entityId, float blendTime)
        {
            Debug.Assert(Sync.IsServer);

            MyMultiplayer.RaiseStaticEvent(s => MySyncDestructions.OnRemoveFracturePieceMessage, entityId, blendTime);
        }

        [Event, Reliable, Broadcast]
        static void OnRemoveFracturePieceMessage(long entityId, float blendTime)
        {
            Debug.Assert(!Sync.IsServer);

            MyFracturedPiece fracturePiece;
            if (MyEntities.TryGetEntityById(entityId, out fracturePiece))
            {
                MyFracturedPiecesManager.Static.RemoveFracturePiece(fracturePiece, blendTime, fromServer: true, sync: false);
            }
            else
            {
                System.Diagnostics.Debug.Fail("Not existing fracture piece");
            }
        }

        [Conditional("DEBUG")]
        public static void FPManagerDbgMessage(long createdId, long removedId)
        {
            MyMultiplayer.RaiseStaticEvent(s => MySyncDestructions.OnFPManagerDbgMessage, createdId, removedId);
        }

        [Event, Reliable, Server]
        static void OnFPManagerDbgMessage(long createdId, long removedId)
        {
            MyFracturedPiecesManager.Static.DbgCheck(createdId, removedId);
        }

        public static void CreateFracturedBlock(MyObjectBuilder_FracturedBlock fracturedBlock, long gridId, Vector3I position)
        {
            Debug.Assert(Sync.IsServer);

            MyMultiplayer.RaiseStaticEvent(s => MySyncDestructions.OnCreateFracturedBlockMessage, gridId, position, fracturedBlock);
        }

        [Event, Reliable, Broadcast]
        static void OnCreateFracturedBlockMessage(long gridId, Vector3I position,
            [Serialize(MyObjectFlags.Dynamic, DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))]MyObjectBuilder_FracturedBlock fracturedBlock)
        {
            MyCubeGrid grid;
            if (MyEntities.TryGetEntityById(gridId, out grid))
            {
                grid.EnableGenerators(false, true);
                grid.CreateFracturedBlock(fracturedBlock, position);
                grid.EnableGenerators(true, true);
            }
        }

        public static void CreateFractureComponent(long gridId, Vector3I position, ushort compoundBlockId, MyObjectBuilder_FractureComponentBase component)
        {
            Debug.Assert(Sync.IsServer);
            Debug.Assert(gridId != 0);
            Debug.Assert(component.Shapes != null && component.Shapes.Count > 0);

            MyMultiplayer.RaiseStaticEvent(s => MySyncDestructions.OnCreateFractureComponentMessage, gridId, position, compoundBlockId, component);
        }

        [Event, Reliable, Broadcast]
        static void OnCreateFractureComponentMessage(long gridId, Vector3I position, ushort compoundBlockId,
            [Serialize(MyObjectFlags.Dynamic, DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))]MyObjectBuilder_FractureComponentBase component)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(gridId, out entity))
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                var cubeBlock = grid.GetCubeBlock(position);
                if (cubeBlock != null && cubeBlock.FatBlock != null)
                {
                    var compound = cubeBlock.FatBlock as MyCompoundCubeBlock;
                    if (compound != null)
                    {
                        var blockInCompound = compound.GetBlock(compoundBlockId);
                        if (blockInCompound != null)
                            AddFractureComponent(component, blockInCompound.FatBlock);
                    }
                    else
                    {
                        AddFractureComponent(component, cubeBlock.FatBlock);
                    }
                }
            }
        }

        private static void AddFractureComponent(MyObjectBuilder_FractureComponentBase obFractureComponent, MyEntity entity)
        {
            var component = MyComponentFactory.CreateInstanceByTypeId(obFractureComponent.TypeId);
            var fractureComponent = component as MyFractureComponentBase;
            Debug.Assert(fractureComponent != null);
            if (fractureComponent != null)
            {
                try
                {
                    bool hasComponent = entity.Components.Has<MyFractureComponentBase>();
                    Debug.Assert(!hasComponent);
                    if (!hasComponent)
                    {
                        entity.Components.Add<MyFractureComponentBase>(fractureComponent);
                        fractureComponent.Deserialize(obFractureComponent);
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("Cannot add received fracture component: " + e.Message);
                    if (entity.Components.Has<MyFractureComponentBase>())
                    {
                        MyCubeBlock block = entity as MyCubeBlock;
                        if (block != null && block.SlimBlock != null)
                        {
                            block.SlimBlock.RemoveFractureComponent();
                        }
                        else
                        {
                            Debug.Fail("Fracture component not supported for other entities than MyCubeBlock for now!");
                            // Renderer has to be set to entity default because it is changed in fracture component.
                            entity.Components.Remove<MyFractureComponentBase>();
                        }
                    }

                    StringBuilder sb = new StringBuilder();
                    foreach (var shape in obFractureComponent.Shapes)
                        sb.Append(shape.Name).Append(" ");

                    Debug.Fail("Recieved fracture component not added");
                    MyLog.Default.WriteLine("Received fracture component not added, no shape found. Shapes: " + sb.ToString());
                }
            }
        }

        public static void RemoveShapeFromFractureComponent(long gridId, Vector3I position, ushort compoundBlockId, string shapeName)
        {
            Debug.Assert(Sync.IsServer);
            Debug.Assert(gridId != 0);
            Debug.Assert(shapeName != null && !string.IsNullOrEmpty(shapeName));

            MyMultiplayer.RaiseStaticEvent(s => MySyncDestructions.OnRemoveShapeFromFractureComponentMessage, gridId, position, compoundBlockId, new string[] { shapeName });
        }

        public static void RemoveShapesFromFractureComponent(long gridId, Vector3I position, ushort compoundBlockId, List<string> shapeNames)
        {
            Debug.Assert(Sync.IsServer);
            Debug.Assert(gridId != 0);
            Debug.Assert(shapeNames != null && shapeNames.Count > 0);

            MyMultiplayer.RaiseStaticEvent(s => MySyncDestructions.OnRemoveShapeFromFractureComponentMessage, gridId, position, compoundBlockId, shapeNames.ToArray());
        }

        [Event, Reliable, Broadcast]
        static void OnRemoveShapeFromFractureComponentMessage(long gridId, Vector3I position, ushort compoundBlockId, string[] shapeNames)
        {
            Debug.Assert(shapeNames != null && shapeNames.Length > 0);

            MyEntity entity;
            if (MyEntities.TryGetEntityById(gridId, out entity))
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                var cubeBlock = grid.GetCubeBlock(position);
                if (cubeBlock != null && cubeBlock.FatBlock != null)
                {
                    var compound = cubeBlock.FatBlock as MyCompoundCubeBlock;
                    if (compound != null)
                    {
                        var blockInCompound = compound.GetBlock(compoundBlockId);
                        if (blockInCompound != null)
                            RemoveFractureComponentChildShapes(blockInCompound, shapeNames);
                    }
                    else
                    {
                        RemoveFractureComponentChildShapes(cubeBlock, shapeNames);
                    }
                }
            }
        }

        private static void RemoveFractureComponentChildShapes(MySlimBlock block, string[] shapeNames)
        {
            var component = block.GetFractureComponent();
            if (component != null)
            {
                component.RemoveChildShapes(shapeNames);
            }
            else
            {
                Debug.Fail("Cannot remove child shapes from fracture component, fracture component not found in block");
                MyLog.Default.WriteLine("Cannot remove child shapes from fracture component, fracture component not found in block, BlockDefinition: "
                    + block.BlockDefinition.Id.ToString() + ", Shapes: " + string.Join(", ", shapeNames));
            }
        }

        /// <summary>
        /// WARNING: OLD METHOD. Do not use. use MyDecaySystem now.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        public static void RemoveFracturedPiecesRequest(ulong userId, Vector3D center, float radius)
        {
            if (Sync.IsServer)
            {
                MyFracturedPiecesManager.Static.RemoveFracturesInSphere(center, radius);
            }
            else
            {
                MyMultiplayer.RaiseStaticEvent(s => MySyncDestructions.OnRemoveFracturedPiecesMessage, userId, center, radius);
            }
        }

        [Event, Reliable, Server]
        static void OnRemoveFracturedPiecesMessage(ulong userId, Vector3D center, float radius)
        {
            if (MyMultiplayer.Static != null && MyMultiplayer.Static.IsAdmin(userId))
                MyFracturedPiecesManager.Static.RemoveFracturesInSphere(center, radius);
        }
    }
}
