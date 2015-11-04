using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Generics;
using VRage.Utils;
using VRageMath;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;


namespace VRageRender
{
    struct MyDrawSubmesh
    {
        internal int IndexCount;
        internal int StartIndex;
        internal int BaseVertex;

        //internal MyMaterialProxy Material;
        internal MyMaterialProxyId MaterialId;
        internal int[] BonesMapping;

        internal MyDrawSubmesh(int indexCount, int startIndex, int baseVertex, MyMaterialProxyId materialId, int[] bonesMapping = null)
        {
            IndexCount = indexCount;
            StartIndex = startIndex;
            BaseVertex = baseVertex;
            MaterialId = materialId;
            BonesMapping = bonesMapping;
        }

        internal static MyDrawSubmesh[] MergeSubmeshes(MyDrawSubmesh[] list)
        {
            Array.Sort(list, (x, y) => x.StartIndex.CompareTo(y.StartIndex));
            List<MyDrawSubmesh> merged = new List<MyDrawSubmesh>();


            //MyDrawSubmesh first = list[0];
            //for (int i = 1; i < list.Length; i++)
            //{
            //    if (list[i].StartIndex == first.StartIndex + first.IndexCount && list[i].BaseVertex == first.BaseVertex)
            //    {
            //        first.IndexCount += list[i].IndexCount;
            //    }
            //    else
            //    {
            //        merged.Add(first);
            //        first = list[i];
            //    }
            //}
            //merged.Add(first);


            // more aggresive
            bool ok = true;
            for (int i = 1; i < list.Length; i++)
            {
                if (list[i].BaseVertex != list[0].BaseVertex)
                {
                    ok = false;
                    break;
                }
            }
            if (!ok)
            { 
                return list;
            }

            MyDrawSubmesh m = list[0];
            var last = list[list.Length - 1];
            m.IndexCount = last.IndexCount + last.StartIndex - m.StartIndex;
            merged.Add(m);

            return merged.ToArray();
        }

        internal static MyDrawSubmesh[] MergeSubmeshes(MyDrawSubmesh[] listA, MyDrawSubmesh[] listB)
        {
            if(listA != null)
                return MergeSubmeshes(listA.Concat(listB).ToArray());
            return MergeSubmeshes(listB);
        }
    }

    struct MyVertexDataProxy_2
    {
        internal Buffer[] VB;
        internal int[] VertexStrides;
        internal Buffer IB;
        internal Format IndexFormat;
    }

    class MyCullProxy
    {
        internal UInt64 [] SortingKeys;
        internal MyRenderableProxy [] Proxies;
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

    // should NOT own any data!
    class MyRenderableProxy
    {
        internal const float NO_DITHER_FADE = Single.PositiveInfinity;

        internal MatrixD WorldMatrix;
        internal MyObjectData ObjectData;

        internal LodMeshId Mesh;
        internal InstancingId Instancing;

        internal MyMaterialShadersBundleId DepthShaders;
        internal MyMaterialShadersBundleId Shaders;
        internal MyMaterialShadersBundleId ForwardShaders;

        internal MyDrawSubmesh DrawSubmesh;
        //internal uint DrawMaterialIndex; // assigned every frame (frame uses subset of materials index by variable below)
        internal int PerMaterialIndex; // assigned on proxy rebuild

        internal int InstanceCount;
        internal int StartInstance;

        internal bool InstancingEnabled { get { return Instancing != InstancingId.NULL; } }

        internal Matrix[] SkinningMatrices;

        internal MyMaterialType Type;
        internal MyRenderableProxyFlags Flags;

        internal int Lod;

        internal Buffer ObjectBuffer; // different if instancing component/skinning components are on

        internal MyActorComponent Parent;

        internal MyStringId Material;

