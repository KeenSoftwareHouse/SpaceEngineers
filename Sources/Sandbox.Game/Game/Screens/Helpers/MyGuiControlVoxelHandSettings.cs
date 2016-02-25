using Sandbox.Common;
using Sandbox.Game.Localization;
using Sandbox.Game.SessionComponents;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyGuiControlVoxelHandSettings : MyGuiControlBase
    {
        MyGuiControlLabel m_labelSettings;

        public MyGuiControlButton OKButton;

        public MyToolbarItemVoxelHand Item { get; set; }

        public MyGuiControlVoxelHandSettings()
            : base(size: new Vector2(0.25f, 0.4f),
                   backgroundTexture: new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_BG_LARGE_DEFAULT.Texture),
                   originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            m_labelSettings = new MyGuiControlLabel()
            {
                Position = new Vector2(-0.1f, -0.1875f),
                TextEnum = MyCommonTexts.VoxelHandSettingScreen_ShapeSettings,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Font = MyFontEnum.ScreenCaption,
            };

            OKButton = new MyGuiControlButton() { Position = new Vector2(0f, 0.19f), TextEnum = MyCommonTexts.Ok, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM };
        }

        public void UpdateControls()
        {
            IMyVoxelBrush shape = null;

            if (Item.Definition.Id.SubtypeName == "Box") shape = MyBrushBox.Static;
            else if (Item.Definition.Id.SubtypeName == "Capsule") shape = MyBrushCapsule.Static;
            else if (Item.Definition.Id.SubtypeName == "Ramp") shape = MyBrushRamp.Static;
            else if (Item.Definition.Id.SubtypeName == "Sphere") shape = MyBrushSphere.Static;
            else if (Item.Definition.Id.SubtypeName == "AutoLevel") shape = MyBrushAutoLevel.Static;

            if (shape != null)
            {
                Elements.Clear();
                Elements.Add(m_labelSettings);

                foreach (var control in shape.GetGuiControls())
                    Elements.Add(control);

                Elements.Add(OKButton);
            }
        }

        public override MyGuiControlBase HandleInput()
        {
            var handled = base.HandleInput();

            if (handled == null)
                handled = HandleInputElements();

            return handled;
        }

        internal void UpdateFromBrush(IMyVoxelBrush shape)
        {
            Elements.Clear();
            Elements.Add(m_labelSettings);
            foreach (var control in shape.GetGuiControls())
            {
                Elements.Add(control);
            }
            Elements.Add(OKButton);
        }
    }
}
