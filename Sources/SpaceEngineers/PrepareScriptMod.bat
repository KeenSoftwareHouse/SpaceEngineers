echo off
setlocal ENABLEEXTENSIONS
set appdir=%~dp0..\SpaceEngineers\Bin64\
set scriptBasePath=%Appdata%\SpaceEngineers\Mods\Script

echo "%appdir%" 
IF exist "%appdir%" GOTO next
set appdir=%~dp0\OriginalContent


:next
set /a count=0

for /d %%d in (%scriptBasePath%*) do (
    set /a count+=1
)

set /a count+=1

set scriptDirectoryPath=%scriptBasePath%%count%\Data\
mkdir %scriptDirectoryPath%
set scriptPath=%scriptDirectoryPath%\Scripts\
mkdir %scriptPath%
set scriptPath=%scriptPath%\TestScript\
mkdir %scriptPath%
mkdir %scriptPath%\bin\
mkdir %scriptPath%\obj\

attrib +h  %scriptPath%\bin
attrib  +h  %scriptPath%\obj


@set scriptSolutionFile=%scriptPath%\solution.sln
@echo Microsoft Visual Studio Solution File, Format Version 12.00>>%scriptSolutionFile%
@echo # Visual Studio 2012>> %scriptSolutionFile%
@echo Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "script", "script.csproj", "{EBDF9B6E-C9B1-496A-93EE-B5CC1CEA7727}">> %scriptSolutionFile%
@echo EndProject>> %scriptSolutionFile%
@echo Global>> %scriptSolutionFile%
@echo 	GlobalSection(SolutionConfigurationPlatforms) = preSolution>> %scriptSolutionFile%
@echo 		Debug^|Any CPU = Debug^|Any CPU >> %scriptSolutionFile%
@echo 		Release^|Any CPU = Release^|Any CPU >>%scriptSolutionFile%
@echo 	EndGlobalSection >>%scriptSolutionFile%
@echo 	GlobalSection(ProjectConfigurationPlatforms) = postSolution >>%scriptSolutionFile%
@echo 		{EBDF9B6E-C9B1-496A-93EE-B5CC1CEA7727}.Debug^|Any CPU.ActiveCfg = Debug^|Any CPU>>%scriptSolutionFile%
@echo 		{EBDF9B6E-C9B1-496A-93EE-B5CC1CEA7727}.Debug^|Any CPU.Build.0 = Debug^|Any CPU >>%scriptSolutionFile%
@echo 		{EBDF9B6E-C9B1-496A-93EE-B5CC1CEA7727}.Release^|Any CPU.ActiveCfg = Release^|Any CPU >>%scriptSolutionFile%
@echo		{EBDF9B6E-C9B1-496A-93EE-B5CC1CEA7727}.Release^|Any CPU.Build.0 = Release^|Any CPU>>%scriptSolutionFile%
@echo 	EndGlobalSection>>%scriptSolutionFile%
@echo 	GlobalSection(SolutionProperties) = preSolution >>%scriptSolutionFile%
@echo 		HideSolutionNode = FALSE>>%scriptSolutionFile%
@echo 	EndGlobalSection>>%scriptSolutionFile%
@echo EndGlobal>>%scriptSolutionFile%

set scriptProjectFile=%scriptPath%\script.csproj

