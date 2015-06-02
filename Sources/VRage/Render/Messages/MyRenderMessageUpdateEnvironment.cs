using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    /// <summary>
    /// The difference between environment and RenderSettings is that environment are game play values,
    /// on the other hand render settings are render internal/debugging values
    /// </summary>
    public class MyRenderMessageUpdateRenderEnvironment : IMyRenderMessage
    {
        public Vector3 SunDirection;
        public Color SunColor;
        public Color SunBackColor;
        public Color SunSpecularColor;
        public float SunIntensity;
        public float SunBackIntensity;
        public bool SunLightOn;        //  If true, we use the light in lighting calculation. Otherwise it's like turned off, but still in the buffer.
        public Color AmbientColor;
        public float AmbientMultiplier;
        public float EnvAmbientIntensity;
        public Color BackgroundColor;
        public string BackgroundTexture;
        public float SunSizeMultiplier;
        public float DistanceToSun; //in milions km
        public Quaternion BackgroundOrientation;
        public string SunMaterial;
        public bool SunBillboardEnabled;

	    public float DayTime; // 0:00 to 24:00, so 0.5 means day skybox is 1, 0(1) means night skybox is 1
        public bool ResetEyeAdaptation;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateRenderEnvironment; } }
    }
}
