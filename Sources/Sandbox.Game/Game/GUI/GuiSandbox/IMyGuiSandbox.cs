
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    interface IMyGuiSandbox
    {
        void AddScreen(global::Sandbox.Graphics.GUI.MyGuiScreenBase screen);
        void BackToIntroLogos(Action afterLogosAction);
        void BackToMainMenu();
        void Draw();
        void DrawGameLogo(float transitionAlpha);
        float GetDefaultTextScaleWithLanguage();
        void HandleInput();
        void HandleInputAfterSimulation();
        void HandleRenderProfilerInput();
        bool IsDebugScreenEnabled();
        void LoadContent(MyFontDescription[] fonts);
        void LoadData();
        Vector2 MouseCursorPosition { get; }
        bool OpenSteamOverlay(string url);
        void RemoveScreen(global::Sandbox.Graphics.GUI.MyGuiScreenBase screen);
        void SetMouseCursorVisibility(bool visible, bool changePosition = true);
        void SwitchDebugScreensEnabled();
        void ShowModErrors();
        void TakeScreenshot(int width, int height, string saveToPath = null, bool ignoreSprites = false, bool showNotification = true);
        void TakeScreenshot(string saveToPath = null, bool ignoreSprites = false, global::VRageMath.Vector2? sizeMultiplier = null, bool showNotification = true);
        void UnloadContent();
        void Update(int totalTimeInMS);
    }
}
