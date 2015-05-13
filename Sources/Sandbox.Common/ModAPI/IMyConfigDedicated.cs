using System;
namespace Sandbox.ModAPI
{
    public interface IMyConfigDedicated
    {
        System.Collections.Generic.List<string> Administrators { get; set; }
        int AsteroidAmount { get; set; }
        System.Collections.Generic.List<ulong> Banned { get; set; }
        string GetFilePath();
        ulong GroupID { get; set; }
        bool IgnoreLastSession { get; set; }
        string IP { get; set; }
        void Load(string path = null);
        string LoadWorld { get; }
        System.Collections.Generic.List<ulong> Mods { get;}
        bool PauseGameWhenEmpty { get; set; }
        void Save(string path = null);
        Sandbox.Definitions.MyDefinitionId Scenario { get; set; }
        string ServerName { get; set; }
        int ServerPort { get; set; }
        Sandbox.Common.ObjectBuilders.MyObjectBuilder_SessionSettings SessionSettings { get; set; }
        int SteamPort { get; set; }
        string WorldName { get; set; }
    }
}
