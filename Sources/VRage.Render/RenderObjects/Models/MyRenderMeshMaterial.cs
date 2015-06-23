using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using VRage.Import;
using VRageMath;
using VRageRender.Textures;
using VRageRender.Utils;
using System.IO;

namespace VRageRender
{
    //@ Simple stupid material class which enwrapp 2 textures and generate uniqueID form textures
    internal class MyRenderMeshMaterial
    {
        private const string C_FAKE_DIFFUSE_TEXTURE = "Textures\\Models\\fake_de.dds";
        private const string C_FAKE_NORMAL_TEXTURE = "Textures\\Models\\fake_ns.dds";

        public int HashCode;
        private MyTexture2D m_diffuseTex;
        private MyTexture2D m_normalTex;

        private float m_specularIntensity = 1f;
        private float m_specularPower = 1f;
        private Vector3 m_specularColor;
        private Vector3 m_diffuseColor = new Vector3(1f, 1f, 1f);

        private string m_contentDir;
        private string m_materialName;
        private string m_diffuseName;
        private string m_normalName;
        private bool m_hasNormalTexture;
        private MyMeshDrawTechnique m_drawTechnique;
        private Vector2 m_emissiveUVAnim;
        private bool m_emissivityEnabled = true;

        private bool m_loadedContent;

        public bool Enabled = true;

        private float? m_emissivity = null;

        Vector2 m_diffuseUVAnim;
        public Vector2 DiffuseUVAnim
        {
            get { return m_diffuseUVAnim; }
            set { m_diffuseUVAnim = value; ComputeHashCode(); }
        }

        public Vector2 EmissiveUVAnim 
        {
            get 
            {
                if (m_emissivityEnabled)
                {
                    return m_emissiveUVAnim;
                }
                else 
                {
                    return Vector2.Zero;
                }
            }
            set 
            {
                m_emissiveUVAnim = value;
                ComputeHashCode();
            }
        }
                
        public bool EmissivityEnabled 
        {
            get { return m_emissivityEnabled; }
            set
            {
                if (m_emissivityEnabled != value)
                {
                    m_emissivityEnabled = value;
                    ComputeHashCode();
                }
            }
        }

        public float EmissivityOffset 
        {
            get 
            {
                if (EmissivityEnabled)
                {
                    return MyRender.RenderTimeInMS / 1000.0f;
                }
                else 
                {
                    return 0f;
                }
            }
        }

        public float HoloEmissivity 
        {
            get 
            {
                if (EmissivityEnabled)
                {
                    if (SpecularColor.Y > 0)
                    {  //material with animated emissivity
                       return (((float)Math.Sin(MyRender.RenderTimeInMS / 1000.0f * SpecularColor.Y * 10.0f + SpecularColor.Z * 2 * (float)Math.PI)) + 1.0f) / 2.0f;
                    }
                    else //Holos and decals have multiplied emissivity
                    {
                        return 1f;
                    }
                }
                else
                {
                    return 0f;
                }
            }
        }

        public float? Emissivity
        {
            get
            {
                return m_emissivity;
            }
            set
            {
                m_emissivity = value;
                ComputeHashCode();
            }
        }

        public MyMeshDrawTechnique DrawTechnique
        {
            get { return m_drawTechnique; }
            set { m_drawTechnique = value; }
        }

        public bool EnableColorMask { get; private set; }

        public MyTexture2D DiffuseTexture
        {
            get { return m_diffuseTex; }
            set { m_diffuseTex = value; ComputeHashCode(); }
        }
        public MyTexture2D NormalTexture
        {
            get { return m_normalTex; }
            set { m_normalTex = value; ComputeHashCode(); }
        }
        public float SpecularIntensity
        {
            get { return m_specularIntensity; }
            set { m_specularIntensity = value; ComputeHashCode(); }
        }
        public float SpecularPower
        {
            get { return m_specularPower; }
            set { m_specularPower = value; ComputeHashCode(); }
        }
        public Vector3 SpecularColor
        {
            get { return m_specularColor; }
            set { m_specularColor = value; ComputeHashCode(); }
        }
        public Vector3 DiffuseColor
        {
            get { return m_diffuseColor; }
            set { m_diffuseColor = value; ComputeHashCode(); }
        }

