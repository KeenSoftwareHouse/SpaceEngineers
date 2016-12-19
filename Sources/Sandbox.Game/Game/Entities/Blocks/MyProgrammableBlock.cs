using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Graphics.GUI;
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
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using VRage.FileSystem;
using VRage.Game.ModAPI;
using VRage.Scripting;

namespace Sandbox.Game.Entities.Blocks
{
    internal static class StringCompressor
    {
        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
        public static byte[] CompressString(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }
        public static string DecompressString(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

    }

    [MyCubeBlockType(typeof(MyObjectBuilder_MyProgrammableBlock))]
    public class MyProgrammableBlock : MyFunctionalBlock, ModAPI.IMyProgrammableBlock
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

        private static readonly string[] NEW_LINES = {"\r\n", "\n"};

        private const string DEFAULT_SCRIPT_TEMPLATE = @"public Program() {{
{0}
}}

public void Save() {{
{1}
}}

public void Main(string argument) {{
{2}
}}
";
        static readonly double STOPWATCH_MS_FREQUENCY = 1000.0 / Stopwatch.Frequency;
        static readonly double STOPWATCH_TICKS_FREQUENCY = 10000000.0 / Stopwatch.Frequency;

        private const int MAX_NUM_EXECUTED_INSTRUCTIONS = 50000;
		private const int MAX_NUM_METHOD_CALLS = 10000;
        private const int MAX_ECHO_LENGTH = 8000; // 100 lines á 80 characters
        private ModAPI.IMyGridProgram m_instance = null;
        private readonly RuntimeInfo m_runtime = new RuntimeInfo();
        private string m_programData = null;
        private string m_storageData = null;
        private string m_editorData = null;
        private string m_terminalRunArgument = string.Empty;
        private StringBuilder m_echoOutput = new StringBuilder();

        bool m_consoleOpen = false;
        MyGuiScreenEditor m_editorScreen;
        Assembly m_assembly = null;
        List<string> m_compilerErrors = new List<string>();
        List<MyScriptCompiler.Message> m_compilerMessages = new List<MyScriptCompiler.Message>();
        private ScriptTerminationReason m_terminationReason = ScriptTerminationReason.None;
        private bool m_isRunning = false;
        private bool m_mainMethodSupportsArgument;
        private ulong m_userId;

        public string TerminalRunArgument
        {
            get { return this.m_terminalRunArgument; }
            set { this.m_terminalRunArgument = value ?? string.Empty; }
        }

        public MyProgrammableBlock()
        {
            CreateTerminalControls();
        }

        bool IMyProgrammableBlock.TryRun(string argument)
        {
#if !XB1 // XB1_NOILINJECTOR
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
#else // XB1
            System.Diagnostics.Debug.Assert(false, "No scripts on XB1!");
            return false;
#endif // XB1
        }

        public ulong UserId
        {
            get { return m_userId; }
            set { m_userId = value; }
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyProgrammableBlock>())
                return;
            base.CreateTerminalControls();
            var console = new MyTerminalControlButton<MyProgrammableBlock>("Edit", MySpaceTexts.TerminalControlPanel_EditCode, MySpaceTexts.TerminalControlPanel_EditCode_Tooltip, (b) => b.SendOpenEditorRequest());
            console.Visible = (b) => MyFakes.ENABLE_PROGRAMMABLE_BLOCK && MySession.Static.EnableIngameScripts;
            console.Enabled = (b) => MySession.Static.IsScripter;
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

            var recompile = new MyTerminalControlButton<MyProgrammableBlock>("Recompile", MySpaceTexts.TerminalControlPanel_Recompile, MySpaceTexts.TerminalControlPanel_Recompile_Tooltip, (b) => b.Recompile());
            recompile.Visible = (b) => MyFakes.ENABLE_PROGRAMMABLE_BLOCK && MySession.Static.EnableIngameScripts;
            recompile.Enabled = (b) => b.IsWorking == true && b.IsFunctional == true;
            MyTerminalControlFactory.AddControl(recompile);

            var runAction = new MyTerminalAction<MyProgrammableBlock>("Run", MyTexts.Get(MySpaceTexts.TerminalControlPanel_RunCode), OnRunApplied, null, MyTerminalActionIcons.START);
            runAction.Enabled = (b) => b.IsFunctional == true;
            runAction.DoUserParameterRequest = RequestRunArgument;
            runAction.ParameterDefinitions.Add(TerminalActionParameter.Get(string.Empty));
            MyTerminalControlFactory.AddAction(runAction);

            var runwithDefault = new MyTerminalAction<MyProgrammableBlock>("RunWithDefaultArgument", MyTexts.Get(MySpaceTexts.TerminalControlPanel_RunCodeDefault), OnRunDefaultApplied, MyTerminalActionIcons.START);
            runwithDefault.Enabled = (b) => b.IsFunctional == true;
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

        static string ToIndentedComment(string input)
        {
            var lines = input.Split(NEW_LINES, StringSplitOptions.None);
            return "    // " + string.Join("\n    // ", lines);
        }

        void OpenEditor()
        {
            if (m_editorData == null) {
                var constructorInfo = ToIndentedComment(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_DefaultScript_Constructor).Trim());
                var saveInfo = ToIndentedComment(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_DefaultScript_Save).Trim());
                var mainInfo = ToIndentedComment(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_DefaultScript_Main).Trim());
                m_editorData = string.Format(DEFAULT_SCRIPT_TEMPLATE, constructorInfo, saveInfo, mainInfo);
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
            if (Sync.IsServer)
                Recompile();
            else
                SendUpdateProgramRequest(m_programData);
        }

        public void SendRecompile()
        {
            MyMultiplayer.RaiseEvent(this, x => x.Recompile);  
        }

        [Event, Reliable, Server]
        void Recompile()
        {
            m_compilerErrors.Clear();
            m_compilerMessages.Clear();

            UpdateStorage();
            CompileAndCreateInstance(m_programData, m_storageData);
        }

        void UpdateStorage()
        {
            if (m_instance == null)
                return;

            // Save the current storage first in case the following save call does not exist or fails.
            m_storageData = m_instance.Storage;

            if (m_instance.HasSaveMethod) {
                string response;
                RunSandboxedProgramAction(program =>
                {
                    m_runtime.BeginSaveOperation();
                    m_instance.ElapsedTime = TimeSpan.Zero; // Obsoleted, should eventually be removed
                    program.Save();
                }, out response);
                SetDetailedInfo(response);

                // If the save call didn't fail, update the storage again.
                if (m_instance != null) {
                    m_storageData = m_instance.Storage;
                }
            }
        }

        private void SaveCode(ResultEnum result)
        {
            MyGuiScreenGamePlay.ActiveGameplayScreen = MyGuiScreenGamePlay.TmpGameplayScreenHolder;
            MyGuiScreenGamePlay.TmpGameplayScreenHolder = null;
            SendCloseEditor();
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
            return RunSandboxedProgramAction(program =>
            {
                m_runtime.BeginMainOperation();
                m_instance.ElapsedTime = m_runtime.TimeSinceLastRun; // Obsoleted, should eventually be removed
                program.Main(argument);
                m_runtime.EndMainOperation();
            }, out response);
        }

        public ScriptTerminationReason RunSandboxedProgramAction(Action<ModAPI.IMyGridProgram> action, out string response)
        {
            if (m_isRunning) {
                response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_AllreadyRunning);
                return ScriptTerminationReason.AlreadyRunning;
            }
            if (m_terminationReason != ScriptTerminationReason.None) {
                response = DetailedInfo.ToString();
                return m_terminationReason;
            }
            DetailedInfo.Clear();
            m_echoOutput.Clear();
            if (m_assembly == null) {
                response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoAssembly);
                return ScriptTerminationReason.NoScript;
            }
            var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(CubeGrid);
            var terminalSystem = gridGroup.GroupData.TerminalSystem;
            terminalSystem.UpdateGridBlocksOwnership(this.OwnerId);
            m_instance.GridTerminalSystem = terminalSystem;

            m_isRunning = true;
            response = "";
