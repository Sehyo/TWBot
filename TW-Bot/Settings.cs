using System.Collections.Generic;
using System;

namespace TW_Bot
{
    public static class Settings
    {
        public static string USERNAME = "";
        public static string PASSWORD = "";
        public static string WORLD = "";
        public static bool TAKE_CONTROL_ON_START = true;
        public static int MIN_MORAL = 80;
        public static int FARM_RADIUS = 20;
        public static int REPORT_READ_INTERVAL_MINUTES = 6000; //* 24 * 2; // How often to read reports in minutes (minimum)
        // Requires premium, because of report filtering. Will check new reports, send rams to troop less villages with walls > 0. Will not farm villages that were unsuccessful to farm before (spiked, etc).
        public static bool FA_FARMING_ENABLED = false;
        public static bool INTELLIGENT_FARMING_ENABLED = false;
        // Following variables are part of intelligent farming:
        public static bool IGNORE_MORAL_IF_WALL_ZERO = true;
        public static bool SCOUT_SPIKED = false;
        public static bool RAM_SCOUTED_TROOPLESS_WALLS = true;
        public static bool ONLY_ATTACK_NO_WALL = true;
        // ------INTELLIGENT_FARMING FEATURES END HERE ------
        public static bool ONLY_FARM_BARBS = true;
        public static bool SCAVENGING_ENABLED = true;
        public static bool PUSH_NOTIFICATIONS_ENABLED = true;
        public static bool FARMING_ENABLED = false; // If set to false, won't farm at all, even if intelligent farming is enabled.
        public static bool SORT_FARMS_BY_DISTANCE = true; // False -> sort by attack time.
        // UNIT SETTINGS
        public static int LC_PER_BARB_ATTACK = 10;
        public static int LC_PER_PLAYER_ATTACK = 10;
        // GLOBAL FARM VILLAGE VARIABLE
        public static List<FarmVillage> FARM_VILLAGES;
        // GLOBAL REPORT TIME VALUE
        public static DateTime? LastTimeWeReadReports = null;

        public static bool NEXT_RESTART_IS_IMMEDIATE = false;

        public static double PROGRESS = 0.0;
        public static string STATUS = "";
    }
}