using Sandbox.Graphics.GUI;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1
    [MyDebugScreen("Render", "Optimizations", MyDirectXSupport.DX9)]
    class MyGuiScreenDebugRenderOptimizations : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderOptimizations()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render optimizations", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;
            AddLabel("Optimizations", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Show particles overdraw", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.VisualizeOverdraw));
            // TODO: Par
            //AddCheckBox(new StringBuilder("Enable stencil optimization"), null, MemberHelper.GetMember(() => MyRender.EnableStencilOptimization));
            //AddCheckBox(new StringBuilder("Enable LOD blending"), null, MemberHelper.GetMember(() => MyRender.EnableLODBlending));
            //AddCheckBox(new StringBuilder("Enable stencil optimization LOD1"), null, MemberHelper.GetMember(() => MyRender.EnableStencilOptimizationLOD1));
            //AddCheckBox(new StringBuilder("Show stencil optimization"), null, MemberHelper.GetMember(() => MyRender.ShowStencilOptimization));
            
            //AddCheckBox(new StringBuilder("Respect cast shadows flag"), null, MemberHelper.GetMember(() => MyShadowRenderer.RespectCastShadowsFlags));
            //AddCheckBox(new StringBuilder("Multithreaded shadows"), MyRender.GetShadowRenderer(), MemberHelper.GetMember(() => MyRender.GetShadowRenderer().MultiThreaded));
            //AddCheckBox(new StringBuilder("Multithreaded entities prepare"), null, MemberHelper.GetMember(() => MyRender.EnableEntitiesPrepareInBackground));
            

            m_currentPosition.Y += 0.01f;
            AddLabel("HW Occ queries", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Enable", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableHWOcclusionQueries));
            //AddCheckBox(new StringBuilder("Enable shadow occ.q."), null, MemberHelper.GetMember(() => MyRender.EnableHWOcclusionQueriesForShadows));
            //AddCheckBox(new StringBuilder("Show occ queries"), null, MemberHelper.GetMember(() => MyRender.ShowHWOcclusionQueries));

            //m_currentPosition.Y += 0.01f;
            AddLabel("Rendering", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Skip LOD NEAR", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.SkipLOD_NEAR));
            AddCheckBox("Skip LOD0", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.SkipLOD_0));
            AddCheckBox("Skip LOD1", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.SkipLOD_1));
            AddCheckBox("Skip Voxels", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.SkipVoxels));
            AddCheckBox("Show render stats", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowEnhancedRenderStatsEnabled));
            AddCheckBox("Show resources stats", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowResourcesStatsEnabled));
            AddCheckBox("Show textures stats", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowTexturesStatsEnabled));
            AddCheckBox("Wireframe", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.Wireframe));
            //AddCheckBox(new StringBuilder("Show entities stats"), null, MemberHelper.GetMember(() => MyEntities.ShowDebugDrawStatistics));
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderOpts";
        }

    }
#endif
}
