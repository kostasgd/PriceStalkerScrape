﻿using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PriceStalkerScrape.DriverHelper
{
    public abstract class AbstractDriver
    {
        protected string EDGEDRIVEREXENAME = @"\msedgedriver.exe";
        protected string CHROMEDRIVEREXENAME = @"\chromedriver.exe";
        protected string CHROMEDOWNLOADEDDRIVERZIPNAME = @"\chromeDriverZipped.zip";
        protected const string HTTPACCEPTHEADER = "Accept: text/html, application/xhtml+xml, */*";
        protected const string HTTPUSERAGENTHEADER = "User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
        protected const string DRIVEROPTION = "headless";
        protected const string EXTRACTEDFOLDER = @"\extracted\";
        protected string REGEXPATTERN = @".[0-9]{1,}";
        protected DriverType DriverType;
        public void KillBrowserDriverProcesses()
        {
            var DriverProcesses = Process.GetProcesses().Where(pr => pr.ProcessName == DriverType.ToString()); // without '.exe'

            foreach (var process in DriverProcesses)
            {
                process.Kill();
            }
        }
        public string GetInstalledDriverVersionFromPath(string path)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(path);
            return versionInfo.FileVersion;
        }
        public void DownloadZip(string driverDownloadLink, string driverDownloadZipName, string driverexename)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add(HTTPACCEPTHEADER);
            webClient.Headers.Add(HTTPUSERAGENTHEADER);
            webClient.DownloadFile(new Uri(GetLatestLinkVersion(driverDownloadLink)), Directory.GetCurrentDirectory() + driverDownloadZipName);
            UnzipFile(driverexename, driverDownloadZipName);
        }
        public void MoveDriver(string driverName)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + EXTRACTEDFOLDER + driverName))
            {
                File.Move(Directory.GetCurrentDirectory() + EXTRACTEDFOLDER + driverName
                    , Directory.GetCurrentDirectory() + @"\" + driverName);
            }
        }
        public void UnzipFile(string driverName, string zipFileName)
        {
            string path = Directory.GetCurrentDirectory() + driverName;
            if (File.Exists(Directory.GetCurrentDirectory() + driverName))
            {
                File.Delete(Directory.GetCurrentDirectory() + driverName);
                if (File.Exists(Directory.GetCurrentDirectory() + EXTRACTEDFOLDER.TrimEnd('\\') + CHROMEDRIVEREXENAME))
                    File.Delete(Directory.GetCurrentDirectory() + EXTRACTEDFOLDER.TrimEnd('\\') + CHROMEDRIVEREXENAME);
                ZipFile.ExtractToDirectory(Directory.GetCurrentDirectory() + CHROMEDOWNLOADEDDRIVERZIPNAME
                , Directory.GetCurrentDirectory() + EXTRACTEDFOLDER.TrimEnd('\\'));
                MoveDriver(driverName);
            }
        }
        public ChromiumOptions ChromiumOptions()
        {
            ChromiumOptions chromiumOptions = null;
            switch (DriverType)
            {
                case DriverType.chromedriver:
                    chromiumOptions = new ChromeOptions();
                    break;
               
            }
            chromiumOptions.AddArgument(DRIVEROPTION);
            return chromiumOptions;
        }
        public string GetLatestLinkVersion(string driverDowloadLink)
        {
            string htmlCode = "";
            using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
            {
                htmlCode = client.DownloadString(driverDowloadLink);
            }
            return CreateDownloadLink(htmlCode);
        }
        public abstract OpenQA.Selenium.Chromium.ChromiumDriver GetDriver();
        public abstract string CreateDownloadLink(string latestVersionLink);
    }
}
