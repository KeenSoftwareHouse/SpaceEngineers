
using Sandbox.Game.Entities;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenColorPicker : MyGuiScreenBase
    {

        private MyGuiControlSlider m_hueSlider;
        private MyGuiControlSlider m_saturationSlider;
        MyGuiControlSlider m_valueSlider;
        private MyGuiControlLabel m_hueLabel;
        private MyGuiControlLabel m_saturationLabel;
        private MyGuiControlLabel m_valueLabel;
        private MyGuiControlPanel m_colorVariantPanel;
        private List<Vector3> m_oldPaletteList;
        private MyGuiControlPanel m_highlightControlPanel;
        private List<MyGuiControlPanel> m_colorPaletteControlsList = new List<MyGuiControlPanel>(MyPlayer.BuildColorSlotCount);
        private const int x = -170;
        private const int y = -250;
        private const int defColLine = 300;
        private const int defColCol = 25;

        private const string m_hueScaleTexture = "Textures\\GUI\\HueScale.png";

        public MyGuiScreenColorPicker()
            : base(GetInitPosition(), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(400f / 1600f, 700 / 1200f))
        {
            CanHideOthers = false;
            RecreateControls(true);
            m_oldPaletteList = new List<Vector3>();

			Debug.Assert(MySession.Static.LocalHumanPlayer != null, "Creating gui color picker without local human player!");
			foreach (var element in MySession.Static.LocalHumanPlayer.BuildColorSlots)
			{
				m_oldPaletteList.Add(element);
			}
			UpdateSliders(MyPlayer.SelectedColor);
            UpdateLabels();
        }

        private static Vector2 GetInitPosition()
        {
            if ((float)MySandboxGame.ScreenSize.X / MySandboxGame.ScreenSize.Y < 1.5f)
                return new Vector2(0.15f, 0.3f);
            else
                return new Vector2(0.06f, 0.3f);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption("Color picker");

            

            m_hueSlider = new MyGuiControlSlider(
                position: new Vector2(x / 1600f, (y + 50) / 1200f),
                width: 0.25f,
                minValue: 0,
                maxValue: 360,
                labelText: String.Empty,
                labelDecimalPlaces: 0,
                labelSpaceWidth: 50 / 1200f,
                intValue: true,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                visualStyle: MyGuiControlSliderStyleEnum.Hue
                );
            m_saturationSlider  = new MyGuiControlSlider(
                position: new Vector2(x / 1600f, (y + 150) / 1200f),
                width: 0.25f,
                minValue: -100,
                maxValue: 100,
                defaultValue: 0,
                labelText: String.Empty,
                labelDecimalPlaces: 0,
                labelSpaceWidth: 50/1200f,
                intValue: true,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
            m_valueSlider = new MyGuiControlSlider(
                position: new Vector2(x / 1600f, (y + 250) / 1200f),
                width: 0.25f,
                minValue: -100,
                maxValue: 100,
                defaultValue: 0,
                labelText: String.Empty,
                labelDecimalPlaces: 0,
                labelSpaceWidth: 50 / 1200f,
                intValue: true,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );

            m_hueSlider.ValueChanged += OnValueChange;
            m_saturationSlider.ValueChanged += OnValueChange;
            m_valueSlider.ValueChanged += OnValueChange;

            m_hueLabel = new MyGuiControlLabel(position: new Vector2(100 / 1600f, y / 1200f), text: String.Empty, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_saturationLabel = new MyGuiControlLabel(position: new Vector2(100 / 1600f, (y + 100) / 1200f), text: String.Empty, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_valueLabel = new MyGuiControlLabel(position: new Vector2(100 / 1600f, (y + 200) / 1200f), text: String.Empty, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);



            Controls.Add(new MyGuiControlLabel(position: new Vector2(x / 1600f, y / 1200f), text: "Hue:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            Controls.Add(m_hueLabel);
            Controls.Add(m_hueSlider);
            Controls.Add(new MyGuiControlLabel(position: new Vector2(x / 1600f, (y + 100) / 1200f), text: "Saturation:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            Controls.Add(m_saturationLabel);
            Controls.Add(m_saturationSlider);
            Controls.Add(new MyGuiControlLabel(position: new Vector2(x / 1600f, (y + 200) / 1200f), text: "Value:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            Controls.Add(m_valueLabel);
            Controls.Add(m_valueSlider);
            Controls.Add(new MyGuiControlButton(
                size: new Vector2(100f, 0.1f),
				position: new Vector2(0 / 1600f, (y + 285 + (MyPlayer.BuildColorSlotCount / 7 + 1) * 36) / 1200f),
                text: new StringBuilder("Defaults"),
                onButtonClick: OnDefaultsClick));
            Controls.Add(new MyGuiControlButton(
                size: new Vector2 (100f,0.1f),
				position: new Vector2(0 / 1600f, (y + 360 + (MyPlayer.BuildColorSlotCount / 7 + 1) * 36) / 1200f),
                text: new StringBuilder("OK"),
                onButtonClick: OnOkClick));
            Controls.Add(new MyGuiControlButton(
				position: new Vector2(0 / 1600f, (y + 435 + (MyPlayer.BuildColorSlotCount / 7 + 1) * 36) / 1200f),
                text: new StringBuilder("Cancel"),
                onButtonClick: OnCancelClick));
            
            Color c = Color.White;
            int j = 0;
            m_highlightControlPanel = new MyGuiControlPanel(
                size: new Vector2(0.03f, 0.03f),
				position: new Vector2(((x + defColCol) / 1600f) + (MyPlayer.SelectedColorSlot % 7) * 0.03f, (y + defColLine) / 1200f + (MyPlayer.SelectedColorSlot / 7) * 0.03f));
            m_highlightControlPanel.ColorMask = c.ToVector4();
            m_highlightControlPanel.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;
            Controls.Add(m_highlightControlPanel);
            int tmpx = MyPlayer.BuildColorSlotCount;
			for (int i = 0; i < MyPlayer.BuildColorSlotCount; )
            {
                MyGuiControlPanel tmpPanel = new MyGuiControlPanel(
                size: new Vector2(0.025f, 0.025f),
                position: new Vector2(((x + defColCol) / 1600f) + (i % 7) * 0.03f, (y + defColLine) / 1200f + j * 0.03f));
                tmpPanel.ColorMask = (prev(MyPlayer.ColorSlots.ItemAt(i))).HSVtoColor().ToVector4();
                tmpPanel.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;
                m_colorPaletteControlsList.Add(tmpPanel);
                Controls.Add(tmpPanel);
                i++;
                if (i % 7 == 0)
                    j++;
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            OnSetVisible(true);
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            MyGuiScreenGamePlay.ActiveGameplayScreen = null;
            OnSetVisible(false);
        }

        protected override void OnHide()
        {
            base.OnHide();
            OnSetVisible(false);
        }

        private void OnSetVisible(bool visible)
        {
            if (MyCubeBuilder.Static != null)
            {
                MyCubeBuilder.Static.UseTransparency = !visible;
            }
        }
        private void UpdateLabels()
        {
            m_hueLabel.Text = m_hueSlider.Value.ToString() + "°";
            m_saturationLabel.Text = m_saturationSlider.Value.ToString();
            m_valueLabel.Text = m_valueSlider.Value.ToString();
        }

        private void UpdateSliders(Vector3 HSV)
        {
            m_hueSlider.Value = HSV.X * 360;
            m_saturationSlider.Value = HSV.Y * 100;
            m_valueSlider.Value = HSV.Z * 100;
        }
        private Vector3 prev(Vector3 HSV)
        {
            return new Vector3(HSV.X, MathHelper.Clamp(HSV.Y + 0.8f, 0f, 1f), MathHelper.Clamp(HSV.Z + 0.55f, 0f, 1f));
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.LANDING_GEAR))
            {
                CloseScreenNow();
            }
            
			var humanPlayer = MySession.Static.LocalHumanPlayer;
			if (humanPlayer != null && 
				(MyInput.Static.IsNewLeftMousePressed() || MyControllerHelper.IsControl(Sandbox.Engine.Utils.MySpaceBindingCreator.CX_GUI, MyControlsGUI.ACCEPT, MyControlStateType.NEW_PRESSED)))
            {
                for (int i = 0; i < m_colorPaletteControlsList.Count; i++)
                {
                    if (m_colorPaletteControlsList[i].IsMouseOver)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
						humanPlayer.SelectedBuildColorSlot = i;
						m_highlightControlPanel.Position = new Vector2(((x + defColCol) / 1600f) + (humanPlayer.SelectedBuildColorSlot % 7) * 0.03f, (y + defColLine) / 1200f + (humanPlayer.SelectedBuildColorSlot / 7) * 0.03f);
						UpdateSliders(humanPlayer.SelectedBuildColor);
                    }
                }
            }
            base.HandleInput(receivedFocusInThisUpdate);
        } 

        private void OnValueChange(MyGuiControlSlider sender)
        {
			var humanPlayer = MySession.Static.LocalHumanPlayer;
			if (humanPlayer == null)
				return;

            UpdateLabels();
            Color c = new Color();
            c = (new Vector3(m_hueSlider.Value/360f, MathHelper.Clamp(m_saturationSlider.Value/100f + 0.8f,0f,1f), MathHelper.Clamp(m_valueSlider.Value/100f + 0.55f,0f,1f))).HSVtoColor();
			m_colorPaletteControlsList[humanPlayer.SelectedBuildColorSlot].ColorMask = c.ToVector4();
			humanPlayer.SelectedBuildColor = new Vector3((m_hueSlider.Value / 360f), (m_saturationSlider.Value / 100f), (m_valueSlider.Value / 100f));
        }

        private void OnDefaultsClick(MyGuiControlButton sender)
        {
			var humanPlayer = MySession.Static.LocalHumanPlayer;
			if (humanPlayer == null)
				return;

			humanPlayer.SetDefaultColors();
            Color c = Color.White;
            for (int index = 0; index < MyPlayer.BuildColorSlotCount; ++index)
                m_colorPaletteControlsList[index].ColorMask = (prev(humanPlayer.BuildColorSlots[index])).HSVtoColor().ToVector4();
            UpdateSliders(humanPlayer.SelectedBuildColor);
        }

        private void OnOkClick(MyGuiControlButton sender)
        {
			var humanPlayer = MySession.Static.LocalHumanPlayer;
			if (humanPlayer != null)
			{
				bool colorsChanged = false;
				int index = 0;
				foreach(var color in humanPlayer.BuildColorSlots)
				{
					if(m_oldPaletteList[index] != color)
					{
						colorsChanged = true;
						m_oldPaletteList[index] = color;
					}
					++index;
				}

				if(colorsChanged)
					Sync.Players.RequestPlayerColorsChanged(humanPlayer.Id.SerialId, m_oldPaletteList);	
			}

            this.CloseScreenNow();
        }

        private void OnCancelClick(MyGuiControlButton sender)
        {
			var humanPlayer = MySession.Static.LocalHumanPlayer;
			if (humanPlayer != null)
				humanPlayer.SetBuildColorSlots(m_oldPaletteList);

            this.CloseScreenNow();
        }

        public override string GetFriendlyName()
        {
            return "ColorPick";
        }
    }
}
