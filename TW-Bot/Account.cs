using System.Collections.Generic;
using WatiN.Core;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.IO;
using System;
using System.Numerics;

namespace TW_Bot
{
    public class Account
    {
        public List<Village> villages;
        public IE browser;
        public string username;
        string password;
        bool isLoggedIn;
        public string world;

        public Account(string username, string password)
        {
            this.username = username;
            this.password = password;
            this.isLoggedIn = false;
            world = Settings.WORLD;
            Settings.USERNAME = username;
            if(Settings.USE_SERVER) this.villages = ReadFromXmlFile<List<Village>>(Settings.USERNAME + "/" + Settings.WORLD + "/" + "SERVER_villages.xml");
            else this.villages = ReadFromXmlFile<List<Village>>(Settings.USERNAME + "/" + Settings.WORLD + "/" + "villages.xml");
            // Workaround to reduce unnecessary data duplication. Requires write XML to set these to NULL before writing aswell except for first entry.
            for (int i = 1; i < villages.Count; i++) villages[i].farmVillages = villages[0].farmVillages;
                
            
            //villages[0].GetTroops();
            //System.Console.WriteLine("Finding best radius..({0}|{1})", villages[0].x, villages[0].y);
            //System.Console.WriteLine("How many lc you got?");
            //int lcs = Int32.Parse(System.Console.ReadLine());
            //Settings.FARM_RADIUS = SimulateOptimalFarmRadius(lcs, villages[0].x, villages[0].y, villages[0].farmVillages);
            //System.Console.WriteLine("Set farm radius to {0}.", Settings.FARM_RADIUS);
            // Shuffle farm villages ?
        }

        public Account(List<Village> villages, string username, string password)
        {
            this.villages = villages;
            this.username = username;
            this.password = password;
            this.isLoggedIn = false;
        }

        public static int SimulateOptimalLCCountForFarmAssistantFarming(List<Village> villages, List<FarmVillage> farmVillages)
        {
            System.Console.WriteLine("Starting optimal LC count simulation.");
            // Update total lc for each village first.
            System.Console.WriteLine("Checking total LC count for all vilages for simulation purposes.");
            foreach(Village village in villages)
            {
                village.GetTotalTroops();
                village.troops.lc = village.totalTroops.lc;
            }

            // Make copy of farm villages list.
            List<FarmVillage> farmCopy = new List<FarmVillage>();//new List<FarmVillage>(farmVillages);
            foreach (FarmVillage vill in farmVillages)
            {
                FarmVillage newVill = new FarmVillage(vill.x, vill.y, vill.isBarb, vill.wall, vill.clay, vill.wood, vill.iron, vill.minimumAttackIntervalInMinutes);
                farmCopy.Add(newVill);
            }



            int originalCount = Settings.LC_PER_BARB_ATTACK;
            int highestTotalHaul = 0;
            int optimalCount = 0;

            for (int iteration = 2; iteration < 50; iteration++)
            {
                DateTime currentTime = DateTime.Now;
                DateTime finishTime = DateTime.Now.AddDays(1);
                Settings.LC_PER_BARB_ATTACK = iteration;

                for (int i = 0; i < farmCopy.Count; i++)
                {
                    farmCopy[i].lastSentAttackTime = DateTime.Now.AddDays(-10);
                    farmCopy[i].UpdateAttackInterval(Settings.LC_PER_BARB_ATTACK * 80);
                    farmCopy[i].lastAttackETA = DateTime.Now.AddDays(-10);
                }

                List<Tuple<DateTime, Village>> returnsOfAttacks = new List<Tuple<DateTime, Village>>();
                int totalHaul = 0;

                while (currentTime < finishTime)
                {
                    foreach (Village village in villages)
                    {
                        // Sort farm villages by distance to current village.
                        Vector2 ourPos = new Vector2(village.x, village.y);
                        farmCopy.Sort((a, b) =>
                        {
                            Vector2 aPos = new Vector2(a.x, a.y);
                            Vector2 bPos = new Vector2(b.x, b.y);
                            return Vector2.Distance(ourPos, aPos).CompareTo(Vector2.Distance(ourPos, bPos));
                        });

                        // Perform simulated attacks.
                        foreach (FarmVillage farm in farmCopy)
                        {
                            // Attacks take like 1 second each so...
                            currentTime = currentTime.AddSeconds(1);
                            if (village.troops.lc < Settings.LC_PER_BARB_ATTACK) continue;
                            if (currentTime < farm.lastAttackETA.AddMinutes(farm.minimumAttackIntervalInMinutes)) continue;
                            float fields = Vector2.Distance(ourPos, new Vector2(farm.x, farm.y));
                            float travelTime = fields * 10;
                            // Time to attack.
                            village.troops.lc -= Settings.LC_PER_BARB_ATTACK;
                            farm.lastAttackETA = currentTime.AddMinutes(travelTime);
                            farm.lastSentAttackTime = currentTime;
                            DateTime returnTime = currentTime.AddMinutes(travelTime * 2);
                            // Below line is to know when to add haul and more lc back to the village.
                            returnsOfAttacks.Add(new Tuple<DateTime, Village>(returnTime, village));
                        }
                        // Simulate next village load time
                        currentTime = currentTime.AddSeconds(4);
                    }

                    // Simulate Sleep
                    currentTime.AddMinutes(15);

                    // Check if we have LCs back home and add to total haul and current lc count.
                    for (int i = returnsOfAttacks.Count - 1; i >= 0; i--)
                    {
                        if (returnsOfAttacks[i].Item1 > currentTime) continue;
                        totalHaul += Settings.LC_PER_BARB_ATTACK * 80;
                        returnsOfAttacks[i].Item2.troops.lc += Settings.LC_PER_BARB_ATTACK;
                        returnsOfAttacks.RemoveAt(i);
                    }
                }

                if(totalHaul > highestTotalHaul)
                {
                    highestTotalHaul = totalHaul;
                    optimalCount = Settings.LC_PER_BARB_ATTACK;
                }

                System.Console.WriteLine("With {0} lc per attack we can get maximum haul of: {1}.", Settings.LC_PER_BARB_ATTACK, totalHaul);
            }

            System.Console.WriteLine("Simulation finished. Simulated optimal count is {0} with maximum haul of {1} a day.", optimalCount, highestTotalHaul);
            Settings.LC_PER_BARB_ATTACK = originalCount;
            System.Console.ReadLine();
            return 0;
        }

