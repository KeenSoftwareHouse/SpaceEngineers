#region Using 

using Medieval.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;


#endregion

namespace Sandbox.Game.Screens.DebugScreens
{
    #if !XB1

    [MyDebugScreen("Game", "Voxel materials")]
    public class MyGuiScreenDebugVoxelMaterials : MyGuiScreenDebugBase
    {
        MyGuiControlCombobox m_materialsCombo;

        MyDx11VoxelMaterialDefinition m_selectedVoxelMaterial;

        bool m_canUpdate = false;

        MyGuiControlSlider m_sliderInitialScale;
        MyGuiControlSlider m_sliderScaleMultiplier;
        MyGuiControlSlider m_sliderInitialDistance;
        MyGuiControlSlider m_sliderDistanceMultiplier;

        MyGuiControlSlider m_sliderFar1Scale;
        MyGuiControlSlider m_sliderFar1Distance;
        MyGuiControlSlider m_sliderFar2Scale;
        MyGuiControlSlider m_sliderFar2Distance;

        MyGuiControlSlider m_sliderFar3Distance;
        MyGuiControlSlider m_sliderFar3Scale;
        MyGuiControlColor m_colorFar3;

        MyGuiControlSlider m_sliderExtScale;

        public MyGuiScreenDebugVoxelMaterials()
        {
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugVoxelMaterials";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            BackgroundColor = new Vector4(1f, 1f, 1f, 0.5f);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.13f);

            AddCaption("Voxel materials", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_materialsCombo = AddCombo();

            var defList = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().OrderBy(x => x.Id.SubtypeName).ToList();

            foreach (var material in defList)
            {
                m_materialsCombo.AddItem(material.Index, new StringBuilder(material.Id.SubtypeName));
            }
            m_materialsCombo.ItemSelected += materialsCombo_OnSelect;
            m_currentPosition.Y += 0.01f;

            m_sliderInitialScale = AddSlider("Initial scale", 0, 1f, 100f, null);
            m_sliderScaleMultiplier = AddSlider("Scale multiplier", 0, 1f, 100f, null);
            m_sliderInitialDistance = AddSlider("Initial distance", 0, 1f, 100f, null);
            m_sliderDistanceMultiplier = AddSlider("Distance multiplier", 0, 1f, 100f, null);

            m_sliderFar1Distance = AddSlider("Far1 distance", 0, 0f, 20000f, null);
            m_sliderFar1Scale = AddSlider("Far1 scale", 0, 1f, 50000f, null);
            m_sliderFar2Distance = AddSlider("Far2 distance", 0, 0f, 20000f, null);
            m_sliderFar2Scale = AddSlider("Far2 scale", 0, 1f, 50000f, null);
            m_sliderFar3Distance = AddSlider("Far3 distance", 0, 0f, 40000f, null);
            m_sliderFar3Scale = AddSlider("Far3 scale", 0, 1f, 50000f, null);

            m_sliderExtScale = AddSlider("Detail scale (/1000)", 0, 0.01f, 1f, null);

            m_materialsCombo.SelectItemByIndex(0);

            m_colorFar3 = AddColor("Far3 color", m_selectedVoxelMaterial, MemberHelper.GetMember(() => m_selectedVoxelMaterial.Far3Color));
            m_colorFar3.SetColor(m_selectedVoxelMaterial.Far3Color);

            m_currentPosition.Y += 0.01f;

            AddButton(new StringBuilder("Reload definition"), OnReloadDefinition);
        }
        private void materialsCombo_OnSelect()
        {
            m_selectedVoxelMaterial = (MyDx11VoxelMaterialDefinition)MyDefinitionManager.Static.GetVoxelMaterialDefinition((byte)m_materialsCombo.GetSelectedKey());
            UpdateValues();
        }
        void UpdateValues()
        {
            m_canUpdate = false;

            m_sliderInitialScale.Value = m_selectedVoxelMaterial.InitialScale;
            m_sliderScaleMultiplier.Value = m_selectedVoxelMaterial.ScaleMultiplier;
            m_sliderInitialDistance.Value = m_selectedVoxelMaterial.InitialDistance;
            m_sliderDistanceMultiplier.Value = m_selectedVoxelMaterial.DistanceMultiplier;

            m_sliderFar1Scale.Value = m_selectedVoxelMaterial.Far1Scale;
            m_sliderFar1Distance.Value = m_selectedVoxelMaterial.Far1Distance;
            m_sliderFar2Scale.Value = m_selectedVoxelMaterial.Far2Scale;
            m_sliderFar2Distance.Value = m_selectedVoxelMaterial.Far2Distance;
            m_sliderFar3Scale.Value = m_selectedVoxelMaterial.Far3Scale;
            m_sliderFar3Distance.Value = m_selectedVoxelMaterial.Far3Distance;
            if (m_colorFar3 != null)
                m_colorFar3.SetColor(m_selectedVoxelMaterial.Far3Color);
            m_sliderExtScale.Value = 1000.0f * m_selectedVoxelMaterial.ExtensionDetailScale;

            m_canUpdate = true;
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);

            if (!m_canUpdate)
                return;

            m_selectedVoxelMaterial.InitialScale = m_sliderInitialScale.Value;
            m_selectedVoxelMaterial.ScaleMultiplier = m_sliderScaleMultiplier.Value;
            m_selectedVoxelMaterial.InitialDistance = m_sliderInitialDistance.Value;
            m_selectedVoxelMaterial.DistanceMultiplier = m_sliderDistanceMultiplier.Value;

            m_selectedVoxelMaterial.Far1Scale = m_sliderFar1Scale.Value;
            m_selectedVoxelMaterial.Far1Distance = m_sliderFar1Distance.Value;
            m_selectedVoxelMaterial.Far2Scale = m_sliderFar2Scale.Value;
            m_selectedVoxelMaterial.Far2Distance = m_sliderFar2Distance.Value;
            m_selectedVoxelMaterial.Far3Scale = m_sliderFar3Scale.Value;
            m_selectedVoxelMaterial.Far3Distance = m_sliderFar3Distance.Value;
            m_selectedVoxelMaterial.Far3Color = m_colorFar3.GetColor();

            m_selectedVoxelMaterial.ExtensionDetailScale = m_sliderExtScale.Value / 1000.0f;

            MyDefinitionManager.Static.UpdateVoxelMaterial(m_selectedVoxelMaterial);

        }

        void OnReloadDefinition(MyGuiControlButton button)
        {
            MyDefinitionManager.Static.ReloadVoxelMaterials();

            materialsCombo_OnSelect();

            MyDefinitionManager.Static.UpdateVoxelMaterial(m_selectedVoxelMaterial);
        }
    }

#endif
}