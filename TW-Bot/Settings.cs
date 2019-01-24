namespace TW_Bot
{
    public static class Settings
    {
        public static int MIN_MORAL = 80;
        // Requires premium, because of report filtering. Will check new reports, send rams to troop less villages with walls > 0. Will not farm villages that were unsuccessful to farm before (spiked, etc).
        public static bool INTELLIGENT_FARMING_ENABLED = false;
        // Following variables are part of intelligent farming:
        public static bool IGNORE_MORAL_IF_WALL_ZERO = true;
        public static bool SCOUT_SPIKED = false;
        public static bool RAM_SCOUTED_TROOPLESS_WALLS = false;
        public static bool ONLY_ATTACK_NO_WALL = true;
        // ------INTELLIGENT_FARMING FEATURES END HERE ------
        public static bool ONLY_FARM_BARBS = false;
        public static bool SCAVENGING_ENABLED = true;
        public static bool PUSH_NOTIFICATIONS_ENABLED = true;
        public static bool FARMING_ENABLED = true; // If set to false, won't farm at all, even if intelligent farming is enabled.
        public static bool SORT_FARMS_BY_DISTANCE = true; // False -> sort by attack time.
        // UNIT SETTINGS
        public static int LC_PER_BARB_ATTACK = 5;
        public static int LC_PER_PLAYER_ATTACK = 10;
    }
}