        public static int SimulateOptimalFarmRadius(int lcCount, int x, int y, List<FarmVillage> farmVillages)
        {
            int optimalRadius = 0;
            int maximumHaul = 0;
            for (int radius = 0; radius < 30; radius++) // Test these radii.
            {
                // Simulation Variables.
                int currentLc = lcCount;
                int minimumSleepTime = 5;
                int maximumSleepTime = 15;
                int lcPerAttack = 10;
                int simulationDurationInMinutes = 60 * 24;
                int totalHaul = 0;
                int lcLeft = 0;
                DateTime currentTime = DateTime.Now;
                DateTime startTime = DateTime.Now;
                List<DateTime> returnsOfAttacks = new List<DateTime>();
                // Make copy of farm villages list.
                List<FarmVillage> farmCopy = new List<FarmVillage>();//new List<FarmVillage>(farmVillages);
                foreach(FarmVillage vill in farmVillages)
                {
                    FarmVillage newVill = new FarmVillage(vill.x, vill.y, vill.isBarb, vill.wall, vill.clay, vill.wood, vill.iron, vill.minimumAttackIntervalInMinutes);
                    farmCopy.Add(newVill);
                }
                for (int i = 0; i < farmCopy.Count; i++)
                {
                    farmCopy[i].lastSentAttackTime = DateTime.Now.AddDays(-1);
                    farmCopy[i].UpdateAttackInterval(lcPerAttack * 80);
                }
                // Sort farms by distance.
                Vector2 ourPos = new Vector2(x, y);
                farmCopy.Sort((a, b) =>
                {
                    Vector2 aPos = new Vector2(a.x, a.y);
                    Vector2 bPos = new Vector2(b.x, b.y);
                    return Vector2.Distance(ourPos, aPos).CompareTo(Vector2.Distance(ourPos, bPos));
                });
                while (currentTime < startTime.AddMinutes(simulationDurationInMinutes))
                {
                    
                    if(lcCount < lcPerAttack)
                    {
                        // Simulate Sleep.
                        System.Console.WriteLine("Simulating Sleep (1)");
                        currentTime = currentTime.AddMinutes((maximumSleepTime - minimumSleepTime) / 2);
                        continue;
                    }
                    // Check if we have LCs back home and add to total haul and current lc count.
                    for (int i = returnsOfAttacks.Count - 1; i >= 0; i--)
                    {
                        if (returnsOfAttacks[i] > currentTime) continue;
                        totalHaul += lcPerAttack * 80;
                        currentLc += lcPerAttack;
                        returnsOfAttacks.RemoveAt(i);
                    }
                    // Send out LCs.
                    for (int i = 0; i < farmCopy.Count; i++)
                    {
                        if (currentLc < lcPerAttack) continue;
                        if (farmCopy[i].lastSentAttackTime.AddMinutes(farmCopy[i].minimumAttackIntervalInMinutes) > currentTime) continue;
                        float fields = Vector2.Distance(ourPos, new Vector2(farmCopy[i].x, farmCopy[i].y));
                        if (fields > radius) continue;
                        // Time to attack.
                        currentLc -= lcPerAttack;
                        farmCopy[i].lastSentAttackTime = currentTime;
                        // Calculate distance to village.
                        float totalFields = fields * 2;
                        float totalDurationInMinutes = totalFields * 10;
                        DateTime returnTime = currentTime.AddMinutes(totalDurationInMinutes);
                        returnsOfAttacks.Add(returnTime);
                    }
                    //System.Console.WriteLine("Simulating Sleep (2)");
                    // Simulate Sleep.
                    currentTime = currentTime.AddMinutes((maximumSleepTime - minimumSleepTime) / 2);
                }
                if(totalHaul > maximumHaul)
                {
                    optimalRadius = radius;
                    maximumHaul = totalHaul;
                    System.Console.WriteLine("Found better radius of {0} with max loot of {1}, with {2} lc left", radius, totalHaul, currentLc);
                    //System.Console.ReadLine();
                }
            }
            System.Console.WriteLine("Optimal Radius is: {0} with {1} max loot.", optimalRadius, maximumHaul);
            //System.Console.ReadLine();
            return optimalRadius;
        }