#if !XB1 // XB1_NOILINJECTOR
            try {
                using (var handle = IlInjector.BeginRunBlock(MAX_NUM_EXECUTED_INSTRUCTIONS, MAX_NUM_METHOD_CALLS)) {
                    m_runtime.InjectorHandle = handle;
                    action(m_instance);
                }
                if (m_echoOutput.Length > 0) {
                    response = m_echoOutput.ToString();
                }
                return m_terminationReason;
            } catch (Exception ex) {
                // Unwrap the exception if necessary
                if (ex is TargetInvocationException) {
                    ex = ex.InnerException;
                }

                // Since we just had an exception I'm not fussed about using old 
                // fashioned string concatenation here. We'll still want the echo
                // output, since its primary purpose is debugging.
                if (m_echoOutput.Length > 0) {
                    response = m_echoOutput.ToString();
                }
                if (ex is ScriptOutOfRangeException) {
                    if (IlInjector.IsWithinRunBlock()) {
                        // If we're within a nested run, we don't reset the program, we just pass the error
                        response += MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NestedTooComplex);
                        return ScriptTerminationReason.InstructionOverflow;
                    } else {
                        response += MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_TooComplex);
                        OnProgramTermination(ScriptTerminationReason.InstructionOverflow);
                    }
                } else {
                    response += MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.Message;
                    OnProgramTermination(ScriptTerminationReason.RuntimeException);
                }
                return m_terminationReason;
            } finally {
                m_runtime.InjectorHandle = null;
                m_isRunning = false;
            }
