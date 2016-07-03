﻿using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;

namespace VRageRender
{
    struct MyDrawSubmesh
    {
        [Flags]
        internal enum MySubmeshFlags
        {
            None = 0,
            Gbuffer = 1 << 0,
            Depth = 1 << 1,
            Forward = 1 << 2,
            All = 7
        }

        internal int IndexCount;
        internal int StartIndex;
        internal int BaseVertex;

        //internal MyMaterialProxy Material;
        internal MyMaterialProxyId MaterialId;
        internal int[] BonesMapping;
        internal MySubmeshFlags Flags;

        internal MyDrawSubmesh(int indexCount, int startIndex, int baseVertex, MyMaterialProxyId materialId, int[] bonesMapping = null, MySubmeshFlags flags = MySubmeshFlags.All)
        {
            IndexCount = indexCount;
            StartIndex = startIndex;
            BaseVertex = baseVertex;
            MaterialId = materialId;
            BonesMapping = bonesMapping;
            Flags = flags;
        }
    }

    struct MyVertexDataProxy_2
    {
        internal Buffer[] VB;
        internal int[] VertexStrides;
        internal Buffer IB;
        internal Format IndexFormat;
    }

    /// <summary>
    /// Contains data used for culling, but should not own any itself
    /// </summary>
    [PooledObject]
    class MyCullProxy
    {
        internal ulong[] SortingKeys;
        internal MyRenderableProxy [] RenderableProxies;
        internal uint FirstFullyContainingCascadeIndex;
        internal MyRenderableComponent Parent;
        internal bool Updated;

        internal uint OwnerID { get { return Parent != null ? Parent.Owner.ID : 0; } }

        [PooledObjectCleaner]
        public static void Clear(MyCullProxy cullProxy)
        {
            cullProxy.Clear();
        }

        internal void Clear()
        {
            SortingKeys = null;
            RenderableProxies = null;
            Updated = false;
            FirstFullyContainingCascadeIndex = uint.MaxValue;
            Parent = null;
        }
    }

    enum MyMaterialType
    {
        OPAQUE = 0,
        FORWARD = 1,
        ALPHA_MASKED = 2,
        TRANSPARENT = 3,
    }

    [Flags]
    enum MyRenderableProxyFlags
    {
        None = 0,
        DepthSkipTextures = 1 << 0,
        DisableFaceCulling = 1 << 1,
        SkipInMainView =  1 << 2,
		SkipIfTooSmall = 1 << 3,
		DrawOutsideViewDistance = 1 << 4,
		CastShadows = 1 << 5,
    }

    internal static class MyRenderableProxyFlagsExtensions
    {
        internal static bool HasFlags(this MyRenderableProxyFlags proxyFlags, MyRenderableProxyFlags flag)
        {
            return (proxyFlags & flag) == flag;
        }
    }

    /// <summary>
    /// Contains data needed to render an actor or part of it.
    /// Does not own any data
    /// </summary>
    [PooledObject]
    class MyRenderableProxy
    {
        internal const float NO_DITHER_FADE = Single.PositiveInfinity;

        internal MatrixD WorldMatrix;
        internal MyObjectDataCommon CommonObjectData;
        internal MyObjectDataNonVoxel NonVoxelObjectData;
        internal MyObjectDataVoxelCommon VoxelCommonObjectData;
        internal Matrix[] SkinningMatrices;

        internal LodMeshId Mesh;
        internal MyMergedLodMeshId MergedMesh;
        internal InstancingId Instancing;

        internal MyMaterialShadersBundleId DepthShaders;
        internal MyMaterialShadersBundleId Shaders;
        internal MyMaterialShadersBundleId ForwardShaders;

        internal MyDrawSubmesh DrawSubmesh;
        //internal uint DrawMaterialIndex; // assigned every frame (frame uses subset of materials index by variable below)
        internal int PerMaterialIndex; // assigned on proxy rebuild

        internal MyDrawSubmesh[] SectionSubmeshes;

        internal int InstanceCount;
        internal int StartInstance;

        internal bool InstancingEnabled { get { return Instancing != InstancingId.NULL; } }

        internal MyMaterialType Type;
        internal MyRenderableProxyFlags Flags;

        internal int Lod;

        internal Buffer ObjectBuffer; // different if instancing component/skinning components are on

        internal MyActorComponent Parent;

        internal MyStringId Material;

        [PooledObjectCleaner]
        public static void Clear(MyRenderableProxy renderableProxy)
        {
            renderableProxy.Clear();
        }

