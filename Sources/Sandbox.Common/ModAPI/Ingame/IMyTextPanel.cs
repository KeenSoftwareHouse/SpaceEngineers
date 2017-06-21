using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.GUI.TextPanel;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyTextPanel : IMyFunctionalBlock
    {
        bool WritePublicText(string value, bool append = false);
        string GetPublicText();

        bool WritePublicTitle(string value, bool append = false);
        string GetPublicTitle();

        [Obsolete("LCD private text is deprecated")]
        bool WritePrivateText(string value, bool append = false);
        [Obsolete("LCD private text is deprecated")]
        string GetPrivateText();

        [Obsolete("LCD private text is deprecated")]
        bool WritePrivateTitle(string value, bool append = false);
        [Obsolete("LCD private text is deprecated")]
        string GetPrivateTitle();

        void AddImageToSelection(string id, bool checkExistence = false);
        void AddImagesToSelection(List<string> ids, bool checkExistence = false);

        void RemoveImageFromSelection(string id, bool removeDuplicates = false);
        void RemoveImagesFromSelection(List<string> ids, bool removeDuplicates = false);

        void ClearImagesFromSelection();

        /// <summary>
        /// Outputs the selected image ids to the specified list.
        /// 
        /// NOTE: List is not cleared internally.
        /// </summary>
        /// <param name="output"></param>
        void GetSelectedImages(List<string> output);

        /// <summary>
        /// The image that is currently shown on the screen.
        /// 
        /// Returns NULL if there are no images selected OR the screen is in text mode.
        /// </summary>
        string CurrentlyShownImage { get; }

        void ShowPublicTextOnScreen();
        [Obsolete("LCD private text is deprecated")]
        void ShowPrivateTextOnScreen();
        void ShowTextureOnScreen();
        void SetShowOnScreen(ShowTextOnScreenFlag set);

        /// <summary>
        /// Indicates what should be shown on the screen, none being an image.
        /// </summary>
        ShowTextOnScreenFlag ShowOnScreen { get; }

        /// <summary>
        /// Returns true if the ShowOnScreen flag is set to either PUBLIC or PRIVATE
        /// </summary>
        bool ShowText { get; }
    }
}
