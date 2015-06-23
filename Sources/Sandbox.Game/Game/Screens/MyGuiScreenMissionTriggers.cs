using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Game.World.Triggers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens
{
    class MyGuiScreenMissionTriggers : MyGuiScreenBase
    {
        MyGuiControlButton m_okButton, m_cancelButton, m_advancedButton;
        MyGuiControlCombobox[] m_winCombo = new MyGuiControlCombobox[6];
        MyGuiControlCombobox[] m_loseCombo = new MyGuiControlCombobox[6];

        MyTrigger[] m_winTrigger = new MyTrigger[6];
        MyGuiControlButton[] m_winButton = new MyGuiControlButton[6];
        MyTrigger[] m_loseTrigger = new MyTrigger[6];
        MyGuiControlButton[] m_loseButton = new MyGuiControlButton[6];

        MyGuiScreenAdvancedScenarioSettings m_advanced;

        static List<Type> m_triggerTypes;
        static MyGuiScreenMissionTriggers()
        {
            m_triggerTypes=GetTriggerTypes();
        }

        public MyGuiScreenMissionTriggers()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.8f, 0.8f))
        {
            RecreateControls(true);
        }

        public static List<Type> GetTriggerTypes()
        {
            return Assembly.GetCallingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(MyTrigger))).ToList();
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            //CloseButtonEnabled = true;
            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;

            AddCaption(MySpaceTexts.MissionScreenCaption);
            var textBackgroundPanel = AddCompositePanel(MyGuiConstants.TEXTURE_RECTANGLE_DARK, new Vector2(0f,0.08f), new Vector2(0.75f, 0.45f), MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

            m_okButton = new MyGuiControlButton(position: new Vector2(0.17f,0.37f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Refresh),
                onButtonClick: OnOkButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: new Vector2(0.38f,0.37f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Cancel),
                onButtonClick: OnCancelButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);
            m_advancedButton = new MyGuiControlButton(position: new Vector2(0.38f, -0.15f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.WorldSettings_Advanced),
                onButtonClick: OnAdvancedButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            //Controls.Add(m_advancedButton); disabled for now - joining into running game is not finished

            buttonSize = new Vector2(0.05f,0.05f);
            Vector2 pos = new Vector2(0.15f, -0.05f);
            var labelWin = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                position: new Vector2(pos.X-0.37f, pos.Y-0.06f),
                size: (new Vector2(455f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE),
                text: MyTexts.Get(MySpaceTexts.GuiMissionTriggersWinCondition).ToString()
            );
            Controls.Add(labelWin);
            var labelLose = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                position: new Vector2(pos.X, pos.Y - 0.06f),
                size: (new Vector2(455f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE),
                text: MyTexts.Get(MySpaceTexts.GuiMissionTriggersLostCondition).ToString()
            );
            Controls.Add(labelLose);

            for (int i = 0; i < 6; i++)
            {
                //L:
                //combo:
                pos.X -= 0.37f;
                m_winCombo[i] = new MyGuiControlCombobox(pos);
                m_winCombo[i].ItemSelected += OnWinComboSelect;
                m_winCombo[i].AddItem(-1, "");
                foreach(var ttype in m_triggerTypes)
                {
                    //var m = ttype.GetMethod("GetCaption");
                    m_winCombo[i].AddItem(ttype.GetHashCode(), MyTexts.Get((MyStringId)ttype.GetMethod("GetCaption").Invoke(null, null)));
                }
                Controls.Add(m_winCombo[i]);
                //edit button:
                m_winButton[i] = new MyGuiControlButton(position: new Vector2(pos.X + 0.15f, pos.Y), visualStyle: MyGuiControlButtonStyleEnum.Tiny, size: buttonSize, text: new StringBuilder("*"),
                    onButtonClick: OnWinEditButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                m_winButton[i].Enabled = false;
                Controls.Add(m_winButton[i]);
                //R:
                //combo:
                pos.X += 0.37f;
                m_loseCombo[i] = new MyGuiControlCombobox(pos);
                m_loseCombo[i].ItemSelected += OnLoseComboSelect; 
                m_loseCombo[i].AddItem(-1, "");
                foreach (var ttype in m_triggerTypes)
                {
                    var m = ttype.GetMethod("GetFriendlyName");
                    m_loseCombo[i].AddItem(ttype.GetHashCode(), MyTexts.Get((MyStringId)ttype.GetMethod("GetCaption").Invoke(null, null)));
                }
                Controls.Add(m_loseCombo[i]);
                //edit button:
                m_loseButton[i] = new MyGuiControlButton(position: new Vector2(pos.X + 0.15f, pos.Y), visualStyle: MyGuiControlButtonStyleEnum.Tiny, size: buttonSize, text: new StringBuilder("*"),//text: MyTexts.Get(MySpaceTexts.Ok),
                    onButtonClick: OnLoseEditButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                m_loseButton[i].Enabled = false;
                Controls.Add(m_loseButton[i]);

                pos.Y += 0.05f;
            }

            SetDefaultValues();
        }
        private void OnWinComboSelect()
        {
            for (int i = 0; i < 6; i++)
            {
                if (m_winTrigger[i]==null && m_winCombo[i].GetSelectedKey() != -1)
                    m_winTrigger[i] = CreateNew(m_winCombo[i].GetSelectedKey());
                else if (m_winTrigger[i]!=null && m_winCombo[i].GetSelectedKey() == -1)
                    m_winTrigger[i]=null;
                else if (m_winTrigger[i]!=null && m_winCombo[i].GetSelectedKey() != m_winTrigger[i].GetType().GetHashCode())
                    m_winTrigger[i] = CreateNew(m_winCombo[i].GetSelectedKey());

                m_winButton[i].Enabled = m_winCombo[i].GetSelectedKey() != -1;
            }
        }
        private void OnLoseComboSelect()
        {
            for (int i = 0; i < 6; i++)
            {
                if (m_loseTrigger[i] == null && m_loseCombo[i].GetSelectedKey() != -1)
                    m_loseTrigger[i] = CreateNew(m_loseCombo[i].GetSelectedKey());
                else if (m_loseTrigger[i] != null && m_loseCombo[i].GetSelectedKey() == -1)
                    m_loseTrigger[i] = null;
                else if (m_loseTrigger[i] != null && m_loseCombo[i].GetSelectedKey() != m_loseTrigger[i].GetType().GetHashCode())
                    m_loseTrigger[i] = CreateNew(m_loseCombo[i].GetSelectedKey());

                m_loseButton[i].Enabled = m_loseCombo[i].GetSelectedKey() != -1;
            }
        }
        private MyTrigger CreateNew(long hash)
        {
            foreach (var ttype in m_triggerTypes)
                if (ttype.GetHashCode() == hash)
                    return (MyTrigger)Activator.CreateInstance(ttype);
            Debug.Fail("unknown trigger");
            return null;
        }
        
        private void SetDefaultValues()
        {
            //triggers:
            MyMissionTriggers triggers;
            if (!MySessionComponentMissionTriggers.Static.MissionTriggers.TryGetValue(MyMissionTriggers.DefaultPlayerId, out triggers))
            {
                triggers = new MyMissionTriggers();
                MySessionComponentMissionTriggers.Static.MissionTriggers.Add(MyMissionTriggers.DefaultPlayerId, triggers);
                //Debug.Fail("Triggers do not exist");
                return;
            }
            int combo = 0;
            foreach(var trg in triggers.WinTriggers)
            {
                for(int i=0;i<m_winCombo[combo].GetItemsCount();i++)
                {
                    var item=m_winCombo[combo].GetItemByIndex(i);
                    if (item.Key == trg.GetType().GetHashCode())
                    {
                        m_winCombo[combo].ItemSelected -= OnWinComboSelect;
                        m_winCombo[combo].SelectItemByIndex(i);
                        m_winCombo[combo].ItemSelected += OnWinComboSelect;
                        m_winTrigger[combo] = (MyTrigger)trg.Clone();
                        m_winButton[combo].Enabled = true;
                        break;
                    }
                    m_winButton[combo].Enabled = false;
                }
                combo++;
            }

            combo = 0;
            foreach (var trg in triggers.LoseTriggers)
            {
                for (int i = 0; i < m_loseCombo[combo].GetItemsCount(); i++)
                {
                    var item = m_loseCombo[combo].GetItemByIndex(i);
                    if (item.Key == trg.GetType().GetHashCode())
                    {
                        m_loseCombo[combo].ItemSelected -= OnLoseComboSelect;
                        m_loseCombo[combo].SelectItemByIndex(i);
                        m_loseCombo[combo].ItemSelected += OnLoseComboSelect;
                        m_loseTrigger[combo] = (MyTrigger)trg.Clone();
                        m_loseButton[combo].Enabled = true;
                        break;
                    }
                    m_loseButton[combo].Enabled = false;
                }
                combo++;
            }

        }

        protected MyGuiControlCompositePanel AddCompositePanel(MyGuiCompositeTexture texture, Vector2 position, Vector2 size, MyGuiDrawAlignEnum panelAlign)
        {
            var panel = new MyGuiControlCompositePanel()
            {
                BackgroundTexture = texture
            };
            panel.Position = position;
            panel.Size = size;
            panel.OriginAlign = panelAlign;
            Controls.Add(panel);

            return panel;
        }
        private int getButtonNr(object sender)
        {
            for (int i = 0; i < 6; i++)
                if (sender == m_winButton[i] || sender == m_loseButton[i])
                    return i;
            return -1;
        }
        private void OnWinEditButtonClick(object sender)
        {
            m_winTrigger[getButtonNr(sender)].DisplayGUI();
        }
        private void OnLoseEditButtonClick(object sender)
        {
            m_loseTrigger[getButtonNr(sender)].DisplayGUI();
        }
        private void OnOkButtonClick(object sender)
        {
            SaveData();
            CloseScreen();
        }
        private void OnCancelButtonClick(object sender)
        {
            //modified values are forgotten
            CloseScreen();
        }
        private void OnAdvancedButtonClick(object sender)
        {
            m_advanced = new MyGuiScreenAdvancedScenarioSettings(this);
            MyGuiSandbox.AddScreen(m_advanced);
        }

        private void SaveData()
        {
            //delete everyone else's triggers, they will be copied from defaults as needed
            foreach (var trg in MySessionComponentMissionTriggers.Static.MissionTriggers)
                trg.Value.HideNotification();
            MySessionComponentMissionTriggers.Static.MissionTriggers.Clear();
            //create defaults:
            MyMissionTriggers triggers;
            //if (!MySessionComponentMission.Static.MissionTriggers.TryGetValue(m_defaultId, out triggers))
            //{
                //Debug.Fail("Triggers don't exist");
                //return;
                triggers = new MyMissionTriggers();
                MySessionComponentMissionTriggers.Static.MissionTriggers.Add(MyMissionTriggers.DefaultPlayerId, triggers);
            //}
            //triggers.WinTriggers.Clear();
            //triggers.LoseTriggers.Clear();
            for (int i = 0; i < 6; i++)
            {
                if (m_winTrigger[i] != null)
                    triggers.WinTriggers.Add(m_winTrigger[i]);
                if (m_loseTrigger[i] != null)
                    triggers.LoseTriggers.Add(m_loseTrigger[i]);
            }

        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenMissionTriggers";
        }
    }
}
