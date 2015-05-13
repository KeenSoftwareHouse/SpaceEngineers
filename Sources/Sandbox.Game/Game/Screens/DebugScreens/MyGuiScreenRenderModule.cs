using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Gui
{
    // TODO: Par
    //class MyGuiScreenRenderModule : MyGuiScreenDebugBase
    //{
    //    private string m_name;
    //    private MyRenderStage m_renderStage;

    //    public MyGuiScreenRenderModule(MyRenderStage renderStage, string name)
    //        : base(0.35f * Color.Yellow.ToVector4(), false)
    //    {
    //        m_closeOnEsc = true;
    //        m_drawEvenWithoutFocus = true;
    //        m_isTopMostScreen = false;
    //        m_canHaveFocus = false;

    //        m_renderStage = renderStage;
    //        m_name = name;

    //        RecreateControls(true);
    //    }

    //    public override void RecreateControls(bool contructor)
    //    {
    //        Controls.Clear();

    //        AddCaption(new System.Text.StringBuilder("Render module - " + m_name), Color.Yellow.ToVector4());

    //        MyGuiControlLabel label = new MyGuiControlLabel(this, new Vector2(0.01f, -m_size.Value.Y / 2.0f + 0.07f), null, new System.Text.StringBuilder("(press ALT to share focus)"), Color.Yellow.ToVector4(), MyGuiConstants.LABEL_TEXT_SCALE * 0.7f,
    //                           MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
    //        Controls.Add(label);

    //        m_scale = 0.7f;

    //        m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

    //        m_currentPosition.Y += 0.01f;                           
            
    //        foreach (MyRender.MyRenderModuleItem renderModule in MyRender.GetRenderModules(m_renderStage))
    //        {
    //            AddCheckBox(new StringBuilder(renderModule.DisplayName), renderModule, MemberHelper.GetMember(() => renderModule.Enabled));
    //        }
    //    }

    //    public override string GetFriendlyName()
    //    {
    //        return "MyGuiScreenRenderModule";
    //    }    
    //}
}
