using Sandbox.Common;
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
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MyProgrammableBlock))]
    class MyProgrammableBlock : MyFunctionalBlock, IMyProgrammableBlock, IMyPowerConsumer
    {
        private MyGridProgramRuntime m_gridProgramRuntime = null;
        private const int MAX_RUN_QUEUE_SIZE = 100;
        private string m_programData = null;
        private string m_editorData = null;
        private string m_terminalRunArgument = string.Empty;

        public bool ConsoleOpen = false;
        MyGuiScreenEditor m_editorScreen;
        Assembly m_assembly = null;
        List<string> m_compilerErrors = new List<string>();
        public bool ConsoleOpenRequest = false;
        private ulong m_userId;
        private new MySyncProgrammableBlock SyncObject;

        private Queue<TerminalActionParameter> m_enqueuedRuns = new Queue<TerminalActionParameter>();
        private readonly List<TerminalActionParameter> _argumentContainer = new List<TerminalActionParameter>(new[] { TerminalActionParameter.Get("") });

        public string TerminalRunArgument
        {
            get { return this.m_terminalRunArgument; }
            set { this.m_terminalRunArgument = value ?? string.Empty; }
        }
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

            var arg = new MyTerminalControlTextbox<MyProgrammableBlock>("ConsoleCommand", MySpaceTexts.TerminalControlPanel_RunArgument, MySpaceTexts.TerminalControlPanel_RunArgument_ToolTip);
            arg.Visible = (e) => MyFakes.ENABLE_PROGRAMMABLE_BLOCK && MySession.Static.EnableIngameScripts;
            arg.Getter = (e) => new StringBuilder(e.TerminalRunArgument);
            arg.Setter = (e, v) => e.TerminalRunArgument = v.ToString();
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
        }

        public MyProgrammableBlock()
        {
            m_gridProgramRuntime = new MyGridProgramRuntime(this);
        }

        private static void OnRunApplied(MyProgrammableBlock programmableBlock, ListReader<TerminalActionParameter> parameters)
        {
            string argument = null;
            var firstParameter = parameters.FirstOrDefault();
            if (!firstParameter.IsEmpty && firstParameter.TypeCode == TypeCode.String)
                argument = firstParameter.Value as string;
            programmableBlock.Run(argument);
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
            SyncObject.SendUpdateProgramRequest(m_programData, m_gridProgramRuntime.Storage);
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
            if (m_gridProgramRuntime.IsRunning)
            {
                return MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_AllreadyRunning);
            }
            if (m_gridProgramRuntime.IsFaulted)
                return m_gridProgramRuntime.FaultMessage;
            DetailedInfo.Clear();
            if (m_assembly == null)
            {
                return MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoAssembly);
            }
            var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(CubeGrid);
            var terminalSystem = gridGroup.GroupData.TerminalSystem;
            terminalSystem.UpdateGridBlocksOwnership(this.OwnerId);

            string retVal;
            if (!m_gridProgramRuntime.TryRun(terminalSystem, argument, out retVal))
                OnProgramTermination();
            return retVal;
        }

        private void OnProgramTermination()
        {
            m_assembly = null;
            m_enqueuedRuns.Clear();
        }

        public void Run()
        {
            this.Run(this.TerminalRunArgument);
        }

        public void Run(string argument)
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
                    this.SyncObject.SendProgramResponseMessage(response);
                    this.WriteProgramResponse(response);
                }
            }
            else
            {
                this.SyncObject.SendRunProgramRequest(argument);
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            var programmableBlockBuilder = (MyObjectBuilder_MyProgrammableBlock)objectBuilder;
            m_editorData = m_programData = programmableBlockBuilder.Program;
            m_gridProgramRuntime.Storage = programmableBlockBuilder.Storage;
            this.m_terminalRunArgument = programmableBlockBuilder.DefaultRunArgument;

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
                    SyncObject.SendProgramResponseMessage(response);
                    WriteProgramResponse(response);
                }
                else
                {
                    SyncObject.SendRunProgramRequest(string.Empty);
                }
                return;
            }
            if (m_programData != null)
            {
                SyncObject.SendUpdateProgramRequest(m_programData, m_gridProgramRuntime.Storage);
            }
            UpdateEmissivity();
        }

        public override void UpdateBeforeSimulation()
        {
            // I tried using UpdateOnceBeforeFrame, but it collided with the code for updating
            // the program. I considered adding a specific flag for when the program should update,
            // but I don't know where else the BEFORE_NEXT_FRAME flag is set. Any better idea is
            // appreciated - but this _is_ just a simple conditional test.
            if (m_enqueuedRuns.Count > 0)
            {
                var nextArgument = m_enqueuedRuns.Dequeue();
                _argumentContainer[0] = nextArgument;
                this.ApplyAction("Run", _argumentContainer);
                if (m_enqueuedRuns.Count > 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                else
                    NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
            }
            
            base.UpdateBeforeSimulation();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_MyProgrammableBlock objectBuilder = (MyObjectBuilder_MyProgrammableBlock)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.Program = this.m_programData;
            objectBuilder.DefaultRunArgument = this.m_terminalRunArgument;
            objectBuilder.Storage = m_gridProgramRuntime.Storage;
            return objectBuilder;
        }

        private void CompileAndCreateInstance(string program)
        {
            if (MySession.Static.EnableIngameScripts == false)
            {
                return;
            }
            m_enqueuedRuns.Clear();
            Assembly temp = null;
            MyGuiScreenEditor.CompileProgram(program, m_compilerErrors, ref temp);
            if (temp != null)
            {
                try
                {
                    string response;

                    m_assembly = IlInjector.InjectCodeToAssembly("IngameScript_safe", temp, typeof(IlInjector).GetMethod("CountInstructions", BindingFlags.Public | BindingFlags.Static));
                    var type = m_assembly.GetType("Program");
                    if (!m_gridProgramRuntime.TryLoad(type, out response))
                    {
                        if (DetailedInfo.ToString() != response)
                        {
                            SyncObject.SendProgramResponseMessage(response);
                            WriteProgramResponse(response);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.Message;
                    if (DetailedInfo.ToString() != response)
                    {
                        SyncObject.SendProgramResponseMessage(response);
                        WriteProgramResponse(response);
                    }
                }

            }
        }

        public void ShowEditorAllReadyOpen()
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                 styleEnum: MyMessageBoxStyleEnum.Error,
                                 buttonType: MyMessageBoxButtonsType.OK,
                                 messageText: new StringBuilder("Editor is opened by another player.")));
        }

        public void UpdateProgram(string program, string storage)
        {
            this.m_editorData = this.m_programData = program;
            m_gridProgramRuntime.Storage = storage;
            m_compilerErrors.Clear();
            if (Sync.IsServer)
            {
                CompileAndCreateInstance(m_programData);
            }
        }

        public void WriteProgramResponse(string response)
        {
            DetailedInfo.Clear();
            DetailedInfo.Append(response);
            RaisePropertiesChanged();
        }

        bool IMyProgrammableBlock.IsRunning { get { return m_gridProgramRuntime.IsRunning; } }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            //new owner needs to recompile script to be able to run it
            OnProgramTermination();
            SyncObject.SendProgramResponseMessage(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_Ownershipchanged));
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
    
        void IMyProgrammableBlock.Run(string argument)
        {
            _argumentContainer[0] = TerminalActionParameter.Get(argument ?? "");
            this.ApplyAction("Run", _argumentContainer);
        }

        bool IMyProgrammableBlock.EnqueueRun(string argument)
        {
            if (m_enqueuedRuns.Count >= MAX_RUN_QUEUE_SIZE)
                return false;
            m_enqueuedRuns.Enqueue(TerminalActionParameter.Get(argument ?? ""));
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            return true;
        }

        int IMyProgrammableBlock.CurrentRunQueueCount
        {
            get { return m_enqueuedRuns.Count; }
        }

        int IMyProgrammableBlock.MaxRunQueueCount
        {
            get { return MAX_RUN_QUEUE_SIZE; }
        }
    }
}
