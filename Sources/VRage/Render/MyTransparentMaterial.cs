using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Collections;
using VRageMath;

namespace VRageRender
{
    public class MyTransparentMaterial
    {
        public readonly string Name;
        public readonly string Texture;

        //  If true, then we calculate sun shadow value for a particle, and also per-pixel lighting. Set it to true only if really unneceserary as it
        //  will slow down the rendering.
        public readonly bool CanBeAffectedByOtherLights;

        public readonly bool AlphaMistingEnable;
        public readonly bool IgnoreDepth;
        public readonly bool NeedSort;
        public readonly bool UseAtlas;

        public readonly float AlphaMistingStart;
        public readonly float AlphaMistingEnd;

        public readonly float SoftParticleDistanceScale;

        public readonly float Emissivity;

        public readonly float AlphaSaturation;
        public readonly bool AlphaCutout;

        public Vector2 UVOffset;
        public Vector2 UVSize;

        public Vector4 Color = Vector4.One;
        public float Reflectivity;

        // Render sets this and uses that
        public object RenderTexture;

        public MyTransparentMaterial(
            string Name,
            string Texture,
            float SoftParticleDistanceScale,
            bool CanBeAffectedByOtherLights,
            bool AlphaMistingEnable,
            Vector4 Color,
            bool IgnoreDepth = false,
            bool NeedSort = true,
            bool UseAtlas = false,
            float Emissivity = 0,
            float AlphaMistingStart = 1,
            float AlphaMistingEnd = 4,
            float AlphaSaturation = 1,
            float Reflectivity = 0,
            bool AlphaCutout = false)
        {
            this.Name = Name;
            this.Texture = Texture;
            this.SoftParticleDistanceScale = SoftParticleDistanceScale;
            this.CanBeAffectedByOtherLights = CanBeAffectedByOtherLights;
            this.AlphaMistingEnable = AlphaMistingEnable;
            this.IgnoreDepth = IgnoreDepth;
            this.NeedSort = NeedSort;
            this.UseAtlas = UseAtlas;
            this.Emissivity = Emissivity;
            this.AlphaMistingStart = AlphaMistingStart;
            this.AlphaMistingEnd = AlphaMistingEnd;
            this.AlphaSaturation = AlphaSaturation;
            this.AlphaCutout = AlphaCutout;
            this.Color = Color;
            this.Reflectivity = Reflectivity;

            UVOffset = new Vector2(0, 0);
            UVSize = new Vector2(1, 1);
        }
    }

    public static class MyTransparentMaterials
    {
        private static readonly Dictionary<string, MyTransparentMaterial> m_materialsByName = new Dictionary<string, MyTransparentMaterial>();

        private static readonly MyTransparentMaterial ErrorMaterial;

        public static Action OnUpdate;

        static MyTransparentMaterials()
        {
            ErrorMaterial = new MyTransparentMaterial("ErrorMaterial", "Textures\\FAKE.dds", 9999, false, false, Color.Pink.ToVector4());

            Clear();
        }

        public static MyTransparentMaterial GetMaterial(string materialName)
        {
            MyTransparentMaterial material;
            if (m_materialsByName.TryGetValue(materialName, out material))
                return material;
            else
            {
                System.Diagnostics.Debug.Fail("Transparent material not present!");
                return ErrorMaterial;
            }
        }

        public static DictionaryValuesReader<string, MyTransparentMaterial> Materials
        {
            get { return new DictionaryValuesReader<string,MyTransparentMaterial>(m_materialsByName); }
        }

        public static bool ContainsMaterial(string materialName)
        {
            return m_materialsByName.ContainsKey(materialName);
        }

        public static void AddMaterial(MyTransparentMaterial material)
        {
            m_materialsByName[material.Name] = material;
        }

        public static void Clear()
        {
            m_materialsByName.Clear();
            AddMaterial(ErrorMaterial);
        }

        public static int Count
        {
            get { return m_materialsByName.Count; }
        }

        public static void Update()
        {
            if (OnUpdate != null)
                OnUpdate();
        }
    }
}
