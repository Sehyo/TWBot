using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;


namespace TW_Bot
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            WatiN.Core.Settings.AutoMoveMousePointerToTopLeft = false;

            System.Console.WriteLine("Use Server? (Y/N)");
            if (System.Console.ReadLine().ToUpper().Contains("Y")) Settings.USE_SERVER = true;
            else Settings.USE_SERVER = false;

            // First select user.
            string[] users = Directory.GetDirectories(Directory.GetCurrentDirectory());

            System.Console.WriteLine("Please select:");
            for (int i = 0; i < users.Length; i++)
                System.Console.WriteLine(i + " for world: " + users[i]);

            Settings.USERNAME = new DirectoryInfo(users[int.Parse(System.Console.ReadLine())]).Name;

            // Get the password.
            System.IO.StreamReader passwordFile = new System.IO.StreamReader(Settings.USERNAME + "/password.txt");

            string line;
            while ((line = passwordFile.ReadLine()) != null) break; // just read first line.
            Settings.PASSWORD = line;

            string[] worlds = Directory.GetDirectories(Settings.USERNAME + "/");
            System.Console.WriteLine("Please select:");
            for (int i = 0; i < worlds.Length; i++)
                System.Console.WriteLine(i + " for world: " + worlds[i]);
            Settings.WORLD = new DirectoryInfo(worlds[int.Parse(System.Console.ReadLine())]).Name;

            System.Console.WriteLine("You have selected {0} - {1}", Settings.USERNAME, Settings.WORLD);

            System.Console.WriteLine("Activate minting mode? (y/n)");
            int mintX = 0, mintY = 0;
            if (System.Console.ReadLine().Contains("y"))
            {
                Settings.MINT = true;
                System.Console.WriteLine("Please enter the X coordinate for the village you want to mint in.");
            }
            else
            {
                System.Console.WriteLine("Activate Resource Sender Mode? (y/n)");
                if (System.Console.ReadLine().Contains("y"))
                {
                    Settings.SEND_RES = true;
                    System.Console.WriteLine("Please enter the X coordinate for the village you want to send res to.");
                }
                else
                {
                    System.Console.WriteLine("Activate Fake Script Mode? (y/n)");
                    if (System.Console.ReadLine().Contains("y"))
                    {
                        Settings.FAKE_SCRIPT = true;
                        System.Console.WriteLine("Enter name of script in quickbar exactly as it is written.");
                        Settings.SCRIPT_NAME = System.Console.ReadLine();
                    }
                }

            }

            if (Settings.MINT || Settings.SEND_RES)
            {
                mintX = int.Parse(System.Console.ReadLine());
                System.Console.WriteLine("Please enter the Y coordinate");
                mintY = int.Parse(System.Console.ReadLine());

                System.Console.WriteLine("You have chosen village {0}|{1}", mintX, mintY);
            }
            else if(Settings.FAKE_SCRIPT)
            {
                System.Console.WriteLine("How many fakes per village do you want to send?");
                Settings.FAKES_PER_VILLAGE = int.Parse(System.Console.ReadLine());
            }
            else
            {
                System.Console.WriteLine("Enter Maximum Radius for Farming:");
                Settings.FARM_RADIUS = int.Parse(System.Console.ReadLine());
                System.Console.WriteLine("Enable C for farming? (Y/N)");
                Settings.ENABLE_C_FARMING = System.Console.ReadLine().ToUpper().Contains("Y");
            }
            

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
            // Disabled below cos we auto add villas now.
            /*do
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
            } while (System.Console.ReadLine().ToUpper().Contains("Y")); */

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
                    Settings.SESSION_EXPIRED = false;
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

                        // Add all villas automatically :)
                        account.AddAllVillages();

                        if(Settings.SEND_RES)
                        {
                            // Sort village list so we send from closest villas first
                            Vector2 target = new Vector2(mintX, mintY);
                            account.villages.Sort((a, b) =>
                            {
                                Vector2 aPos = new Vector2(a.x, a.y);
                                Vector2 bPos = new Vector2(b.x, b.y);
                                return Vector2.Distance(target, aPos).CompareTo(Vector2.Distance(target, bPos));
                            });

                            foreach (Village village in account.villages)
                            {
                                try
                                {
                                    village.SendRes(mintX, mintY);
                                }
                                catch (Exception e)
                                {
                                    System.Console.WriteLine("Send Res Error: {0}", e.Message);
                                    System.Console.ReadLine();
                                }
                            }
                        }
                        else if (Settings.FAKE_SCRIPT)
                        {
                            foreach (Village village in account.villages) village.SendFakes();
                        }
                        else if(!Settings.MINT)
                        {
                            account.villages[0].TagAttacks();
                            account.FarmWithAllVillages();
                            account.villages[0].TagAttacks();
                        }
                        else
                        {
                            // Get mint village
                            foreach(Village village in account.villages)
                            {
                                if(village.x == mintX && village.y == mintY)
                                {
                                    try
                                    {
                                        village.Mint();
                                    }
                                    catch(Exception e)
                                    {
                                        System.Console.WriteLine("Minting error: {0}", e.Message);
                                    }
                                    break;
                                }
                            }
                        }
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
                if (Settings.SESSION_EXPIRED) sleepTime = 0.5; // Half minute for debugging fast.
                if (Settings.SEND_RES) sleepTime = random.Next(55, 130);
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