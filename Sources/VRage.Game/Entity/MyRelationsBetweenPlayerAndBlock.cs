namespace VRage.Game
{
    public enum MyRelationsBetweenPlayerAndBlock
    {
        // Nobody owns the block
        NoOwnership,

        // The player owns the block
        Owner,

        // Someone from the player's faction owns the block and wants to share it with the player
        FactionShare,

        // Someone from a neutral faction owns the block
        Neutral,

        // Someone from an enemy faction owns the block
        Enemies
    }

    public static class MyRelationsBetweenPlayerAndBlockExtensions
    {
        public static bool IsFriendly(this VRage.Game.MyRelationsBetweenPlayerAndBlock relations)
        {
            return relations == MyRelationsBetweenPlayerAndBlock.NoOwnership || relations == MyRelationsBetweenPlayerAndBlock.Owner || relations == MyRelationsBetweenPlayerAndBlock.FactionShare;
        }
    }
}
