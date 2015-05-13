using System;
using System.IO;
using System.Diagnostics;

using SharpDX;
using SharpDX.Direct3D9;
using VRage.Import;

namespace VRageRender
{
    internal class MyRenderMesh
    {
        internal const string C_POSTFIX_DIFFUSE_EMISSIVE = "_de";
        internal const string C_POSTFIX_MASK_EMISSIVE = "_me";
        internal const string C_POSTFIX_NORMAL_SPECULAR = "_ns";

        private readonly string m_assetName;
        public readonly MyRenderMeshMaterial Material = null;

        public string AssetName { get { return m_assetName; } }

        public int IndexStart;
        public int TriCount;

        public int[] BonesUsed = null;

        public float GlassDithering = 0;

        /// <summary>
        /// c-tor - generic way for collecting resources
        /// </summary>
        /// <param name="meshInfo"></param>
        /// assetName - just for debug output
        public MyRenderMesh(MyMeshPartInfo meshInfo, string assetName)
        {
            string contentDir = "";

            if (Path.IsPathRooted(assetName) && assetName.ToLower().Contains("models"))
                contentDir = assetName.Substring(0, assetName.ToLower().IndexOf("models"));

            MyMaterialDescriptor matDesc = meshInfo.m_MaterialDesc;
            if (matDesc != null)
            {
                bool hasNormalTexture = true;

                string normalPath = null;
                string diffusePath;
                matDesc.Textures.TryGetValue("DiffuseTexture", out diffusePath);
                matDesc.Textures.TryGetValue("NormalTexture", out normalPath);

                if (String.IsNullOrEmpty(normalPath) && !String.IsNullOrEmpty(diffusePath))
                {
                    if(String.IsNullOrEmpty(diffusePath))
                    {
                        diffusePath = null;
                        normalPath = null;
                    }
                    else
                    {
                        string ext = Path.GetExtension(diffusePath);
                        string deMatch = C_POSTFIX_DIFFUSE_EMISSIVE + ext;
                        string meMatch = C_POSTFIX_MASK_EMISSIVE + ext;

                        if (diffusePath.EndsWith(deMatch))
                            normalPath = diffusePath.Substring(0, diffusePath.Length - deMatch.Length) + C_POSTFIX_NORMAL_SPECULAR + ext;
                        else if (diffusePath.EndsWith(meMatch))
                            normalPath = diffusePath.Substring(0, diffusePath.Length - meMatch.Length) + C_POSTFIX_NORMAL_SPECULAR + ext;
                        else
                            normalPath = null;
                    }
                }

                Material = new MyRenderMeshMaterial(matDesc.MaterialName,
                                                          contentDir,
                                                          diffusePath,
                                                          normalPath,
                                                          matDesc.SpecularPower,
                                                          matDesc.SpecularIntensity,
                                                          hasNormalTexture,
                                                          matDesc.DiffuseColor,
                                                          matDesc.ExtraData);

                Material.DrawTechnique = meshInfo.Technique;

                m_assetName = assetName;
            }
            else
            {
                //It is OK because ie. collision meshes dont have materials
                //MyCommonDebugUtils.AssertRelease(false, String.Format("Model {0} has bad material for mesh.", assetName));
                Trace.TraceWarning("Model with null material: " + assetName);

                //We define at least debug material
                VRageMath.Vector3 color = VRageMath.Color.Pink.ToVector3();
                Material = new MyRenderMeshMaterial("", "", @"Textures\Models\Debug\white_de.dds", @"Textures\Models\Debug\white_ns.dds", 0, 0, true, color, color);
            }
        }

        /// <summary>
        /// Render
        /// </summary>
        /// <param name="grDevice"></param>
        /// <param name="vertexCount"></param>
        /// <param name="triCount"></param>
        public void Render(Device grDevice, int vertexCount)
        {
            grDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, vertexCount, IndexStart, TriCount);
            MyPerformanceCounter.PerCameraDrawWrite.TotalDrawCalls++;
        }
    }
}
