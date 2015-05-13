using System;
using System.IO;

using Sandbox;
using System.Diagnostics;


//using VRageMath.Graphics;

using VRage.Import;
using VRageRender;
using VRage.Utils;

namespace Sandbox.Common
{
    public class MyMesh
    {
        private const string C_CONTENT_ID = "Content\\";
        private const string C_POSTFIX_DIFFUSE = "_d";
        internal const string C_POSTFIX_DIFFUSE_EMISSIVE = "_de";
        internal const string C_POSTFIX_MASK_EMISSIVE = "_me";
        private const string C_POSTFIX_DONT_HAVE_NORMAL = "_dn";
        internal const string C_POSTFIX_NORMAL_SPECULAR = "_ns";
        private const string DEFAULT_DIRECTORY = "\\v01\\";

        public readonly string AssetName;
        public readonly MyMeshMaterial Material = null;

        public int IndexStart;
        public int TriStart;
        public int TriCount;

        /// <summary>
        /// c-tor - generic way for collecting resources
        /// </summary>
        /// <param name="meshInfo"></param>
        /// assetName - just for debug output
        public MyMesh(MyMeshPartInfo meshInfo, string assetName)
        {
            MyMaterialDescriptor matDesc = meshInfo.m_MaterialDesc;
            if (matDesc != null)
            {
                string texName;
                matDesc.Textures.TryGetValue("DiffuseTexture", out texName);

                var material = new MyMeshMaterial();
                material.Name = meshInfo.m_MaterialDesc.MaterialName;
                material.DiffuseTexture = texName;
                material.Textures = matDesc.Textures;
                material.SpecularIntensity = meshInfo.m_MaterialDesc.SpecularIntensity;
                material.SpecularPower = meshInfo.m_MaterialDesc.SpecularPower;
                material.DrawTechnique = meshInfo.Technique;
                material.GlassCW = meshInfo.m_MaterialDesc.GlassCW;
                material.GlassCCW = meshInfo.m_MaterialDesc.GlassCCW;
                material.GlassSmooth = meshInfo.m_MaterialDesc.GlassSmoothNormals;

                Material = material;
            }
            else
            {
                //It is OK because ie. collision meshes dont have materials
                Material = new MyMeshMaterial();
            }

            AssetName = assetName;
        }
    }
}
