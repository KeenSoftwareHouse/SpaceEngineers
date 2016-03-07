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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Definitions;
using VRage.Compiler;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens;
using VRage;
using VRage.Collections;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using VRage.Game;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MyProgrammableBlock))]
    class MyProgrammableBlock : MyFunctionalBlock, IMyProgrammableBlock
    {
        /// <summary>
        /// Determines why (if at all) a script was terminated.
        /// </summary>
        public enum ScriptTerminationReason
        {
            /// <summary>
            /// The script was not terminated.
            /// </summary>
            None,

            /// <summary>
            /// There is no script (assembly) available.
            /// </summary>
            NoScript,

            /// <summary>
            /// No entry point (void Main(), void Main(string argument)) could be found.
            /// </summary>
            NoEntryPoint,

            /// <summary>
            /// The maximum allowed number of instructions has been reached.
            /// </summary>
            InstructionOverflow,

            /// <summary>
            /// The programmable block has changed ownership and must be rebuilt.
            /// </summary>
            OwnershipChange,

            /// <summary>
            /// A runtime exception happened during the execution of the script.
            /// </summary>
            RuntimeException,

            /// <summary>
            /// The script is already running (technically not a termination reason, but will be returned if a script tries to run itself in a nested fashion).
            /// </summary>
            AlreadyRunning
        }

        private const int MAX_NUM_EXECUTED_INSTRUCTIONS = 50000;
		private const int MAX_NUM_METHOD_CALLS = 10000;
        private const int MAX_ECHO_LENGTH = 8000; // 100 lines á 80 characters
        private static readonly double STOPWATCH_FREQUENCY = 1.0 / Stopwatch.Frequency;
        private IMyGridProgram m_instance = null;
        private string m_programData = null;
        private string m_storageData = null;
        private string m_editorData = null;
        private string m_terminalRunArgument = string.Empty;
        private StringBuilder m_echoOutput = new StringBuilder();
        private long m_previousRunTimestamp = 0;

        public bool ConsoleOpen = false;
        MyGuiScreenEditor m_editorScreen;
        Assembly m_assembly = null;
        List<string> m_compilerErrors = new List<string>();
        private ScriptTerminationReason m_terminationReason = ScriptTerminationReason.None;
        //private bool m_wasTerminated = false;
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

        bool IMyProgrammableBlock.TryRun(string argument)
        {
            // If we find some reason why a run couldn't possibly work, return false
            if (m_instance == null || m_isRunning || this.IsWorking == false || this.IsFunctional == false)
            {
                return false;
            }

            if (!IsFunctional || !IsWorking)
            {
                return false;
            }

            string response;
            var result = this.ExecuteCode(argument ?? "", out response);
            SetDetailedInfo(response);
            if (result == ScriptTerminationReason.InstructionOverflow)
                throw new ScriptOutOfRangeException();
            return result == ScriptTerminationReason.None;
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

            var runwithDefault = new MyTerminalAction<MyProgrammableBlock>("RunWithDefaultArgument", MyTexts.Get(MySpaceTexts.TerminalControlPanel_RunCodeDefault), OnRunDefaultApplied, MyTerminalActionIcons.START);
            runwithDefault.Enabled = (b) => b.IsWorking == true && b.IsFunctional == true;
            MyTerminalControlFactory.AddAction(runwithDefault);
        }

        private static void OnRunApplied(MyProgrammableBlock programmableBlock, ListReader<TerminalActionParameter> parameters)
        {
            string argument = null;
            var firstParameter = parameters.FirstOrDefault();
            if (!firstParameter.IsEmpty && firstParameter.TypeCode == TypeCode.String)
                argument = firstParameter.Value as string;
            programmableBlock.Run(argument);
        }

        private static void OnRunDefaultApplied(MyProgrammableBlock programmableBlock)
        {
            programmableBlock.Run();
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
            // If there is an existing instance, make sure the storage data is updated before sending
            // an update request
            if (m_instance != null)
            {
                m_storageData = m_instance.Storage;
            }
            SyncObject.SendUpdateProgramRequest(m_programData, m_storageData);
        }

        public void SendRecompile()
        {
            MyMultiplayer.RaiseEvent(this, x => x.Recompile);  
        }

        [Event, Reliable, Server]
        void Recompile()
        {
            m_compilerErrors.Clear();
            // If there is an existing instance, make sure the storage data is updated before 
            // rebuilding the program.
            if (m_instance != null)
            {
                m_storageData = m_instance.Storage;
            }
            CompileAndCreateInstance(m_programData, m_storageData);
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

        public ScriptTerminationReason ExecuteCode(string argument, out string response)
        {
            if (m_isRunning)
            {
                response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_AllreadyRunning);
                return ScriptTerminationReason.AlreadyRunning;
            }
            if (m_terminationReason != ScriptTerminationReason.None)
            {
                response = DetailedInfo.ToString();
                return m_terminationReason;
            }
            DetailedInfo.Clear();
            m_echoOutput.Clear();
            if (m_assembly == null)
            {
                response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoAssembly);
                return ScriptTerminationReason.NoScript;
            }
            if (!m_instance.HasMainMethod)
            {
                response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoMain);
                return ScriptTerminationReason.NoEntryPoint;
            }
            if (m_previousRunTimestamp == 0)
            {
                m_previousRunTimestamp = Stopwatch.GetTimestamp();
                m_instance.ElapsedTime = TimeSpan.Zero;
            }
            else
            {
                var currentTimestamp = Stopwatch.GetTimestamp();
                var elapsedTime = (currentTimestamp - m_previousRunTimestamp) * Sync.RelativeSimulationRatio;
                m_instance.ElapsedTime = TimeSpan.FromSeconds(elapsedTime * STOPWATCH_FREQUENCY);
                m_previousRunTimestamp = currentTimestamp;
            }
            var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(CubeGrid);
            var terminalSystem = gridGroup.GroupData.TerminalSystem;
            terminalSystem.UpdateGridBlocksOwnership(this.OwnerId);
            m_instance.GridTerminalSystem = terminalSystem;

            m_isRunning = true;
            response = "";
            try
            {
                using (IlInjector.BeginRunBlock(MAX_NUM_EXECUTED_INSTRUCTIONS, MAX_NUM_METHOD_CALLS))
                {
                    m_instance.Main(argument);
                }
                if (m_echoOutput.Length > 0)
                    response = m_echoOutput.ToString();
            }
            catch (Exception ex)
            {
                // Since we just had an exception I'm not fussed about using old 
                // fashioned string concatenation here. We'll still want the echo
                // output, since its primary purpose is debugging.
                if (m_echoOutput.Length > 0)
                    response = m_echoOutput.ToString();
                if (ex is ScriptOutOfRangeException)
                {
                    if (IlInjector.IsWithinRunBlock())
                    {
                        // If we're within a nested run, we don't reset the program, we just pass the error
                        response += MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NestedTooComplex);
                        return ScriptTerminationReason.InstructionOverflow;
                    }
                    else
                    {
                        response += MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_TooComplex);
                        OnProgramTermination(ScriptTerminationReason.InstructionOverflow);
                    }
                }
                else
                {
                    response += MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.Message;
                    OnProgramTermination(ScriptTerminationReason.RuntimeException);
                }
            }
            finally
            {
                m_isRunning = false;
            }
            return m_terminationReason;
        }

        private void OnProgramTermination(ScriptTerminationReason reason)
        {
            m_terminationReason = reason;
            m_instance = null;
            m_assembly = null;
            m_echoOutput.Clear();
            m_previousRunTimestamp = 0;
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
                string response;
                this.ExecuteCode(argument, out response);
                SetDetailedInfo(response);
            }
            else
            {
                this.SyncObject.SendRunProgramRequest(argument);
            }
        }

        private void SetDetailedInfo(string detailedInfo)
        {
            if (this.DetailedInfo.ToString() != detailedInfo)
            {
                MyMultiplayer.RaiseEvent(this, x => x.WriteProgramResponse, detailedInfo);
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            var blockDefinition = BlockDefinition as MyProgrammableBlockDefinition;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
              blockDefinition.ResourceSinkGroup,
              0.0005f,
              () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInput : 0f);
            sinkComp.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);
            var programmableBlockBuilder = (MyObjectBuilder_MyProgrammableBlock)objectBuilder;
            m_editorData = m_programData = programmableBlockBuilder.Program;
            m_storageData = programmableBlockBuilder.Storage;
            this.m_terminalRunArgument = programmableBlockBuilder.DefaultRunArgument;

            this.SyncObject = new MySyncProgrammableBlock(this);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
	    	
			ResourceSink.Update();

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
                    MyMultiplayer.RaiseEvent(this, x => x.WriteProgramResponse, response);   
                }
                else
                {
                    SyncObject.SendRunProgramRequest(string.Empty);
                }
                return;
            }
            if (m_programData != null)
            {
                // If there is an existing instance, make sure the storage data is updated before sending
                // an update request
                if (m_instance != null)
                {
                    m_storageData = m_instance.Storage;
                }
                SyncObject.SendUpdateProgramRequest(m_programData, m_storageData);
            }
            UpdateEmissivity();
        }
        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_MyProgrammableBlock objectBuilder = (MyObjectBuilder_MyProgrammableBlock)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.Program = this.m_programData;
            objectBuilder.DefaultRunArgument = this.m_terminalRunArgument;
            if (m_instance != null)
            {
                objectBuilder.Storage = m_instance.Storage;
            }

            return objectBuilder;
        }

        private void CompileAndCreateInstance(string program,string storage)
        {
            if (MySession.Static.EnableIngameScripts == false)
            {
                return;
            }
            m_terminationReason = ScriptTerminationReason.None;
            try
            {
                Assembly temp = null;
                MyGuiScreenEditor.CompileProgram(program, m_compilerErrors, ref temp);
                if (temp != null)
                {
                    m_assembly = IlInjector.InjectCodeToAssembly("IngameScript_safe", temp, typeof(IlInjector).GetMethod("CountInstructions", BindingFlags.Public | BindingFlags.Static), typeof(IlInjector).GetMethod("CountMethodCalls", BindingFlags.Public | BindingFlags.Static));

                    var type = m_assembly.GetType("Program");
                    if (type != null)
                    {
                        try
                        {
                            using (IlInjector.BeginRunBlock(MAX_NUM_EXECUTED_INSTRUCTIONS, MAX_NUM_METHOD_CALLS))
                            {
                                m_instance = Activator.CreateInstance(type) as IMyGridProgram;
                            }
                            if (m_instance != null)
                            {
                                m_previousRunTimestamp = 0;
                                m_instance.Storage = storage;
                                m_instance.Me = this;
                                m_instance.Echo = EchoTextToDetailInfo;
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            if (ex.InnerException != null)
                            {
                                string response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.InnerException.Message;
                                if (DetailedInfo.ToString() != response)
                                {
                                    MyMultiplayer.RaiseEvent(this, x => x.WriteProgramResponse, response);   
                                }
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
                    MyMultiplayer.RaiseEvent(this, x => x.WriteProgramResponse, response);   
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

        public void UpdateProgram(string program, string storage)
        {
            this.m_editorData = this.m_programData = program;
            this.m_storageData = storage;
            m_compilerErrors.Clear();
            if (Sync.IsServer)
            {
                CompileAndCreateInstance(m_programData,storage);
            }
        }

        [Event,Reliable,Server,Broadcast]
        void WriteProgramResponse(string response)
        {
            DetailedInfo.Clear();
            DetailedInfo.Append(response);
            RaisePropertiesChanged();
        }

        bool IMyProgrammableBlock.IsRunning { get { return m_isRunning; } }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            //In survival mode, the new owner needs to recompile the script to be able to run it
            if (MySession.Static.SurvivalMode)
            {
                OnProgramTermination(ScriptTerminationReason.OwnershipChange);
                if (Sync.IsServer)
                {
                    MyMultiplayer.RaiseEvent(this, x => x.WriteProgramResponse, MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_Ownershipchanged));
                }
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
            return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
			ResourceSink.Update();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
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
			ResourceSink.Update();
            UpdateEmissivity();
            base.OnEnabledChanged();
        }
    }
}