#else // XB1
            System.Diagnostics.Debug.Assert(false, "No scripts on XB1!");
            return m_terminationReason;
#endif // XB1
        }

        private void OnProgramTermination(ScriptTerminationReason reason)
        {
            m_terminationReason = reason;
            m_instance = null;
            m_assembly = null;
            m_echoOutput.Clear();
            m_runtime.Reset();
        }

        public void Run()
        {
            this.Run(this.TerminalRunArgument);
        }

        public void Run(string argument)
        {
            MySimpleProfiler.Begin("Scripts");
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
               SendRunProgramRequest(argument);
            }
            MySimpleProfiler.End("Scripts");
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
              () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0f);
            sinkComp.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);
            var programmableBlockBuilder = (MyObjectBuilder_MyProgrammableBlock)objectBuilder;
            m_editorData = m_programData = programmableBlockBuilder.Program;
            m_storageData = programmableBlockBuilder.Storage;
            this.m_terminalRunArgument = programmableBlockBuilder.DefaultRunArgument;

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

            // Programs are only compiled and run on a server.
            if (Sync.IsServer)
            {
                if (MySession.Static.EnableIngameScripts)
                {
                    if (m_programData != null)
                    {
                        Recompile();
                    }
                }
                else
                {
                    // Programs are disabled. Send the "not allowed" message to connected clients.
                    string response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NotAllowed);
                    MyMultiplayer.RaiseEvent(this, x => x.WriteProgramResponse, response);
                }
            }

            UpdateEmissivity();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_MyProgrammableBlock objectBuilder = (MyObjectBuilder_MyProgrammableBlock)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.Program = this.m_programData;
            objectBuilder.DefaultRunArgument = this.m_terminalRunArgument;
            if (Sync.IsServer)
            {
                UpdateStorage();
                if (m_instance != null)
                    objectBuilder.Storage = m_instance.Storage;
                else
                    objectBuilder.Storage = m_storageData;
            }

            return objectBuilder;
        }

        private void CompileAndCreateInstance(string program, string storage)
        {
            if (MySession.Static.EnableIngameScripts == false)
            {
                return;
            }
            m_terminationReason = ScriptTerminationReason.None;
            try
            {
                if (MyFakes.ENABLE_ROSLYN_SCRIPTS)
                {
#if !XB1
                    m_assembly = MyScriptCompiler.Static.Compile(
                        MyApiTarget.Ingame,
                        Path.Combine(MyFileSystem.UserDataPath, GetAssemblyName()),
                        MyScriptCompiler.Static.GetIngameScript(program, "Program", typeof(MyGridProgram).Name),
                        m_compilerMessages).Result;

                    m_compilerErrors.Clear();
                    m_compilerErrors.AddRange(m_compilerMessages.Select(m => m.Text));

                    CreateInstance(m_assembly, m_compilerErrors, storage);
#else // XB1
#if !XB1_SKIPASSERTFORNOW
                    System.Diagnostics.Debug.Assert(false, "No scripts on XB1");
#endif // !XB1_SKIPASSERTFORNOW
#endif // XB1
                }
                else
                {
                    Assembly temp = null;
                    MyGuiScreenEditor.CompileProgram(program, m_compilerErrors, ref temp);
                    if (temp != null)
                    {
#if !XB1 // XB1_NOILINJECTOR
                        m_assembly = IlInjector.InjectCodeToAssembly("IngameScript_safe", temp, typeof(IlInjector).GetMethod("CountInstructions", BindingFlags.Public | BindingFlags.Static), typeof(IlInjector).GetMethod("CountMethodCalls", BindingFlags.Public | BindingFlags.Static));
#else // XB1
                        System.Diagnostics.Debug.Assert(false, "No scripts on XB1");
                        return;
#endif // XB1

                        CreateInstance(m_assembly, m_compilerErrors, storage);
                    }
                }
            }
            catch (Exception ex)
            {
                string response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.Message;
                SetDetailedInfo(response);
            }
        }

        string GetAssemblyName()
        {
            var invalidPathChars = Path.GetInvalidFileNameChars();
            var nameBuilder = new StringBuilder();
            nameBuilder.Append(this.EntityId);
            nameBuilder.Append("-");
            for (var i = 0; i < this.CustomName.Length; i++)
            {
                var ch = this.CustomName[i];
                if (invalidPathChars.Contains(ch))
                    nameBuilder.Append("_");
                else
                    nameBuilder.Append(ch);
            }
            nameBuilder.Append(".dll");
            return nameBuilder.ToString();
        }

        bool CreateInstance(Assembly assembly, IEnumerable<string> messages, string storage)
        {
            var response = string.Join("\n", messages);
            if (assembly == null)
            {
                return false;
            }
            var type = assembly.GetType("Program");
            if (type != null)
            {
                m_instance = FormatterServices.GetUninitializedObject(type) as ModAPI.IMyGridProgram;
                var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (m_instance == null || constructor == null)
                {
                    response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoValidConstructor) + "\n\n" + response;
                    SetDetailedInfo(response);
                    return false;
                }
                m_runtime.Reset();
                m_instance.Runtime = m_runtime;
                m_instance.Storage = storage;
                m_instance.Me = this;
                m_instance.Echo = EchoTextToDetailInfo;
                RunSandboxedProgramAction(p =>
                {
                    constructor.Invoke(p, null);

                    if (!m_instance.HasMainMethod)
                    {
                        if (m_echoOutput.Length > 0)
                        {
                            response += "\n\n" + m_echoOutput.ToString();
                        }
                        response = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoMain) + "\n\n" + response; 
                        OnProgramTermination(ScriptTerminationReason.NoEntryPoint);
                    }
                }, out response);
                SetDetailedInfo(response);
            }
            return true;
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

        void ShowEditorAllReadyOpen()
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                 styleEnum: MyMessageBoxStyleEnum.Error,
                                 buttonType: MyMessageBoxButtonsType.OK,
                                 messageText: new StringBuilder("Editor is opened by another player.")));
        }

        public void UpdateProgram(string program)
        {
            this.m_editorData = this.m_programData = program;
            Recompile();
        }

        [Event,Reliable,Server,Broadcast]
        void WriteProgramResponse(string response)
        {
            DetailedInfo.Clear();
            DetailedInfo.Append(response);
            RaisePropertiesChanged();
        }

        bool ModAPI.Ingame.IMyProgrammableBlock.IsRunning { get { return m_isRunning; } }

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
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
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
                SendCloseEditor();
            }
        }

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            UpdateEmissivity();
            base.OnEnabledChanged();
        }

        void SendOpenEditorRequest()
        {
            if (Sync.IsServer)
            {
                if (m_consoleOpen == false)
                {
                    m_consoleOpen = true;
                    OpenEditor();
                }
                else
                {
                   ShowEditorAllReadyOpen();
                }
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.OpenEditorRequest);
            }
        }

        [Event, Reliable, Server]
        void OpenEditorRequest()
        {
            if (m_consoleOpen == false)
            {
                UserId = MyEventContext.Current.Sender.Value;
                m_consoleOpen = true;
                MyMultiplayer.RaiseEvent(this, x => x.OpenEditorSucess, new EndpointId(UserId));
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.OpenEditorFailure,new EndpointId(UserId));
            } 
        }

        [Event, Reliable, Client]
        void OpenEditorSucess()
        {
            OpenEditor();
        }

        [Event, Reliable, Client]
        void OpenEditorFailure()
        {
            ShowEditorAllReadyOpen();
        }

        void SendCloseEditor()
        {
            if (Sync.IsServer)
            {
               m_consoleOpen = false;
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.CloseEditor);
            }
        }

        [Event, Reliable, Server]
        void CloseEditor()
        {
            m_consoleOpen = false;
        }

        void SendUpdateProgramRequest(string program)
        {
            MyMultiplayer.RaiseEvent(this, x => x.UpdateProgram, StringCompressor.CompressString(program));
        }

        [Event, Reliable, Server,Broadcast]
        void UpdateProgram(byte[] program)
        {
            if (!MySession.Static.IsUserScripter(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
            UpdateProgram(StringCompressor.DecompressString(program));
        }

        void SendRunProgramRequest(string argument)
        {
            MyMultiplayer.RaiseEvent(this, x => x.RunProgramRequest, StringCompressor.CompressString(argument ?? string.Empty));
        }

        [Event, Reliable, Server]
        void RunProgramRequest(byte[] argument)
        {
            Run(StringCompressor.DecompressString(argument));
        }

        class RuntimeInfo : IMyGridProgramRuntimeInfo
        {
            double m_lastMainRunTimeMs;
            long m_startTicks;
#if !XB1 // XB1_NOILINJECTOR
            public IlInjector.ICounterHandle InjectorHandle { get; set; }
#endif // !XB1

            public TimeSpan TimeSinceLastRun { get; private set; }

            public double LastRunTimeMs { get; private set; }

#if !XB1 // XB1_NOILINJECTOR
            public int MaxInstructionCount
            {
                get { return InjectorHandle.MaxInstructionCount; }
            }

            public int CurrentInstructionCount
            {
                get { return InjectorHandle.InstructionCount; }
            }

            public int MaxMethodCallCount
            {
                get { return InjectorHandle.MaxMethodCallCount; }
            }

            public int CurrentMethodCallCount
            {
                get { return InjectorHandle.MethodCallCount; }
            }
#else // XB1
            public int MaxInstructionCount
            {
                get { System.Diagnostics.Debug.Assert(false, "No scripts on XB1"); return 0; }
            }

            public int CurrentInstructionCount
            {
                get { System.Diagnostics.Debug.Assert(false, "No scripts on XB1"); return 0; }
            }

            public int MaxMethodCallCount
            {
                get { System.Diagnostics.Debug.Assert(false, "No scripts on XB1"); return 0; }
            }

            public int CurrentMethodCallCount
            {
                get { System.Diagnostics.Debug.Assert(false, "No scripts on XB1"); return 0; }
            }
#endif // XB1

            public void Reset()
            {
                m_lastMainRunTimeMs = 0;
                m_startTicks = 0;
                TimeSinceLastRun = TimeSpan.Zero;
                LastRunTimeMs = 0;
            }

            public void BeginMainOperation()
            {
                double elapsedTimeTicks;
                if (m_startTicks == 0)
                {
                    m_startTicks = Stopwatch.GetTimestamp();
                    elapsedTimeTicks = 0;
                }
                else
                {
                    var ticks = Stopwatch.GetTimestamp();
                    elapsedTimeTicks = (ticks - m_startTicks) * STOPWATCH_TICKS_FREQUENCY;
                    m_startTicks = ticks;
                }
                var scaledTicks = (long)(elapsedTimeTicks);
                TimeSinceLastRun = new TimeSpan(scaledTicks);
                LastRunTimeMs = m_lastMainRunTimeMs;
            }

            public void EndMainOperation()
            {
                var ticks = Stopwatch.GetTimestamp();
                m_lastMainRunTimeMs = (ticks - m_startTicks) * STOPWATCH_MS_FREQUENCY;
            }

            public void BeginSaveOperation()
            {
                // Timing is ignored during save
                TimeSinceLastRun = TimeSpan.Zero;
                LastRunTimeMs = 0;
            }
        }
    }
}
