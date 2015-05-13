using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;

namespace Sandbox.Game.Gui
{
    //Prepared to be render debug screen

    class MyGuiScreenRenderModules : MyGuiScreenDebugBase
    {
        public MyGuiScreenRenderModules()
        {
            m_closeOnEsc = true;
            m_drawEvenWithoutFocus = true;
            m_isTopMostScreen = false;
            CanHaveFocus = false;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption("Render modules", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_scale = 0.7f;

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;

            // TODO: Par
            //AddLabel(new StringBuilder("Prepare for draw"), Color.Yellow.ToVector4(), 1.2f);
            //foreach (MyRender.MyRenderModuleItem renderModule in MyRender.GetRenderModules(MyRenderStage.PrepareForDraw))
            //{
            //    AddCheckBox(new StringBuilder(renderModule.DisplayName), renderModule, MemberHelper.GetMember(() => renderModule.Enabled));
            //}

            //AddLabel(new StringBuilder("Background"), Color.Yellow.ToVector4(), 1.2f);
            //foreach (MyRender.MyRenderModuleItem renderModule in MyRender.GetRenderModules(MyRenderStage.Background))
            //{
            //    AddCheckBox(new StringBuilder(renderModule.DisplayName), renderModule, MemberHelper.GetMember(() => renderModule.Enabled));
            //}

            //AddLabel(new StringBuilder("Pre-HDR Alpha blend"), Color.Yellow.ToVector4(), 1.2f);
            //foreach (MyRender.MyRenderModuleItem renderModule in MyRender.GetRenderModules(MyRenderStage.AlphaBlendPreHDR))
            //{
            //    AddCheckBox(new StringBuilder(renderModule.DisplayName), renderModule, MemberHelper.GetMember(() => renderModule.Enabled));
            //}

            //AddLabel(new StringBuilder("Alpha blend"), Color.Yellow.ToVector4(), 1.2f);
            //foreach (MyRender.MyRenderModuleItem renderModule in MyRender.GetRenderModules(MyRenderStage.AlphaBlend))
            //{
            //    AddCheckBox(new StringBuilder(renderModule.DisplayName), renderModule, MemberHelper.GetMember(() => renderModule.Enabled));
            //}

            //AddLabel(new StringBuilder("Debug draw"), Color.Yellow.ToVector4(), 1.2f);
            //foreach (MyRender.MyRenderModuleItem renderModule in MyRender.GetRenderModules(MyRenderStage.DebugDraw))
            //{
            //    AddCheckBox(new StringBuilder(renderModule.DisplayName), renderModule, MemberHelper.GetMember(() => renderModule.Enabled));
            //}
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenRenderModules";
        }

    }
}
