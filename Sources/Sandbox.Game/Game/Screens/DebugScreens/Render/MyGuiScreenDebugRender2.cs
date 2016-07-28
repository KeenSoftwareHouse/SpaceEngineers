using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1

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

            bool rendererIsDirectX11 = MySandboxGame.Config.GraphicsRenderer.ToString().Equals("DirectX 11");
            //MySandboxGame.Log.WriteLineAndConsole(rendererIsDirectX11.ToString());
            //rendererIsDirectX11 = true;
            

            m_currentPosition.Y += 0.01f;
            if (rendererIsDirectX11)
            {
                AddLabel("Gbuffer", Color.Yellow.ToVector4(), 1.2f);
                AddCheckBox("Base color", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferColor));
                AddCheckBox("Base color linear", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferColorLinear));
                AddCheckBox("Normals", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferNormal));
                AddCheckBox("Normals view", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferNormalView));
                AddCheckBox("Glossiness", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferGlossiness));
                AddCheckBox("Metalness", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferMetalness));
                AddCheckBox("NDotL", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayNDotL));
                AddCheckBox("Material ID", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferMaterialID));
                AddCheckBox("Ambient occlusion", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayGbufferAO));
                AddCheckBox("Emissive", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayEmissive));
                AddCheckBox("Edge mask", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayEdgeMask));

                AddLabel("Environment light", Color.Yellow.ToVector4(), 1.2f);
                AddCheckBox("Ambient diffuse", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayAmbientDiffuse));
                AddCheckBox("Ambient specular", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayAmbientSpecular));
                AddCheckBox("Wireframe", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.Wireframe));

                AddLabel("Scene objects", Color.Yellow.ToVector4(), 1.2f);
                AddCheckBox("Draw IDs", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayIDs));
                AddCheckBox("Draw AABBs", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayAabbs));
                AddCheckBox("Draw non-merge-instanced", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DrawNonMergeInstanced));
                AddCheckBox("Draw merge-instanced", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DrawMergeInstanced));
            }
            else
            {
                AddLabel("Environment light", Color.Yellow.ToVector4(), 1.2f);
                AddCheckBox("Wireframe", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.Wireframe));
            }

            AddLabel("Internal", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Enable multithreading", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableParallelRendering));
            AddCheckBox("Amortize workload", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.AmortizeBatchWork));
            AddCheckBox("Force IC", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ForceImmediateContext));
            AddCheckBox("Loop object-pass", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.LoopObjectThenPass));
            AddCheckBox("Render thread as worker", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.RenderThreadAsWorker));
            //AddSlider("Batch size", 1.0f, 1000.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.RenderBatchSize));
            AddCheckBox("Total parrot view", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_VOXEL_NAMES));
            AddCheckBox("Particle Render Target", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DrawParticleRenderTarget));

            if(rendererIsDirectX11)
            {
                AddCheckBox("Depth", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayDepth));
                AddCheckBox("Stencil", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayStencil));
            }
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

#endif
}
