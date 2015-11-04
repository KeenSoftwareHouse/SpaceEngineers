using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage
{
    public struct MyRenderVoxelMaterialData
    {
        public byte Index;

        public string DiffuseXZ;
        public string NormalXZ;
        public string DiffuseY;
        public string NormalY;
        public float SpecularShininess;
        public float SpecularPower;

        public string ColorMetalXZnY;
        public string ColorMetalY;
        public string NormalGlossXZnY;
        public string NormalGlossY;
        public string ExtXZnY;
        public string ExtY;

        public string ColorMetalXZnYFar1;
        public string ColorMetalYFar1;
        public string NormalGlossXZnYFar1;
        public string NormalGlossYFar1;
        public string ExtXZnYFar1;
        public string ExtYFar1;

        public string ColorMetalXZnYFar2;
        public string ColorMetalYFar2;
        public string NormalGlossXZnYFar2;
        public string NormalGlossYFar2;
        public string ExtXZnYFar2;
        public string ExtYFar2;

        public Vector4 DistanceAndScale;
        public Vector4 DistanceAndScaleFar;
        public Vector2 DistanceAndScaleFar3;
        public Vector4 Far3Color;
        public float ExtensionDetailScale;

        public string ExtensionTextureArray1;
        public string ExtensionTextureArray2;
        public string[] FoliageColorTextureArray;
        public string[] FoliageNormalTextureArray;

        public float ExtensionDensity;
        public Vector2 ExtensionScale;
        public float ExtensionRandomRescaleMult;
        public int ExtensionType;
    }
}
