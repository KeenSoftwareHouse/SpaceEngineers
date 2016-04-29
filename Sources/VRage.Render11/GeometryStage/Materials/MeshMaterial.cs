using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using Color = VRageMath.Color;
using SharpDX.D3DCompiler;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.Import;

namespace VRageRender
{
	struct MyMeshMaterialId
	{
		internal int Index;

		public static bool operator ==(MyMeshMaterialId x, MyMeshMaterialId y)
		{
			return x.Index == y.Index;
		}

		public static bool operator !=(MyMeshMaterialId x, MyMeshMaterialId y)
		{
			return x.Index != y.Index;
		}

		internal static readonly MyMeshMaterialId NULL = new MyMeshMaterialId { Index = -1 };

		internal MyMeshMaterialInfo Info { get { return MyMeshMaterials1.Table[Index]; } }
	}

    struct MyMeshMaterialInfo
    {
        internal MyMeshMaterialId Id; // key - direct index, out 
        internal int RepresentationKey; // key - external ref, out 
        internal MyStringId Name;
        internal string ContentPath;
        internal MyStringId ColorMetal_Texture;
        internal MyStringId NormalGloss_Texture;
        internal MyStringId Extensions_Texture;
        internal MyStringId Alphamask_Texture;
        internal string Technique;
        internal MyFacingEnum Facing;
        internal Vector2 WindScaleAndFreq;


        internal static void RequestResources(ref MyMeshMaterialInfo info)
        {
            MyTextures.GetTexture(info.ColorMetal_Texture, info.ContentPath, MyTextureEnum.COLOR_METAL, false, info.Facing == MyFacingEnum.Impostor);
            MyTextures.GetTexture(info.NormalGloss_Texture, info.ContentPath, MyTextureEnum.NORMALMAP_GLOSS);
            MyTextures.GetTexture(info.Extensions_Texture, info.ContentPath, MyTextureEnum.EXTENSIONS);
            MyTextures.GetTexture(info.Alphamask_Texture, info.ContentPath, MyTextureEnum.ALPHAMASK);
        }

        internal static MyMaterialProxy_2 CreateProxy(ref MyMeshMaterialInfo info)
        {
            var A = MyTextures.GetTexture(info.ColorMetal_Texture, info.ContentPath, MyTextureEnum.COLOR_METAL);
            var B = MyTextures.GetTexture(info.NormalGloss_Texture, info.ContentPath, MyTextureEnum.NORMALMAP_GLOSS);
            var C = MyTextures.GetTexture(info.Extensions_Texture, info.ContentPath, MyTextureEnum.EXTENSIONS);
            var D = MyTextures.GetTexture(info.Alphamask_Texture, info.ContentPath, MyTextureEnum.ALPHAMASK);

			var materialSrvs = new MySrvTable
					{ 
                        BindFlag = MyBindFlag.BIND_PS, 
                        StartSlot = 0,
                        SRVs = new IShaderResourceBindable[] { A, B, C, D },
                        Version = info.Id.GetHashCode()
                    };
            return
				new MyMaterialProxy_2 { MaterialSRVs = materialSrvs };
        }
    }

	class MyMeshMaterials1
	{
		#region DATA
		static MyFreelist<MyMeshMaterialInfo> MaterialsPool = new MyFreelist<MyMeshMaterialInfo>(256);

		internal static MyMeshMaterialInfo[] Table { get { return MaterialsPool.Data; } }

		static Dictionary<MyMeshMaterialId, MyMaterialProxyId> MaterialProxyIndex = new Dictionary<MyMeshMaterialId, MyMaterialProxyId>();
		internal static Dictionary<int, MyMeshMaterialId> MaterialRkIndex = new Dictionary<int, MyMeshMaterialId>();

		static Dictionary<MyStringId, MyMeshMaterialId> MaterialNameIndex = new Dictionary<MyStringId, MyMeshMaterialId>(MyStringId.Comparer); // only for uniquely named materials! used by destruction models

		internal static HashSet<int> MergableRKs = new HashSet<int>();

