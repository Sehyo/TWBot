using System;
using System.Collections.Generic;

namespace TW_Bot
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Utils.SendPush("TWBot Started");
            Account account = new Account("sehyo", "BonxeL141!");
            Random random = new Random();
            while (true)
            {
                account.villages[0].addBarbFarmVillagesFromFile("F:\\Users\\Alex\\source\\repos\\TW-Bot\\TW-Bot\\bin\\Debug\\sehyo\\en105\\barbs.txt");
                bool success = false;
                while (!success)
                {
                    try
                    {
                        //Utils.SendPush("Logging in.");
                        account.login();
                        //Utils.SendPush("Handling villages...");
                        account.FarmWithAllVillages();
                        account.logout();
                        //Utils.SendPush("Logged out.");
                        success = true;
                    }
                    catch(Exception e)
                    {
                        System.Console.WriteLine("Program error: {0}.\nSaving progress and estarting session.", e.Message);
                        Account.WriteToXmlFile<List<Village>>(account.username + "/" + account.world + "/villages.xml", account.villages);
                        try
                        {
                            account.browser.ForceClose();
                        }
                        catch
                        {

                        }
                    }
                }
                double sleepTime = random.Next(20, 60);
                foreach(Village village in account.villages)
                {
                    if (village.SoonestScavengeCompletionTime == null) continue;
                    if (village.SoonestScavengeCompletionTime < DateTime.Now) continue;
                    if (DateTime.Now.AddMinutes(sleepTime) > village.SoonestScavengeCompletionTime)
                    {
                        sleepTime = ((DateTime)village.SoonestScavengeCompletionTime).Subtract(DateTime.Now).TotalMinutes;
                    }
                }
                sleepTime = sleepTime * 1000 * 60;
                System.Console.WriteLine("Sleeping until {0}", DateTime.Now.AddMinutes(((sleepTime / 1000) / 60)));
                Utils.SendPush("Sleeping until " + DateTime.Now.AddMinutes(((sleepTime / 1000) / 60)));
                System.Threading.Thread.Sleep((int)sleepTime);
            }
        }
    }
}