        public void login()
        {
            browser = new IE();
            browser.ClearCache();
            browser.ClearCookies();
            Utils.GoTo(browser, "http://www.tribalwars.net/sv-se/page/logout");
            Utils.GoTo(browser, "http://www.tribalwars.net");
            //browser.Body.Focus();
            browser.TextField(Find.ById("user")).TypeText(username);
            browser.TextField(Find.ById("password")).TypeText(password);
            System.Console.WriteLine("Clicking login.");
            browser.Link(Find.ByClass("btn-login")).Click();
            System.Console.WriteLine("Waiting");
            Div wContainer = browser.Div(Find.ByClass("worlds-container"));
            wContainer.WaitUntilExists();
            System.Console.WriteLine("Going to world");
            Utils.GoTo(browser, "http://www.tribalwars.net/sv-se/page/play/" + world);
            System.Console.WriteLine("Logged in");
            while (browser.Html == null) System.Threading.Thread.Sleep(100);
            Utils.CheckBotProtection(browser.Html);
            isLoggedIn = true;
            // Give browser object to villages.
            foreach(Village village in villages)
            {
                village.SetBrowser(ref browser);
                village.SetWorld(world);
            }
        }

        public void logout()
        {
            browser.GoTo("http://www.tribalwars.net/sv-se/page/logout");
            browser.Close();
            isLoggedIn = false;
            for (int i = 1; i < villages.Count; i++)
            {
                villages[i].farmVillages = null;
                villages[i].spikedVillages = null;
            }
            // Save the states.
            WriteToXmlFile<List<Village>>(username + "/" + world + "/villages.xml", villages);
            Client.UploadAll();
            for (int i = 1; i < villages.Count; i++)
            {
                villages[i].farmVillages = villages[0].farmVillages;
                villages[i].spikedVillages = villages[0].spikedVillages;
            }
        }

        public int AddAllVillages() // Adds all villages and returns # of villages.
        {
            // public List<Village> villages;
            List<Village> autoAddedVillages = new List<Village>();
            // We don't have to worry about not owning village[0] cos even invalid IDs are fine for this.
            string overviewScreenUrl = "https://" + this.world + ".tribalwars.net/game.php?village=" + this.villages[0].villageId + "&screen=overview_villages&mode=combined&group=0";
            //&screen=overview_villages&page=-1&order=ram&dir=desc&mode=combined&group=0
            if (Settings.FAKE_SCRIPT) overviewScreenUrl = "https://" + this.world + ".tribalwars.net/game.php?village=" + this.villages[0].villageId + "&screen=overview_villages&page=-1&order=ram&dir=desc&mode=combined&group=0";
            Utils.GoTo(browser, overviewScreenUrl);
            Utils.CheckBotProtection(browser.Html);
            try
            {
                TextField textField = browser.TextField(Find.ByName("page_size"));
                textField.Value = "1000";
                browser.Button(Find.ByClass("btn")).Click();
                Utils.CheckBotProtection(browser.Html);
            }
            catch(Exception e)
            {

            }

            if(Settings.FAKE_SCRIPT)
            {
                Utils.GoTo(browser, overviewScreenUrl);
                Utils.CheckBotProtection(browser.Html);
            }

            SpanCollection spans = browser.Spans.Filter(Find.ByClass("quickedit-label"));

            for(int i = 0; i < spans.Count; i++)
            {
                string idString = ((Link)(spans[i].Parent)).Url;
                idString = idString.Substring((idString.LastIndexOf("game.php") + 8));
                System.Console.WriteLine(idString);
                int villageId = int.Parse(Regex.Replace(idString, "[^0-9]", ""));
                
                string coordinateString = spans[i].InnerHtml;
                var match = Regex.Match(coordinateString, @"(\d+\|\d+)");
                int x = -1, y = -1;
                try
                {
                    string[] coords = match.ToString().Split('|');
                    x = int.Parse(coords[0]);
                    y = int.Parse(coords[1]);
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }
                System.Console.WriteLine("Adding: {0} - {1}|{2}", villageId, x, y);
                autoAddedVillages.Add(new Village(villageId, x, y));
            }

            foreach (Village village in autoAddedVillages)
            {
                village.SetBrowser(ref browser);
                village.SetWorld(world);
                village.farmVillages = villages[0].farmVillages;
            }

            villages = autoAddedVillages;

            System.Console.WriteLine("Auto added all villages ({0} villas.)", villages.Count);
            return villages.Count;
        }