        public string MaterialName
        {
            get { return m_materialName; }
        }

        public string DiffuseName { get { return m_diffuseName; } }
        public string NormalName { get { return m_normalName; } }

        public MyRenderMeshMaterial()
        {
        }

        public MyRenderMeshMaterial(string name, string contentDir, string materialName, MyTexture2D diff, MyTexture2D norm)
        {
            if (name != null)
            {
                m_diffuseName = name + MyRenderMesh.C_POSTFIX_DIFFUSE_EMISSIVE + ".dds";
                m_normalName = name + MyRenderMesh.C_POSTFIX_NORMAL_SPECULAR + ".dds";
            }

            m_contentDir = contentDir;
            m_materialName = materialName;
            m_drawTechnique = MyMeshDrawTechnique.MESH;
            HashCode = 0;
            m_diffuseTex = diff;
            m_normalTex = norm;
            m_hasNormalTexture = m_normalTex != null;

            ComputeHashCode();
        }

        /// <summary>
        /// MyMeshMaterial
        /// </summary>
        /// <param name="specularLevel"></param>
        /// <param name="glossiness"></param>
        public MyRenderMeshMaterial(string materialName, string contentDir, string diffuseName, string normalName, float specularPower, float specularIntensity, bool hasNormalTexture, Vector3 diffuseColor, Vector3 extra)
        {
            m_contentDir = contentDir;

            if (diffuseColor == Vector3.Zero)
            {
                // Debug.Assert(diffuseColor != Vector3.Zero);
                diffuseColor = Vector3.One;
            }

            m_materialName = materialName;
            m_diffuseName = diffuseName;

            m_normalName = normalName;
            m_specularPower = specularPower;
            m_specularIntensity = specularIntensity;
            m_diffuseColor = diffuseColor;
            m_hasNormalTexture = hasNormalTexture;

            // just use it to store extra data (animation of holos)
            m_specularColor = extra;
        }

        /// <summary>
        /// Preload textures into manager
        /// </summary>
        public void PreloadTexture(LoadingMode loadingMode = LoadingMode.Immediate)
        {
            if (m_loadedContent || m_diffuseName == null)
            {
                return;
            }

            string ext = Path.GetExtension(m_diffuseName);
            string deMatch = "_de" + ext;
            string meMatch = "_me" + ext;

            string baseName;
            if (m_diffuseName.EndsWith(deMatch))
                baseName = m_diffuseName.Substring(0, m_diffuseName.Length - deMatch.Length);
            else if(m_diffuseName.EndsWith(meMatch))
                baseName = m_diffuseName.Substring(0, m_diffuseName.Length - meMatch.Length);
            else
                baseName = m_diffuseName.Substring(0, m_diffuseName.Length - ext.Length);

            m_diffuseName = baseName + deMatch;
            DiffuseTexture = MyTextureManager.GetTexture<MyTexture2D>(m_diffuseName, m_contentDir, CheckTexture, loadingMode);
            if (DiffuseTexture == null)
            {
                m_diffuseName = baseName + ext;
                DiffuseTexture = MyTextureManager.GetTexture<MyTexture2D>(m_diffuseName, m_contentDir, CheckTexture, loadingMode);
            }
            if (DiffuseTexture == null)
            {
                m_diffuseName = baseName + meMatch;
                DiffuseTexture = MyTextureManager.GetTexture<MyTexture2D>(m_diffuseName, m_contentDir, CheckTexture, loadingMode);
            }


            EnableColorMask = m_diffuseName.EndsWith(meMatch, StringComparison.InvariantCultureIgnoreCase);

            if (DiffuseTexture == null)
            { //we dont want to see just pure black
                DiffuseTexture = MyTextureManager.GetTexture<MyTexture2D>(C_FAKE_DIFFUSE_TEXTURE, m_contentDir, CheckTexture, loadingMode);
            }

            if (MyRenderConstants.RenderQualityProfile.UseNormals)
            {
                string tex = m_hasNormalTexture ? (m_normalName ?? C_FAKE_NORMAL_TEXTURE) : C_FAKE_NORMAL_TEXTURE;
                NormalTexture = MyTextureManager.GetTexture<MyTexture2D>(tex, m_contentDir, CheckTexture, loadingMode);
            }
                        
            m_loadedContent = true;
        }

