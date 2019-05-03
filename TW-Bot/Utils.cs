using System.Net.Http;
using System.Collections.Generic;
using WatiN.Core;
using System;
using System.Runtime.InteropServices;

namespace TW_Bot
{
    public static class Utils
    {
        private static readonly HttpClient client = new HttpClient();
        public static DateTime? lastServerCommunicationTime = null;
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;

        [DllImport("user32.dll")]
        public static extern bool PostMessage(int hWnd, uint Msg, int wParam, int lParam);

        public static void CheckBotProtection(string html)
        {
            if(html.Contains("bot_protection") || html.Contains("bot_check"))
            {
                System.Console.WriteLine("BOT PROTECTION DETECTED");
                SendPush(Settings.USERNAME + " - " + Settings.WORLD + ": BOT PROTECTION DETECTED");
                Client.player.Play();
                System.Console.ReadLine();
                Client.player.Stop();
            }
        }

        public static async void SendPush(string message = "BOT PROTECTION DETECTED")
        {
            var values = new Dictionary<string, string>
            {
                { "token", "anxdjsmgavf9f39v954yhx7a99jh1k" },
                { "user", "goyg973rfzm36uy96vcri92zs2kifv" },
                { "message", message }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://api.pushover.net/1/messages.json", content);

            var responseString = await response.Content.ReadAsStringAsync();
        }

        public static void GoTo(IE browser, string url)
        {
            Client.WaitForTurn();
            browser.GoTo(url);
            while (((SHDocVw.InternetExplorerClass)(browser.InternetExplorer)).Busy || browser.Html == null)
            {
                System.Threading.Thread.Sleep(100);
            }
            bool upload = false;
            if (lastServerCommunicationTime == null)
            {
                lastServerCommunicationTime = DateTime.Now;
                upload = true;
            }
            else
            {
                TimeSpan timeSpan = DateTime.Now.Subtract((DateTime)lastServerCommunicationTime);
                if (timeSpan.Minutes <= 5) upload = false;
                else
                {
                    upload = true;
                    lastServerCommunicationTime = DateTime.Now;
                }
            }
            if (upload)
            {
                browser.CaptureWebPageToFile("latest.jpg");
                Client.UploadScreenCapture();
            }
        }

        public static void WaitUntilExistsOrFail(this Element element, int timeoutInSeconds, string failureMessage)
        {
            try
            {
                element.WaitUntilExists(timeoutInSeconds);
            }
            catch (WatiN.Core.Exceptions.TimeoutException)
            {
                System.Console.WriteLine("ELement never exists.");
                System.Console.ReadLine();
            }
        }
    }
}