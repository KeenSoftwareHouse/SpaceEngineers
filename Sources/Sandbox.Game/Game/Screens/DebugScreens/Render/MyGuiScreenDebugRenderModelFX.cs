using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;
using Sandbox.Common;
using VRage.Game.Models;

namespace Sandbox.Game.Gui
{
#if !XB1
    [MyDebugScreen("Render", "Model FX", MyDirectXSupport.DX9)]
    class MyGuiScreenDebugRenderModelFX : MyGuiScreenDebugBase
    {
        public static bool EnableRenderLights = true;

        static int m_currentModelSelectedItem = 0;
        static int m_currentSelectedMeshItem = 0;
        static int m_currentSelectedVoxelItem = -1;
        MyGuiControlCombobox m_modelsCombo;
        MyGuiControlCombobox m_meshesCombo;
        MyGuiControlCombobox m_voxelsCombo;
        MyGuiControlColor m_diffuseColor;
        MyGuiControlSlider m_specularIntensity;
        MyGuiControlSlider m_specularPower;
        MyModel m_model = null;

        public MyGuiScreenDebugRenderModelFX()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;

            
            m_scale = 0.7f;

            AddCaption("Render Model FX", Color.Yellow.ToVector4());
            AddShareFocusHint();

            //if (MySession.Static.ControlledObject == null)
                //return;

            AddButton(new StringBuilder("Reload textures"), delegate { VRageRender.MyRenderProxy.ReloadTextures(); });


            //Line line = new Line(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 10);
            //var res = MyEntities.GetIntersectionWithLine(ref line, null, null);
            //if (!res.HasValue)
            //    return;

            ////MyModel model = MySession.Static.ControlledObject.ModelLod0;
            //m_model = res.Value.Entity.ModelLod0;

            m_modelsCombo = AddCombo();
            var modelList = VRage.Game.Models.MyModels.GetLoadedModels();

            if (modelList.Count == 0)
                return;

            for (int i = 0; i < modelList.Count; i++)
            {
                var model = modelList[i];
                m_modelsCombo.AddItem((int)i, new StringBuilder(System.IO.Path.GetFileNameWithoutExtension(model.AssetName)));
            }

            m_modelsCombo.SelectItemByIndex(m_currentModelSelectedItem);
            m_modelsCombo.ItemSelected += new MyGuiControlCombobox.ItemSelectedDelegate(modelsCombo_OnSelect);

            m_model = modelList[m_currentModelSelectedItem];

            if (m_model == null)
                return;

            m_meshesCombo = AddCombo();
            for (int i = 0; i < m_model.GetMeshList().Count; i++)
            {
                var mesh = m_model.GetMeshList()[i];
                m_meshesCombo.AddItem((int)i, new StringBuilder(mesh.Material.Name));
            }
            m_meshesCombo.SelectItemByIndex(m_currentSelectedMeshItem);
            m_meshesCombo.ItemSelected += new MyGuiControlCombobox.ItemSelectedDelegate(meshesCombo_OnSelect);

            if (MySector.MainCamera != null)
            {
                m_voxelsCombo = AddCombo();
                m_voxelsCombo.AddItem(-1, new StringBuilder("None"));
                int i = 0;
                foreach (var voxelMaterial in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
                {
                    m_voxelsCombo.AddItem(i++, new StringBuilder(voxelMaterial.Id.SubtypeName));
                }
                m_voxelsCombo.SelectItemByIndex(m_currentSelectedVoxelItem + 1);
                m_voxelsCombo.ItemSelected += new MyGuiControlCombobox.ItemSelectedDelegate(voxelsCombo_OnSelect);
            }

			if (m_model.GetMeshList().Count <= m_currentSelectedMeshItem)
				return;

            var selectedMesh = m_model.GetMeshList()[m_currentSelectedMeshItem];
            var selectedMaterial = selectedMesh.Material;
            m_diffuseColor = AddColor(new StringBuilder("Diffuse"), selectedMaterial, MemberHelper.GetMember(() => selectedMaterial.DiffuseColor));

            m_specularIntensity = AddSlider("Specular intensity", selectedMaterial.SpecularIntensity, 0, 32, null);

            m_specularPower = AddSlider("Specular power", selectedMaterial.SpecularPower, 0, 128, null);
        }

        void meshesCombo_OnSelect()
        {
            m_currentSelectedMeshItem = (int)m_meshesCombo.GetSelectedKey();

            RecreateControls(false);
        }

        void voxelsCombo_OnSelect()
        {
            m_currentSelectedVoxelItem = (int)m_voxelsCombo.GetSelectedKey();
        }

        void modelsCombo_OnSelect()
        {
            m_currentModelSelectedItem = (int)m_modelsCombo.GetSelectedKey();

            RecreateControls(false);
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);

            MyModel model = m_model;
            var material = model.GetMeshList()[m_currentSelectedMeshItem].Material;
            material.DiffuseColor = m_diffuseColor.GetColor();
            material.SpecularPower = m_specularPower.Value;
            material.SpecularIntensity = m_specularIntensity.Value;

            /*VRageRender.MyRenderProxy.UpdateModelProperties(
                VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED,
                0,
                model.AssetName,
                m_currentSelectedMeshItem,
                null,
                null,
                sender == m_diffuseColor ? material.DiffuseColor : (Color?)null,
                material.SpecularPower,
                material.SpecularIntensity,
                null);*/

            if (m_currentSelectedVoxelItem != -1)
            {
                VRageRender.MyRenderProxy.UpdateVoxelMaterialProperties(
                    (byte)m_currentSelectedVoxelItem,
                    m_specularPower.Value,
                    m_specularIntensity.Value);
            }

        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderModelFX";
        }

    }
#endif
}