		#region Methods
		public bool SkipIfTooSmall()
		{
			if ((Flags & MyRenderableProxyFlags.SkipIfTooSmall) == MyRenderableProxyFlags.SkipIfTooSmall)
			{
				var distanceFromCamera = Vector3D.Distance(MyEnvironment.CameraPosition, WorldMatrix.Translation);
				float cullRatio = MyRenderConstants.DISTANCE_CULL_RATIO;
				if (Parent.m_owner.Aabb.HalfExtents.Length() < distanceFromCamera / cullRatio)
					return true;
			}
			return false;
		}

		public bool IsInViewDistance()
		{
			if ((Flags & MyRenderableProxyFlags.DrawOutsideViewDistance) == 0)
			{
                var distanceFromCamera = Parent.m_owner.CalculateCameraDistance();
                if (distanceFromCamera > MyEnvironment.FarClipping)
                    return false;
			}

			return true;
		}
		#endregion
	};

    struct MyConstantsPack
    {
        internal byte[] Data;
        internal Buffer CB;
        internal int Version;
        internal MyBindFlag BindFlag;
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
        internal ShaderResourceView [] SRVs;
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
        internal MyMaterialShadersBundleId DepthShaders;
        internal MyMaterialShadersBundleId Shaders;
        internal MyMaterialShadersBundleId ForwardShaders;

        // drawcalls + material
        internal MyDrawSubmesh_2[] SubmeshesDepthOnly;
        internal MyDrawSubmesh_2[] Submeshes;


        internal readonly static MyRenderableProxy_2[] EmptyList = new MyRenderableProxy_2[0];
        internal readonly static UInt64[] EmptyKeyList = new UInt64[0];
    }

    

    static class MyProxiesFactory
    {
        static MyObjectsPool<MyCullProxy> m_cullProxyPool = new MyObjectsPool<MyCullProxy>(100);
        static MyObjectsPool<MyRenderableProxy> m_renderableProxyPool = new MyObjectsPool<MyRenderableProxy>(200);

        internal static MyCullProxy CreateCullProxy()
        {
            MyCullProxy item;
            m_cullProxyPool.AllocateOrCreate(out item);
            return item;
        }

        internal static void Remove(MyCullProxy proxy)
        {
            m_cullProxyPool.Deallocate(proxy);
        }

        internal static MyRenderableProxy CreateRenderableProxy()
        {
            MyRenderableProxy item;
            m_renderableProxyPool.AllocateOrCreate(out item);

            //item.geometry = null;
            item.Mesh = LodMeshId.NULL;
            item.Instancing = InstancingId.NULL;
            item.DepthShaders = MyMaterialShadersBundleId.NULL;
            item.Shaders = MyMaterialShadersBundleId.NULL;
            //item.depthOnlyShaders = null;
            //item.shaders = null;
            item.SkinningMatrices = null;
            //item.depthOnlySubmeshes = null;
            //item.submeshes = null;
            item.InstanceCount = 0;
            item.Flags = 0;
            item.Type = MyMaterialType.OPAQUE;
            item.ObjectBuffer = null;
            item.Parent = null;
            item.Lod = 0;

            return item;
        }

        internal static void Remove(MyRenderableProxy proxy)
        {
            m_renderableProxyPool.Deallocate(proxy);
        }

		internal static MyRenderableProxyFlags GetRenderableProxyFlags(RenderFlags flags)
		{
			MyRenderableProxyFlags proxyFlags = MyRenderableProxyFlags.None;

			if ((flags & RenderFlags.SkipIfTooSmall) == RenderFlags.SkipIfTooSmall)
				proxyFlags |= MyRenderableProxyFlags.SkipIfTooSmall;

			if ((flags & RenderFlags.DrawOutsideViewDistance) == RenderFlags.DrawOutsideViewDistance)
				proxyFlags |= MyRenderableProxyFlags.DrawOutsideViewDistance;

			if ((flags & RenderFlags.CastShadows) == RenderFlags.CastShadows)
				proxyFlags |= MyRenderableProxyFlags.CastShadows;

			if ((flags & RenderFlags.NoBackFaceCulling) == RenderFlags.NoBackFaceCulling)
				proxyFlags |= MyRenderableProxyFlags.DisableFaceCulling;

			return proxyFlags;
		}
    }
}