        internal void Clear()
        {
            WorldMatrix = MatrixD.Zero;
            CommonObjectData = default(MyObjectDataCommon);
            NonVoxelObjectData = MyObjectDataNonVoxel.Invalid;
            VoxelCommonObjectData = MyObjectDataVoxelCommon.Invalid;
            Mesh = LodMeshId.NULL;
            MergedMesh = MyMergedLodMeshId.NULL;
            Instancing = InstancingId.NULL;
            DepthShaders = MyMaterialShadersBundleId.NULL;
            Shaders = MyMaterialShadersBundleId.NULL;
            ForwardShaders = MyMaterialShadersBundleId.NULL;
            DrawSubmesh = default(MyDrawSubmesh);
            PerMaterialIndex = 0;
            SectionSubmeshes = null;
            InstanceCount = 0;
            StartInstance = 0;
            SkinningMatrices = null;
            Type = MyMaterialType.OPAQUE;
            Flags = 0;
            Lod = 0;
            ObjectBuffer = null;
            Parent = null;
            Material = MyStringId.NullOrEmpty;
        }
	};

    struct MyConstantsPack
    {
        internal byte[] Data;
        internal Buffer CB;
        internal int Version;
        internal MyBindFlag BindFlag;

        public override string ToString()
        {
            return string.Format(
                "Data Length {0}, {1}, Version {2}, BindFlags {3}", 
                Data != null ? Data.Length.ToString() : "null", 
                CB != null ? string.Format("CB Desc (BindFlags {0}, CpuAccessFlags {1}, OptionFlags {2}, Usage {3}, SizeInBytes {4}, StructureByteStride {5})",
                    CB.Description.BindFlags, CB.Description.CpuAccessFlags, CB.Description.OptionFlags, CB.Description.Usage, 
                    CB.Description.SizeInBytes, CB.Description.StructureByteStride): "CB null",
                Version, BindFlag
            );
        }
    }

    [Flags]
    enum MyBindFlag
    {
        BIND_VS = 1,
        BIND_PS = 2
    }
   
    struct MySrvTable
    {
        internal int StartSlot;
        internal IShaderResourceBindable[] SRVs;
        internal MyBindFlag BindFlag;
        internal int Version;
    }

    struct MyMaterialProxy_2
    {
        internal MyConstantsPack MaterialConstants;
        internal MySrvTable MaterialSRVs;
    }

    enum MyDrawCommandEnum
    {
        Draw,
        DrawIndexed
    }

    struct MyDrawSubmesh_2
    {
        internal int Count;
        internal int Start;
        internal int BaseVertex;
        internal MyDrawCommandEnum DrawCommand;

        internal MyMaterialProxyId MaterialId;
        internal int[] BonesMapping;

        // possible optimization - pooling for 1-4 length array allocations (used a LOT)
        internal static readonly MyDrawSubmesh_2[] EmptyList = new MyDrawSubmesh_2[0];
    }

    /// <summary>
    /// Renderable proxies for merge-instancing
    /// </summary>
    struct MyRenderableProxy_2
    {
        internal MyMaterialType MaterialType;
        internal MyRenderableProxyFlags RenderFlags;        

        // object
        internal MyConstantsPack ObjectConstants;
        internal MySrvTable ObjectSRVs;
        internal MyVertexDataProxy_2 VertexData;
        internal int InstanceCount;
        internal int StartInstance;

        // shader material 
        internal MyMergeInstancingShaderBundle DepthShaders;
        internal MyMergeInstancingShaderBundle Shaders;
        internal MyMergeInstancingShaderBundle HighlightShaders;
        internal MyMergeInstancingShaderBundle ForwardShaders;

        // drawcalls + material
        internal MyDrawSubmesh_2[] SubmeshesDepthOnly;
        internal MyDrawSubmesh_2[] Submeshes;
        internal MyDrawSubmesh_2[][] SectionSubmeshes;

        internal readonly static MyRenderableProxy_2[] EmptyList = new MyRenderableProxy_2[0];
        internal readonly static UInt64[] EmptyKeyList = new UInt64[0];
    }

    struct MyMergeInstancingShaderBundle
    {
        public MyMaterialShadersBundleId MultiInstance;
        public MyMaterialShadersBundleId SingleInstance;
    }

    static class MyProxiesFactory
    {
		internal static MyRenderableProxyFlags GetRenderableProxyFlags(RenderFlags flags)
		{
			MyRenderableProxyFlags proxyFlags = MyRenderableProxyFlags.None;

			if (flags.HasFlags(RenderFlags.SkipIfTooSmall))
				proxyFlags |= MyRenderableProxyFlags.SkipIfTooSmall;

            if (flags.HasFlags(RenderFlags.DrawOutsideViewDistance))
				proxyFlags |= MyRenderableProxyFlags.DrawOutsideViewDistance;

            if (flags.HasFlags(RenderFlags.CastShadows))
				proxyFlags |= MyRenderableProxyFlags.CastShadows;

            if (flags.HasFlags(RenderFlags.NoBackFaceCulling))
				proxyFlags |= MyRenderableProxyFlags.DisableFaceCulling;

            if (flags.HasFlags(RenderFlags.SkipInMainView) || !flags.HasFlags(RenderFlags.Visible))
                proxyFlags |= MyRenderableProxyFlags.SkipInMainView;

			return proxyFlags;
		}
    }
}
