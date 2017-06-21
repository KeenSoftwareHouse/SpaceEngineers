using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using VRageMath;

namespace Sandbox.Game.Screens.DebugScreens
{
#if !XB1
    [MyDebugScreen("Game", "Thrusts visual")]
    class MyGuiScreenDebugThrusts : MyGuiScreenDebugBase
    {
        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugThrusts";
        }

        public MyGuiScreenDebugThrusts()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;
            m_scale = 0.7f;

            AddCaption("Thrusts visual", Color.Yellow.ToVector4());
            AddShareFocusHint();
            m_currentPosition.Y += 0.01f;

            AddLabel("Jetpack Light", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Intensity const", Components.MyRenderComponentCharacter.JETPACK_LIGHT_INTENSITY_BASE, 0, 1000.0f,
                (slider) => { Components.MyRenderComponentCharacter.JETPACK_LIGHT_INTENSITY_BASE = slider.Value; });
            AddSlider("Intensity from thrust length", Components.MyRenderComponentCharacter.JETPACK_LIGHT_INTENSITY_LENGTH, 0, 1000.0f,
                (slider) => { Components.MyRenderComponentCharacter.JETPACK_LIGHT_INTENSITY_LENGTH = slider.Value; });
            AddSlider("Range from thrust radius", Components.MyRenderComponentCharacter.JETPACK_LIGHT_RANGE_RADIUS, 0, 10.0f,
                (slider) => { Components.MyRenderComponentCharacter.JETPACK_LIGHT_RANGE_RADIUS = slider.Value; });
            AddSlider("Range from thrust length", Components.MyRenderComponentCharacter.JETPACK_LIGHT_RANGE_LENGTH, 0, 10.0f,
                (slider) => { Components.MyRenderComponentCharacter.JETPACK_LIGHT_RANGE_LENGTH = slider.Value; });
            m_currentPosition.Y += 0.01f;
            AddLabel("Jetpack Glare", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Intensity const", Components.MyRenderComponentCharacter.JETPACK_GLARE_INTENSITY_BASE, 0, 10.0f,
                (slider) => { Components.MyRenderComponentCharacter.JETPACK_GLARE_INTENSITY_BASE = slider.Value; });
            AddSlider("Intensity from thrust length", Components.MyRenderComponentCharacter.JETPACK_GLARE_INTENSITY_LENGTH, 0, 100.0f,
                (slider) => { Components.MyRenderComponentCharacter.JETPACK_GLARE_INTENSITY_LENGTH = slider.Value; });
            AddSlider("Size from thrust radius", Components.MyRenderComponentCharacter.JETPACK_GLARE_SIZE_RADIUS, 0, 10.0f,
                (slider) => { Components.MyRenderComponentCharacter.JETPACK_GLARE_SIZE_RADIUS = slider.Value; });
            AddSlider("Size from thrust length", Components.MyRenderComponentCharacter.JETPACK_GLARE_SIZE_LENGTH, 0, 10.0f,
                (slider) => { Components.MyRenderComponentCharacter.JETPACK_GLARE_SIZE_LENGTH = slider.Value; });
            m_currentPosition.Y += 0.02f;

            AddLabel("Thrust Light", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Intensity const", MyThrust.LIGHT_INTENSITY_BASE, 0, 1000.0f,
                (slider) => { MyThrust.LIGHT_INTENSITY_BASE = slider.Value; });
            AddSlider("Intensity from thrust length", MyThrust.LIGHT_INTENSITY_LENGTH, 0, 1000.0f,
                (slider) => { MyThrust.LIGHT_INTENSITY_LENGTH = slider.Value; });
            AddSlider("Range from thrust radius", MyThrust.LIGHT_RANGE_RADIUS, 0, 10.0f,
                (slider) => { MyThrust.LIGHT_RANGE_RADIUS = slider.Value; });
            AddSlider("Range from thrust length", MyThrust.LIGHT_RANGE_LENGTH, 0, 10.0f,
                (slider) => { MyThrust.LIGHT_RANGE_LENGTH = slider.Value; });
            m_currentPosition.Y += 0.01f;
            AddLabel("Thrust Glare", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Intensity const", MyThrust.GLARE_INTENSITY_BASE, 0, 10.0f,
                (slider) => { MyThrust.GLARE_INTENSITY_BASE = slider.Value; });
            AddSlider("Intensity from thrust length", MyThrust.GLARE_INTENSITY_LENGTH, 0, 100.0f,
                (slider) => { MyThrust.GLARE_INTENSITY_LENGTH = slider.Value; });
            AddSlider("Size from thrust radius", MyThrust.GLARE_SIZE_RADIUS, 0, 10.0f,
                (slider) => { MyThrust.GLARE_SIZE_RADIUS = slider.Value; });
            AddSlider("Size from thrust length", MyThrust.GLARE_SIZE_LENGTH, 0, 10.0f,
                (slider) => { MyThrust.GLARE_SIZE_LENGTH = slider.Value; });
        }
    }
#endif
}
