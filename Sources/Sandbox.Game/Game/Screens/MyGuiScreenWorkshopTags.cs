#if !XB1 // XB1_NOWORKSHOP
using Sandbox.Engine.Networking;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VRage;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenWorkshopTags : MyGuiScreenBase
    {
        private MyGuiControlButton m_okButton;
        private MyGuiControlButton m_cancelButton;

        private static List<MyGuiControlCheckbox> m_checkboxes = new List<MyGuiControlCheckbox>();

        private Action<MyGuiScreenMessageBox.ResultEnum, string[]> m_callback;

        private string m_typeTag;

        private static MyGuiScreenWorkshopTags Static;

        private const int TAGS_MAX_LENGTH = 128; // DONT TOUCH THIS

        private static Vector2 GetScreenSize(MySteamWorkshop.Category[] categories)
        {
            return new Vector2(0.4f, 0.05f * (categories.Length + 1) + 0.2f);
        }

        public MyGuiScreenWorkshopTags(string typeTag, MySteamWorkshop.Category[] categories, string[] tags, Action<MyGuiScreenMessageBox.ResultEnum, string[]> callback)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, GetScreenSize(categories))
        {
            Static = this;
            m_typeTag = typeTag ?? "";

            m_activeTags = new Dictionary<string, MyStringId>(categories.Length);
            foreach (var category in categories)
            {
                m_activeTags.Add(category.Id, category.LocalizableName);
            }

            m_callback = callback;
            EnabledBackgroundFade = true;
            RecreateControls(true);
            SetSelectedTags(tags);
        }

        private static Dictionary<string, MyStringId> m_activeTags;

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            

            AddCaption(MyCommonTexts.ScreenCaptionWorkshopTags);

            Vector2 origin = new Vector2(0f, -0.025f * (m_activeTags.Count + 2));
            Vector2 offset = new Vector2(0f,  0.05f);

            m_checkboxes.Clear();

            foreach (var pair in m_activeTags)
            {
                AddLabeledCheckbox(origin += offset, pair.Key, pair.Value);
                if (m_typeTag == MySteamWorkshop.WORKSHOP_MOD_TAG)
                {
                    var name = pair.Key.Replace(" ", string.Empty);
                    var path = Path.Combine(MyFileSystem.ContentPath, "Textures", "GUI", "Icons", "buttons", name + ".dds");
                    if (File.Exists(path))
                    {
                        AddIcon(origin + new Vector2(-0.05f, 0f), path, new Vector2(0.04f, 0.05f));
                    }
                }
            }

            origin += offset;

            Controls.Add(m_okButton = MakeButton(origin += offset, MyCommonTexts.Ok, MyCommonTexts.Ok, OnOkClick, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER));
            Controls.Add(m_cancelButton = MakeButton(origin, MyCommonTexts.Cancel, MyCommonTexts.Cancel, OnCancelClick, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));

            CloseButtonEnabled = true;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenWorkshopTags";
        }

        private MyGuiControlCheckbox AddLabeledCheckbox(Vector2 position, string tag, MyStringId text)
        {
            MyGuiControlLabel label = MakeLabel(position, text);
            MyGuiControlCheckbox checkbox = MakeCheckbox(position, text);
            Controls.Add(label);
            Controls.Add(checkbox);
            checkbox.UserData = tag;
            m_checkboxes.Add(checkbox);
            return checkbox;
        }

        private MyGuiControlImage AddIcon(Vector2 position, string texture, Vector2 size)
        {
            var image = new MyGuiControlImage()
            {
                Position = position,
                Size = size,
            };
            image.SetTexture(texture);
            Controls.Add(image);
            return image;
        }

        private MyGuiControlLabel MakeLabel(Vector2 position, MyStringId text)
        {
            return new MyGuiControlLabel(position: position, text: MyTexts.GetString(text), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }

        private MyGuiControlCheckbox MakeCheckbox(Vector2 position, MyStringId tooltip)
        {
            var checkbox = new MyGuiControlCheckbox(position: position, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER, toolTip: MyTexts.GetString(tooltip));
            checkbox.IsCheckedChanged += OnCheckboxChanged;
            return checkbox;
        }

        private MyGuiControlButton MakeButton(Vector2 position, MyStringId text, MyStringId toolTip, Action<MyGuiControlButton> onClick, MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            return new MyGuiControlButton(
                            position: position,
                            text: MyTexts.Get(text),
                            toolTip: MyTexts.GetString(toolTip),
                            onButtonClick: onClick,
                            originAlign: originAlign);
        }

        private static void OnCheckboxChanged(MyGuiControlCheckbox obj)
        {
            if (obj == null)
                return;

            if (obj.IsChecked)
            {
                if (Static.GetSelectedTagsLength() >= TAGS_MAX_LENGTH)
                {
                    obj.IsChecked = false;
                }
            }
        }

        private void OnOkClick(MyGuiControlButton obj)
        {
            this.CloseScreen();
            m_callback(Sandbox.Graphics.GUI.MyGuiScreenMessageBox.ResultEnum.YES, GetSelectedTags());
        }

        private void OnCancelClick(MyGuiControlButton obj)
        {
            this.CloseScreen();
            m_callback(Sandbox.Graphics.GUI.MyGuiScreenMessageBox.ResultEnum.CANCEL, GetSelectedTags());
        }

        protected override void Canceling()
        {
            base.Canceling();
            m_callback(Sandbox.Graphics.GUI.MyGuiScreenMessageBox.ResultEnum.CANCEL, GetSelectedTags());
        }

        public int GetSelectedTagsLength()
        {
            int length = m_typeTag.Length;

            foreach (var checkbox in m_checkboxes)
            {
                if (checkbox.IsChecked)
                {
                    length += ((string)checkbox.UserData).Length;
                }
            }

            return length;
        }

        public string[] GetSelectedTags()
        {
            var tags = new List<string>();

            if (!string.IsNullOrEmpty(m_typeTag))
            {
                tags.Add(m_typeTag);
            }

            foreach (var checkbox in m_checkboxes)
            {
                if (checkbox.IsChecked)
                {
                    tags.Add((string)checkbox.UserData);
                }
            }
            return tags.ToArray();
        }

        public void SetSelectedTags(string[] tags)
        {
            if (tags == null)
                return;

            foreach (var tag in tags)
            {
                foreach (var checkbox in m_checkboxes)
                {
                    if (tag.Equals((string)checkbox.UserData,StringComparison.InvariantCultureIgnoreCase))
                    {
                        checkbox.IsChecked = true;
                    }
                }
            }
        }
    }
}
#endif // !XB1
