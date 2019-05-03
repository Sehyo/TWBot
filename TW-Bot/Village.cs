using System.Collections.Generic;
using WatiN.Core;
using System.Text.RegularExpressions;
using System;
using System.Windows.Forms;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TW_Bot
{
    public class Village
    {
        public int x, y;
        public int villageId;
        public string world;
        IE browser;
        public Troops troops; // Troops of this village.
        public Troops totalTroops;
        public Buildings buildings;
        public List<FarmVillage> farmVillages;
        public List<FarmVillage> spikedVillages;
        public DateTime? SoonestScavengeCompletionTime = null;
        public DateTime? LastTimeWeReadReports = null;

        [DllImport("User32.dll")]
        public static extern Int32 SetForegroundWindow(IntPtr hWnd);
        
        private Village()
        {
            farmVillages = new List<FarmVillage>();
            spikedVillages = new List<FarmVillage>();
            this.troops = new Troops();
            this.totalTroops = new Troops();
            this.villageId = -1;
        }

        public Village TagAttacks()
        {
            try
            {
                // https://en105.tribalwars.net/game.php?village=20012&screen=overview_villages&mode=incomings&subtype=attacks
                System.Console.WriteLine("---Tagging Incomings---");
                Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=overview_villages&mode=incomings&subtype=attacks");
                Utils.CheckBotProtection(browser.Html);
                if (!browser.Elements.Exists("select_all")) return this;
                WatiN.Core.CheckBox selectAll = browser.CheckBox(Find.ById("select_all"));
                if (!selectAll.Checked)
                {
                    System.Console.WriteLine("Checking Check All Incomings CheckBox.");
                    selectAll.Click();
                }
                System.Console.WriteLine("Clicking Mark Button.");
                WatiN.Core.Button markButton = browser.Button(Find.ByName("label"));
                markButton.Click();
                System.Console.WriteLine("Attacks marked.");
                return this;
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.Message);
                return this;
            }
        }

        public void removeDuplicateFarmVillages()
        {
            int dupesRemoved = 0;
            System.Console.WriteLine("Starting removal of duped villages.");
            for(int i = farmVillages.Count - 1; i >= 0; i--)
            {
                // Check if i is a duplicate in farmVillage list.
                for(int y = 0; y < farmVillages.Count; y++)
                {
                    if (i == y) break;
                    if(farmVillages[i].x == farmVillages[y].x && farmVillages[i].y == farmVillages[y].y)
                    {
                        ++dupesRemoved;
                        // Remove this duplicate village backwards because we assume fresher ones have more updated information.
                        // Due to loops usually looping from the start.
                        farmVillages.RemoveAt(i);
                        break; // OK to break here. If there are more than one duplicate it will be caught later anyway.
                    }
                }
            }

            // Same thing but with spiked villages list.
            for (int i = spikedVillages.Count - 1; i >= 0; i--)
            {
                for (int y = 0; y < farmVillages.Count; y++)
                {
                    if (spikedVillages[i].x == farmVillages[y].x && spikedVillages[i].y == farmVillages[y].y)
                    {
                        ++dupesRemoved;
                        spikedVillages.RemoveAt(i);
                        break;
                    }
                }
            }

            System.Console.WriteLine("Removed {0} dupes.", dupesRemoved);
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

        public double DistanceToFarm(FarmVillage farm)
        {
            Vector2 ourPos = new Vector2(x, y);
            return Vector2.Distance(ourPos, new Vector2(farm.x, farm.y));
        }

        public Village(List<FarmVillage> farmVillages, int villageId)
        {
            this.farmVillages = farmVillages;
            this.spikedVillages = new List<FarmVillage>();
            this.villageId = villageId;
            this.troops = new Troops();
        }

        public Village(int villageId, int x, int y)
        {
            this.farmVillages = new List<FarmVillage>();
            this.spikedVillages = new List<FarmVillage>();
            this.villageId = villageId;
            this.troops = new Troops();
            this.x = x;
            this.y = y;
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
            Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=report");
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
                int x, y;
                try
                {
                    x = int.Parse(match.ToString());
                    y = int.Parse(match.NextMatch().ToString());
                }
                catch
                {
                    System.Console.WriteLine("Report \"{0}\" is not relevant.", report.Text);
                    continue;
                }
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
                try // Yellow dot reports from assisting other villages will cause an exception.
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
                catch
                {
                    continue;
                }
            }

            System.Console.WriteLine("Spiked villages removed.");
        }

        /*public Village TagIncomings()
        {
            Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=overview_villages&mode=incomings&subtype=attacks");
            // 1. Get all incomings.

            // 2. Check if they are already tagged or not.

            // 3. Condition 1: Only keep incomings if they didn't exist last time we checked incs.
            //    Condition 2: The time difference from now and last time we checked incs can't be greater than X minutes. (To make sure we tag correctly).

            // 4. Calculate most likely unit type.

            // 5. Tag.

            // 6. ???

            // 7. Profit.
            return this;
        }*/
        
        public Village Scavenge()
        {
            System.Console.WriteLine("Scavenge Function Started");
            //SetForegroundWindow(browser.hWnd);
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
                /*TextField spearText = browser.TextField(Find.ByName("spear"));
                //spearText.Focus();
                //spearText.Click();
                //spearText.Id = "CHANGEME";
                //string jsCode = "var e = jQuery.Event('keydown'); e.which = 50; $('#CHANGEME').trigger(e);";

                //string jsCode = "var evt = new KeyboardEvent('keydown', { 'keyCode':50, 'which':50}); document.getElementById('CHANGEME').dispatchEvent(evt);";
                //string jsCode = "$(\"#CHANGEME\").val('200');";
                //browser.Eval(jsCode);
                //spearText.SetAttributeValue("readonly", "true");

                //System.Console.ReadLine();
                //spearText.TypeText("2");
                //string jsElementRef = spearText.GetJavascriptElementReference();
                //browser.
                //browser.
                //spearText.SetAttributeValue("value", spearCount.ToString());
                //spearText.
                //System.Console.WriteLine("Entered 200.");


                //spearText.Value = "200"; // Doesn't work. Text disappears.
                //spearText.TypeText("200"); // Doesn't work. Text disappears.
                // Work Around:
                SetForegroundWindow(browser.hWnd);
                browser.Body.Focus();
                SetForegroundWindow(browser.hWnd);
                spearText.Focus();
                SendKeys.SendWait(spearCount.ToString()); // spearText.Value and .TypeText here doesn't work. Input gets removed. Wtf?*/
                string js = "$(\"input.unitsInput[name='spear']\").val(" + spearCount + ").trigger(\"change\")";
                System.Console.WriteLine("Injecting javascript for unit entry..");
                browser.Eval(js);
                System.Console.WriteLine("Entered Spear Count.");
                System.Threading.Thread.Sleep(200);
                options[i].Click();
                System.Threading.Thread.Sleep(200);
                //System.Console.ReadLine();
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
            Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=main");
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
            if (!browser.Url.Contains("command") || !browser.Url.Contains(villageId.ToString()))
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

        public void GetTotalTroops()
        {
            // We just get total LC count for now cos lazy.
            // https://en105.tribalwars.net/game.php?village=20012&screen=stable
            // https://en105.tribalwars.net/game.php?village=20012&screen=barracks
            // https://en105.tribalwars.net/game.php?village=20012&screen=garage
            Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=stable");
            LinkCollection links = browser.Links;
            Link lcLink = null;

            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].ClassName == null) continue;
                if(links[i].ClassName.Equals("unit_link"))
                {
                    if(links[i].GetAttributeValue("data-unit").Equals("light"))
                    {
                        lcLink = links[i];
                        break;
                    }
                }
            }

            if(lcLink == null)
            {
                System.Console.WriteLine("Could not find lcLink..");
                System.Console.ReadLine();
            }
            try
            {
                TableCell parentCell = (TableCell)lcLink.Parent;
                TableRow parentRow = (TableRow)parentCell.Parent;
                TableCell lcCountCell = (TableCell)(parentRow.Children()[2]);

                string[] counts = lcCountCell.InnerHtml.Split('/');
                troops.lc = int.Parse(counts[0]);
                totalTroops.lc = int.Parse(counts[1]);
            }
            catch
            {
                troops.lc = 0;
                totalTroops.lc = 0;
            }

            System.Console.WriteLine("We currently have {0} / {1} lc.", troops.lc, totalTroops.lc);
        }

        public Village CheckScoutReports()
        {
            if (Settings.REPORT_READ_INTERVAL_MINUTES <= 0) return this;
            bool readReports = false;
            //if (LastTimeWeReadReports == null) readReports = true;
            //else if (((DateTime)LastTimeWeReadReports).AddMinutes(Settings.REPORT_READ_INTERVAL_MINUTES) < DateTime.Now) readReports = true;
            if (Settings.LastTimeWeReadReports == null) readReports = true;
            else if (((DateTime)Settings.LastTimeWeReadReports).AddMinutes(Settings.REPORT_READ_INTERVAL_MINUTES) < DateTime.Now) readReports = true;
            if (!readReports)
            {
                //System.Console.WriteLine("Skipping reading of reports for now.");
                return this;
            }
            System.Console.WriteLine("Checking Scout Reports.");
            List<FarmVillage> villagesAwaitingScouts = new List<FarmVillage>();
            foreach(FarmVillage village in farmVillages)
            {
                //if(village.scoutOnTheWay)
                //{
                    villagesAwaitingScouts.Add(village);
                //}
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
            // Implement earlier discovered agnostic way when time allows.
            Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=report&mode=all&group_id=22988");
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
            // Save link to report and the text.
            List<Tuple<string, string>> scoutReportTuples = new List<Tuple<string, string>>();
            foreach(Span rep in scoutReports)
            {
                string link;
                if(rep.Parent.OuterHtml.Contains("flipthis-wrapper"))
                {
                    link = ((Link)(rep.Parent.Parent)).Url;
                }
                else
                {
                    link = ((Link)(rep.Parent)).Url;
                }
                string text = rep.Text;
                scoutReportTuples.Add(new Tuple<string, string>(link, text));
            }
            System.Console.WriteLine("We have {0} reports.", scoutReports.Count);
            // USE REPORT ID!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! NOT TIME!!!!!!!!!!!!!!!!!
            int abc = 0;
            int reportCount = scoutReportTuples.Count;
            for (int reportNumber = 0; reportNumber < scoutReportTuples.Count; reportNumber++)
            {
                ++abc;
                var match = Regex.Match(scoutReportTuples[reportNumber].Item2, @"(\d+)\|(\d+)");
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                //foreach (Group group in match.Groups) System.Console.WriteLine(group.Value);
                //int x = int.Parse(match.ToString());
                //int y = int.Parse(match.NextMatch().ToString());
                System.Console.WriteLine("Current report is for village {0}|{1}", x, y);
                for (int i = villagesAwaitingScouts.Count - 1; i >= 0; i--) // Quadratic.. Must optimise later..
                {
                    if (villagesAwaitingScouts[i].x == x && villagesAwaitingScouts[i].y == y)
                    {
                        System.Console.WriteLine("Current report is for village: ({0}|{1}).", x, y);
                        int currentReportId;
                        // https://en105.tribalwars.net/game.php?village=19360&screen=report&mode=all&group_id=0&view=23866502
                        var idMatch = Regex.Match(scoutReportTuples[reportNumber].Item1, @"view=\d+");
                        currentReportId = int.Parse(Regex.Replace(idMatch.ToString(), "[^0-9]", ""));
                        if (currentReportId > villagesAwaitingScouts[i].lastReadReportId)
                        {
                            System.Console.WriteLine("Report is newer than last read for this village!");
                        }
                        else
                        {
                            System.Console.WriteLine("There is a newer report for this village. Skipping");
                            continue;
                        }
                        villagesAwaitingScouts[i].lastReadReportId = currentReportId;
                        // Only use reports from blue, green, and red/blue
                        /*TableCell reportCell = (TableCell)scoutReports[reportNumber].Parent.Parent.Parent.Parent;
                        Image successType = (Image)(reportCell.Children()[1]);
                        if (!(successType.Src.Contains("green") || successType.Src.Contains("blue") || successType.Src.Contains("red_blue")))
                        {
                            System.Console.WriteLine("Success Level of this report is not green, blue or red_blue. Skipping this report.");
                            continue;
                        }*/
                        // Open report page
                        System.Console.WriteLine("Opening Report.");
                        Utils.GoTo(browser, scoutReportTuples[reportNumber].Item1);
                        while (browser.Html == null) { }
                        Utils.CheckBotProtection(browser.Html);
                        System.Console.WriteLine("Report opened.");
                        int wallLevel = -1;

                        

                        // Check resource levels.
                        // IMAGE LINKS ARE NOT ALWAYS THE SAME APPEARENTLY.

                        ImageCollection allImages = browser.Images;
                        Image woodImage = null;
                        Image clayImage = null;
                        Image ironImage = null;
                        Image wallImage = null;
                        foreach (Image image in allImages)
                        {
                            if (image.Src.Contains("wood.png")) woodImage = image;
                            else if (image.Src.Contains("stone.png")) clayImage = image;
                            else if (image.Src.Contains("iron.png")) ironImage = image;
                            else if (image.Src.Contains("wall.png")) wallImage = image;
                        }

                        if (wallImage != null)
                        {
                            wallLevel = int.Parse(wallImage.Parent.NextSibling.InnerHtml);
                        }
                        else wallLevel = 0;
                        // Save wall level.
                        System.Console.WriteLine("Changed wall level! Previous: {0}, new: {1}", villagesAwaitingScouts[i].wall, wallLevel);
                        villagesAwaitingScouts[i].wall = wallLevel;

                        // https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/wood.png
                        // = browser.Image(Find.BySrc("https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/wood.png"));
                        int woodLevel = -1;
                        if (woodImage != null)
                        {
                            woodLevel = int.Parse(woodImage.Parent.NextSibling.InnerHtml);
                        }
                        else woodLevel = 0;
                        // https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/stone.png
                         //= browser.Image(Find.BySrc("https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/stone.png"));
                        int clayLevel = -1;
                        if (clayImage != null)
                        {
                            clayLevel = int.Parse(clayImage.Parent.NextSibling.InnerHtml);
                        }
                        else clayLevel = 0;
                        // https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/iron.png
                        
                        //  = browser.Image(Find.BySrc("https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/iron.png"));
                         
                        int ironLevel = -1;
                        if (ironImage != null)
                        {
                            ironLevel = int.Parse(ironImage.Parent.NextSibling.InnerHtml);
                        }
                        else ironLevel = 0;

                        System.Console.WriteLine("Scouted Resource Levels: {0}, {1}, {2}", woodLevel, clayLevel, ironLevel);
                        villagesAwaitingScouts[i].wood = woodLevel;
                        villagesAwaitingScouts[i].clay = clayLevel;
                        villagesAwaitingScouts[i].iron = ironLevel;

                        try
                        {
                            // Check if village contains troops.
                            // If no troop, unit type is faded.
                            Table defInfo = browser.Table(Find.ById("attack_info_def"));
                            TableBody defInfoBody = (TableBody)(defInfo.Children()[0]); // 0 is out of range
                            TableCell unitInfo = (TableCell)(((TableRow)(defInfoBody.Children()[2])).Children()[0]);
                            Table unitTable;
                            try
                            {
                                unitTable = (Table)(unitInfo.Children()[0]);
                            }
                            catch
                            {
                                System.Console.WriteLine("Current report is 100 % fail.");
                                System.Console.WriteLine("Keeping village in spiked villages list.");
                                System.Console.WriteLine("Added village to spiked village list.");
                                spikedVillages.Remove(villagesAwaitingScouts[i]);
                                farmVillages.Remove(villagesAwaitingScouts[i]);
                                spikedVillages.Add(villagesAwaitingScouts[i]);
                                villagesAwaitingScouts.Remove(villagesAwaitingScouts[i]);
                                System.Console.WriteLine("Continuing..");
                                break; // Break out of inner for loop as it only concerns this village.
                            }
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
                                string fadeStatus = innerImage.ClassName; // Non-Faded units have a null Class Name.
                                if (fadeStatus != null)
                                {
                                    //System.Console.WriteLine("Current unit type is faded!");
                                    // Do nothing.
                                }
                                else
                                {
                                    System.Console.WriteLine("Current unit type is _NOT_ faded!");
                                    unitsExists = true; // There are units in this village.
                                    break;
                                }
                            }

                            //System.Console.WriteLine("Enemy units in this report? : {0}", unitsExists);

                            // If contains troops, make sure it keeps existing in only spikedVillages.
                            villagesAwaitingScouts[i].scoutOnTheWay = false;

                            spikedVillages.Remove(villagesAwaitingScouts[i]);
                            farmVillages.Remove(villagesAwaitingScouts[i]);
                            Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=report"); // Go back to report overview.
                            Utils.CheckBotProtection(browser.Html);
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
                        catch
                        {
                            System.Console.WriteLine("Some error checking units of this report. Continuing.");
                        }
                    }
                }
            }
            /*
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
                        if(GetDateOfReport(scoutReports[reportNumber]) > villagesAwaitingScouts[i].lastScoutReportTime)
                        {
                            System.Console.WriteLine("This is the newest report!");
                        }
                        else
                        {
                            System.Console.WriteLine("There is a newer report for this village. Skipping");
                            continue;
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
                        while(browser.Html == null) { }
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

                        // Check resource levels.
                        // https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/wood.png
                        Image woodImage = browser.Image(Find.BySrc("https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/wood.png"));
                        int woodLevel = -1;
                        if (woodImage.Exists)
                        {
                            woodLevel = int.Parse(woodImage.Parent.NextSibling.InnerHtml);
                        }
                        else woodLevel = 0;
                        // https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/stone.png
                        Image clayImage = browser.Image(Find.BySrc("https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/stone.png"));
                        int clayLevel = -1;
                        if (clayImage.Exists)
                        {
                            clayLevel = int.Parse(clayImage.Parent.NextSibling.InnerHtml);
                        }
                        else clayLevel = 0;
                        // https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/iron.png
                        Image ironImage = browser.Image(Find.BySrc("https://dsen.innogamescdn.com/asset/76fbc28978/graphic/buildings/iron.png"));
                        int ironLevel = -1;
                        if (ironImage.Exists)
                        {
                            ironLevel = int.Parse(ironImage.Parent.NextSibling.InnerHtml);
                        }
                        else ironLevel = 0;

                        System.Console.WriteLine("Scouted Resource Levels: {0}, {1}, {2}", woodLevel, clayLevel, ironLevel);
                        villagesAwaitingScouts[i].wood = woodLevel;
                        villagesAwaitingScouts[i].clay = clayLevel;
                        villagesAwaitingScouts[i].iron = ironLevel;

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
            */
            System.Console.WriteLine("We checked {0} reports.", abc);
            System.Console.WriteLine("Finished Checking Scout Reports.");
            Settings.LastTimeWeReadReports = DateTime.Now;
            //LastTimeWeReadReports = DateTime.Now;
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
                Table visTable = browser.Table(Find.ByClass("vis"));
                TableBody visTableBody = (TableBody)(visTable.Children()[0]);
                village.isBarb = visTableBody.Children().Count == 6; // Players have 7 children here.
                if(!village.isBarb)
                {
                    // Village used to be a barb, but is now a player village.
                    GetTroops();
                    continue; // Skip this village.
                }
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

        public Village FAFarm()
        {
            // https://en105.tribalwars.net/game.php?village=20012&screen=am_farm
            Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=am_farm");
            Utils.CheckBotProtection(browser.Html);
            Random random = new Random(DateTime.Now.Millisecond);
            // Make sure attacked checkbox is checked.
            System.Console.WriteLine("STARTING FA FARMING");
            for (int i = 0; !browser.Url.Contains("&&"); i++) // url will contain && only when we have passed the last page.
            {
                Utils.GoTo(browser, "https://" + world + ".tribalwars.net/game.php?village=" + villageId + "&screen=am_farm&order=distance&dir=asc&Farm_page=" + i);
                Utils.CheckBotProtection(browser.Html);
                WatiN.Core.CheckBox attackedCheckBox = browser.CheckBox(Find.ById("attacked_checkbox"));
                if (!attackedCheckBox.Checked) attackedCheckBox.Click();
                Table plunderList = browser.Table(Find.ById("plunder_list"));
                TableBody plunderBody = (TableBody)(plunderList.Children()[0]);
                TableRowCollection farms = plunderBody.OwnTableRows; // First 2 entries are not farms though.

                for (int j = 2; j < farms.Count; j++)
                {
                    TableCell lcCountCell = browser.TableCell(Find.ById("light"));
                    int lcCount = int.Parse(lcCountCell.InnerHtml);
                    if (lcCount < 5) return this;

                    // 8 = A, 9 = B, 10 = C
                    TableCell cellA = (TableCell)farms[j].Children()[8];
                    TableCell cellB = (TableCell)farms[j].Children()[9];
                    TableCell cellC = (TableCell)farms[j].Children()[10];

                    if (cellA.InnerHtml.Contains("disabled") && cellB.InnerHtml.Contains("disabled")) continue;

                    int targetX, targetY;
                    TableCell coordsCell = (TableCell)farms[j].Children()[3];
                    Link coordsLink = (Link)coordsCell.Children()[0];
                    //Div coordsDiv = (Div)coordsCell.Children()[0];
                    //Link coordsLink = (Link)coordsDiv.Children()[0];
                    string coordsText = coordsLink.InnerHtml.Split(' ')[1].Substring(1);
                    coordsText = coordsText.Remove(coordsText.Length - 1);
                    string[] coords = coordsText.Split('|');
                    targetX = int.Parse(coords[0]);
                    targetY = int.Parse(coords[1]);
                    double distance = Math.Sqrt(Math.Pow(targetX - x, 2) + Math.Pow(targetY - y, 2));
                    double travelTime = distance * 10;
                    DateTime eta = DateTime.Now.AddMinutes(travelTime);
                    // Find Farm Village if exists.
                    FarmVillage targetVillage = null;
                    foreach (FarmVillage farmVillage in farmVillages)
                    {
                        if (!(farmVillage.x == targetX && farmVillage.y == targetY)) continue;
                        targetVillage = farmVillage;
                        break;
                    }
                    if (targetVillage == null)
                    {
                        // Target Village doesn't exist in our list.
                        // Let's add it.
                        // public FarmVillage(int x, int y, bool isBarb = false, int wall = -1, int clay = -1, int wood = -1, int iron = -1, double minimumAttackIntervalInMinutes = 120)
                        targetVillage = new FarmVillage(targetX, targetY, true);
                        farmVillages.Add(targetVillage);
                    }

                    DateTime filterTime = targetVillage.lastAttackETA.AddMinutes(targetVillage.minimumAttackIntervalInMinutes);
                    if (eta < filterTime) continue;

                    Link buttonA = (Link)cellA.Children()[0];
                    Link buttonB = (Link)cellB.Children()[0];
                    //Link buttonC = (Link)cellC.Children()[0];

                    //Link buttonA = (Link)((Div)cellA.Children()[0]).Children()[0];
                    //Link buttonB = (Link)((Div)cellB.Children()[0]).Children()[0];
                    //Link buttonC = (Link)((Div)cellC.Children()[0]).Children()[0];

                    TableCell lcs = browser.TableCell(Find.ById("light"));
                    TableCell scouts = browser.TableCell(Find.ById("spy"));

                    int lcLeft = int.Parse(lcs.InnerHtml);
                    int scoutsLeft = int.Parse(scouts.InnerHtml);

                    System.Console.WriteLine("LCs left: {0}", lcLeft);
                    System.Console.WriteLine("Scouts left: {0}", scoutsLeft);

                    if (!buttonA.OuterHtml.Contains("disabled") && lcLeft >= Settings.LC_PER_BARB_ATTACK && scoutsLeft >= 1)
                    {
                        buttonA.Click();
                        targetVillage.lastAttackETA = eta;
                        targetVillage.lastSentAttackTime = DateTime.Now;
                        System.Threading.Thread.Sleep(random.Next(200, 300));
                        Utils.CheckBotProtection(browser.Html);
                    }
                    else if (!buttonB.OuterHtml.Contains("disabled") && lcLeft >= Settings.LC_PER_BARB_ATTACK)
                    {
                        buttonB.Click();
                        targetVillage.lastAttackETA = eta;
                        targetVillage.lastSentAttackTime = DateTime.Now;
                        System.Threading.Thread.Sleep(random.Next(200, 300));
                        Utils.CheckBotProtection(browser.Html);
                    }
                    else return this;
                }
            }
            System.Console.WriteLine("ENDING FA FARMING");
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
                if (DistanceToFarm(village) > Settings.FARM_RADIUS)
                {
                    System.Console.WriteLine("Skipping current village because of too large radius.");
                    continue;
                }
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
                //if (troops.lc < lcCount)
                //{
                    //System.Console.WriteLine("Not enough LC. Exiting farm function.");
                    //return this;
                //}
                // Fill in the troops.
                if (troops.lc >= lcCount && (village.wall == 0 || !Settings.INTELLIGENT_FARMING_ENABLED) || (village.wall > 0 && !Settings.RAM_SCOUTED_TROOPLESS_WALLS && !Settings.ONLY_ATTACK_NO_WALL))
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
                    if (troops.rams < 10 || troops.axes < 50 || troops.scouts < 1 || village.scoutOnTheWay)
                    {
                        GetTroops();
                        continue;
                    }
                    browser.TextField(Find.ById("unit_input_spy")).TypeText("1");
                    browser.TextField(Find.ById("unit_input_ram")).TypeText("4");
                    browser.TextField(Find.ById("unit_input_axe")).TypeText("50");
                    troops.scouts--;
                    troops.rams -= 10;
                    troops.axes -= 50;
                    village.scoutOnTheWay = true;
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
                Table visTable = browser.Table(Find.ByClass("vis"));
                TableBody visTableBody = (TableBody)(visTable.Children()[0]);
                village.isBarb = visTableBody.Children().Count == 6; // Players have 7 children here.
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

                if(!village.isBarb && onlyBarbs)
                {
                    System.Console.WriteLine("This village is now a player village, used to be a barb. Skipping.");
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