        /// <summary>
        /// Checks the normal map.
        /// </summary>
        /// <param name="texture">The texture.</param>
        private static void CheckTexture(MyTexture texture)
        {
            MyUtilsRender9.AssertTexture((MyTexture2D)texture);

            texture.TextureLoaded -= CheckTexture;
        }

        /// <summary>
        /// ComputeHashCode
        /// </summary>
        /// <returns></returns>
        private void ComputeHashCode()
        {
            int result = 1;
            int modCode = 0;

            if (m_diffuseTex != null)
            {
                result = m_diffuseTex.GetHashCode();
                modCode = (1 << 1);
            }

            if (m_normalTex != null)
            {
                result = (result * 397) ^ m_normalTex.GetHashCode();
                modCode += (1 << 2);
            }

            if (m_specularIntensity != 0)
            {
                result = (result * 397) ^ m_specularIntensity.GetHashCode();
                modCode += (1 << 4);
            }

            if (m_specularPower != 0)
            {
                result = (result * 397) ^ m_specularPower.GetHashCode();
                modCode += (1 << 5);
            }

            if (m_diffuseColor.GetHashCode() != 0)
            {
                result = (result * 397) ^ m_diffuseColor.GetHashCode();
                modCode += (1 << 6);
            }

            if (DiffuseUVAnim.GetHashCode() != 0)
            {
                result = (result * 397) ^ DiffuseUVAnim.GetHashCode();
                modCode += (1 << 7);               
            }

            if (EmissiveUVAnim.GetHashCode() != 0)
            {
                result = (result * 397) ^ EmissiveUVAnim.GetHashCode();
                modCode += (1 << 8);
            }

            result = (result * 397) ^ EmissivityEnabled.GetHashCode();
            modCode += (1 << 9);

            if (Emissivity.GetHashCode() != 0)
            {
                result = (result * 397) ^ Emissivity.GetHashCode();
                modCode += (1 << 10);
            }

            if (m_contentDir != null)
            {
                result = (result * 397) ^ m_contentDir.GetHashCode();
                modCode += (1 << 11);
            }

            HashCode = (result * 397) ^ modCode;
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return HashCode;
            }
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) 
                return false;
            if (obj.GetType() != typeof(MyRenderMeshMaterial)) 
                return false;
            return HashCode == obj.GetHashCode();
        }


        /// <summary>
        /// Return modelDrawTechnique based on textures
        /// </summary>
        /// <returns></returns>
        public MyMeshDrawTechnique GenerateDrawTechnique()
        {
            return MyMeshDrawTechnique.MESH;
        }

        public MyRenderMeshMaterial Clone()
        {
            MyRenderMeshMaterial newMaterial = new MyRenderMeshMaterial();
            newMaterial.m_contentDir = m_contentDir;
            newMaterial.m_diffuseTex = m_diffuseTex;
            newMaterial.m_normalTex = m_normalTex;

            newMaterial.m_specularIntensity = m_specularIntensity;
            newMaterial.m_specularPower = m_specularPower;
            newMaterial.m_specularColor = m_specularColor;
            newMaterial.m_diffuseColor = m_diffuseColor;

            newMaterial.m_materialName = m_materialName;
            newMaterial.m_diffuseName = m_diffuseName;
            newMaterial.m_normalName = m_normalName;
            newMaterial.m_hasNormalTexture = m_hasNormalTexture;
            newMaterial.m_drawTechnique = m_drawTechnique;
            newMaterial.m_emissiveUVAnim = m_emissiveUVAnim;
            newMaterial.m_emissivityEnabled = m_emissivityEnabled;

            if (m_loadedContent)
                newMaterial.PreloadTexture();

            newMaterial.Enabled = Enabled;
            newMaterial.m_emissivity = m_emissivity;

            newMaterial.ComputeHashCode();

            return newMaterial;
        }
    }
}
