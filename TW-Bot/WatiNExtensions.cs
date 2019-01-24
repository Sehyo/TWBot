using WatiN.Core;

namespace TW_Bot
{
    public static class WatiNExtensions
    {
        public static void WaitUntilElementExists(this Browser browser, string element)
        {
            while(!browser.Elements.Exists(element))
            {
                System.Threading.Thread.Sleep(100);
            }
        }
    }
}
