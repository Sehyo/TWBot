namespace TW_Bot
{
    public class Troops // Class for troop counts.
    {
        public int spears = 0, swords = 0, axes = 0, archers = 0;
        public int cats = 0, rams = 0;
        public int scouts = 0, lc = 0, hc = 0, ma = 0;
        public int nobles = 0;

        public int GetCapacity()
        {
            int capacity = 0;
            capacity += spears * 25;
            capacity += swords * 15;
            capacity += axes * 10;
            capacity += archers * 10;
            capacity += lc * 80;
            capacity += hc * 50;
            capacity += ma * 50;
            return capacity;
        }

        public int GetSlowestSpeed()
        {
            if (nobles > 0) return 35;
            if (rams > 0 || cats > 0) return 30;
            if (swords > 0) return 22;
            if (spears > 0 || archers > 0 || axes > 0) return 18;
            if (hc > 0) return 11;
            if (lc > 0 || ma > 0) return 10;
            if (scouts > 0) return 9;
            // If for some reason we come here..Probably not possible xD
            System.Console.WriteLine("POTENTIAL ERROR IN GET SLOWEST SPEED FUNC");
            System.Console.ReadLine();
            return 0;
        }

        public bool DoWeHaveTheseTroops(Troops otherTroops)
        {
            return spears >= otherTroops.spears && swords >= otherTroops.swords && axes >= otherTroops.axes && archers >= otherTroops.archers && cats >= otherTroops.cats && rams >= otherTroops.rams && scouts >= otherTroops.scouts && lc >= otherTroops.lc && hc >= otherTroops.hc && ma >= otherTroops.ma && nobles >= otherTroops.nobles;
        }
    }
}