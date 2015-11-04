using Sandbox.Graphics.GUI;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "Render debug")]
    class MyGuiScreenDebugRender2 : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRender2()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render debug 2", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;
            AddLabel("Gbuffer", Color.Yellow.ToVector4(), 1.2f);
            
            AddCheckBox("Base color", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferColor));
            AddCheckBox("Base color linear", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferColorLinear));
            AddCheckBox("Normals", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferNormal));
            AddCheckBox("Glossiness", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferGlossiness));
            AddCheckBox("Metalness", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferMetalness));
            AddCheckBox("NDotL", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayNDotL));
            AddCheckBox("Material ID", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferMaterialID));
            AddCheckBox("Ambient occlusion", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayAO));
            AddCheckBox("Emissive", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayEmissive));
            AddCheckBox("Edge mask", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayEdgeMask));

            m_currentPosition.Y += 0.01f;
            AddLabel("Environment light", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Ambient diffuse", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayAmbientDiffuse));
            AddCheckBox("Ambient specular", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayAmbientSpecular));

            AddCheckBox("Wireframe", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.Wireframe));

            m_currentPosition.Y += 0.01f;
            AddLabel("Scene objects", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Draw IDs", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayIDs));
            AddCheckBox("Draw AABBs", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayAabbs));
            AddCheckBox("Draw only merge-instanced", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DrawOnlyMergedMeshes));

            m_currentPosition.Y += 0.01f;
            AddLabel("Internal", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Enable multithreading", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableParallelRendering));
            AddCheckBox("Amortize workload", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.AmortizeBatchWork));
            AddCheckBox("Force IC", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ForceImmediateContext));
            AddCheckBox("Loop object-pass", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.LoopObjectThenPass));
            AddCheckBox("Render thread as worker", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.RenderThreadAsWorker));
            //AddSlider("Batch size", 1.0f, 1000.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.RenderBatchSize));
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRender2";
        }

    }
}
