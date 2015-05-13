using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyTextPanel : IMyFunctionalBlock
    {
        bool WritePublicText(string value, bool append = false);
        string GetPublicText();

        bool WritePublicTitle(string value, bool append = false);
        string GetPublicTitle();

        bool WritePrivateText(string value, bool append = false);
        string GetPrivateText();

        bool WritePrivateTitle(string value, bool append = false);
        string GetPrivateTitle();

        void AddImageToSelection(string id, bool checkExistance = false);
        void AddImagesToSelection(List<string> ids, bool checkExistance = false);

        void RemoveImageFromSelection(string id, bool removeDuplicates = false);
        void RemoveImagesFromSelection(List<string> ids, bool removeDuplicates = false);

        void ClearImagesFromSelection();

        void ShowPublicTextOnScreen();
        void ShowPrivateTextOnScreen();
        void ShowTextureOnScreen();
        void SetShowOnScreen(Sandbox.Common.ObjectBuilders.ShowTextOnScreenFlag set);
    }
}
