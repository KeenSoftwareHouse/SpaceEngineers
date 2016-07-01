using System.CodeDom;
using System.IO;
using System.Reflection;
using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using VRage.Compiler;
using VRage.FileSystem;
using VRage.Scripting;

namespace SpaceEngineers.Game
{
    public class MySpaceGameCustomInitialization : MySandboxGame.IGameCustomInitialization
    {
        public void InitIlChecker()
        {
            if (MyFakes.ENABLE_ROSLYN_SCRIPTS)
            {
                using (var handle = MyScriptCompiler.Static.Whitelist.OpenBatch())
                {
                    handle.AllowNamespaceOfTypes(MyWhitelistTarget.Both,
                        typeof(SpaceEngineers.Game.ModAPI.Ingame.IMyButtonPanel));

                    handle.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi,
                        typeof(SpaceEngineers.Game.ModAPI.IMyButtonPanel));
                }
                return;
            }

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(SpaceEngineers.Game.ModAPI.IMyButtonPanel));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(SpaceEngineers.Game.ModAPI.Ingame.IMyButtonPanel));
            //TODO: While refactoring sandbox game, move related stuff to space engineers here
        }

        public void InitIlCompiler()
        {
            if (MyFakes.ENABLE_ROSLYN_SCRIPTS)
            {
                // The compatibility layer causes duplicate usings in scripts. This is completely harmless
                // so we switch off the reporting of this warning.
                MyScriptCompiler.Static.IgnoredWarnings.Add("CS0105"); // The using directive for 'X' appeared previously in this namespace

                MyScriptCompiler.Static.AddReferencedAssemblies(
                    Path.Combine(MyFileSystem.ExePath, "Sandbox.Game.dll"),
                    Path.Combine(MyFileSystem.ExePath, "Sandbox.Common.dll"),
                    Path.Combine(MyFileSystem.ExePath, "Sandbox.Graphics.dll"),
                    Path.Combine(MyFileSystem.ExePath, "VRage.dll"),
                    Path.Combine(MyFileSystem.ExePath, "VRage.Library.dll"),
                    Path.Combine(MyFileSystem.ExePath, "VRage.Math.dll"),
                    Path.Combine(MyFileSystem.ExePath, "VRage.Game.dll"),
                    Path.Combine(MyFileSystem.ExePath, "VRage.Input.dll"),
                    Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.ObjectBuilders.dll"),
                    Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.Game.dll")
                );

                MyScriptCompiler.Static.AddImplicitIngameNamespacesFromTypes(
                        typeof(VRageMath.Vector2),
                        typeof(VRage.Game.Game),
                        typeof(Sandbox.ModAPI.Interfaces.ITerminalAction),
                        typeof(Sandbox.ModAPI.Ingame.IMyGridTerminalSystem),
                        typeof(Sandbox.Game.EntityComponents.MyModelComponent),
                        typeof(VRage.Game.Components.IMyComponentAggregate),
                        typeof(VRage.Collections.ListReader<>),
                        typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_FactionDefinition),
                        typeof(VRage.Game.ModAPI.Ingame.IMyCubeBlock),
                        typeof(SpaceEngineers.Game.ModAPI.Ingame.IMyAirVent)
                        );

                if (MyFakes.ENABLE_ROSLYN_SCRIPT_DIAGNOSTICS)
                {
                    MyScriptCompiler.Static.DiagnosticOutputPath = Path.Combine(MyFileSystem.UserDataPath, "ScriptDiagnostics");
                }

                return;
            }

            IlCompiler.Options = new System.CodeDom.Compiler.CompilerParameters(new string[] {
                "System.Xml.dll"
                ,Path.Combine(MyFileSystem.ExePath, "Sandbox.Game.dll")
                ,Path.Combine(MyFileSystem.ExePath, "Sandbox.Common.dll")
                ,Path.Combine(MyFileSystem.ExePath, "Sandbox.Graphics.dll")
                ,Path.Combine(MyFileSystem.ExePath, "VRage.dll")
                ,Path.Combine(MyFileSystem.ExePath, "VRage.Library.dll")
                ,Path.Combine(MyFileSystem.ExePath, "VRage.Math.dll")
                ,Path.Combine(MyFileSystem.ExePath, "VRage.Game.dll")
                ,"System.Core.dll"
                ,"System.dll"
                ,Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.ObjectBuilders.dll")
                ,Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.Game.dll")
                //, "Microsoft.CSharp.dll"
            });
            IlCompiler.Options.GenerateInMemory = true;
        }
    }
}
