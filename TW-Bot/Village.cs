using System.Collections.Generic;
using WatiN.Core;
using System.Text.RegularExpressions;
using System;
using System.Windows.Forms;
using System.Numerics;

namespace TW_Bot
{
    public class Village
    {
        public int x, y;
        public int villageId;
        public string world;
        IE browser;
        public Troops troops; // Troops of this village.
        public Buildings buildings;
        public List<FarmVillage> farmVillages;
        public List<FarmVillage> spikedVillages;
        public DateTime? SoonestScavengeCompletionTime = null;

        private Village()
        {
            this.farmVillages = new List<FarmVillage>();
            this.spikedVillages = new List<FarmVillage>();
            this.troops = new Troops();
            this.villageId = -1;
        }

        public void addBarbFarmVillagesFromFile(string path)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(@path);
            string line;
            int amount = 0;
            while ((line = file.ReadLine()) != null)
            {
                string[] coords = line.Split('|');
                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);
                System.Console.WriteLine("Checking if villa {0}|{1} is a duplicate..", x, y);
                // Skip this village if it already exists.
                bool exists = false;
                foreach (FarmVillage village in farmVillages)
                    if (village.x == x && village.y == y)
                        exists = true;
                if (exists) continue;
                foreach (FarmVillage village in spikedVillages)
                    if (village.x == x && village.y == y)
                        exists = true;
                if (exists) continue;
                System.Console.WriteLine("Not a duplicate! Adding it!");
                // We don't have this village entered already.
                amount++;
                farmVillages.Add(new FarmVillage(x, y, true));
            }
            System.Console.WriteLine("Added {0} barbs!", amount);
        }

        public Village SortFarmsByAttackTime()
        {
            farmVillages.Sort((x, y) => DateTime.Compare(x.lastSentAttackTime, y.lastSentAttackTime));
            return this;
        }

        public Village SortFarmsByDistance()
        {
            Vector2 ourPos = new Vector2(x, y);
            farmVillages.Sort((a, b) =>
            {
                Vector2 aPos = new Vector2(a.x, a.y);
                Vector2 bPos = new Vector2(b.x, b.y);
                return Vector2.Distance(ourPos, aPos).CompareTo(Vector2.Distance(ourPos, bPos));
            });
            return this;
        }

        public Village(List<FarmVillage> farmVillages, int villageId)
        {
            this.farmVillages = farmVillages;
            this.spikedVillages = new List<FarmVillage>();
            this.villageId = villageId;
            this.troops = new Troops();
        }

        public void SetBrowser(ref IE browser)
        {
            this.browser = browser;
        }

        public void SetWorld(string world)
        {
            this.world = world;
        }

        public void RemoveSpikedFarms()
        {
            System.Console.WriteLine("Starting removal of spiked villages.");
            browser.GoTo("https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=report");
            Utils.CheckBotProtection(browser.Html);
            browser.RadioButton(Find.ById("filter_dots_red")).Click();
            Utils.CheckBotProtection(browser.Html);
            if (browser.CheckBox(Find.ByValue("64")).Checked) // Make sure only scouts are not selected
            {
                browser.CheckBox(Find.ByValue("64")).Click();
                Utils.CheckBotProtection(browser.Html);
            }
            if (browser.CheckBox(Find.ByValue("2")).Checked) // Make sure nobles are not selected
            {
                browser.CheckBox(Find.ByValue("2")).Click();
                Utils.CheckBotProtection(browser.Html);
            }

            RadioButtonCollection radioButtons = browser.RadioButtons;
            for(int i = 0; i < radioButtons.Count; i++)
            {
                if(radioButtons[i].Name.Equals("filter_attack_type") && radioButtons[i].GetAttributeValue("value").Equals("0"))
                {
                    if(!radioButtons[i].Checked)
                    {
                        radioButtons[i].Click(); // All attack types
                        Utils.CheckBotProtection(browser.Html);
                        break;
                    }
                }
            }

            SpanCollection spikeReports = browser.Spans.Filter(Find.ByClass("quickedit-label"));

            foreach (Span report in spikeReports)
            {
                var match = Regex.Match(report.Text, @"\d+|\d+");
                int x = int.Parse(match.ToString());
                int y = int.Parse(match.NextMatch().ToString());
                for (int i = farmVillages.Count - 1; i >= 0; i--)
                {
                    if (farmVillages[i].x == x && farmVillages[i].y == y)
                    {
                        System.Console.WriteLine("Removing village: ({0}|{1})", x, y);
                        spikedVillages.Add(farmVillages[i]);
                        farmVillages.RemoveAt(i);
                    }
                }
            }

            browser.RadioButton(Find.ById("filter_dots_yellow")).Click();
            Utils.CheckBotProtection(browser.Html);
            SpanCollection partialLosses = browser.Spans.Filter(Find.ByClass("quickedit-label"));

            foreach (Span report in partialLosses)
            {
                var match = Regex.Match(report.Text, @"\d+|\d+");
                int x = int.Parse(match.ToString());
                int y = int.Parse(match.NextMatch().ToString());
                for (int i = farmVillages.Count - 1; i >= 0; i--)
                {
                    if (farmVillages[i].x == x && farmVillages[i].y == y)
                    {
                        System.Console.WriteLine("Removing village: ({0}|{1})", x, y);
                        spikedVillages.Add(farmVillages[i]);
                        farmVillages.RemoveAt(i);
                    }
                }
            }

            System.Console.WriteLine("Spiked villages removed.");
        }
        
        public Village Scavenge()
        {
            System.Console.WriteLine("Scavenge Function Started");
            GetTroops();
            if (troops.spears < 10) return this;
            Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=place&mode=scavenge");
            Utils.CheckBotProtection(browser.Html);
            Div optionsDiv = browser.Div(Find.ByClass("options-container"));
            while (optionsDiv.Children().Count == 0)
            {
                System.Threading.Thread.Sleep(100);
            }
            Div optionOneDiv = (Div)optionsDiv.Children()[0];
            Div optionTwoDiv = (Div)optionsDiv.Children()[1];
            Div optionThreeDiv = (Div)optionsDiv.Children()[2];
            Div optionFourDiv = (Div)optionsDiv.Children()[3];

            List<Div> optionDivs = new List<Div>();
            optionDivs.Add(optionOneDiv);
            optionDivs.Add(optionTwoDiv);
            optionDivs.Add(optionThreeDiv);
            optionDivs.Add(optionFourDiv);

            // We need to check children count of status-specific divs first element.

            int optionOneViewIndex = 2, optionTwoViewIndex = 2, optionThreeViewIndex = 2, optionFourViewIndex = 2;
            if (((Div)((Div)optionOneDiv.Children()[2]).Children()[0]).Children().Count == 2) optionOneViewIndex = 1;
            if (((Div)((Div)optionTwoDiv.Children()[2]).Children()[0]).Children().Count == 2) optionTwoViewIndex = 1;
            if (((Div)((Div)optionThreeDiv.Children()[2]).Children()[0]).Children().Count == 2) optionThreeViewIndex = 1;
            if (((Div)((Div)optionFourDiv.Children()[2]).Children()[0]).Children().Count == 2) optionFourViewIndex = 1;
            //((Link)(((Div)(((Div)(((Div)(optionOneDiv.Children()[2])).Children()[0])).Children()[2])).Children()[0]));

            Link optionOneStart = ((Link)(((Div)(((Div)(((Div)(optionOneDiv.Children()[2])).Children()[0])).Children()[optionOneViewIndex])).Children()[0]));
            Link optionTwoStart = ((Link)(((Div)(((Div)(((Div)(optionTwoDiv.Children()[2])).Children()[0])).Children()[optionTwoViewIndex])).Children()[0]));
            Link optionThreeStart = ((Link)(((Div)(((Div)(((Div)(optionThreeDiv.Children()[2])).Children()[0])).Children()[optionThreeViewIndex])).Children()[0]));
            Link optionFourStart = ((Link)(((Div)(((Div)(((Div)(optionFourDiv.Children()[2])).Children()[0])).Children()[optionFourViewIndex])).Children()[0]));

            List<Link> options = new List<Link>();

            if (!((Div)(optionOneDiv.Children()[0])).OuterHtml.Contains("gray") && !optionOneStart.ClassName.Contains("disabled"))
                options.Add(optionOneStart);
            if (!((Div)(optionTwoDiv.Children()[0])).OuterHtml.Contains("gray") && !optionTwoStart.ClassName.Contains("disabled"))
                options.Add(optionTwoStart);
            if (!((Div)(optionThreeDiv.Children()[0])).OuterHtml.Contains("gray") && !optionThreeStart.ClassName.Contains("disabled"))
                options.Add(optionThreeStart);
            if (!((Div)(optionFourDiv.Children()[0])).OuterHtml.Contains("gray") && !optionFourStart.ClassName.Contains("disabled"))
                options.Add(optionFourStart);

            for(int i = options.Count - 1; i >= 0; i--)
            {
                int spearCount = troops.spears;
                if (spearCount > 200) spearCount = 200;
                if (spearCount < 10) return this;
                System.Console.WriteLine("Scavenge - current option index: {0}", i);
                TextField spearText = browser.TextField(Find.ByName("spear"));
                //spearText.Value = "200"; // Doesn't work. Text disappears.
                //spearText.TypeText("200"); // Doesn't work. Text disappears.
                // Work Around:

                browser.Body.Focus();
                spearText.Focus();
                SendKeys.SendWait(spearCount.ToString()); // spearText.Value and .TypeText here doesn't work. Input gets removed. Wtf?
                System.Console.WriteLine("Entered Spear Count.");
                options[i].Click();
                // Wait for the countdown to appear.
                Span cd = optionDivs[i].Span(Find.ByClass("return-countdown"));
                System.Console.WriteLine("Waiting for count down to exist.");
                cd.WaitUntilExists();
                System.Console.WriteLine("Exists!");
                Utils.CheckBotProtection(browser.Html);
                troops.spears -= spearCount;
                if (troops.spears < 10) break;
            }
            System.Console.WriteLine("Did all scavenging!");

            // Check remaining scavenging times and save shortest one.
            // return-countdown
            SpanCollection countdowns = browser.Spans.Filter(Find.ByClass("return-countdown"));
            DateTime? soonest = null;
            for(int i = 0; i < countdowns.Count; i++)
            {
                string timeString = countdowns[i].InnerHtml;
                string[] splitTime = timeString.Split(':');
                int hoursRemaining = int.Parse(splitTime[0]);
                int minutesRemaining = int.Parse(splitTime[1]);
                int secondsRemaining = int.Parse(splitTime[2]);
                DateTime currentTime = DateTime.Now;
                currentTime = currentTime.AddHours(hoursRemaining);
                currentTime = currentTime.AddMinutes(minutesRemaining);
                currentTime = currentTime.AddSeconds(secondsRemaining);
                if (soonest == null) soonest = currentTime;
                else if (currentTime < soonest) soonest = currentTime;
            }
            SoonestScavengeCompletionTime = soonest;
            return this;
        }

        public DateTime GetServerTime()
        {
            string timeString = browser.Span(Find.ById("serverTime")).InnerHtml;
            string dateString = browser.Span(Find.ById("serverDate")).InnerHtml;
            string[] timeInfo = timeString.Split(':');
            string[] dateInfo = dateString.Split('/');
            // format of timeString: hh:mm:ss
            // format of dateString: DD/MM/YYYY
            return new DateTime(int.Parse(dateInfo[2]), int.Parse(dateInfo[1]), int.Parse(dateInfo[0]), int.Parse(timeInfo[0]), int.Parse(timeInfo[1]), int.Parse(timeInfo[2]));
        }

        public void GetBuildings()
        {
            browser.GoTo("https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=main");
            Utils.CheckBotProtection(browser.Html);
            Regex digitsRegex = new Regex(@"[^\d]");
            buildings.headquarters = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_main"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.barracks = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_barracks"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.stables = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_stable"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.workshop = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_garage"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.watchtower = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_watchtower"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.smithy = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_smith"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.place = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_place"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.market = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_market"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.lumbermill = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_wood"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.claypit = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_stone"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.ironcave = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_iron"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.farm = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_farm"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.warehouse = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_storage"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.hidingplace = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_hide"))).Children()[0])).Children()[3]).InnerHtml, ""));
            buildings.wall = int.Parse(digitsRegex.Replace(((Span)((TableCell)((browser.TableRow(Find.ById("main_buildrow_wall"))).Children()[0])).Children()[3]).InnerHtml, ""));
        }

        public void GetTroops()
        {// https://en105.tribalwars.net/game.php?village=20012&screen=place&mode=command
            if (!browser.Url.Contains("command"))
                Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=place&mode=command");
            Utils.CheckBotProtection(browser.Html);
            // Now we are at the place where we can send attacks and shit.
            // Fetch our troop counts.
            troops.spears = int.Parse(browser.Link(Find.ById("units_entry_all_spear")).Text.Substring(1).TrimEnd(')'));
            troops.swords = int.Parse(browser.Link(Find.ById("units_entry_all_sword")).Text.Substring(1).TrimEnd(')'));
            troops.axes = int.Parse(browser.Link(Find.ById("units_entry_all_axe")).Text.Substring(1).TrimEnd(')'));
            troops.archers = int.Parse(browser.Link(Find.ById("units_entry_all_archer")).Text.Substring(1).TrimEnd(')'));
            troops.scouts = int.Parse(browser.Link(Find.ById("units_entry_all_spy")).Text.Substring(1).TrimEnd(')'));
            troops.lc = int.Parse(browser.Link(Find.ById("units_entry_all_light")).Text.Substring(1).TrimEnd(')'));
            troops.ma = int.Parse(browser.Link(Find.ById("units_entry_all_marcher")).Text.Substring(1).TrimEnd(')'));
            troops.hc = int.Parse(browser.Link(Find.ById("units_entry_all_heavy")).Text.Substring(1).TrimEnd(')'));
            troops.rams = int.Parse(browser.Link(Find.ById("units_entry_all_ram")).Text.Substring(1).TrimEnd(')'));
            troops.cats = int.Parse(browser.Link(Find.ById("units_entry_all_catapult")).Text.Substring(1).TrimEnd(')'));
            troops.nobles = int.Parse(browser.Link(Find.ById("units_entry_all_snob")).Text.Substring(1).TrimEnd(')'));
            System.Console.WriteLine("Updated troop counts for village {0}", villageId);
        }

        public Village CheckScoutReports()
        {
            System.Console.WriteLine("Checking Scout Reports.");
            List<FarmVillage> villagesAwaitingScouts = new List<FarmVillage>();
            foreach(FarmVillage village in farmVillages)
            {
                if(village.scoutOnTheWay)
                {
                    villagesAwaitingScouts.Add(village);
                }
            }
            foreach (FarmVillage village in spikedVillages)
            {
                if (village.scoutOnTheWay)
                {
                    villagesAwaitingScouts.Add(village);
                }
            }

            System.Console.WriteLine("-----Villages Awaiting Scout Check-----");
            foreach (FarmVillage village in villagesAwaitingScouts) System.Console.WriteLine(village.GetCoords());
            System.Console.WriteLine("-----Ends Here-----");

            // Go to reports tab.
            browser.GoTo("https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=report");
            Utils.CheckBotProtection(browser.Html);
            browser.RadioButton(Find.ById("filter_dots_none")).Click();
            Utils.CheckBotProtection(browser.Html);
            if (!browser.CheckBox(Find.ByValue("64")).Checked) // View only stuff with scouts in.
            {
                browser.CheckBox(Find.ByValue("64")).Click();
                Utils.CheckBotProtection(browser.Html);
            }
            if (browser.CheckBox(Find.ByValue("2")).Checked) // Make sure nobles are not selected
            {
                browser.CheckBox(Find.ByValue("2")).Click();
                Utils.CheckBotProtection(browser.Html);
            }
            SpanCollection scoutReports = browser.Spans.Filter(Find.ByClass("quickedit-label"));
            System.Console.WriteLine("We have {0} reports.", scoutReports.Count);

            int abc = 0;
            int reportCount = scoutReports.Count;
            for(int reportNumber = 0; reportNumber < scoutReports.Count; reportNumber++)
            {
                ++abc;
                var match = Regex.Match(scoutReports[reportNumber].Text, @"\d+|\d+");
                int x = int.Parse(match.ToString());
                int y = int.Parse(match.NextMatch().ToString());
                System.Console.WriteLine("Current report is for village {0}|{1}", x, y);
                for (int i = villagesAwaitingScouts.Count - 1; i >= 0; i--)
                {
                    if (villagesAwaitingScouts[i].x == x && villagesAwaitingScouts[i].y == y)
                    {
                        System.Console.WriteLine("Current village is awaiting scout report! ({0}|{1}).", x, y);
                        System.Console.WriteLine("Scout Time is: {0}", villagesAwaitingScouts[i].scoutTime);
                        System.Console.WriteLine("Time of report is: {0}", GetDateOfReport(scoutReports[reportNumber]));
                        System.Console.WriteLine("scoutTime > dateOfReport -> {0}", villagesAwaitingScouts[i].scoutTime > GetDateOfReport(scoutReports[reportNumber]));
                        if (villagesAwaitingScouts[i].scoutTime > GetDateOfReport(scoutReports[reportNumber]))
                        {
                            System.Console.WriteLine("But the report is older than the scout attack.\nTherefor Skipping this report.");
                            continue;
                        }
                        else
                        {
                            System.Console.WriteLine("The report is newer than the scout attacK!");
                        }
                        // Only use reports from blue, green, and red/blue
                        TableCell reportCell = (TableCell)scoutReports[reportNumber].Parent.Parent.Parent.Parent;
                        Image successType = (Image)(reportCell.Children()[1]);
                        if (!(successType.Src.Contains("green") || successType.Src.Contains("blue") || successType.Src.Contains("red_blue")))
                        {
                            System.Console.WriteLine("Success Level of this report is not green, blue or red_blue. Skipping this report.");
                            continue;
                        }
                        // Open report page
                        System.Console.WriteLine("Opening Report.");
                        scoutReports[reportNumber].Click();
                        Utils.CheckBotProtection(browser.Html);
                        System.Console.WriteLine("Report opened.");
                        int wallLevel = -1;

                        Image wallImage = browser.Image(Find.BySrc("https://dsen.innogamescdn.com/8.153/40020/graphic/buildings/wall.png"));
                        if (wallImage.Exists)
                        {
                            wallLevel = int.Parse(wallImage.Parent.NextSibling.InnerHtml);
                        }
                        else wallLevel = 0;
                        // Save wall level.
                        System.Console.WriteLine("Changed wall level! Previous: {0}, new: {1}", villagesAwaitingScouts[i].wall, wallLevel);
                        villagesAwaitingScouts[i].wall = wallLevel;

                        // Check if village contains troops.
                        // If no troop, unit type is faded.
                        Table defInfo = browser.Table(Find.ById("attack_info_def"));
                        TableBody defInfoBody = (TableBody)(defInfo.Children()[0]); // 0 is out of range
                        TableCell unitInfo = (TableCell)(((TableRow)(defInfoBody.Children()[2])).Children()[0]);
                        Table unitTable = (Table)(unitInfo.Children()[0]);
                        TableBody innerUnitTableBody = (TableBody)unitTable.Children()[0];
                        TableRow centerUnitStuff = (TableRow)innerUnitTableBody.Children()[0];

                        // First element is not a unit.
                        bool unitsExists = false;
                        for (int z = 1; z < centerUnitStuff.Children().Count; z++)
                        {
                            TableCell currentUnitCell = (TableCell)centerUnitStuff.Children()[z];
                            Link innerLink = (Link)currentUnitCell.Children()[0];
                            Image innerImage = (Image)innerLink.Children()[0];
                            //System.Console.WriteLine("Image outer html is: {0}.\nImage inner html is: {1}.", innerImage.OuterHtml, innerImage.InnerHtml);
                            string fadeStatus = innerImage.ClassName;
                            if (fadeStatus.Equals("faded"))
                            {
                                //System.Console.WriteLine("Current unit type is faded!");
                                // Do nothing.
                            }
                            else
                            {
                                System.Console.WriteLine("Current unit type is _NOT_ faded!");
                                unitsExists = true; // There are units in this village.
                            }
                        }

                        //System.Console.WriteLine("Enemy units in this report? : {0}", unitsExists);

                        // If contains troops, make sure it keeps existing in only spikedVillages.
                        villagesAwaitingScouts[i].scoutOnTheWay = false;
                        
                        spikedVillages.Remove(villagesAwaitingScouts[i]);
                        farmVillages.Remove(villagesAwaitingScouts[i]);
                        browser.GoTo("https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=report"); // Go back to report overview.
                        Utils.CheckBotProtection(browser.Html);
                        scoutReports = browser.Spans.Filter(Find.ByClass("quickedit-label")); // Fix DOM
                        if(scoutReports.Count > reportCount)
                        {
                            // We received more reports while reading.
                            // Fix the problem.
                            reportNumber += scoutReports.Count - reportCount;
                            reportCount = scoutReports.Count;
                        }
                        if (unitsExists)
                        {
                            System.Console.WriteLine("Added village to spiked village list.");
                            spikedVillages.Add(villagesAwaitingScouts[i]);
                        }
                        else
                        {
                            //System.Console.WriteLine("Adding village to farm villages list as there are no troops in this report.");
                            farmVillages.Add(villagesAwaitingScouts[i]);
                        }
                        //System.Console.WriteLine("Removing village from Awaiting Scout List.");
                        villagesAwaitingScouts.Remove(villagesAwaitingScouts[i]);
                    }
                }
            }
            System.Console.WriteLine("We checked {0} reports.", abc);
            System.Console.WriteLine("Finished Checking Scout Reports.");
            return this;
        }

        public DateTime GetDateOfReport(Span report)
        {
            TableCell reportTime = (TableCell)report.Parent.Parent.Parent.Parent.NextSibling;
            // jan 09, 16:52
            string[] dateTimeInfo = reportTime.InnerHtml.Split(' ');
            int month = -1;
            if (dateTimeInfo[0].Equals("jan")) month = 1;
            if (dateTimeInfo[0].Equals("feb")) month = 2;
            if (dateTimeInfo[0].Equals("mar")) month = 3;
            if (dateTimeInfo[0].Equals("apr")) month = 4;
            if (dateTimeInfo[0].Equals("maj")) month = 5;
            if (dateTimeInfo[0].Equals("jun")) month = 6;
            if (dateTimeInfo[0].Equals("jul")) month = 7;
            if (dateTimeInfo[0].Equals("aug")) month = 8;
            if (dateTimeInfo[0].Equals("sep")) month = 9;
            if (dateTimeInfo[0].Equals("okt")) month = 10;
            if (dateTimeInfo[0].Equals("nov")) month = 11;
            if (dateTimeInfo[0].Equals("dec")) month = 12;

            dateTimeInfo[1] = dateTimeInfo[1].Remove(dateTimeInfo[1].Length - 1);

            int day = int.Parse(dateTimeInfo[1]);

            string[] time = dateTimeInfo[2].Split(':');

            int hour, minute;
            hour = int.Parse(time[0]);
            minute = int.Parse(time[1]);

            int year = DateTime.Now.Year;

            if (DateTime.Now.Month < month) --year;

            // public DateTime (int year, int month, int day, int hour, int minute, int second);
            DateTime reportDateTime = new DateTime(year, month, day, hour, minute, 0);

            return reportDateTime;
        }

        public Village ScoutSpiked()
        {
            System.Console.WriteLine("Scouting spiked villages.");
            GetTroops();
            foreach(FarmVillage village in spikedVillages)
            {
                if (troops.scouts <= 0) return this;
                if (village.scoutOnTheWay) continue;
                browser.TextField(Find.ByClass(p => p.Contains("target-input-field"))).TypeText(village.GetCoords());
                browser.TextField(Find.ById("unit_input_spy")).TypeText("1");
                --troops.scouts;
                browser.Button(Find.ById("target_attack")).Click();
                Utils.CheckBotProtection(browser.Html);
                browser.Button(Find.ById("troop_confirm_go")).Click();
                Utils.CheckBotProtection(browser.Html);
                village.scoutOnTheWay = true;
                village.scoutTime = GetServerTime();
                System.Console.WriteLine("Sending scout to village {0}.", village.GetCoords());
                System.Console.WriteLine("Server Time is: {0}", GetServerTime());
            }
            System.Console.WriteLine("Finished scouting of spiked villages.");
            return this;
        }

        // Execute the farms for this village.
        public Village Farm(bool onlyBarbs = true)
        {
            System.Console.WriteLine("Starting farming.");
            System.Console.WriteLine("Sorting Farms.");
            if (Settings.SORT_FARMS_BY_DISTANCE) SortFarmsByDistance();
            else SortFarmsByAttackTime();
            System.Console.WriteLine("Farms Sorted.");
            GetTroops(); // Update troop counts.
            System.Console.WriteLine("We have {0} farm villages.", farmVillages.Count);
            foreach (FarmVillage village in farmVillages)
            {
                int lcCount;
                if (village.isBarb) lcCount = Settings.LC_PER_BARB_ATTACK;
                else lcCount = Settings.LC_PER_PLAYER_ATTACK;

                if (village.lastSentAttackTime.AddMinutes(village.minimumAttackIntervalInMinutes) > DateTime.Now)
                {
                    System.Console.WriteLine("Last sent attack time: {0}\nAttack Interval: {1}.\nTime now: {2}", village.lastSentAttackTime, village.minimumAttackIntervalInMinutes, DateTime.Now);
                    continue;
                }
                else
                {
                    // ...
                }
                
                if (onlyBarbs && !village.isBarb || (village.scoutOnTheWay && Settings.INTELLIGENT_FARMING_ENABLED)) continue;
                if (troops.lc < lcCount)
                {
                    System.Console.WriteLine("Not enough LC. Exiting farm function.");
                    return this;
                }
                // Fill in the troops.
                if ((village.wall == 0 || !Settings.INTELLIGENT_FARMING_ENABLED) || (village.wall > 0 && !Settings.RAM_SCOUTED_TROOPLESS_WALLS && !Settings.ONLY_ATTACK_NO_WALL))
                {
                    browser.TextField(Find.ById("unit_input_light")).TypeText(lcCount.ToString());
                    troops.lc -= lcCount;
                    if (troops.scouts > 0)
                    {
                        browser.TextField(Find.ById("unit_input_spy")).TypeText("1");
                        --troops.scouts;
                    }
                    // Fill in the village coords.
                    browser.TextField(Find.ByClass(p => p.Contains("target-input-field"))).TypeText(village.GetCoords());
                    browser.Button(Find.ById("target_attack")).Click();
                    village.lastSentAttackTime = DateTime.Now;
                    Utils.CheckBotProtection(browser.Html);
                }
                else if(village.wall > 0 && Settings.RAM_SCOUTED_TROOPLESS_WALLS)
                {
                    // Send some rams.
                    if (troops.rams < 4 || troops.lc < 25 || troops.scouts < 1)
                    {
                        GetTroops();
                        continue;
                    }
                    browser.TextField(Find.ById("unit_input_spy")).TypeText("1");
                    browser.TextField(Find.ById("unit_input_ram")).TypeText("4");
                    browser.TextField(Find.ById("unit_input_light")).TypeText("25");
                    troops.scouts--;
                    troops.rams -= 4;
                    troops.lc -= 25;
                    // Fill in the village coords.
                    browser.TextField(Find.ByClass(p => p.Contains("target-input-field"))).TypeText(village.GetCoords());
                    browser.Button(Find.ById("target_attack")).Click();
                    village.lastSentAttackTime = DateTime.Now;
                    Utils.CheckBotProtection(browser.Html);
                }
                else if(village.wall < 0) // Unknown wall level.
                {
                    // Scout.
                    if (troops.scouts < 1)
                    {
                        GetTroops();
                        continue;
                    }
                    if(village.scoutOnTheWay)
                    {
                        GetTroops();
                        continue;
                    }
                    browser.TextField(Find.ById("unit_input_spy")).TypeText("1");
                    --troops.scouts;
                    // Fill in the village coords.
                    browser.TextField(Find.ByClass(p => p.Contains("target-input-field"))).TypeText(village.GetCoords());
                    village.scoutOnTheWay = true;
                    village.scoutTime = GetServerTime();
                    System.Console.WriteLine("Server Time is: {0}", GetServerTime());
                    browser.Button(Find.ById("target_attack")).Click();
                    village.lastSentAttackTime = DateTime.Now;
                    Utils.CheckBotProtection(browser.Html);
                }
                else
                {
                    GetTroops();
                    continue;
                }
                
                // Fetch the moral.
                int moralAmount = 100; // Barbs always have 100 moral.
                if (!village.isBarb)
                {
                    string moral = browser.Table(Find.ByClass("vis")).TableBodies[0].TableRows[5].TableCells[1].InnerHtml;
                    System.Console.WriteLine("Moral is: {0}", moral);
                    moral = Regex.Replace(moral, "[^0-9]", "");
                    System.Console.WriteLine("Moral number is {0}", moral);
                    moralAmount = int.Parse(moral);
                }

                if(moralAmount < Settings.MIN_MORAL && !(Settings.IGNORE_MORAL_IF_WALL_ZERO && village.wall == 0))
                {
                    System.Console.WriteLine("Ignoring this village because of moral.");
                    GetTroops();
                    continue;
                }

                browser.Button(Find.ById("troop_confirm_go")).Click();
                Utils.CheckBotProtection(browser.Html);
                System.Console.WriteLine("Attacked village {0}.", village.GetCoords());
            }
            System.Console.WriteLine("Finished farming for village {0}", villageId);
            return this;
        }
    }
}