echo ^<?xml version="1.0" encoding="utf-8"?^>  >>%scriptProjectFile%
echo ^<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"^>  >>%scriptProjectFile%
echo   ^<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" /^> >>%scriptProjectFile%
echo   ^<PropertyGroup^> >>%scriptProjectFile%
echo     ^<Configuration Condition=" '$(Configuration)' == '' "^>Debug^</Configuration^> >>%scriptProjectFile%
echo     ^<Platform Condition=" '$(Platform)' == '' "^>AnyCPU^</Platform^> >>%scriptProjectFile%
echo     ^<ProjectGuid^>{EBDF9B6E-C9B1-496A-93EE-B5CC1CEA7727}^</ProjectGuid^> >>%scriptProjectFile%
echo     ^<OutputType^>Library^</OutputType^> >>%scriptProjectFile%
echo     ^<AppDesignerFolder^>Properties^</AppDesignerFolder^> >>%scriptProjectFile%
echo     ^<RootNamespace^>TestScript^</RootNamespace^> >>%scriptProjectFile%
echo     ^<AssemblyName^>TestScript^</AssemblyName^> >>%scriptProjectFile%
echo    ^<TargetFrameworkVersion^>v4.5^</TargetFrameworkVersion^> >>%scriptProjectFile%
echo     ^<FileAlignment^>512^</FileAlignment^> >>%scriptProjectFile%
echo   ^</PropertyGroup^> >>%scriptProjectFile%
echo   ^<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "^> >>%scriptProjectFile%
echo     ^<PlatformTarget^>AnyCPU^</PlatformTarget^> >>%scriptProjectFile%
echo     ^<DebugSymbols^>true^</DebugSymbols^> >>%scriptProjectFile%
echo     ^<DebugType^>full^</DebugType^> >>%scriptProjectFile%
echo     ^<Optimize^>false^</Optimize^> >>%scriptProjectFile%
echo     ^<OutputPath^>bin\Debug\^</OutputPath^> >>%scriptProjectFile%
echo     ^<DefineConstants^>DEBUG;TRACE^</DefineConstants^> >>%scriptProjectFile%
echo    ^<ErrorReport^>prompt^</ErrorReport^> >>%scriptProjectFile%
echo     ^<WarningLevel^>4^</WarningLevel^> >>%scriptProjectFile%
echo   ^</PropertyGroup^> >>%scriptProjectFile%
echo   ^<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "^> >>%scriptProjectFile%
echo     ^<PlatformTarget^>AnyCPU^</PlatformTarget^> >>%scriptProjectFile%
echo     ^<DebugType^>pdbonly^</DebugType^> >>%scriptProjectFile%
echo     ^<Optimize^>true^</Optimize^> >>%scriptProjectFile%
echo     ^<OutputPath^>bin\Release\^</OutputPath^> >>%scriptProjectFile%
echo    ^<DefineConstants^>TRACE^</DefineConstants^> >>%scriptProjectFile%
echo     ^<ErrorReport^>prompt^</ErrorReport^> >>%scriptProjectFile%
echo     ^<WarningLevel^>4^</WarningLevel^> >>%scriptProjectFile%
echo   ^</PropertyGroup^> >>%scriptProjectFile%
echo   ^<ItemGroup^> >>%scriptProjectFile%
echo ^<Reference Include="Sandbox.Game"^> >>%scriptProjectFile%
echo      ^<HintPath^>%appdir%\Sandbox.Game.dll^</HintPath^> >>%scriptProjectFile%
echo    ^</Reference^>  >>%scriptProjectFile%
echo     ^<Reference Include="Sandbox.Common"^> >>%scriptProjectFile%  >>%scriptProjectFile%
echo       ^<HintPath^>%appdir%\Sandbox.Common.dll ^</HintPath^> >>%scriptProjectFile%
echo     ^</Reference^> >>%scriptProjectFile% 
echo     ^<Reference Include="System" /^>>>%scriptProjectFile%
echo     ^<Reference Include="System.Core" /^>>>%scriptProjectFile%
echo     ^<Reference Include="System.Xml.Linq" /^>>>%scriptProjectFile%
echo     ^<Reference Include="System.Data.DataSetExtensions" /^>>>%scriptProjectFile%
echo     ^<Reference Include="Microsoft.CSharp" /^>>>%scriptProjectFile%
echo     ^<Reference Include="System.Data" /^>>>%scriptProjectFile%
echo     ^<Reference Include="System.Xml" /^>>>%scriptProjectFile%
echo     ^<Reference Include="VRage"^>>>%scriptProjectFile%
echo       ^<HintPath^>%appdir%\VRage.dll^</HintPath^>>>%scriptProjectFile%
echo     ^</Reference^>>>%scriptProjectFile%
echo     ^<Reference Include="VRage.Library"^>>>%scriptProjectFile%
echo       ^<HintPath^>%appdir%\VRage.Library.dll^</HintPath^>>>%scriptProjectFile%
echo     ^</Reference^>>>%scriptProjectFile%
echo     ^<Reference Include="VRage.Math"^>>>%scriptProjectFile%
echo       ^<HintPath^>%appdir%\VRage.Math.dll^</HintPath^>>>%scriptProjectFile%
echo     ^</Reference^>>> %scriptProjectFile%
echo     ^<Reference Include="VRage.Game"^>>>%scriptProjectFile%
echo       ^<HintPath^>%appdir%\VRage.Game.dll^</HintPath^>>>%scriptProjectFile%
echo     ^</Reference^>>> %scriptProjectFile%
echo     ^<Reference Include="VRage.Render"^>>>%scriptProjectFile%
echo       ^<HintPath^>%appdir%\VRage.Render.dll^</HintPath^>>>%scriptProjectFile%
echo     ^</Reference^>>> %scriptProjectFile%
echo     ^<Reference Include="SpaceEngineers.ObjectBuilders"^>>>%scriptProjectFile%
echo       ^<HintPath^>%appdir%\SpaceEngineers.ObjectBuilders.dll^</HintPath^>>>%scriptProjectFile%
echo     ^</Reference^>>> %scriptProjectFile%
echo   ^</ItemGroup^>>>%scriptProjectFile%
echo ^<ItemGroup^>>>%scriptProjectFile%
echo     ^<Compile Include="ModAPISample.cs" /^> >>%scriptProjectFile% 
echo    ^<Compile Include="GameBlockLogicOverrideSample.cs" /^> >>%scriptProjectFile% 
echo   ^</ItemGroup^>>>%scriptProjectFile%
echo   ^<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" /^>>>%scriptProjectFile%
echo   ^<!-- To modify your build process, add your task inside one of the targets below and uncomment it.  >>%scriptProjectFile%
echo        Other similar extension points exist, see Microsoft.Common.targets. >>%scriptProjectFile%
echo   ^<Target Name="BeforeBuild"^>>>%scriptProjectFile%
echo   ^</Target^>>>%scriptProjectFile%
echo   ^<Target Name="AfterBuild"^>>>%scriptProjectFile%
echo   ^</Target^>>>%scriptProjectFile%
echo   --^>>>%scriptProjectFile%
echo ^</Project^>>>%scriptProjectFile%

