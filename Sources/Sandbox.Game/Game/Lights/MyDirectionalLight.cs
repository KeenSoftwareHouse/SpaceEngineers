using VRageMath;

namespace Sandbox.Game.Lights
{
    class MyDirectionalLight
    {
        //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        //  So don't initialize members here, do it in Start()

        public Vector3 Direction;
        public Vector4 Color;
        public Vector3 BackColor;
        public Vector3 SpecularColor = Vector3.One;
        public float Intensity;
        public float BackIntensity;
        public bool LightOn;        //  If true, we use the light in lighting calculation. Otherwise it's like turned off, but still in the buffer.

        //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        //  So don't initialize members here, do it in Start()
        public MyDirectionalLight()
        {
        }

        //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        public void Start()
        {
            LightOn = true;
            Intensity = 1.0f;
            BackIntensity = 0.1f;
        }

        //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        public void Start(Vector3 direction, Vector4 color, Vector3 backColor)
        {
            Start();

            Direction = direction;
            Color = color;
            BackColor = backColor;
        }
    }
}
