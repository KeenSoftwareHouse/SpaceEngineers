using VRage.Utils;
using VRageMath;
using VRage.Library;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlRotatingWheel : MyGuiControlBase
    {
        private float m_rotationSpeed; // radians per second
        private float m_rotatingAngle;
        private float m_wheelScale;
        private string m_texture;
        private Vector2 m_textureResolution;
        public bool MultipleSpinningWheels;
        public bool ManualRotationUpdate;

        public MyGuiControlRotatingWheel(
            Vector2? position = null,
            Vector4? colorMask = null,
            float scale = MyGuiConstants.ROTATING_WHEEL_DEFAULT_SCALE,
            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            string texture = MyGuiConstants.LOADING_TEXTURE,
            bool manualRotationUpdate = true,
            bool multipleSpinningWheels = true,
            Vector2? textureResolution = null,
            float radiansPerSecond = 1.5f)
            : base(position: position,
                    size: null,
                    colorMask: colorMask,
                    toolTip: null,
                    isActiveControl: false)
        {
            UpdateRotation();

            m_wheelScale = scale;
            m_texture = texture;
            m_textureResolution = textureResolution.HasValue ? textureResolution.Value : new Vector2(256, 256);

            MultipleSpinningWheels = multipleSpinningWheels;
            ManualRotationUpdate = manualRotationUpdate;

            m_rotationSpeed = radiansPerSecond;
        }

        public override void Update()
        {
            if (ManualRotationUpdate && Visible)
                UpdateRotation();
            base.Update();
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            Vector2 rotatingSize = MyGuiManager.GetNormalizedSize(m_textureResolution, m_wheelScale);
            Vector2 rotatingPosition = GetPositionAbsolute() + new Vector2(rotatingSize.X / 2.0f, rotatingSize.Y / 2.0f);
            
            //  Large wheel - shadow only
            var shadowColor = new Color(transitionAlpha * (new Color(0, 0, 0, 80)).ToVector4());
            DrawWheel(rotatingPosition + MyGuiConstants.SHADOW_OFFSET, m_wheelScale, shadowColor, m_rotatingAngle, m_rotationSpeed);

            //  Large wheel - wheel
            var color = ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha);
            DrawWheel(rotatingPosition, m_wheelScale, color, m_rotatingAngle, m_rotationSpeed);

            if (MultipleSpinningWheels)
            {
                //  Small wheel - without shadow
                const float SMALL_SCALE = 0.6f;
                Vector2 smallRotatingPosition = rotatingPosition - (rotatingSize * ((1 - SMALL_SCALE) / 2.0f));
                DrawWheel(smallRotatingPosition, SMALL_SCALE * m_wheelScale, color, -m_rotatingAngle * 1.1f, -m_rotationSpeed);

                //  Mini wheel - without shadow
                const float MINI_SCALE = SMALL_SCALE * 0.6f;
                var miniRotatingPosition = rotatingPosition - (rotatingSize * ((1 - MINI_SCALE) / 2.0f));
                DrawWheel(miniRotatingPosition, MINI_SCALE * m_wheelScale, color, m_rotatingAngle * 1.2f, m_rotationSpeed);
            }
        }

        private void UpdateRotation()
        {
            // not using GameTime, because its nort updated sometimes
            m_rotatingAngle = (MyEnvironment.TickCount / 1000f) * m_rotationSpeed;
        }

        private void DrawWheel(Vector2 position, float scale, Color color, float rotationAngle, float rotationSpeed)
        {
            const MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            var origin = new Vector2(0.5f, 0.5f);
            if (ManualRotationUpdate)
                MyGuiManager.DrawSpriteBatch(m_texture, position, scale, color, align, rotationAngle, origin);
            else
                MyGuiManager.DrawSpriteBatchRotate(m_texture, position, scale, color, align, rotationAngle, origin, rotationSpeed);
        }

    }
}