set scriptFile=%scriptPath%\ModAPISample.cs

echo using System; >>%scriptFile%
echo using System.Collections.Generic;>>%scriptFile%
echo using System.Linq; >>%scriptFile%
echo using System.Text; >>%scriptFile% 
echo using System.Threading.Tasks;>>%scriptFile%
echo using Sandbox.ModAPI; >>%scriptFile%
echo.>>%scriptFile%
echo /*   >>%scriptFile%
echo   Welcome to Modding API. This is one of two sample scripts that you can modify for your needs, >>%scriptFile%
echo   in this case simple script is prepared that will show Hello world message in chat. >>%scriptFile%
echo   You need to run this script manually from chat to see it. To run it you first need to enable this in game >>%scriptFile%
echo   (press new World, than Custom World and Mods , you should see Script1 at the top), when world with mod loads, >>%scriptFile%
echo   please press F11 to see if there was any loading error during loading of the mod. When there is no mod loading errors  >>%scriptFile%
echo   you can activate mod by opening chat window (by pressing Enter key). Than you need to call Main method of script class. >>%scriptFile%
echo.   >>%scriptFile%
echo   To do that you need to write this command : //call Script1_TestScript TestScript.Script ShowHelloWorld>>%scriptFile%
echo   //call means that you want to call script>>%scriptFile%
echo   Script1_TestScript is name of directory (if you have more script directories e.g. Script1, Script2 ... you need to change Script1 to your actual directory)>>%scriptFile%
echo   TestScript.Script is name of tthe class with namespace , if you define new class you need to use new name e.g. when you create class Test in TestScript namespace>>%scriptFile%
echo   you need to write : TestScript.Test >>%scriptFile%
echo   ShowHelloWorld is name of method, you can call only public static methods from chat window. >>%scriptFile%
echo. >>%scriptFile%  
echo    You can define your own namespaces / classes / methods to call >>%scriptFile%
echo  */ >>%scriptFile%
echo. >>%scriptFile%
echo namespace TestScript >>%scriptFile%
echo {>>%scriptFile%
echo     class Script>>%scriptFile%
echo     {>>%scriptFile%
echo       // ShowHelloWorld must be public static, you can define your own methods,>>%scriptFile%
echo       // but to be able to call them from chat they must be public static >>%scriptFile%
echo        static public void ShowHelloWorld()>>%scriptFile%
echo        {>>%scriptFile%
echo             MyAPIGateway.Utilities.ShowMessage("Hello", "World !");>>%scriptFile%
echo        }>>%scriptFile%
echo        //by calling this method, you will see mission Screen>>%scriptFile%
echo       static public void ShowMissionScreen()>>%scriptFile%
echo       {>>%scriptFile%
echo           MyAPIGateway.Utilities.ShowMissionScreen();>>%scriptFile%
echo       }>>%scriptFile%
echo    }>>%scriptFile%
echo }>>%scriptFile%

