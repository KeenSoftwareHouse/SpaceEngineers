using System.IO;
using Sandbox;
using VRage.Compiler;
using VRage.FileSystem;

namespace SpaceEngineers.Game
{
    public class MySpaceGameCustomInitialization : MySandboxGame.IGameCustomInitialization
    {
        public void InitIlChecker()
        {
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(SpaceEngineers.Game.ModAPI.IMyButtonPanel));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(SpaceEngineers.Game.ModAPI.Ingame.IMyButtonPanel));
            //TODO: While refactoring sandbox game, move related stuff to spacengineers here
        }

        public void InitIlCompiler()
        {
            IlCompiler.Options = new System.CodeDom.Compiler.CompilerParameters(new string[] {
                "System.Xml.dll"
                ,Path.Combine(MyFileSystem.ExePath, "Sandbox.Game.dll")
                ,Path.Combine(MyFileSystem.ExePath, "Sandbox.Common.dll")
                ,Path.Combine(MyFileSystem.ExePath, "Sandbox.Graphics.dll")
                ,Path.Combine(MyFileSystem.ExePath, "VRage.dll")
                ,Path.Combine(MyFileSystem.ExePath, "VRage.Library.dll")
                ,Path.Combine(MyFileSystem.ExePath, "VRage.Math.dll")
                ,Path.Combine(MyFileSystem.ExePath, "VRage.Game.dll")
                ,Path.Combine(MyFileSystem.ExePath, "VRage.Input.dll")
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
