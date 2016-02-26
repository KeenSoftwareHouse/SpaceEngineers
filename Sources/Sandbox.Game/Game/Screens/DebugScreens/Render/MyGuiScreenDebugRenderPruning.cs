using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using VRage;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "Pruning and culling", MyDirectXSupport.DX9)]
    class MyGuiScreenDebugRenderPruning : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderPruning()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Pruning and culling", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            // TODO: Par
            //MyRender.MyRenderModuleItem renderModule = MyRender.GetRenderModules(MyRenderStage.DebugDraw).Find((x) => x.Name == MyRenderModuleEnum.PrunningStructure);
            //MyRender.MyRenderModuleItem renderModulePhysics = MyRender.GetRenderModules(MyRenderStage.DebugDraw).Find((x) => x.Name == MyRenderModuleEnum.PhysicsPrunningStructure);
            
            //AddSlider(new StringBuilder("Worst allowed balance"), 0.01f, 0.5f, null, MemberHelper.GetMember(() => MyRender.CullingStructureWorstAllowedBalance));
            //AddSlider(new StringBuilder("Box cut penalty"), 0, 30, null, MemberHelper.GetMember(() => MyRender.CullingStructureCutBadness));
            //AddSlider(new StringBuilder("Imbalance penalty"), 0, 5, null, MemberHelper.GetMember(() => MyRender.CullingStructureImbalanceBadness));
            //AddSlider(new StringBuilder("Off-center penalty"), 0, 5, null, MemberHelper.GetMember(() => MyRender.CullingStructureOffsetBadness));

            m_currentPosition.Y += 0.01f;
            AddCheckBox("Show prunning structure", () => EnableRenderPrunning, (x) => EnableRenderPrunning = x);

            // TODO: Par
            //m_currentPosition.Y += 0.01f;
            //AddCheckBox(new StringBuilder("Show physics prunning structure"), renderModulePhysics, MemberHelper.GetMember(() => renderModulePhysics.Enabled));

            m_currentPosition.Y += 0.01f;
            AddButton(new StringBuilder("Rebuild now"), delegate { VRageRender.MyRenderProxy.RebuildCullingStructure(); });

            //m_currentPosition.Y += 0.01f;
            //AddButton(new StringBuilder("Rebuild to test lowest triangle count"), delegate { MyRender.RebuildCullingStructureCullEveryPrefab(); });
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderPruning";
        }

        static bool m_enableRenderPrunning = false;
        static bool EnableRenderPrunning
        {
            get { return m_enableRenderPrunning; }
            set
            {
                // TODO: Par
                m_enableRenderPrunning = value;
                VRageRender.MyRenderProxy.EnableRenderModule((uint)VRageRender.MyRenderModuleEnum.PrunningStructure, value);
            }
        }

    }
}
