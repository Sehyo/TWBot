using System.Collections.Generic;
using WatiN.Core;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.IO;

namespace TW_Bot
{
    public class Account
    {
        public List<Village> villages;
        public IE browser;
        public string username;
        string password;
        bool isLoggedIn;
        public string world = "en105";

        public Account(string username, string password)
        {
            this.username = username;
            this.password = password;
            this.isLoggedIn = false;
            this.villages = ReadFromXmlFile<List<Village>>(username + "/" + world + "/villages.xml");
            // Shuffle farm villages ?
        }

        public Account(List<Village> villages, string username, string password)
        {
            this.villages = villages;
            this.username = username;
            this.password = password;
            this.isLoggedIn = false;
        }

        public void login()
        {
            browser = new IE();
            browser.ClearCache();
            browser.ClearCookies();
            browser.GoTo("https://www.tribalwars.net/sv-se/page/logout");
            browser.GoTo("http://www.tribalwars.net");
            //browser.Body.Focus();
            browser.TextField(Find.ById("user")).TypeText(username);
            browser.TextField(Find.ById("password")).TypeText(password);
            System.Console.WriteLine("Clicking login.");
            browser.Link(Find.ByClass("btn-login")).Click();
            System.Console.WriteLine("Waiting");
            Div wContainer = browser.Div(Find.ByClass("worlds-container"));
            wContainer.WaitUntilExists();
            System.Console.WriteLine("Going to world");
            browser.GoTo("https://www.tribalwars.net/sv-se/page/play/" + world);
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
            browser.GoTo("https://www.tribalwars.net/sv-se/page/logout");
            browser.Close();
            isLoggedIn = false;
            // Save the states.
            WriteToXmlFile<List<Village>>(username + "/" + world + "/villages.xml", villages);
        }

        public void FarmWithAllVillages()
        {
            foreach (Village village in villages)
            {
                System.Console.WriteLine("--------------VILLAGE {0}-------------", village.x + " | " + village.y);
                if (Settings.SCAVENGING_ENABLED)
                {
                    System.Console.WriteLine("VILLAGE {0} SCAVENGING", village.x + " | " + village.y);
                    village.Scavenge();
                }
                if (Settings.INTELLIGENT_FARMING_ENABLED)
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
                System.Console.WriteLine("VILLAGE {0} UPDATING ATTACKINTERVALS", village.x + " | " + village.y);
                foreach (FarmVillage farm in village.farmVillages)
                {
                    int capacity = (farm.isBarb ? Settings.LC_PER_BARB_ATTACK : Settings.LC_PER_PLAYER_ATTACK) * 80;
                    farm.UpdateAttackInterval(capacity);
                    if (farm.minimumAttackIntervalInMinutes <= 0) farm.minimumAttackIntervalInMinutes = 30;
                }
                if (Settings.FARMING_ENABLED)
                {
                    System.Console.WriteLine("VILLAGE {0} FARMING", village.x + " | " + village.y);
                    village.Farm(Settings.ONLY_FARM_BARBS);
                }
            }
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