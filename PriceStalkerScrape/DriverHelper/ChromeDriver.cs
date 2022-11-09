using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PriceStalkerScrape.DriverHelper
{
    public class ChromeDriver : AbstractDriver
    {
        private static string CHROMEBASELINK = "https://chromedriver.storage.googleapis.com/";
        private static string CHROMEDRIVERZIPNAME = "/chromedriver_win32.zip";
        private static string CHROMEDRIVERPATH = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        private string CHROMEDOWNLOADLINK = "https://chromedriver.chromium.org/downloads";
        public ChromeDriver()
        {
            DriverType = DriverType.chromedriver;
        }
        public override string CreateDownloadLink(string latestVersionLink)
        {
            string installedVersionFromPath = GetInstalledDriverVersionFromPath(CHROMEDRIVERPATH);
            int lastIndex = installedVersionFromPath.LastIndexOf('.');
            String regex = installedVersionFromPath.Substring(0, lastIndex) + REGEXPATTERN;
            MatchCollection coll = Regex.Matches(latestVersionLink, regex);
            String regResult = coll[0].Groups[0].Value;
            string result = CHROMEBASELINK + regResult + CHROMEDRIVERZIPNAME;
            return result;
        }

        public override OpenQA.Selenium.Chromium.ChromiumDriver GetDriver()
        {
            try
            {
                return new OpenQA.Selenium.Chrome.ChromeDriver((ChromeOptions)ChromiumOptions());
            }
            catch (System.InvalidOperationException ex)
            {
                if (ex.Message.Contains("This version of ChromeDriver only supports"))
                {
                    base.KillBrowserDriverProcesses();
                    DownloadZip(CHROMEDOWNLOADLINK, CHROMEDOWNLOADEDDRIVERZIPNAME, CHROMEDRIVEREXENAME);
                }
            }
            return new OpenQA.Selenium.Chrome.ChromeDriver((ChromeOptions)ChromiumOptions());
        }
    }
}