set scriptFile=%scriptPath%\GameBlockLogicOverrideSample.cs

echo using System; >>%scriptFile%
echo using System.Collections.Generic; >>%scriptFile%
echo using System.Linq;>>%scriptFile%
echo using System.Text;>>%scriptFile%
echo using System.Threading.Tasks;>>%scriptFile%

echo using Sandbox.Common;>>%scriptFile%
echo using VRage.Game.Components;>>%scriptFile%
echo using Sandbox.Common.ObjectBuilders;>>%scriptFile%
echo using Sandbox.Definitions;>>%scriptFile%
echo using Sandbox.Engine;>>%scriptFile%
echo using Sandbox.Game;>>%scriptFile%
echo using Sandbox.ModAPI;>>%scriptFile%
echo using Sandbox.ModAPI.Ingame;>>%scriptFile%
echo using Sandbox.ModAPI.Interfaces;>>%scriptFile%
echo using VRage.Game;>>%scriptFile%
echo using VRage.Game.Components;>>%scriptFile%
echo using VRage.ObjectBuilders;>>%scriptFile%
echo using VRage.ModAPI;>>%scriptFile%
echo using VRage;>>%scriptFile%
echo.>>%scriptFile%
echo /*  >>%scriptFile%
echo   Welcome to Modding API. This is second of two sample scripts that you can modify for your needs,>>%scriptFile%
echo   in this case simple script is prepared that will alter behaviour of sensor block>>%scriptFile%
echo   This type of scripts will be executed automatically  when sensor (or your defined) block is added to world>>%scriptFile%
echo  */>>%scriptFile%
echo namespace TestScript>>%scriptFile%
echo {>>%scriptFile%
echo    //here you can use any objectbuiler e.g. MyObjectBuilder_Door, MyObjectBuilder_Decoy>>%scriptFile%
echo    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SensorBlock))]>>%scriptFile%
echo    public class EvilSensor : MyGameLogicComponent>>%scriptFile%
echo   {>>%scriptFile%
echo        MyObjectBuilder_EntityBase m_objectBuilder = null;>>%scriptFile%
echo        static String[] OreNames;>>%scriptFile%
echo        //here you can use any inferface to your block type e.g. Sandbox.ModAPI.IMyDoort>>%scriptFile%
echo        //if block is missing in Sandbox.ModAPI, you can use Sandbox.ModAPI.Ingame namespace to search for blockt>>%scriptFile%
echo        Sandbox.ModAPI.IMySensorBlock Sensor;>>%scriptFile%
echo.>>%scriptFile%
echo        //if you suscribed to events, please always unsuscribe them in close method >>%scriptFile%
echo        public override void Close() >>%scriptFile%
echo        { >>%scriptFile%
echo            Sensor.StateChanged -= sensor_StateChanged; >>%scriptFile%
echo        } >>%scriptFile%
echo.>>%scriptFile%
echo        public override void Init(MyObjectBuilder_EntityBase objectBuilder)>>%scriptFile%
echo        {>>%scriptFile%
echo            //here you can add new update interval, in this case we would like to update each 100TH frame>>%scriptFile%
echo            //you can also update each frame, each 10Th frame >>%scriptFile%
echo            // you can combine update intervals, so you can update every frame , every 10TH frame and every 100TH frame>>%scriptFile%
echo            Entity.NeedsUpdate ^|= MyEntityUpdateEnum.EACH_100TH_FRAME;>>%scriptFile%
echo            if (OreNames == null)>>%scriptFile%
echo            {>>%scriptFile%
echo                MyDefinitionManager.Static.GetOreTypeNames(out OreNames);>>%scriptFile%
echo            }>>%scriptFile%
echo            m_objectBuilder = objectBuilder;>>%scriptFile%
echo            Sensor = Entity as Sandbox.ModAPI.IMySensorBlock;>>%scriptFile%
echo            Sensor.StateChanged += sensor_StateChanged;>>%scriptFile%
echo        }>>%scriptFile%
echo.>>%scriptFile%
echo        void sensor_StateChanged(bool obj)>>%scriptFile%
echo        {>>%scriptFile%
echo            if(!obj) return;>>%scriptFile%
echo            string ore = null;>>%scriptFile%
echo            foreach(var o in OreNames)>>%scriptFile%
echo            {>>%scriptFile%
echo                if (Sensor.CustomName.StartsWith(o, StringComparison.InvariantCultureIgnoreCase))>>%scriptFile%
echo                {>>%scriptFile%
echo                    ore = o;>>%scriptFile%
echo                    break;>>%scriptFile%
echo                }>>%scriptFile%
echo            }>>%scriptFile%
echo            if (ore == null)>>%scriptFile%
echo                return;>>%scriptFile%
echo            // We want to spawn ore and throw it at entity which entered sensor>>%scriptFile%
echo            MyObjectBuilder_FloatingObject floatingBuilder = new MyObjectBuilder_FloatingObject();>>%scriptFile%
echo            floatingBuilder.Item = new MyObjectBuilder_InventoryItem() { Amount = 100, Content = new MyObjectBuilder_Ore() { SubtypeName = ore } };>>%scriptFile%
echo            floatingBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene; // Very important>>%scriptFile%
echo            floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation()>>%scriptFile%
echo            {>>%scriptFile%
echo                Position = Sensor.WorldMatrix.Translation + Sensor.WorldMatrix.Forward * 1.5, // Spawn ore 1.5m in front of the sensor>>%scriptFile%
echo                Forward = (VRageMath.Vector3)Sensor.WorldMatrix.Forward,>>%scriptFile%
echo                Up = (VRageMath.Vector3)Sensor.WorldMatrix.Up,>>%scriptFile%
echo            };>>%scriptFile%
echo            var floatingObject = Sandbox.ModAPI.MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(floatingBuilder);>>%scriptFile%
echo            // Now it only creates ore, we will throw it later>>%scriptFile%
echo        }>>%scriptFile%
echo.>>%scriptFile%
echo        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)>>%scriptFile%
echo        {>>%scriptFile%
echo            return m_objectBuilder;>>%scriptFile%
echo        }>>%scriptFile%
echo.>>%scriptFile%
echo       //diferrence between UpdateAfter and UpdateBefore simulation is that UpdateAfter  is called after physics simulation and UpdateBefore is called>>%scriptFile%
echo       //before physics simulation>>%scriptFile%
echo.>>%scriptFile%>>%scriptFile%
echo       //this is called when  MyEntityUpdateEnum.EACH_FRAME is used as update interval>>%scriptFile%
echo       public override void UpdateAfterSimulation()>>%scriptFile%
echo       {>>%scriptFile%
echo       }>>%scriptFile%
echo.>>%scriptFile%
echo       //this is called when  MyEntityUpdateEnum.EACH_10TH_FRAME is used as update interval>>%scriptFile%
echo       public override void UpdateAfterSimulation10()>>%scriptFile%
echo       {>>%scriptFile%
echo       }>>%scriptFile%
echo.>>%scriptFile%
echo       //this is called when  MyEntityUpdateEnum.EACH_100TH_FRAME is used as update interval>>%scriptFile%
echo       public override void UpdateAfterSimulation100()>>%scriptFile%
echo       {>>%scriptFile%
echo       }>>%scriptFile%
echo.>>%scriptFile%
echo       public override void UpdateBeforeSimulation()>>%scriptFile%
echo       {>>%scriptFile%
echo       }>>%scriptFile%
echo.>>%scriptFile%
echo       public override void UpdateBeforeSimulation10()>>%scriptFile%
echo       {>>%scriptFile%
echo       }>>%scriptFile%
echo.>>%scriptFile%
echo       public override void UpdateBeforeSimulation100()>>%scriptFile%
echo       {>>%scriptFile%
echo       }>>%scriptFile%
echo 	}>>%scriptFile%
echo }>>%scriptFile%

%scriptSolutionFile%
