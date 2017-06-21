
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public delegate void MyWardrobeChangeDelegate(string prevModel, Vector3 prevColorMask, string newModel, Vector3 newColorMask);

    public class MyGuiScreenWardrobe : MyGuiScreenBase
    {
        public static event MyWardrobeChangeDelegate LookChanged;

        private const string m_hueScaleTexture = "Textures\\GUI\\HueScale.png";

        private MyGuiControlCombobox m_modelPicker;

        private MyGuiControlSlider m_sliderHue;
        private MyGuiControlSlider m_sliderSaturation;
        private MyGuiControlSlider m_sliderValue;

        private MyGuiControlLabel m_labelHue;
        private MyGuiControlLabel m_labelSaturation;
        private MyGuiControlLabel m_labelValue;

        private string  m_selectedModel;
        private Vector3 m_selectedHSV;

        private MyCharacter m_user;

        private Dictionary<string, int> m_displayModels;
        private Dictionary<int, string> m_models;

        private string  m_storedModel;
        private Vector3 m_storedHSV;
        private MyCameraControllerSettings m_storedCamera;
        private bool m_colorOrModelChanged;

        public MyGuiScreenWardrobe(MyCharacter user, HashSet<string> customCharacterNames = null)
            : base(size: new Vector2(0.31f, 0.55f),
                   position: MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP),
                   backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR,
                   backgroundTexture: MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.Texture)
        {
            EnabledBackgroundFade = false;

            m_user        = user;
            m_storedModel = m_user.ModelName;
            m_storedHSV   = m_user.ColorMask;

            m_selectedModel = GetDisplayName(m_user.ModelName);
            m_selectedHSV   = m_storedHSV;

            m_displayModels = new Dictionary<string, int>();
            m_models = new Dictionary<int, string>();

            int i = 0;
            if (customCharacterNames == null)
            {
                foreach (var character in MyDefinitionManager.Static.Characters)
                {
                    // NPCs can't be played with while in survival mode
                    if (MySession.Static.SurvivalMode && !character.UsableByPlayer) continue;
                    if (!character.Public) continue;

                    var key = GetDisplayName(character.Name);
                    m_displayModels[key] = i;
                    m_models[i++] = character.Name;
                }

            }
            else
            {
                var definedCharacters = MyDefinitionManager.Static.Characters;

                foreach (var characterName in customCharacterNames)
                {
                    MyCharacterDefinition definition;
                    // NPCs can't be played with while in survival mode
                    if (!definedCharacters.TryGetValue(characterName, out definition) || MySession.Static.SurvivalMode && !definition.UsableByPlayer) continue;
                    if (!definition.Public) continue;

                    var key = GetDisplayName(definition.Name);
                    m_displayModels[key] = i;
                    m_models[i++] = definition.Name;
                }
            }

            RecreateControls(true);

            m_sliderHue.Value        = m_selectedHSV.X * 360f;
            m_sliderSaturation.Value = m_selectedHSV.Y * 100f;
            m_sliderValue.Value      = m_selectedHSV.Z * 100f;

            m_sliderHue.ValueChanged        += OnValueChange;
            m_sliderSaturation.ValueChanged += OnValueChange;
            m_sliderValue.ValueChanged      += OnValueChange;

            ChangeCamera();
            UpdateLabels();
        }

        private string GetDisplayName(string name)
        {
            //MyStringId tmp = MyStringId.GetOrCompute(name);

            //string result;
            //if (!MyTexts.TryGet(tmp, out result))
            //    result = name;

            //return name;
            return MyTexts.GetString(name);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenWardrobe";
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.USE))
            {
                ChangeCameraBack();
                CloseScreen();
            }
            base.HandleInput(receivedFocusInThisUpdate);
        }


        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            var caption = AddCaption(MyCommonTexts.PlayerCharacterModel);
            var listSize = MyGuiControlListbox.GetVisualStyle(MyGuiControlListboxStyleEnum.Default).ItemSize;

            //m_modelPicker = new MyGuiControlCombobox(position: new Vector2(0f, -0.18f));
            float currY = -0.19f;
            m_modelPicker = new MyGuiControlCombobox(position: new Vector2(0f, currY));
            foreach (var entry in m_displayModels)
                m_modelPicker.AddItem(entry.Value, new StringBuilder(entry.Key));

            if (m_displayModels.ContainsKey(m_selectedModel))
                m_modelPicker.SelectItemByKey(m_displayModels[m_selectedModel]);
            else if (m_displayModels.Count > 0)
                m_modelPicker.SelectItemByKey(m_displayModels.First().Value);
            else
                System.Diagnostics.Debug.Fail("No character models loaded.");

            m_modelPicker.ItemSelected += OnItemSelected;
            currY += 0.045f;
            var positionOffset = listSize + caption.Size;

            m_position.X -= (positionOffset.X / 2.5f);
            m_position.Y += (positionOffset.Y * 3.6f);

            Controls.Add(new MyGuiControlLabel(position: new Vector2(0f, currY), text: MyTexts.GetString(MyCommonTexts.PlayerCharacterColor), originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));
            currY += 0.04f;

            Controls.Add( new MyGuiControlLabel(position: new Vector2(-0.135f, currY), text: "Hue:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            m_labelHue = new MyGuiControlLabel(position: new Vector2(0.090f, currY), text: String.Empty, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            currY += 0.035f;
            m_sliderHue = new MyGuiControlSlider(
                position: new Vector2(-0.135f, currY),
                width: 0.3f,
                minValue: 0,
                maxValue: 360,
                labelDecimalPlaces: 0,
                labelSpaceWidth: 50 / 1200f,
                intValue: true,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                visualStyle: MyGuiControlSliderStyleEnum.Hue
                );
            currY += 0.045f;
            Controls.Add(new MyGuiControlLabel(position: new Vector2(-0.135f, currY), text: "Saturation:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            m_labelSaturation = new MyGuiControlLabel(position: new Vector2(0.09f, currY), text: String.Empty, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            currY += 0.035f;
            m_sliderSaturation = new MyGuiControlSlider(
                position: new Vector2(-0.135f, currY),
                width: 0.3f,
                minValue: -100,
                maxValue: 100,
                defaultValue: 0,
                labelDecimalPlaces: 0,
                labelSpaceWidth: 50 / 1200f,
                intValue: true,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
            currY += 0.045f;
            Controls.Add(new MyGuiControlLabel(position: new Vector2(-0.135f, currY), text: "Value:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            m_labelValue = new MyGuiControlLabel(position: new Vector2(0.09f, currY), text: String.Empty, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            currY += 0.035f;
            m_sliderValue = new MyGuiControlSlider(
               position: new Vector2(-0.135f, currY),
               width: 0.3f,
               minValue: -100,
               maxValue: 100,
               defaultValue: 0,
               labelDecimalPlaces: 0,
               labelSpaceWidth: 50 / 1200f,
               intValue: true,
               originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
               );
            currY += 0.045f;

            Controls.Add(caption);
            Controls.Add(m_modelPicker);
            Controls.Add(m_labelHue);
            Controls.Add(m_labelSaturation);
            Controls.Add(m_labelValue);
            Controls.Add(m_sliderHue);
            Controls.Add(m_sliderSaturation);
            Controls.Add(m_sliderValue);
            Controls.Add(new MyGuiControlButton(position: new Vector2(0f, 0.16f), text: new StringBuilder("OK"), onButtonClick: OnOkClick));
            Controls.Add(new MyGuiControlButton(position: new Vector2(0f, 0.22f), text: new StringBuilder("Cancel"), onButtonClick: OnCancelClick));

            m_colorOrModelChanged = false;
        }

        protected override void Canceling()
        {
            m_sliderHue.ValueChanged        -= OnValueChange;
            m_sliderSaturation.ValueChanged -= OnValueChange;
            m_sliderValue.ValueChanged      -= OnValueChange;

            ChangeCharacter(m_storedModel, m_storedHSV);
            ChangeCameraBack();
            base.Canceling();
        }

        protected override void OnClosed()
        {
            m_sliderHue.ValueChanged        -= OnValueChange;
            m_sliderSaturation.ValueChanged -= OnValueChange;
            m_sliderValue.ValueChanged      -= OnValueChange;

            MyGuiScreenGamePlay.ActiveGameplayScreen = null;

            base.OnClosed();
        }

        private void OnOkClick(MyGuiControlButton sender)
        {
            if(m_colorOrModelChanged && LookChanged != null)
                LookChanged(m_storedModel, m_storedHSV, m_user.ModelName, m_user.ColorMask);

            ChangeCameraBack();
            CloseScreenNow();
        }

        private void OnCancelClick(MyGuiControlButton sender)
        {
            ChangeCharacter(m_storedModel, m_storedHSV);
            ChangeCameraBack();
            CloseScreenNow();
        }

        private void OnItemSelected()
        {
            m_selectedModel = m_models[(int)m_modelPicker.GetSelectedKey()];
            ChangeCharacter(m_selectedModel, m_selectedHSV);
        }

        private void OnValueChange(MyGuiControlSlider sender)
        {
            UpdateLabels();
            m_selectedHSV.X = m_sliderHue.Value / 360f;
            m_selectedHSV.Y = m_sliderSaturation.Value / 100f;
            m_selectedHSV.Z = m_sliderValue.Value / 100f;
            m_selectedModel = m_models[(int)m_modelPicker.GetSelectedKey()];
            ChangeCharacter(m_selectedModel, m_selectedHSV);
        }

        private void UpdateLabels()
        {
            m_labelHue.Text        = m_sliderHue.Value.ToString() + "°";
            m_labelSaturation.Text = m_sliderSaturation.Value.ToString();
            m_labelValue.Text      = m_sliderValue.Value.ToString();
        }

        private void ChangeCamera()
        {
            if (MySession.Static.Settings.Enable3rdPersonView)
            {
                m_storedCamera.Controller = MySession.Static.GetCameraControllerEnum();
                m_storedCamera.Distance = MySession.Static.GetCameraTargetDistance();

                MySession.Static.SetCameraController(MyCameraControllerEnum.ThirdPersonSpectator);
                MySession.Static.SetCameraTargetDistance(2f);
            }
        }

        private void ChangeCameraBack()
        {
            if (MySession.Static.Settings.Enable3rdPersonView)
            {
                MySession.Static.SetCameraController(m_storedCamera.Controller, m_user);
                MySession.Static.SetCameraTargetDistance(m_storedCamera.Distance);
            }
        }

        private void ChangeCharacter(string model, Vector3 colorMaskHSV)
        {
            m_colorOrModelChanged = true;
            m_user.ChangeModelAndColor(model, colorMaskHSV);
        }
    }
}
