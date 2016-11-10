namespace VRage.Game.ModAPI
{
    public interface IMyHudNotification
    {
        /// <summary>
        /// Get or set the notification text.
        /// Setting the text will immediatly update it if the notification is shown.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Get or set the font for this notification.
        /// Setting it will immediatly update it if the notification is shown.
        /// </summary>
        string Font { get; set; }

        /// <summary>
        /// Get or set the time for the notification to be shown, in miliseconds.
        /// </summary>
        int AliveTime { get; set; }

        /// <summary>
        /// Shows the notification on the HUD.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the notification on the HUD even if it was supposed to show for longer.
        /// </summary>
        void Hide();

        /// <summary>
        /// Reset the alive time for the text.
        /// This is called when setting AliveTime as well.
        /// </summary>
        void ResetAliveTime();
    }
}
