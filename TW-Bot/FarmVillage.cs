using System;
using System.Collections.Generic;

namespace TW_Bot
{
    public class FarmVillage
    {
        public int x, y;
        public bool isBarb;
        public int wall;
        public int clay, wood, iron;
        public double minimumAttackIntervalInMinutes;
        public bool scoutOnTheWay;
        public DateTime scoutTime;
        public DateTime lastSentAttackTime;
        public DateTime lastScoutReportTime; // Deprecated
        public DateTime lastAttackETA;
        public DateTime lastCFarmETA;
        static Dictionary<int, int> production;
        public int lastReadReportId; // Replacement for lastScoutReportTime

        private FarmVillage()
        {
            lastSentAttackTime = DateTime.Now.AddYears(-1);
            lastScoutReportTime = DateTime.Now.AddYears(-1);
            lastAttackETA = DateTime.Now.AddYears(-1);
            lastCFarmETA = DateTime.Now.AddYears(-1);
            lastReadReportId = -1;
        }

        static FarmVillage()
        {
            production = new Dictionary<int, int>();
            production.Add(1, 30);
            production.Add(2, 35);
            production.Add(3, 41);
            production.Add(4, 47);
            production.Add(5, 55);
            production.Add(6, 64);
            production.Add(7, 74);
            production.Add(8, 86);
            production.Add(9, 100);
            production.Add(10, 117);
            production.Add(11, 136);
            production.Add(12, 158);
            production.Add(13, 184);
            production.Add(14, 214);
            production.Add(15, 249);
            production.Add(16, 289);
            production.Add(17, 337);
            production.Add(18, 391);
            production.Add(19, 455);
            production.Add(20, 530);
            production.Add(21, 616);
            production.Add(22, 717);
            production.Add(23, 833);
            production.Add(24, 969);
            production.Add(25, 1127);
            production.Add(26, 1311);
            production.Add(27, 1525);
            production.Add(28, 1774);
            production.Add(29, 2063);
            production.Add(30, 2400);
        }

        public FarmVillage(int x, int y, bool isBarb = false, int wall = -1, int clay = -1, int wood = -1, int iron = -1, double minimumAttackIntervalInMinutes = 30)
        {
            this.x = x;
            this.y = y;
            this.isBarb = isBarb;
            this.wall = wall;
            this.clay = clay;
            this.wood = wood;
            this.iron = iron;
            scoutOnTheWay = false;
            lastSentAttackTime = DateTime.Now.AddYears(-1);
            lastScoutReportTime = DateTime.Now.AddYears(-1);
            lastAttackETA = DateTime.Now.AddYears(-1);
            lastCFarmETA = DateTime.Now.AddYears(-1);
            lastReadReportId = -1;
            this.minimumAttackIntervalInMinutes = minimumAttackIntervalInMinutes;
        }

        public void UpdateAttackInterval(int capacity)
        {
            if (clay <= 0 || wood <= 0 || iron <= 0) return;
            double resourcesProducedPerMinute = 0;
            resourcesProducedPerMinute += production[clay] / 60.0;
            resourcesProducedPerMinute += production[iron] / 60.0;
            resourcesProducedPerMinute += production[wood] / 60.0;
            minimumAttackIntervalInMinutes = (double)capacity / (double)resourcesProducedPerMinute;
        }

        public string GetCoords()
        {
            return x + "|" + y;
        }
    }
}