		static List<MyMeshMaterialId> MaterialQueryResourcesTable = new List<MyMeshMaterialId>();
		#endregion

		internal static MyMeshMaterialId DebugMaterialId;
		internal static MyMeshMaterialId NullMaterialId;

		static readonly HashSet<MyStringId> MERGABLE_MATERIAL_NAMES;
		static MyMeshMaterials1()
		{
			MERGABLE_MATERIAL_NAMES = new HashSet<MyStringId>(MyStringId.Comparer) ;
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("BlockSheet")); 
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("CubesSheet")); 
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("CubesMetalSheet")); 
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("RoofSheet")); 
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("StoneSheet")); 
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("House_Texture")); 
            MERGABLE_MATERIAL_NAMES.Add(X.TEXT_("RoofSheetRound"));
		}

		internal static bool IsMergable(MyMeshMaterialId matId)
		{
			return MergableRKs.Contains(Table[matId.Index].RepresentationKey);
		}

		internal static MyMeshMaterialId GetMaterialId(string name)
		{
			return MaterialNameIndex.Get(X.TEXT_(name));
		}

		internal static MyMaterialProxyId GetProxyId(MyMeshMaterialId id)
		{
			if (MaterialProxyIndex.ContainsKey(id))
			{
				return MaterialProxyIndex[id];
			}

			MyRender11.Log.WriteLine("MeshMaterialId missing");

			return MaterialProxyIndex[DebugMaterialId];
		}

		internal static int CalculateRK(ref MyMeshMaterialInfo desc)
		{
			var key = desc.ColorMetal_Texture.GetHashCode();
			MyHashHelper.Combine(ref key, desc.NormalGloss_Texture.GetHashCode());
			MyHashHelper.Combine(ref key, desc.Extensions_Texture.GetHashCode());
			MyHashHelper.Combine(ref key, desc.Alphamask_Texture.GetHashCode());
			MyHashHelper.Combine(ref key, desc.Technique.GetHashCode());

			MyHashHelper.Combine(ref key, desc.Name.GetHashCode());
			if (desc.ContentPath != null)
			{
				MyHashHelper.Combine(ref key, desc.ContentPath.GetHashCode());
			}

			return key;
		}

		internal static MyMeshMaterialId GetMaterialId(ref MyMeshMaterialInfo desc, string assetFile = null)
		{
			var rk = CalculateRK(ref desc);

			if (!MaterialRkIndex.ContainsKey(rk))
			{
				var id = MaterialRkIndex[rk] = new MyMeshMaterialId { Index = MaterialsPool.Allocate() };

				desc.Id = id;
				desc.RepresentationKey = rk;

				MaterialsPool.Data[id.Index] = desc;
				MaterialProxyIndex[id] = MyMaterials1.AllocateProxy();

				MaterialQueryResourcesTable.Add(id);

				var nameIndex = desc.Name;

				if (MERGABLE_MATERIAL_NAMES.Contains(nameIndex))
				{
					MergableRKs.Add(desc.RepresentationKey);
				}

				if (!MaterialNameIndex.ContainsKey(nameIndex))
				{
					MaterialNameIndex[nameIndex] = id;
				}
				else if (assetFile != null)
				{
					VRageRender.MyRender11.Log.WriteLine(String.Format("Asset {0} tries to overrwrite material {1} with different textures", assetFile, desc.Name.ToString()));
				}

				return id;
			}

			return MaterialRkIndex[rk];
		}

		internal static MyMeshMaterialId GetMaterialId(string name, string contentPath, string colorMetalTexture, string normalGlossTexture, string extensionTexture, string technique)
		{
			MyMeshMaterialInfo desc;
			desc = new MyMeshMaterialInfo
			{
				Name = X.TEXT_(name),
				ContentPath = contentPath,
				ColorMetal_Texture = X.TEXT_(colorMetalTexture),
				NormalGloss_Texture = X.TEXT_(normalGlossTexture),
				Extensions_Texture = X.TEXT_(extensionTexture),
				Technique = technique,
				Facing = MyFacingEnum.None,
			};

			return GetMaterialId(ref desc);
		}

		internal static MyMeshMaterialId GetMaterialId(MyMaterialDescriptor importDesc, string contentPath, string assetFile = null)
		{
			MyMeshMaterialInfo desc;
			if (importDesc != null)
			{
				desc = new MyMeshMaterialInfo
				{
					Name = X.TEXT_(importDesc.MaterialName),
					ContentPath = contentPath,
					ColorMetal_Texture = X.TEXT_(importDesc.Textures.Get("ColorMetalTexture", "")),
					NormalGloss_Texture = X.TEXT_(importDesc.Textures.Get("NormalGlossTexture", "")),
					Extensions_Texture = X.TEXT_(importDesc.Textures.Get("AddMapsTexture", "")),
					Alphamask_Texture = X.TEXT_(importDesc.Textures.Get("AlphamaskTexture", null)),
					Technique = importDesc.Technique,
					Facing = importDesc.Facing,
					WindScaleAndFreq = importDesc.WindScaleAndFreq
				};
			}
			else
			{
				return NullMaterialId;
			}

			return GetMaterialId(ref desc, assetFile);
		}

		internal static void OnResourcesRequesting()
		{
			foreach (var id in MaterialQueryResourcesTable)
			{
				// ask for resources
				MyMeshMaterialInfo.RequestResources(ref MaterialsPool.Data[id.Index]);
			}
		}

		internal static void OnResourcesGathering()
		{
			if (MaterialQueryResourcesTable.Count > 0)
			{
				// update proxies foreach material
				foreach (var id in MaterialQueryResourcesTable)
				{
					MyMaterials1.ProxyPool.Data[MaterialProxyIndex[id].Index] = MyMeshMaterialInfo.CreateProxy(ref MaterialsPool.Data[id.Index]);
				}
			}

			MaterialQueryResourcesTable.Clear();
		}

		internal static void CreateCommonMaterials()
		{
			var nullMatDesc = new MyMeshMaterialInfo
			{
				Name = X.TEXT_("__NULL_MATERIAL"),
				ColorMetal_Texture = MyStringId.NullOrEmpty,
				NormalGloss_Texture = MyStringId.NullOrEmpty,
				Extensions_Texture = MyStringId.NullOrEmpty,
				Alphamask_Texture = MyStringId.NullOrEmpty,
				Technique = "MESH"
			};
			NullMaterialId = GetMaterialId(ref nullMatDesc);

			var debugMatDesc = new MyMeshMaterialInfo
			{
				Name = X.TEXT_("__DEBUG_MATERIAL"),
				ColorMetal_Texture = MyRender11.DebugMode ? X.TEXT_("Pink") : MyStringId.NullOrEmpty,
				NormalGloss_Texture = MyStringId.NullOrEmpty,
				Extensions_Texture = MyStringId.NullOrEmpty,
				Alphamask_Texture = MyStringId.NullOrEmpty,
				Technique = "MESH"
			};
			DebugMaterialId = GetMaterialId(ref debugMatDesc);
		}

		internal static void Init()
		{
			//MyCallbacks.RegisterResourceRequestListener(new OnResourceRequestDelegate(OnResourcesRequesting));
			//MyCallbacks.RegisterResourceGatherListener(new OnResourceGatherDelegate(OnResourcesGathering));

			CreateCommonMaterials();
		}

        internal static void OnDeviceReset()
        {
            InvalidateMaterials();
        }

		internal static void InvalidateMaterials()
		{
			foreach (var id in MaterialRkIndex.Values)
			{
				MaterialQueryResourcesTable.Add(id);
			}
		}

		internal static void OnSessionEnd()
		{
			MergableRKs.Clear();
			MaterialQueryResourcesTable.Clear();
			MaterialRkIndex.Clear();
			MaterialsPool.Clear();
			MaterialProxyIndex.Clear();
			MaterialNameIndex.Clear();

			CreateCommonMaterials();
		}
	}

}