        public void FarmWithAllVillages()
        {
            Settings.PROGRESS = 0.0;
            int villasProcessed = 0;
            if (Settings.SCAVENGING_ENABLED)
                foreach (Village village in villages)
                    try
                    {
                        village.Scavenge();
                        villasProcessed++;
                        double scavdPercentage = villasProcessed / villages.Count;
                        Settings.PROGRESS = scavdPercentage * 0.4; // Let's say 100% of scav is 40 % of bot run.
                        //Settings.PROGRESS = 0.0;
                    }
                    catch(Exception e)
                    {
                        System.Console.WriteLine("Skipping Current Villas Scav, error:");
                        System.Console.WriteLine(e.Message);
                    }

            villasProcessed = 0;
            foreach (Village village in villages)
            {
                if (Settings.INTELLIGENT_FARMING_ENABLED && !Settings.FA_FARMING_ENABLED)
                {
                    System.Console.WriteLine("VILLAGE {0} REMOVING SPIKES", village.x + " | " + village.y);
                    village.RemoveSpikedFarms();
                    System.Console.WriteLine("VILLAGE {0} CHECKING REPORTS", village.x + " | " + village.y);
                    village.CheckScoutReports();
                    if (Settings.SCOUT_SPIKED)
                    {
                        System.Console.WriteLine("VILLAGE {0} SCOUTING SPIKED", village.x + " | " + village.y);
                        village.ScoutSpiked();
                    }
                }
                //System.Console.WriteLine("VILLAGE {0} UPDATING ATTACKINTERVALS", village.x + " | " + village.y);
                foreach (FarmVillage farm in village.farmVillages)
                {
                    int capacity = (farm.isBarb ? Settings.LC_PER_BARB_ATTACK : Settings.LC_PER_PLAYER_ATTACK) * 80;
                    farm.UpdateAttackInterval(capacity);
                    if (farm.minimumAttackIntervalInMinutes <= 0) farm.minimumAttackIntervalInMinutes = 30;
                }
                if (Settings.FA_FARMING_ENABLED) village.FAFarm();
                else if (Settings.FARMING_ENABLED)
                {
                    System.Console.WriteLine("VILLAGE {0} FARMING", village.x + " | " + village.y);
                    village.Farm(Settings.ONLY_FARM_BARBS);
                }
                try
                {
                    village.CheckScoutReports();
                }
                catch
                {

                }
                Settings.PROGRESS += (1.0 / villages.Count) * 0.6;
            }
            Settings.PROGRESS = 1;
        }

        // http://blog.danskingdom.com/saving-and-loading-a-c-objects-data-to-an-xml-json-or-binary-file/

        /// <summary>
        /// Writes the given object instance to an XML file.
        /// <para>Only Public properties and variables will be written to the file. These can be any type though, even other classes.</para>
        /// <para>If there are public properties/variables that you do not want written to the file, decorate them with the [XmlIgnore] attribute.</para>
        /// <para>Object type must have a parameterless constructor.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToXmlFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                writer = new StreamWriter(filePath, append);
                serializer.Serialize(writer, objectToWrite);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        /// <summary>
        /// Reads an object instance from an XML file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// </summary>
        /// <typeparam name="T">The type of object to read from the file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the XML file.</returns>
        public static T ReadFromXmlFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                reader = new StreamReader(filePath);
                return (T)serializer.Deserialize(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
    }
}