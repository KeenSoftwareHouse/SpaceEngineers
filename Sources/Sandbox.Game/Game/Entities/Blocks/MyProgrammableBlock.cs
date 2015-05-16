﻿using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms.VisualStyles;
using Sandbox.Engine.Physics;
using VRage.Utils;
using VRage.Compiler;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Helpers;
using VRage;
using VRage.Collections;
using VRage.Library.Utils;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MyProgrammableBlock))]
    class MyProgrammableBlock : MyFunctionalBlock, IMyProgrammableBlock, IMyPowerConsumer
    {
        private const int MAX_NUM_EXECUTED_INSTRUCTIONS = 50000;
        private const int MAX_ECHO_LENGTH = 8000; // 100 lines á 80 characters
        private static readonly double STOPWATCH_FREQUENCY = 1.0 / Stopwatch.Frequency;
        private object m_instance = null;
        private string m_programData = null;
        private string m_storageData = null;
        private string m_editorData = null;
        private string m_terminalRunArgument = string.Empty;
        private MethodInfo m_mainMethod = null;
        private FieldInfo m_programGridGroup = null;
        private FieldInfo m_storageField = null;
        private FieldInfo m_meField = null;
        private FieldInfo m_echoField = null;
        private StringBuilder m_echoOutput = new StringBuilder();
        private FieldInfo m_elapsedTimeField = null;
        private long m_previousRunTimestamp = 0;
        
        private readonly object[] m_argumentArray = new object[1]; 

        public bool ConsoleOpen = false;
        MyGuiScreenEditor m_editorScreen;
        Assembly m_assembly = null;
        List<string> m_compilerErrors = new List<string>();
        private bool m_wasTerminated = false;
        private bool m_isRunning = false;
        private bool m_mainMethodSupportsArgument;
        public bool ConsoleOpenRequest = false;
        private ulong m_userId;
        private new MySyncProgrammableBlock SyncObject;

        public string TerminalRunArgument
        {
            get { return this.m_terminalRunArgument; }
            set { this.m_terminalRunArgument = value ?? string.Empty; }
        }
        public bool ClearArgumentOnRun { get; set; }
        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }
        public ulong UserId
        {
            get { return m_userId; }
            set { m_userId = value; }
        }
        static MyProgrammableBlock()
        {
            var console = new MyTerminalControlButton<MyProgrammableBlock>("Edit", MySpaceTexts.TerminalControlPanel_EditCode, MySpaceTexts.TerminalControlPanel_EditCode_Tooltip, (b) => b.SyncObject.SendOpenEditorRequest(Sync.MyId));
            console.Visible = (b) => MyFakes.ENABLE_PROGRAMMABLE_BLOCK && MySession.Static.EnableIngameScripts;
            MyTerminalControlFactory.AddControl(console);

            var arg = new MyTerminalControlTextbox<MyProgrammableBlock>("TerminalRunArgument", MySpaceTexts.TerminalControlPanel_RunArgument, MySpaceTexts.TerminalControlPanel_RunArgument_ToolTip);
            arg.Visible = (e) => MyFakes.ENABLE_PROGRAMMABLE_BLOCK && MySession.Static.EnableIngameScripts;
            arg.Getter = (e) => new StringBuilder(e.TerminalRunArgument);
            arg.Setter = (e, v) => e.SyncObject.RequestChangeTerminalRunArgument(v.ToString());
            arg.EnterPressed = (b) => b.Run();
            MyTerminalControlFactory.AddControl(arg);
            
            var terminalRun = new MyTerminalControlButton<MyProgrammableBlock>("TerminalRun", MySpaceTexts.TerminalControlPanel_RunCode, MySpaceTexts.TerminalControlPanel_RunCode_Tooltip, (b) => b.Run());
            terminalRun.Visible = (b) => MyFakes.ENABLE_PROGRAMMABLE_BLOCK && MySession.Static.EnableIngameScripts;
            terminalRun.Enabled = (b) => b.IsWorking == true && b.IsFunctional == true;
            MyTerminalControlFactory.AddControl(terminalRun);
            
            var runAction = new MyTerminalAction<MyProgrammableBlock>("Run", MyTexts.Get(MySpaceTexts.TerminalControlPanel_RunCode), OnRunApplied, null, MyTerminalActionIcons.START);
            runAction.Enabled = (b) => b.IsWorking == true && b.IsFunctional == true;
            runAction.DoUserParameterRequest = RequestRunArgument;
            runAction.ParameterDefinitions.Add(TerminalActionParameter.Get(string.Empty));
            MyTerminalControlFactory.AddAction(runAction);

            var slaveCheck = new MyTerminalControlCheckbox<MyProgrammableBlock>("ClearArgumentOnRun", MySpaceTexts.TerminalControlPanel_ClearArgumentOnRun, MySpaceTexts.TerminalControlPanel_ClearArgumentOnRun_ToolTip);
            slaveCheck.Getter = (x) => x.ClearArgumentOnRun;
            slaveCheck.Setter = (x, v) => x.SyncObject.RequestChangeClearArgumentOnRun(v);
            MyTerminalControlFactory.AddControl(slaveCheck);
        }

        private static void OnRunApplied(MyProgrammableBlock programmableBlock, ListReader<TerminalActionParameter> parameters)
        {
            string argument = null;
            var firstParameter = parameters.FirstOrDefault();
            if (!firstParameter.IsEmpty && firstParameter.TypeCode == TypeCode.String)
                argument = firstParameter.Value as string;
            // Running this way should never respect the clear programmable argument setting.
            programmableBlock.Run(argument, false);
        }

        /// <summary>
        /// Shows a dialog to configure a custom argument to provide the Run action.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="callback"></param>
        private static void RequestRunArgument(IList<TerminalActionParameter> list, Action<bool> callback)
        {
            // TODO: allocations, needs GUI redo
            MyGuiScreenDialogText dialog = new MyGuiScreenDialogText(string.Empty, MySpaceTexts.DialogText_RunArgument);
            dialog.OnConfirmed += argument =>
            {
                list[0] = TerminalActionParameter.Get(argument);
                callback(true);
            };
            MyGuiSandbox.AddScreen(dialog);
        }

        public void OpenEditor()
        {
            if (m_editorData == null)
            {
                m_editorData = "void Main(string argument)\n{\n}";
            }

            m_editorScreen = new MyGuiScreenEditor(missionTitle: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_CodeEditor_Title),
               currentObjectivePrefix: "",
               currentObjective: "",
               description: m_editorData,
               resultCallback: SaveCode,
               saveCodeCallback: SaveCode,
               okButtonCaption: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_CodeEditor_SaveExit));
            MyGuiScreenGamePlay.TmpGameplayScreenHolder = MyGuiScreenGamePlay.ActiveGameplayScreen;
            MyScreenManager.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = m_editorScreen);
        }

        private void SaveCode()
        {
            if (m_editorScreen.TextTooLong() == true)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MySpaceTexts.ProgrammableBlock_CodeChanged),
                        messageText: MyTexts.Get(MySpaceTexts.ProgrammableBlock_Editor_TextTooLong),
                        buttonType: MyMessageBoxButtonsType.OK,
                        canHideOthers: false);
                MyScreenManager.AddScreen(messageBox);
                return;
            }
            m_editorData = m_programData = m_editorScreen.Description.Text.ToString();
            m_compilerErrors.Clear();
            SyncObject.SendUpdateProgramRequest(m_programData, m_storageData);
        }

        private void SaveCode(ResultEnum result)
        {
            MyGuiScreenGamePlay.ActiveGameplayScreen = MyGuiScreenGamePlay.TmpGameplayScreenHolder;
            MyGuiScreenGamePlay.TmpGameplayScreenHolder = null;
            SyncObject.SendCloseEditor();
            if (m_editorScreen.TextTooLong() == true)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MySpaceTexts.ProgrammableBlock_CodeChanged),
                        messageText: MyTexts.Get(MySpaceTexts.ProgrammableBlock_Editor_TextTooLong),
                        buttonType: MyMessageBoxButtonsType.OK,
                        canHideOthers: false);
                MyScreenManager.AddScreen(messageBox);
                return;
            }

            DetailedInfo.Clear();
            RaisePropertiesChanged();
            if (result == ResultEnum.OK)
            {
                SaveCode();
            }
            else
            {
                string editorText = m_editorScreen.Description.Text.ToString();
                if (editorText != m_programData)
                {
                    var messageBox = MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MySpaceTexts.ProgrammableBlock_CodeChanged),
                        messageText: MyTexts.Get(MySpaceTexts.ProgrammableBlock_SaveChanges),
                        buttonType: MyMessageBoxButtonsType.YES_NO,
                        canHideOthers: false);

                    messageBox.ResultCallback = delegate(MyGuiScreenMessageBox.ResultEnum result2)
                    {
                        if (result2 == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            SaveCode(ResultEnum.OK);
                        }
                        else
                        {
                            m_editorData = m_programData;
                        }
                    };
                    MyScreenManager.AddScreen(messageBox);
                }
            }
        }

        public string ExecuteCode(string argument)
        {
            if (m_isRunning)
            {
                return MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_AllreadyRunning);
            }
            if (m_wasTerminated == true)
            {
                return DetailedInfo.ToString();
            }
            DetailedInfo.Clear();
            m_echoOutput.Clear();
            if (m_assembly == null)
            {
                return MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoAssembly);
            }
            if (m_mainMethod == null)
            {
                return MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoMain);
            }
            if (this.m_elapsedTimeField != null)
            {
                if (m_previousRunTimestamp == 0)
                {
                    m_previousRunTimestamp = Stopwatch.GetTimestamp();
                    m_elapsedTimeField.SetValue(m_instance, TimeSpan.Zero);
                }
                else
                {
                    var currentTimestamp = Stopwatch.GetTimestamp();
                    var elapsedTime = (currentTimestamp - m_previousRunTimestamp) * Sync.RelativeSimulationRatio;
                    m_elapsedTimeField.SetValue(m_instance, TimeSpan.FromSeconds(elapsedTime * STOPWATCH_FREQUENCY));
                    m_previousRunTimestamp = currentTimestamp;
                }
            }
            if (m_programGridGroup != null)
            {
                var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(CubeGrid);
                var terminalSystem = gridGroup.GroupData.TerminalSystem;
                terminalSystem.UpdateGridBlocksOwnership(this.OwnerId);
                m_programGridGroup.SetValue(m_instance, terminalSystem);
            }

            m_isRunning = true;
            string retVal = "";
            IlInjector.RestartCountingInstructions(MAX_NUM_EXECUTED_INSTRUCTIONS);
            try
            {
                if (m_mainMethodSupportsArgument)
                {
                    // Don't know if it's really necessary to predefine this argument array, I suspect not
                    // due to the cleverness of the compiler, but I do it this way just in case. 
                    // Obviously if programmable block execution becomes asynchronous at some point this 
                    // must be reworked.
                    m_argumentArray[0] = argument ?? string.Empty;
                    m_mainMethod.Invoke(m_instance, m_argumentArray);
                }
                else
                {
                    m_mainMethod.Invoke(m_instance, null);
                }
                if (m_echoOutput.Length > 0)
                    retVal = m_echoOutput.ToString();
            }
            catch (TargetInvocationException ex)
            {
                // Since we just had an exception I'm not fussed about using old 
                // fashioned string concatenation here. We'll still want the echo
                // output, since its primary purpose is debugging.
                if (m_echoOutput.Length > 0)
                    retVal = m_echoOutput.ToString();
                OnProgramTermination();
                if (ex.InnerException is ScriptOutOfRangeException)
                {
                    retVal += MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_TooComplex);
                }
                else if (ex.InnerException != null)
                {
                    retVal += MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.InnerException.Message;
                }
            }
            m_isRunning = false;
            return retVal;
        }

        private void OnProgramTermination()
        {
            m_wasTerminated = true;
            m_mainMethod = null;
            m_programGridGroup = null;
            m_storageField = null;
            m_meField = null;
            m_echoField = null;
            m_assembly = null;
            m_echoOutput.Clear();
            m_previousRunTimestamp = 0;
        }

        public void Run()
        {
            this.Run(this.TerminalRunArgument, ClearArgumentOnRun);
        }

        public void Run(string argument, bool clearArgument)
        {
            if (this.IsWorking == false || this.IsFunctional == false)
            {
                return;
            }
            if (Sync.IsServer)
            {
                string response = this.ExecuteCode(argument);
                if (this.DetailedInfo.ToString() != response)
                {
                    this.SyncObject.SendProgramResponseMessage(response, clearArgument);
                    if (clearArgument)
                        this.TerminalRunArgument = "";
                    this.WriteProgramResponse(response);
                }
            }
            else
            {
                this.SyncObject.SendRunProgramRequest(argument, clearArgument);
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            var programmableBlockBuilder = (MyObjectBuilder_MyProgrammableBlock)objectBuilder;
            m_editorData = m_programData = programmableBlockBuilder.Program;
            m_storageData = programmableBlockBuilder.Storage;
            m_terminalRunArgument = programmableBlockBuilder.DefaultRunArgument;
            ClearArgumentOnRun = programmableBlockBuilder.ClearArgumentOnRun;

            this.SyncObject = new MySyncProgrammableBlock(this);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            PowerReceiver = new MyPowerReceiver(
              MyConsumerGroupEnum.Utility,
              false,
              0.0005f,
              () => (Enabled && IsFunctional) ? PowerReceiver.MaxRequiredInput : 0f);

            PowerReceiver.Update();
            PowerReceiver.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyProgrammableBlock_IsWorkingChanged;

            if (Sync.IsServer && Sync.Clients != null)
            {
                Sync.Clients.ClientRemoved += ProgrammableBlock_ClientRemoved;
            }
        }
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (MySession.Static.EnableIngameScripts == false)
            {
                string response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NotAllowed);
                if (Sync.IsServer)
                {
                    SyncObject.SendProgramResponseMessage(response, false);
                    WriteProgramResponse(response);
                }
                else
                {
                    SyncObject.SendRunProgramRequest(string.Empty, false);
                }
                return;
            }
            if (m_programData != null)
            {
                SyncObject.SendUpdateProgramRequest(m_programData, m_storageData);
            }
            UpdateEmissivity();
        }
        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_MyProgrammableBlock objectBuilder = (MyObjectBuilder_MyProgrammableBlock)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.Program = this.m_programData;
            objectBuilder.DefaultRunArgument = this.m_terminalRunArgument;
            objectBuilder.ClearArgumentOnRun = ClearArgumentOnRun;
            if (m_storageField != null && m_instance != null)
            {
                objectBuilder.Storage = (string)m_storageField.GetValue(m_instance);
            }

            return objectBuilder;
        }

        private void CompileAndCreateInstance(string program,string storage)
        {
            if (MySession.Static.EnableIngameScripts == false)
            {
                return;
            }
            m_wasTerminated = false;
            m_mainMethod = null;
            Assembly temp = null;
            MyGuiScreenEditor.CompileProgram(program, m_compilerErrors, ref temp);
            if (temp != null)
            {
                try
                {
                    m_assembly = IlInjector.InjectCodeToAssembly("IngameScript_safe", temp, typeof(IlInjector).GetMethod("CountInstructions", BindingFlags.Public | BindingFlags.Static));

                    var type = m_assembly.GetType("Program");
                    if (type != null)
                    {
                        IlInjector.RestartCountingInstructions(MAX_NUM_EXECUTED_INSTRUCTIONS);
                        try
                        {
                            m_instance = Activator.CreateInstance(type);
                            m_programGridGroup = type.GetField("GridTerminalSystem", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                            m_storageField = type.GetField("Storage", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                            m_meField = type.GetField("Me", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                            m_echoField = type.GetField("Echo", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                            m_elapsedTimeField = type.GetField("ElapsedTime", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                            if (m_programGridGroup != null)
                            {
                                // First try to get the main method with a string argument. If this fails, try to get one without.
                                m_mainMethod = type.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                                m_mainMethodSupportsArgument = m_mainMethod != null;
                                if (m_mainMethod == null)
                                {
                                    m_mainMethod = type.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                                }
                            }
                            if (m_storageField != null)
                            {
                                m_storageField.SetValue(m_instance, storage);
                            }
                            if (m_meField != null)
                            {
                                m_meField.SetValue(m_instance, this);
                            }
                            if (m_echoField != null)
                            {
                                m_echoField.SetValue(m_instance, new Action<string>(EchoTextToDetailInfo));
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            if (ex.InnerException != null)
                            {
                                string response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.InnerException.Message;
                                if (DetailedInfo.ToString() != response)
                                {
                                    SyncObject.SendProgramResponseMessage(response, false);
                                    WriteProgramResponse(response);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.Message;
                    if (DetailedInfo.ToString() != response)
                    {
                        SyncObject.SendProgramResponseMessage(response, false);
                        WriteProgramResponse(response);
                    }
                }

            }
        }

        private void EchoTextToDetailInfo(string line)
        {
            line = line ?? string.Empty;
            var lineLength = line.Length + 1; // line length + lineshift
            if (lineLength > MAX_ECHO_LENGTH)
            {
                // If the input line is already longer than the maximum allowed length,
                // we clear the current output and add only allowed portion of the string
                // to the output. Obviously this is unlikely to happen but it could.
                m_echoOutput.Clear();
                line = line.Substring(0, MAX_ECHO_LENGTH);
            }

            // Now we make sure the addition of this new line does not overshoot the 
            // maximum size by removing any excess amount of characters from the beginning
            // of the stream.
            var newLength = m_echoOutput.Length + lineLength;
            if (newLength > MAX_ECHO_LENGTH)
            {
                m_echoOutput.Remove(0, newLength - MAX_ECHO_LENGTH);
            }

            // Append the new line.
            m_echoOutput.Append(line);
            m_echoOutput.Append('\n');
        }

        public void ShowEditorAllReadyOpen()
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                 styleEnum: MyMessageBoxStyleEnum.Error,
                                 buttonType: MyMessageBoxButtonsType.OK,
                                 messageText: new StringBuilder("Editor is opened by another player.")));
        }

        public void UpdateProgram(string program,string storage)
        {
            this.m_editorData = this.m_programData = program;
            this.m_storageData = storage;
            m_compilerErrors.Clear();
            if (Sync.IsServer)
            {
                CompileAndCreateInstance(m_programData,storage);
            }
        }

        public void WriteProgramResponse(string response)
        {
            DetailedInfo.Clear();
            DetailedInfo.Append(response);
            RaisePropertiesChanged();
        }

        bool IMyProgrammableBlock.IsRunning { get { return m_isRunning; } }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            //new owner needs to recompile script to be able to run it
            OnProgramTermination();
            SyncObject.SendProgramResponseMessage(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_Ownershipchanged), false);
            if (Sync.IsServer)
            {
                WriteProgramResponse(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_Ownershipchanged));
            }
        }

        private void PowerReceiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
        }

        void MyProgrammableBlock_IsWorkingChanged(MyCubeBlock obj)
        {
            UpdateEmissivity();
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            PowerReceiver.Update();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            UpdateIsWorking();
            if (IsWorking)
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
            }
            else
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Red, Color.White);
            }
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        public void ProgrammableBlock_ClientRemoved(ulong playerId)
        {
            if (playerId == m_userId)
            {
                SyncObject.SendCloseEditor();
            }
        }

        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();
            UpdateEmissivity();
            base.OnEnabledChanged();
        }

        public void RefreshProperties()
        {
            this.RaisePropertiesChanged();
        }
    }
}
