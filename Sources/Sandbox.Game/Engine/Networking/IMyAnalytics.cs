using System;
using System.Collections.Generic;
using VRage;
using Sandbox.Common.ObjectBuilders;

namespace Sandbox.Engine.Networking
{
    public struct MyProcessStartAnalytics
    {
        public string OsVersion;
        public string CpuInfo;
        public ulong TotalPhysMemBytes;
        public string GpuInfo;
        public string GameVersion;
    }

    public enum MyGameEntryEnum
    {
        Quickstart,
        Custom,
        Scenario,
        Tutorial,
        Load,
        Join
    }

    public struct MyGameplayStartAnalytics
    {
        public MyGameEntryEnum GameEntry;
        public MyGameModeEnum GameMode;
        public MyOnlineModeEnum OnlineMode;
        public List<string> ActiveMods;
    }

    public struct MyGameplayEndAnalytics
    {
        public uint TimeOnFootSeconds;
        public uint TimeOnJetpackSeconds;
        public uint TimeOnSmallShipSeconds;
        public uint TimeOnBigShipSeconds;
        public int AverageFramesPerSecond;
        public int MinFramesPerSecond;
        public int MaxFramesPerSecond;
        public int AverageLatencyMilliseconds;
        public int TotalBlocksChanged;
        public int TotalDamageDealt;
        public Dictionary<string, MyFixedPoint> TotalAmountMined;
    }

    public interface IMyAnalytics
    {
        void IdentifyPlayer(ulong playerId, string playerName, bool isOnline);

        void ReportProcessStart(MyProcessStartAnalytics attributes, bool firstRun = false);
        void ReportProcessEnd();

        void ReportGameplayStart(MyGameplayStartAnalytics attributes);
        void ReportGameplayEnd(MyGameplayEndAnalytics attributes);

        void ReportActivityStart(string activityName, string activityFocus, string activityType, string activityItemUsage, bool expectActivityEnd = true);
        void ReportActivityEnd(string activityName);

        void ReportPlayerDeath(string deathType, string deathCause);

        void ReportTutorialStart(string tutorialName);
        void ReportTutorialStep(string stepName, int stepNumber);
        void ReportTutorialEnd();

        void FlushAndDispose();
    }
}
