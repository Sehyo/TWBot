using System;
using System.Collections.Generic;
using System.IO;


namespace TW_Bot
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            WatiN.Core.Settings.AutoMoveMousePointerToTopLeft = false;
            Settings.USERNAME = "sehyo";
            Settings.PASSWORD = "FuckHedge!";
            string[] worlds = Directory.GetDirectories(Settings.USERNAME + "/");
            System.Console.WriteLine("Please select:");
            for (int i = 0; i < worlds.Length; i++)
                System.Console.WriteLine(i + " for world: " + worlds[i]);
            Settings.WORLD = new DirectoryInfo(worlds[int.Parse(System.Console.ReadLine())]).Name;
            System.Console.WriteLine("You have selected {0}", Settings.WORLD);
            System.Console.WriteLine("Starting");
            System.Console.WriteLine("Contacting server.");
            Client.WaitForTurn();
            System.Console.WriteLine("Downloading ALL data files.");
            Client.DownloadAll();
            Client.SaveAll();
            //Client.UploadAll();
            //System.Console.WriteLine("Username pls");
            //Settings.USERNAME = System.Console.ReadLine();
            //System.Console.WriteLine("Password pls");
            //Settings.PASSWORD = System.Console.ReadLine();

            Account account = new Account(Settings.USERNAME, Settings.PASSWORD);
            do
            {
                System.Console.Clear();
                System.Console.WriteLine("Your Current Added Villages:");
                foreach(Village village in account.villages)
                    System.Console.WriteLine("{0}|{1}", village.x, village.y);
                System.Console.WriteLine("Add new village?");
                if (!System.Console.ReadLine().ToUpper().Contains("Y")) break;
                int x, y;
                int id;
                System.Console.WriteLine("Enter village id:");
                id = int.Parse(System.Console.ReadLine());
                System.Console.WriteLine("Enter X:");
                x = int.Parse(System.Console.ReadLine());
                System.Console.WriteLine("Enter y:");
                y = int.Parse(System.Console.ReadLine());
                account.villages.Add(new Village(id, x, y));
                System.Console.WriteLine("Add more villages?");
            } while (System.Console.ReadLine().ToUpper().Contains("Y"));
            // Disabling this for now.
            //System.Console.WriteLine("Finding best radius..({0}|{1})", account.villages[0].x, account.villages[0].y);
            //System.Console.WriteLine("How many lc you got?");
            //int lcs = Int32.Parse(System.Console.ReadLine());
            //Settings.FARM_RADIUS = Account.SimulateOptimalFarmRadius(lcs, account.villages[0].x, account.villages[0].y, account.villages[0].farmVillages);
            //System.Console.WriteLine("Enable Scavenging? (y/n)");
            //if (System.Console.ReadLine().Contains("y")) Settings.SCAVENGING_ENABLED = true;
            //else Settings.SCAVENGING_ENABLED = false;
            //System.Console.WriteLine("Enable Farm Assistant Farming? (y/n)");
            //if (System.Console.ReadLine().Contains("y")) Settings.FA_FARMING_ENABLED = true;
            //else Settings.FA_FARMING_ENABLED = false;
            //System.Console.WriteLine("How many minutes between report reading? (0 = never, 60 = check all new reports once per hour.)");
            //Settings.REPORT_READ_INTERVAL_MINUTES = int.Parse(System.Console.ReadLine());
            /*if (!Settings.FA_FARMING_ENABLED)
            {
                System.Console.WriteLine("Enable Farming? (y/n)");
                if (System.Console.ReadLine().Contains("y")) Settings.FARMING_ENABLED = true;
                else Settings.FARMING_ENABLED = false;
                System.Console.WriteLine("Intelligent Farming Mode? (y/n)");
                if (System.Console.ReadLine().Contains("y")) Settings.INTELLIGENT_FARMING_ENABLED = true;
                else Settings.INTELLIGENT_FARMING_ENABLED = false;
                System.Console.WriteLine("Scout Spiked? (y/n)");
                if (System.Console.ReadLine().Contains("y")) Settings.SCOUT_SPIKED = true;
                else Settings.SCOUT_SPIKED = false;
            }*/
            for (int i = 1; i < account.villages.Count; i++) account.villages[i].farmVillages = account.villages[0].farmVillages;
            for (int i = 1; i < account.villages.Count; i++) account.villages[i].spikedVillages = account.villages[0].spikedVillages;
            for (int i = 1; i < account.villages.Count; i++) account.villages[i].LastTimeWeReadReports = account.villages[0].LastTimeWeReadReports;
            Random random = new Random();
            while (true)
            {
                //account.villages[0].addBarbFarmVillagesFromFile(account.username + "/" + account.world + "/barbs.txt");
                account.villages[0].removeDuplicateFarmVillages();
                bool success = false;
                while (!success)
                {
                    try
                    {
                        Client.WaitForTurn();
                        Utils.SendPush("Logging in (" + Settings.USERNAME + ", " + Settings.WORLD + ").");
                        account.login();
                        //Client.DownloadAll();
                        //Client.SaveAll();
                        //account.browser.CaptureWebPageToFile("latest.jpg");
                        //System.Console.ReadLine();
                        //Account.SimulateOptimalLCCountForFarmAssistantFarming(account.villages, account.villages[0].farmVillages);
                        account.villages[0].TagAttacks();
                        account.FarmWithAllVillages();
                        account.villages[0].TagAttacks();
                        account.logout();
                        Utils.SendPush("Logged out (" + Settings.USERNAME + ", " + Settings.WORLD + ").");
                        success = true;
                    }
                    catch(Exception e)
                    {
                        System.Console.WriteLine("Program error: {0}.\nSaving progress and estarting session.", e.Message);
                        System.Console.WriteLine("Stack Trace: {0}", e.StackTrace);
                        for (int i = 1; i < account.villages.Count; i++)
                        {
                            account.villages[i].farmVillages = null;
                            account.villages[i].spikedVillages = null;
                        }
                        // Save the states.
                        Account.WriteToXmlFile<List<Village>>(account.username + "/" + account.world + "/villages.xml", account.villages);
                        Client.UploadAll();
                        for (int i = 1; i < account.villages.Count; i++)
                        {
                            account.villages[i].farmVillages = account.villages[0].farmVillages;
                            account.villages[i].spikedVillages = account.villages[0].spikedVillages;
                        }
                        try
                        {
                            account.browser.ForceClose();
                        }
                        catch
                        {

                        }
                        if (!Settings.NEXT_RESTART_IS_IMMEDIATE)
                        {
                            System.Console.WriteLine("Sleeping for 5 min cos of crash.");
                            Utils.SendPush(Settings.USERNAME + " - " + Settings.WORLD + ": Sleeping for 5 min cos of crash.");
                            System.Threading.Thread.Sleep(1000 * 60 * 5);
                        }
                    }
                }
                double sleepTime = random.Next(15, 25);
                /*foreach(Village village in account.villages)
                {
                    if (village.SoonestScavengeCompletionTime == null) continue;
                    if (village.SoonestScavengeCompletionTime < DateTime.Now) continue;
                    if (DateTime.Now.AddMinutes(sleepTime) > village.SoonestScavengeCompletionTime)
                    {
                        sleepTime = ((DateTime)village.SoonestScavengeCompletionTime).Subtract(DateTime.Now).TotalMinutes;
                    }
                }*/
                sleepTime = sleepTime * 1000 * 60;
                System.Console.WriteLine("Sleeping until {0}", DateTime.Now.AddMinutes(((sleepTime / 1000) / 60)));
                Utils.SendPush(Settings.USERNAME + " - " + Settings.WORLD + ": Sleeping until " + DateTime.Now.AddMinutes(((sleepTime / 1000) / 60)));
                System.Threading.Thread.Sleep((int)sleepTime);
            }
        }
    }
}