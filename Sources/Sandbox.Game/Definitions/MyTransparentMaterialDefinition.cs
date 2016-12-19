using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_TransparentMaterialDefinition))]
    public class MyTransparentMaterialDefinition : MyDefinitionBase
    {
        public string Texture;
        public MyTransparentMaterialTextureType TextureType; 
        public bool CanBeAffectedByLights;
        public bool AlphaMistingEnable;
        public bool IgnoreDepth;
        public bool NeedSort;
        public bool UseAtlas;
        public float AlphaMistingStart;
        public float AlphaMistingEnd;
        public float SoftParticleDistanceScale;
        public float Emissivity;
        public float AlphaSaturation;
        public bool Reflection;
        public float Reflectivity;
        public Vector4 Color = Vector4.One;
        public bool AlphaCutout;
        public Vector2I TargetSize;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var materialBuilder = builder as MyObjectBuilder_TransparentMaterialDefinition;
            MyDebug.AssertDebug(materialBuilder != null, "Initializing transparent material definition using wrong object builder.");

            Texture = materialBuilder.Texture;
            if (Texture == null)
                Texture = string.Empty;
            TextureType = materialBuilder.TextureType;
            CanBeAffectedByLights = materialBuilder.CanBeAffectedByOtherLights;
            AlphaMistingEnable = materialBuilder.AlphaMistingEnable;
            IgnoreDepth = materialBuilder.IgnoreDepth;
            NeedSort = materialBuilder.NeedSort;
            UseAtlas = materialBuilder.UseAtlas;
            AlphaMistingStart = materialBuilder.AlphaMistingStart;
            AlphaMistingEnd = materialBuilder.AlphaMistingEnd;
            SoftParticleDistanceScale = materialBuilder.SoftParticleDistanceScale;
            Emissivity = materialBuilder.Emissivity;
            AlphaSaturation = materialBuilder.AlphaSaturation;
            Reflection = materialBuilder.Reflection;
            Reflectivity = materialBuilder.Reflectivity;
            Color = materialBuilder.Color;
            AlphaCutout = materialBuilder.AlphaCutout;
            TargetSize = materialBuilder.TargetSize;
        }   
    }
}
