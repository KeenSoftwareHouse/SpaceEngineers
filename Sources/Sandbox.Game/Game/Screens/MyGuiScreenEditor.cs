using Sandbox.Common;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using VRage;
using VRage.Utils;
using VRage.Compiler;
using VRageMath;

using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Blocks;
using VRage;
using Sandbox.Game.Localization;
using VRage.Library.Utils;
using VRage.FileSystem;
using Sandbox.Game.Screens;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Scripting;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenEditor : MyGuiScreenText
    {
        private const int MAX_NUMBER_CHARACTERS = 100000;
        private static Vector2 m_editorWindowSize = new Vector2(1.0f, 0.9f);
        private static Vector2 m_editorDescSize = new Vector2(0.94f, 0.73f);

        #region Old Scripting System
        const string CODE_WRAPPER_BEFORE = "using System;\n" +
                                           "using System.Collections.Generic;\n" +
                                           "using VRageMath;\n" +
                                           "using VRage.Game;\n" +
                                           "using System.Text;\n" +
                                           "using Sandbox.ModAPI.Interfaces;\n" +
                                           "using Sandbox.ModAPI.Ingame;\n" +
                                           "using Sandbox.Game.EntityComponents;\n" +
                                           "using VRage.Game.Components;\n" +
                                           "using VRage.Collections;\n" +
                                           "using VRage.Game.ObjectBuilders.Definitions;\n" +
                                           "using VRage.Game.ModAPI.Ingame;\n" +
                                           "using SpaceEngineers.Game.ModAPI.Ingame;\n" +
                                           "public class Program: MyGridProgram\n" +
                                           "{\n";
        const string CODE_WRAPPER_AFTER = "\n}";
        #endregion

        private MyGuiControlButton m_openWorkshopButton;
        private MyGuiControlButton m_checkCodeButton;
        private MyGuiControlButton m_saveChanges;
        private MyGuiControlButton m_help;

        private MyGuiControlLabel m_lineCounter;
        private MyGuiControlLabel m_TextTooLongMessage;
        private MyGuiControlLabel m_LetterCounter;
        private MyGuiControlMultilineEditableText m_editorWindow;
        List<string> m_compilerErrors = new List<string>();
        Action m_saveCodeCallback = null;

        public MyGuiScreenEditor(
            string missionTitle = null,
            string currentObjectivePrefix = null,
            string currentObjective = null,
            string description = null,

            Action<ResultEnum> resultCallback = null,
            Action saveCodeCallback = null,
            string okButtonCaption = null)
            : base(missionTitle, currentObjectivePrefix, currentObjective, description, resultCallback, okButtonCaption, m_editorWindowSize, m_editorDescSize,true)
        {
            m_saveCodeCallback = saveCodeCallback;
            CanHideOthers = true;
        }

        public override void RecreateControls(bool constructor) 
        {
            base.RecreateControls(constructor);
            m_openWorkshopButton = new MyGuiControlButton(position: new Vector2(0.384f, 0.4f), size: MyGuiConstants.BACK_BUTTON_SIZE, text: MyTexts.Get(MyCommonTexts.ScreenLoadSubscribedWorldBrowseWorkshop), toolTip: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_BrowseWorkshop_Tooltip), onButtonClick: OpenWorkshopButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            Controls.Add(m_openWorkshopButton);

            m_okButton.Position = new Vector2(-0.016f, 0.4f);
            m_okButton.SetToolTip(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_CodeEditor_SaveExit_Tooltip));

            m_saveChanges = new MyGuiControlButton(position: new Vector2(0.184f, 0.4f), size: MyGuiConstants.BACK_BUTTON_SIZE, text: MyTexts.Get(MySpaceTexts.ProgrammableBlock_Editor_RememberCode), toolTip: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_RememberCode_Tooltip), onButtonClick: SaveCodeButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            Controls.Add(m_saveChanges);

            m_checkCodeButton = new MyGuiControlButton(position: new Vector2(-0.216f, 0.4f), size: MyGuiConstants.BACK_BUTTON_SIZE, text: MyTexts.Get(MySpaceTexts.ProgrammableBlock_Editor_CheckCode), toolTip: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_CheckCode_Tooltip), onButtonClick: CheckCodeButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            Controls.Add(m_checkCodeButton);

            m_help = new MyGuiControlButton(position: new Vector2(0.384f, -0.4f), size: MyGuiConstants.BACK_BUTTON_SIZE, text: MyTexts.Get(MySpaceTexts.ProgrammableBlock_Editor_Help), toolTip: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_HelpTooltip), onButtonClick: HelpButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            Controls.Add(m_help);

            m_descriptionBox.Position = Vector2.Zero;
            m_descriptionBackgroundPanel.Size *= new Vector2(1.01f, 1.01f);
            m_descriptionBackgroundPanel.Position  = new Vector2(-0.48f,-0.37f);


            m_titleLabel.Position = new Vector2(0.0f, -0.4f);


            m_lineCounter = new MyGuiControlLabel(new Vector2(-0.479f, 0.4f), null, string.Format(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_LineNo), 1, m_editorWindow.GetTotalNumLines()));
            Elements.Add(m_lineCounter);
            m_LetterCounter = new MyGuiControlLabel(new Vector2(-0.479f, -0.4f));
            Elements.Add(m_LetterCounter);
            m_TextTooLongMessage = new MyGuiControlLabel(new Vector2(-0.34f, -0.4f), font: MyFontEnum.Red);
            Elements.Add(m_TextTooLongMessage);

            EnableButtons();
            FocusedControl = m_descriptionBox;
        }

        private void EnableButtons()
        {
            m_openWorkshopButton.Enabled = true;
            m_checkCodeButton.Enabled = true;
            m_saveChanges.Enabled = true;
            m_editorWindow.Enabled = true;
            m_okButton.Enabled = true;
        }

        void OpenWorkshopButtonClicked(MyGuiControlButton button)
        {
            DisableButtons();
            MyScreenManager.AddScreen(new MyGuiIngameScriptsPage(ScriptSelected, GetCode,WorkshopWindowClosed));
        }

        private void DisableButtons()
        {
            m_openWorkshopButton.Enabled = false;
            m_checkCodeButton.Enabled = false;
            m_saveChanges.Enabled = false;
            m_editorWindow.Enabled = false;
            m_okButton.Enabled = false;
        }

        void WorkshopWindowClosed()
        {
            FocusedControl = m_descriptionBox;
            m_openWorkshopButton.Enabled = true;
            m_checkCodeButton.Enabled = true;
            m_saveChanges.Enabled = true;
            m_editorWindow.Enabled = true;
            m_okButton.Enabled = true;
        }

        void CheckCodeButtonClicked(MyGuiControlButton button)
        {
            string code = Description.Text.ToString();
            m_compilerErrors.Clear();
            Assembly assembly = null;
            if (CompileProgram(code, m_compilerErrors, ref assembly))
            {
                if (MyFakes.ENABLE_ROSLYN_SCRIPTS && m_compilerErrors.Count > 0)
                {
                    var messageBuilder = new StringBuilder();
                    foreach (var message in m_compilerErrors)
                    {
                        messageBuilder.Append(message);
                        messageBuilder.Append('\n');
                    }
                    var errorListScreen = new MyGuiScreenMission(missionTitle: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_CompilationOk),
                        currentObjectivePrefix: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_CompilationOkWarningList),
                        currentObjective: "",
                        description: messageBuilder.ToString(),
                        canHideOthers: false,
                        enableBackgroundFade: true,
                        style: MyMissionScreenStyleEnum.BLUE);

                    MyScreenManager.AddScreen(errorListScreen);
                }
                else
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        styleEnum: MyMessageBoxStyleEnum.Info,
                        buttonType: MyMessageBoxButtonsType.OK,
                        messageText: MyTexts.Get(MySpaceTexts.ProgrammableBlock_Editor_CompilationOk),
                        canHideOthers: false));
                }
            }
            else
            {
                string compilerErrors;
                if (MyFakes.ENABLE_ROSLYN_SCRIPTS && m_compilerErrors.Count > 0)
                {
                    compilerErrors = string.Join("\n", m_compilerErrors);
                }
                else
                {
                    compilerErrors = "";
                    foreach (var error in m_compilerErrors)
                    {
                        compilerErrors += FormatError(error) + "\n";
                    }
                }
                var errorListScreen = new MyGuiScreenMission(missionTitle: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_CompilationFailed),
                currentObjectivePrefix: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_CompilationFailedErrorList),
                currentObjective: "",
                description: compilerErrors,
                canHideOthers: false,
                enableBackgroundFade:true,
                style: MyMissionScreenStyleEnum.RED);

                MyScreenManager.AddScreen(errorListScreen);
            }
            FocusedControl = m_descriptionBox;
        }

        void HelpButtonClicked(MyGuiControlButton button)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_BROWSE_WORKSHOP_INGAMESCRIPTS_HELP, "Steam Workshop");

        }
        
        void SaveCodeButtonClicked(MyGuiControlButton button)
        {
            if (m_saveCodeCallback != null)
            {
                m_saveCodeCallback();
            }
        }

        string FormatError(string error)
        {
            try
            {
                char[] sepators = new char[] { ':', ')', '(', ',' };
                string[] errorParts = error.Split(sepators);
                if (errorParts.Length > 2)
                {
                    int line = Convert.ToInt32(errorParts[2]) - m_editorWindow.MeasureNumLines(CODE_WRAPPER_BEFORE);
                    string description = errorParts[6];
                    for (int i = 7; i < errorParts.Length; ++i)
                    {
                        if (string.IsNullOrWhiteSpace(errorParts[i]))
                        {
                            continue;
                        }
                        description += "," + errorParts[i];
                    }
                    return String.Format(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_CompilationFailedErrorFormat), line, description);
                }
                else
                {
                    return error;
                }
            }
            catch (Exception e) { };//unknown error format
            return error;
        }

        public static bool CompileProgram(string program, List<string> errors, ref Assembly assembly)
        {
#if !XB1
            if (!string.IsNullOrEmpty(program))
            {
                if (MyFakes.ENABLE_ROSLYN_SCRIPTS)
                {
                    var messageList = new List<MyScriptCompiler.Message>();
                    assembly = MyScriptCompiler.Static.Compile(
                        MyApiTarget.Ingame,
                        Path.Combine(MyFileSystem.UserDataPath, "EditorCode.dll"),
                        MyScriptCompiler.Static.GetIngameScript(program, "Program", typeof(MyGridProgram).Name),
                        messageList).Result;
                    errors.Clear();
                    errors.AddRange(messageList.OrderByDescending(m => m.Severity).Select(m => m.Text));

                    return assembly != null;
                }

                string finalCode = CODE_WRAPPER_BEFORE + program + CODE_WRAPPER_AFTER;
                if (true == IlCompiler.CompileStringIngame(Path.Combine(MyFileSystem.UserDataPath, "IngameScript.dll"), new string[] { finalCode }, out assembly, errors))
                {
                    return true;
                }
            }
#else // XB1
            System.Diagnostics.Debug.Assert(false, "No scripts on XB1");
#endif // XB1
            return false;
        }

        private void ScriptSelected(string scriptPath)
        {
            string programData = null;
            string fileExtension = Path.GetExtension(scriptPath);
            if (fileExtension == MyGuiIngameScriptsPage.SCRIPT_EXTENSION &&File.Exists(scriptPath))
            {
              programData= File.ReadAllText(scriptPath);
            }
            else if (fileExtension == MyGuiIngameScriptsPage.WORKSHOP_SCRIPT_EXTENSION)
            {
                foreach (var file in MyFileSystem.GetFiles(scriptPath, MyGuiIngameScriptsPage.SCRIPT_EXTENSION, VRage.FileSystem.MySearchOption.AllDirectories))
                {
                    if (MyFileSystem.FileExists(file))
                    {
                        using (var stream = MyFileSystem.OpenRead(file))
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                programData = reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            if (programData != null)
            {
#if XB1
                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
                SetDescription(Regex.Replace(programData, "\r\n", " \n"));
#endif // !XB1
                m_lineCounter.Text = string.Format(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_LineNo), m_editorWindow.GetCurrentCarriageLine(), m_editorWindow.GetTotalNumLines());
                EnableButtons();
            }
        }

        private string GetCode()
        {
            return m_descriptionBox.Text.ToString();
        }

        protected override MyGuiControlMultilineText AddMultilineText(Vector2? size = null, Vector2? offset = null, float textScale = 1.0f, bool selectable = false, MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, MyGuiDrawAlignEnum textBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            Vector2 textboxSize = size ?? this.Size ?? new Vector2(1.2f, 0.5f);

            MyGuiControlMultilineEditableText textbox = new MyGuiControlMultilineEditableText(
                position: textboxSize / 2.0f + (offset ?? Vector2.Zero),
                size: textboxSize,
                backgroundColor: Color.White.ToVector4(),
                textAlign: textAlign,
                textBoxAlign: textBoxAlign,
                font: MyFontEnum.White);

            m_editorWindow = textbox;
            Controls.Add(textbox);

            return textbox;
        }

        public override bool Update(bool hasFocus)
        {
            if (hasFocus && m_editorWindow.CarriageMoved())
            {
                m_lineCounter.Text = string.Format(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_LineNo), m_editorWindow.GetCurrentCarriageLine(), m_editorWindow.GetTotalNumLines());
            }
            if (hasFocus)
            {
                m_LetterCounter.Text = String.Format("{0} / {1}", m_editorWindow.Text.Length, MAX_NUMBER_CHARACTERS);
                if (TextTooLong())
                {
                    m_LetterCounter.Font = MyFontEnum.Red;
                }
                else
                {
                    m_LetterCounter.Font = MyFontEnum.Blue;
                }
                m_TextTooLongMessage.Text = TextTooLong() ? MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Editor_TextTooLong) : "";
            }
            return base.Update(hasFocus);
        }

        public bool TextTooLong()
        {
            return m_editorWindow.Text.Length > MAX_NUMBER_CHARACTERS;
        }
    }
}
