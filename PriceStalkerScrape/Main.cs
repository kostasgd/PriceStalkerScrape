using HtmlAgilityPack;
using LiveCharts;
using LiveCharts.Wpf;
using MaterialSkin;
using MaterialSkin.Controls;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;

namespace PriceStalkerScrape
{
    public partial class Main : MaterialForm
    {
        #region "Initialize & load"
        public Main()
        {
            InitializeComponent();
            log4net.Config.XmlConfigurator.Configure();
            //LoadData();
            //FillDatagridStats();
            //DownloadZip();
        }
        private void DownloadZip()
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
            webClient.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
            webClient.DownloadFile(new Uri(GetLatestLinkVersion()), "driverZipped.zip");
            UnzipFile();
        }
        private void UnzipFile()
        {
            if (File.Exists(Directory.GetCurrentDirectory()+@"\chromedriver.exe"))
            {
                File.Delete(Directory.GetCurrentDirectory()+@"\chromedriver.exe");
                if (File.Exists(Directory.GetCurrentDirectory()+@"\extracted\chromedriver.exe"))
                    File.Delete(Directory.GetCurrentDirectory() + @"\extracted\chromedriver.exe");
                ZipFile.ExtractToDirectory(Directory.GetCurrentDirectory()+ @"\driverZipped.zip"
                , Directory.GetCurrentDirectory() + @"\extracted");
                MoveDriver();
            }
        }
        private void MoveDriver()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + @"\extracted\chromedriver.exe")) {
                File.Move(Directory.GetCurrentDirectory() + @"\extracted\chromedriver.exe"
                    , Directory.GetCurrentDirectory() + @"\chromedriver.exe");
            }
        }
        private string GetLatestLinkVersion()
        {
            var edgeOptions = new EdgeOptions();
            edgeOptions.AddArguments(
                "--headless"
            );
            var edgeDriverService = EdgeDriverService.CreateDefaultService();
            edgeDriverService.HideCommandPromptWindow = true;
            var driver = new EdgeDriver(edgeDriverService, edgeOptions);
            driver.Navigate().GoToUrl("https://chromedriver.chromium.org/downloads");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            var latestLink = wait.Until(x => x.FindElements(By.XPath("//a[starts-with(text(), 'ChromeDriver ')]")));
            string href = "";
            string installedChromePath = GetChromeVersionFromPath();
            foreach (var link in latestLink)
            {
                string hre = link.GetAttribute("href");
                if (hre.Contains(installedChromePath))
                {
                    href = link.GetAttribute("href");
                }
            }
            driver.Navigate().GoToUrl(href);
            driver.Quit();
            return CreateDownloadLink(href); 
        }
        private string CreateDownloadLink(string latestVersionLink)
        {
            string baseLink = "https://chromedriver.storage.googleapis.com/";
            String regex = @"[0-9]{1,}.[0-9]{1,}.[0-9]{1,}.[0-9]{1,}";
            MatchCollection coll = Regex.Matches(latestVersionLink, regex);
            String regResult = coll[0].Groups[0].Value;
            string result = baseLink + regResult + "/chromedriver_win32.zip";
            
            return result;
        }
        private string GetChromeVersionFromPath()
        {
            string path = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            var versionInfo = FileVersionInfo.GetVersionInfo(path);
            int lastindex = versionInfo.FileVersion.LastIndexOf('.');
            string result = versionInfo.FileVersion.Substring(0, lastindex - 1);
            return result;
        }
        public void FillDatagridStats()
        {
            try
            {
                using (var context = new Data.StalkerEntities())
                {
                    var data = context.tblProducts.ToList();
                    dgvProductsForCheck.DataSource = data.Select(x => new { x.Id, x.Title }).ToList();
                    dgvProductsForCheck.Columns[0].Width = 80;
                    dgvProductsForCheck.Columns[1].Width = 550;
                    Logger.Instance.WriteDebug("Debuging");
                }
            }catch(Exception ex)
            {
                Logger.Instance.WriteError("Error loading data.",ex);
            }
        }

        private void Main_Load_1(object sender, EventArgs e)
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey600, Primary.BlueGrey900, Primary.BlueGrey700, Accent.Blue100, TextShade.WHITE);
            //dgvProducts.Columns[0].Width = 100;
            //dgvProducts.Columns[1].Width = 670;
            //dgvProducts.Columns[3].Width = 140;
            //dgvProducts.Columns[5].Width = 500;
            //dgvProductsForCheck.Columns[1].Width = 650;
            //dgvOrders.Columns[4].Width = 650;
        }

        public void RemoveOldPrices()
        {
            try
            {
                using (var context = new Data.StalkerEntities())
                {
                    var data = context.PriceHistory.ToList();
                    foreach (var d in data)
                    {
                        DateTime now = DateTime.Now;
                        DateTime priceHistory = d.Date;
                        uint totalDays = (uint)now.Subtract(priceHistory).TotalDays;
                        if (totalDays > 50)
                        {
                            var deletedRecord = context.PriceHistory.FirstOrDefault(x => x.Id == d.Id);
                            context.PriceHistory.Remove(deletedRecord);
                            context.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteError("Error removing old prices.", ex);
                throw;
            } 
        }

        [STAThread]
        private void Initialize(string title, string price, string rating, string summary)
        {
            lblProductPrice.Invoke(new Action(() => lblProductPrice.Text = price));
            lblProductTitle.Invoke(new Action(() => lblProductTitle.Text = title));
            lblProductRating.Invoke(new Action(() => lblProductRating.Text = rating));
            txtDescription.Invoke(new Action(() => txtDescription.Text = summary));
        }

        private void LoadData()
        {
            try
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                var stalkerEntities = new Data.StalkerEntities();
                var ProductQuery = from t in stalkerEntities.tblProducts
                                   select new { t.Id, t.Title, t.Price, t.Rating, t.Link, t.Description };
                dgvProducts.DataSource = ProductQuery.ToList();
                var OrderQuery = from x in stalkerEntities.Orders
                                 select new { x.Id, x.CustomerId, x.Customer.Name, x.ProductId, x.tblProducts.Title, x.Address };
                dgvOrders.DataSource = OrderQuery.ToList();
                dgvOrders.Refresh();
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteError("Error loading data from entity framework.", ex);
            }
        }

        #endregion "Initialize & load"

        #region "Insert & Checks"

        private void InsertIntoDb()
        {
            if (Check())
            {
                try
                {
                    using (var context = new Data.StalkerEntities())
                    {
                        Data.tblProducts product = new Data.tblProducts();

                        if (context.tblProducts.Any(x => x.Link == txtLink.Text))
                        {
                            System.Windows.MessageBox.Show("Record already exists or something went wrong, please try again..", "Warning", MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Warning);
                            return;
                        }
                        product.Link = txtLink.Text;
                        product.Title = lblProductTitle.Text;
                        string ignoreSign = lblProductPrice.Text.ToString().Replace("€", "").Trim();
                        decimal price = (decimal)Math.Round(decimal.Parse(ignoreSign), 2);
                        product.Price = (double)price;
                        string rating = lblProductRating.Text.Replace(".", ",");
                        decimal rate = (decimal)Math.Round(decimal.Parse(rating), 2);
                        product.Rating = (double)rate;
                        product.Description = txtDescription.Text;
                        context.tblProducts.Add(product);

                        context.SaveChanges();
                        int pid = context.tblProducts.Max(x => x.Id);

                        Data.PriceHistory priceHistory = new Data.PriceHistory()
                        {
                            PId = product.Id,
                            Price = (double)price,
                            Date = DateTime.Now
                        };
                        context.PriceHistory.Add(priceHistory);

                        context.SaveChanges();
                        new ToastContentBuilder()
                        .AddArgument("action", "viewConversation")
                        .AddArgument("conversationId", 123)
                        .AddText("Product with title " + lblProductTitle.Text + " Successfully Saved")
                        .Show();
                    }
                }
                catch (DbEntityValidationException e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message);
                    foreach (var eve in e.EntityValidationErrors)
                    {
                        //Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        //    eve.Entry.Entity.GetType().Name, eve.Entry.State);
                        foreach (var ve in eve.ValidationErrors)
                        {
                            //Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            //    ve.PropertyName, ve.ErrorMessage);
                        }
                    }
                }
            }
        }
        private bool Check()
        {
            if (lblProductPrice.Text.Length > 0 && lblProductPrice.Text.Length > 0 && lblProductRating.Text.Length > 0)
            {
                return true;
            }
            return false;
        }

        #endregion "Insert"

        private void materialFlatButton1_Click_1(object sender, EventArgs e)
        {
            InsertIntoDb();
            LoadData();
            FillDatagridStats();
        }
        

        private void dgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                DialogResult result = System.Windows.Forms.MessageBox.Show("Are you sure you wanna delete this record ?", "Delete ", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    string Link = dgvProducts.SelectedRows[0].Cells["Link"].Value.ToString();
                    var stalkerEntities = new Data.StalkerEntities();
                    var selected = stalkerEntities.tblProducts.FirstOrDefault(x => x.Link == Link);
                    stalkerEntities.tblProducts.Remove(selected);
                    stalkerEntities.SaveChanges();
                    dgvProducts.DataSource = stalkerEntities.tblProducts.ToList();
                }
            }
            catch(Exception ex)
            {
                Logger.Instance.WriteError("Error deleted data from datagridview.", ex);
            } 
        }

        private void InitBrowser(ChromeOptions options)
        {
            options.AddArguments("--disable-gpu", "log-level=3", "--ignore-certificate-errors", "window-size=1920x1080", "--disable-extensions", "no-sandbox");
            options.AddUserProfilePreference("profile.default_content_setting_values.cookies", 2);
            options.AddUserProfilePreference("profile.cookie_controls_mode", 1);
        }

        [STAThread]
        private Task SetImpression()
        {
            var tsk = Task.Run(() =>
            {
                var url = txtLink.Text;

                rtbProsImpressions.Invoke(new Action(() => rtbProsImpressions.Text = ""));
                List<string> listpros = new List<string>(), listsoso = new List<string>(), listcons = new List<string>();
                var chromeOptions = new ChromeOptions();
                //InitBrowser(chromeOptions);
                
                var chromeDriverService = ChromeDriverService.CreateDefaultService();
                chromeDriverService.HideCommandPromptWindow = true;
                var driver = new DriverHelper.ChromeDriver();
                var browser = driver.GetDriver();
                try
                {
                    //using (var browser = new ChromeDriver(chromeDriverService, chromeOptions))
                    using (browser)
                    {
                        browser.Manage().Cookies.DeleteAllCookies();
                        browser.Url = url;
                        browser.Manage().Window.Position = new System.Drawing.Point(0, -2000);
                        var wait = new WebDriverWait(browser, TimeSpan.FromSeconds(120));
                        Thread.Sleep(200);
                        var btn = wait.Until(x => x.FindElement(By.Id("accept-all")));
                        btn.Click();
                        var myElement = wait.Until(x => x.FindElements(By.XPath("//ul[contains(@class,'pros')]/li"))).ToList();
                        var chars = wait.Until(x => x.FindElements(By.XPath("//ul[@class='sku-reviews-aggregation']/li"))).ToList();
                        if (myElement != null)
                        {
                            foreach (var pros in myElement.GroupBy(x => x.Text))
                            {
                                listpros.Add(pros.Key.ToString());
                            }
                        }
                        var queryForPros = myElement.GroupBy(x => x.Text)
                          .Where(g => g.Count() > 1)
                          .ToDictionary(x => x.Key, y => y.Count());

                        List<string> testpro = new List<string>();
                        foreach (var qfp in queryForPros)
                        {
                            for (int i = 0; i < qfp.Value; i++)
                            {
                                testpro.Add(qfp.Key.ToString() + ",positive");
                            }
                        }

                        var soso = wait.Until(x => x.FindElements(By.XPath("//ul[contains(@class,'so-so')]/li"))).ToList();
                        if (soso != null)
                        {
                            foreach (var so in soso.GroupBy(x => x.Text))
                            {
                                listsoso.Add(so.Key.ToString());
                            }
                        }

                        var cons = wait.Until(x => x.FindElements(By.XPath("//ul[contains(@class,'cons')]/li"))).ToList();
                        if (cons != null)
                        {
                            foreach (var c in cons.GroupBy(x => x.Text))
                            {
                                listcons.Add(c.Key.ToString());
                            }
                        }
                        var queryForCons = cons?.GroupBy(x => x.Text)?
                          .Where(g => g.Count() > 1)
                          .ToDictionary(x => x.Key, y => y.Count());

                        List<string> testcons = new List<string>();
                        if (queryForCons != null)
                        {
                            foreach (var qfp in queryForCons)
                            {
                                for (int i = 0; i < qfp.Value; i++)
                                {
                                    testcons.Add(qfp.Key.ToString() + ",negative");
                                }
                            }
                        }

                        int? safepros = myElement == null ? 0 : myElement.Count();
                        int? safesoso = soso == null ? 0 : soso.Count();
                        int? safecons = cons == null ? 0 : cons.Count();
                        int? total = safepros - safesoso - safecons;

                        List<string> commons = myElement.Select(s1 => s1.Text).ToList().Intersect(soso.Select(s2 => s2.Text).ToList()).ToList();

                        var joinpros = String.Join(",", listpros.ToArray());
                        var joinsoso = String.Join(",", listsoso.ToArray());
                        var joincons = String.Join(",", listcons.ToArray());
                        string common = "";
                        if (commons.Count > 0)
                        {
                            foreach (var k in commons)
                            {
                                common += k + ",";
                            }
                            common = common.Remove(common.Length - 1);
                        }

                        GenerateRatingText(joinpros, rtbProsImpressions);
                        GenerateRatingText(joinsoso, rtbSoso);
                        GenerateRatingText(joincons, rtbCons);

                        List<int> ListLengths = new List<int>();

                        InsertLengths(safepros.Value, ListLengths);
                        InsertLengths(safesoso.Value, ListLengths);
                        InsertLengths(safecons.Value, ListLengths);

                        List<string> Labels = new List<string>();
                        InsertLabels(safepros.Value, Labels, "Θετικά");
                        InsertLabels(safesoso.Value, Labels, "Ετσι και έτσι");
                        InsertLabels(safecons.Value, Labels, "Αρνητικά");

                        List<IWebElement> e = new List<IWebElement>();
                        e.AddRange(wait.Until(x => x.FindElements(By.XPath("//*[text()='To προϊόν δεν υπάρχει πλέον στο Skroutz']"))));
                        string rtg = "";
                        if (e.Count < 1)
                        {
                            var title = wait?.Until(x => x.FindElement(By.XPath("//h1[@class='page-title']"))).Text ?? "Title Not Found";
                            var prices = wait?.Until(x => x.FindElements(By.ClassName("dominant-price")))?.FirstOrDefault();
                            var defaultprices = wait?.Until(x => x.FindElements(By.XPath("//span[@class='default']/span/strong")))?.FirstOrDefault();
                            List<IWebElement> elementList = new List<IWebElement>();
                            elementList.AddRange(browser.FindElements(By.XPath("//span[@itemprop='ratingValue']")));
                            if (elementList.Count() > 0)
                            {
                                rtg = wait?.Until(x => x.FindElement(By.XPath("//span[@itemprop='ratingValue']"))).Text;
                            }
                            else
                            {
                                rtg = "0";
                            }
                            //var rating = wait?.Until(x => x.FindElement(By.XPath("//span[@itemprop='ratingValue']"))).Text ;
                            //https://www.skroutz.gr/s/37875447/Adidas-Adicolor-Classics-3-Stripes-%CE%91%CE%BD%CE%B4%CF%81%CE%B9%CE%BA%CF%8C-%CE%A6%CE%BF%CF%8D%CF%84%CE%B5%CF%81-Shadow-Maroon-HK7291.html?from=timeline
                            var summary = wait?.Until(x => x.FindElement(By.XPath("//div[contains(@class,'summary')]")))?.Text;
                            if (title != null && prices != null)
                            {
                                Initialize(title.ToString(), prices.Text, rtg, summary);
                            }
                            FillStatsChart(ListLengths, Labels);
                        }
                        browser.Close();
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                    Logger.Instance.WriteError("Error from setimpression method.", ex);
                }
            });
            return tsk;
        }
        private void InsertLengths(int? size, List<int> ListLengths)
        {
            if (size.Value != 0)
            {
                ListLengths.Add((int)size);
            }
        }
        private void InsertLabels(int? size, List<string> ListLabels,string value)
        {
            if (size.Value != 0)
            {
                ListLabels.Add(value);
            }
        }
        private void GenerateRatingText(string lst ,RichTextBox rtb)
        {
            if (lst.Length > 0)
            {
                rtb.Invoke(new Action(() => rtb.Text += lst.ToString() + "\n"));
                rtb.Invoke(new Action(() => rtb.Text += "\n"));
            }
        }
        private async void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            try
            {
                //RemoveOldPrices();
                if (txtLink.Text.StartsWith("https://www.skroutz.gr/"))
                {
                    pictureBox1.Visible = false;
                    cartesianChart1.Visibility = Visibility.Visible;
                    await SetImpression();
                }
                else if (txtLink.Text.StartsWith("https://www.bestprice.gr/"))
                {
                    cartesianChart1.Visibility = Visibility.Hidden;
                    pictureBox1.Visible = true;
                    await ScrapeBestPrice();
                }
                Logger.Instance.WriteDebug("Error from scrape button.");
            }
            catch (System.NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
                Logger.Instance.WriteError("Error from scrape button.", ex);
            }
        }

        public void FillStatsChart(List<int> lst, List<string> Labels)
        {
            Func<ChartPoint, string> labelPoint = chartPoint =>
            string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);
            List<int> array = lst;
            SeriesCollection series = new SeriesCollection();
            List<Brush> listBrush = new List<Brush>();
            foreach (var lbl in Labels)
            {
                if (lbl == "Θετικά")
                {
                    listBrush.Add(Brushes.ForestGreen);
                }
                else if (lbl == "Ετσι και έτσι")
                {
                    listBrush.Add(Brushes.Gold);
                }
                else
                {
                    listBrush.Add(Brushes.DarkRed);
                }
            }
            for (int i = 0; i < array.Count; i++)
            {
                pieChart1.Invoke(new Action(() =>
                    series.Add(new PieSeries
                    {
                        Title = Labels[i].ToString(),
                        Foreground = Brushes.Black,
                        Values = new ChartValues<double> { array[i] },
                        DataLabels = true,
                        LabelPoint = labelPoint,
                        Fill = listBrush[i]
                    })));
            }
            pieChart1.Invoke(new Action(() => pieChart1.Series = series));
            pieChart1.Invoke(new Action(() => pieChart1.LegendLocation = LegendLocation.Bottom));
        }

        #region "Scrape"

        private Task ScrapeBestPrice()
        {
            var tsk = Task.Run(() =>
            {
                try
                {
                    var web = new HtmlWeb();
                    var doc = web.Load(txtLink.Text);
                    var title = doc?.DocumentNode?.SelectSingleNode("//div[@class='hgroup']/h1")?.InnerText;
                    var prices = doc?.DocumentNode?.SelectNodes("//div[@class='prices__price']/a")?.ToList();
                    string price = prices?.FirstOrDefault().InnerText.ToString();
                    var rating = doc?.DocumentNode?.SelectSingleNode("//div[contains(@class,'sc-bczRLJ bUJLMc')]/span")?.InnerText; //actual-rating
                    var picture = doc.DocumentNode.SelectSingleNode("//img[@itemprop='image']").Attributes["src"].Value;
                    var summary = doc?.DocumentNode?.SelectNodes("//div[contains(@class,'item-header__specs-list')]/ul/li")?.ToList(); //summary
                    string description = "";
                    WebClient wc = new WebClient();
                    rtbCons.Invoke(new Action(()=>rtbCons.Clear()));
                    rtbProsImpressions.Invoke(new Action(() =>rtbProsImpressions.Clear()));
                    rtbSoso.Invoke(new Action(() =>rtbSoso.Clear()));
                    if (picture != null)
                    {
                        if (!picture.Contains(".webp"))
                        {
                            string prefix = "";
                            if (!picture.StartsWith("https:"))
                            {
                                prefix = "https:";
                            }
                            byte[] bytes = wc.DownloadData(prefix + picture);
                            MemoryStream ms = new MemoryStream(bytes);
                            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                            pictureBox1.Invoke(new Action(() => pictureBox1.Image = img));
                        }
                    }
                    if (summary.Count > 0)
                    {
                        foreach (var s in summary)
                        {
                            description += s.InnerText + "\n";
                        }
                    }
                    int safeRate = 0;
                    if (rating != null)
                    {
                        safeRate = Int32.Parse(rating.ToString());
                    }
                    else
                    {
                        safeRate = 0;
                    }

                    if (title != null && price != null)
                    {
                        Initialize(title.ToString(), price.ToString(), safeRate.ToString(), description);
                    }
                }
                catch (System.NullReferenceException ex)
                {
                    MessageBox.Show(ex.Message);
                    Logger.Instance.WriteError("Error from scraping bestprice proccess.", ex);
                }
            });
            return tsk;
        }

        #endregion "Scrape"

        private async void materialRaisedButton2_Click(object sender, EventArgs e) => await GetPriceHistoryInfo();
        #region
        public Task GetPriceHistoryInfo()
        {
            var tsk = Task.Run(() =>
            {
                try
                {
                    using (var context = new Data.StalkerEntities())
                    {
                        var data = context.tblProducts.ToList().Select(i => new { i.Link, i.Id, i.Price, i.Rating, i.Title }).ToList();
                        materialRaisedButton2.Invoke(new Action(() => materialRaisedButton2.Enabled = false));
                        var chromeOptions = new ChromeOptions();
                        InitBrowser(chromeOptions);
                        var chromeDriverService = ChromeDriverService.CreateDefaultService();
                        chromeDriverService.HideCommandPromptWindow = true;

                        using (var browser = new ChromeDriver(chromeDriverService, chromeOptions))
                        {
                            foreach (var link in data)
                            {
                                browser.Url = link.Link;
                                Thread.Sleep(100);
                                var wait = new WebDriverWait(browser, TimeSpan.FromSeconds(60));
                                browser.Manage().Window.Position = new System.Drawing.Point(0, -2000);
                                var mprices = wait.Until(x => x.FindElements(By.XPath("//strong[@class='dominant-price']"))).FirstOrDefault();
                                var prices = wait?.Until(x => x.FindElements(By.XPath("//span[@class='default']/span/strong")))?.FirstOrDefault();
                                Application.DoEvents();
                                var bestpprices = wait.Until(x => x.FindElements(By.XPath("//div[@class='prices__price']/a"))).FirstOrDefault();
                                string bestpprice = bestpprices == null ? "" : bestpprices?.Text?.ToString().Replace("€", "");
                                string newskroutzprice = prices == null ? "" : prices?.Text?.ToString().Replace("€", "");
                                var testlink = link.Id;
                                decimal? saveprice = link == null ? 0 : (decimal)Math.Round(decimal.Parse(link.Price.ToString()), 2);

                                var joinprice = context.PriceHistory.Select(i => new { i.PId, i.Price, i.Date }).Where(x => x.PId == link.Id).OrderByDescending(x => x.Date).FirstOrDefault();
                                if (joinprice != null)
                                {
                                    decimal compPrice = 0;
                                    if (newskroutzprice != "")
                                    {
                                        compPrice = (decimal)Math.Round(decimal.Parse(newskroutzprice.ToString()), 2);
                                    }
                                    else if (bestpprice != "")
                                    {
                                        compPrice = (decimal)Math.Round(decimal.Parse(bestpprice.ToString()), 2);
                                    }
                                    CheckPrices(link.Title, testlink, compPrice, (decimal)joinprice.Price);
                                    context.SaveChanges();
                                }
                            }
                        }
                        IsButtonHandled();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Logger.Instance.WriteError("Error from checking new prices.", ex);
                }
            });
            return tsk;
        }
        LiveCharts.Wpf.ColumnSeries[] columnSeries;
        private Task LoadGraph()
        {
            var tsk = Task.Run(() =>
            {
                try
                {
                    if (dgvProductsForCheck.SelectedRows.Count > 0)
                    {
                        int id = Int32.Parse(dgvProductsForCheck.SelectedRows[0].Cells["Id"].Value.ToString());
                        dgvProductsForCheck.Invoke(new Action(() => cartesianChart1.Series.Clear()));
                        using (var context = new Data.StalkerEntities())
                        {
                            var data = context.PriceHistory.Where(x => x.PId == id).OrderByDescending(x => x.Date).ToList();
                            int counter = 0;
                            columnSeries = new ColumnSeries[data.Count];
                            foreach (var item in data.Take(10).OrderBy(x=>x.Date))
                            {
                                double[] ys2 = { item.Price };
                                dgvProductsForCheck.Invoke(new Action(() =>
                                    columnSeries[counter] = new LiveCharts.Wpf.ColumnSeries()
                                    {
                                        Title = item.Date.ToString(),
                                        DataLabels = true,
                                        VerticalAlignment = VerticalAlignment.Stretch,
                                        Margin = new Thickness(6, 6,6, 6),
                                        PointGeometry = DefaultGeometries.Circle,
                                        Values = new LiveCharts.ChartValues<double>(ys2),
                                    }
                                ));
                                dgvProductsForCheck.Invoke(new Action(() => cartesianChart1.LegendLocation = LegendLocation.Right));
                                dgvProductsForCheck.Invoke(new Action(() => cartesianChart1.FontStretch = new FontStretch()));
                                dgvProductsForCheck.Invoke(new Action(() => cartesianChart1.Series.Add(columnSeries[counter])));
                                counter++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Logger.Instance.WriteError("Error from loading statistic graph.", ex);
                }
            });
            return tsk;
        }
        #endregion

        private void IsButtonHandled()
        {
            if (materialRaisedButton2.IsHandleCreated)
            {
                materialRaisedButton2.Invoke(new Action(() => materialRaisedButton2.Enabled = true));
            }
            else
            {
                materialRaisedButton2.Invoke(new Action(() => materialRaisedButton2.Enabled = false));
            }
        }
        private void CheckPrices(string title, int pid, decimal newprice, decimal oldprice)
        {
            if (newprice != oldprice)
            {
                float deservedDifference = (float)Math.Abs(newprice - oldprice);
                if (deservedDifference >= double.Parse( differenceValue.Value.ToString()))//η διαφορά της προηγούμενης τιμής με την νέα 
                {
                    using (var context = new Data.StalkerEntities())
                    {
                        if (newprice > 0)
                        {
                            Data.PriceHistory priceHistory = new Data.PriceHistory()
                            {
                                PId = pid,
                                Price = (double)Math.Round(newprice, 2),
                                Date = DateTime.Now
                            };
                            context.PriceHistory.Add(priceHistory);
                            Data.tblProducts updProduct = context.tblProducts.Where(x => x.Id == pid).FirstOrDefault();
                            updProduct.Price = (double)Math.Round(newprice, 2);
                            context.SaveChanges();
                            LoadData();
                            new ToastContentBuilder()
                            .AddArgument("action", "viewConversation")
                                .AddArgument("conversationId", 123)
                                .AddText(title + " price has changed from " + oldprice.ToString() + "€ to " + newprice.ToString() + "€")
                                .Show();
                        }
                    }
                }
            }
        }

        private void Order_FormClosed(object sender, FormClosedEventArgs e) => LoadData();
        private void dgvProducts_CellClick(object sender, DataGridViewCellEventArgs e){ }
        private void btnCompare_Click(object sender, EventArgs e) => ComparePrices();

        #region "Price Compare"

        private async void ComparePrices()
        {
            if (lblProductTitle.Text != null)
            {
                if (txtLink.Text.StartsWith("https://www.bestprice.gr/"))
                {
                    pieChart1.Visible = false;
                    await ComparePriceWithSkroutzPrice();
                }
                else if (txtLink.Text.StartsWith("https://www.skroutz.gr/"))
                {
                    pieChart1.Visible = true;
                    await ComparePriceWithBestPrice();
                }
            }
        }
        //Να κανω ελεγχο για να ξερω πως να ξεχωριζει τις αναζητησεις που γινονται με λιστα προιοντων η κατευθειαν στο προφιλ του προιοντος
        private Task ComparePriceWithBestPrice()
        {
            var tsk = Task.Run(() =>
            {
                if (lblProductTitle.Text.Length > 0)
                {
                    lblCompare.Invoke(new Action(() => lblCompare.Text = "Price loading.."));
                    var chromeOptions = new ChromeOptions();
                    //chromeOptions.AddArguments(
                    //   "--disable-gpu", "--disable-extensions", "--start-minimized", "--headless", "--no-sandbox"
                    //);
                    var chromeDriverService = ChromeDriverService.CreateDefaultService();
                    chromeDriverService.HideCommandPromptWindow = true;
                    ChromeDriver browser = new ChromeDriver(chromeDriverService, chromeOptions);
                    using (var driver = browser)
                    {
                        driver.Navigate().GoToUrl(@"https://www.bestprice.gr/");
                        try
                        {
                            Thread.Sleep(250);
                            WebElement form = (WebElement)driver.FindElement(By.Id("search-form"));
                            browser.Manage().Window.Position = new System.Drawing.Point(0, -2000);
                            form.FindElement(By.Name("q")).SendKeys(lblProductTitle.Text);
                            form.Submit();

                            Thread.Sleep(1000);
                            List<IWebElement> e = new List<IWebElement>();
                            e.AddRange(browser.FindElements(By.Id("full-price-container")));
                            if (e.Count > 0)
                            {
                                Regex re = new Regex(@"[0-9]{1,},[0-9]{0,2}€");

                                var resultsPrices = driver?.FindElements(By.ClassName("prices__price"));
                                var searchTitle = driver?.FindElements(By.ClassName("product__title"));
                                var priceOnConteiner = driver?.FindElements(By.ClassName("product__cost-price")).FirstOrDefault();
                                int index = 0, counter = 0;
                                double max = 0;
                                foreach (var t in searchTitle)
                                {
                                    if (CompareStrings(lblProductTitle.Text, t.Text.ToString()) > max)
                                    {
                                        max = CompareStrings(lblProductTitle.Text, t.Text.ToString());
                                        index = counter;
                                    }
                                    counter++;
                                }
                                if (priceOnConteiner.Text != string.Empty || priceOnConteiner.Text=="0")
                                {
                                    var searchResult = resultsPrices.FirstOrDefault();
                                    if (resultsPrices != null)
                                    {
                                        if (re.IsMatch(resultsPrices.FirstOrDefault().Text))
                                        {
                                            MatchCollection matchedAuthors = re.Matches(resultsPrices.FirstOrDefault().Text);
                                            lblCompare.Invoke(new Action(() => lblCompare.Text = matchedAuthors[0].Value.ToString()));
                                        }
                                    }
                                }
                                else
                                {
                                    lblCompare.Invoke(new Action(() => lblCompare.Text = "Cannot be found..."));
                                }
                            }
                            else
                            {
                                Regex re = new Regex(@"[0-9]{1,},[0-9]{0,2}€");

                                var resultsPrices = driver?.FindElements(By.ClassName("p__price--current")).FirstOrDefault();
                                var searchTitle = driver?.FindElements(By.ClassName("p__title"));//
                                var priceOnConteiner = driver?.FindElements(By.ClassName("product__cost-price")).FirstOrDefault();
                                int index = 0, counter = 0;
                                double max = 0;
                                foreach (var t in searchTitle)
                                {
                                    if (CompareStrings(lblProductTitle.Text, t.Text.ToString()) > max)
                                    {
                                        max = CompareStrings(lblProductTitle.Text, t.Text.ToString());
                                        index = counter;
                                    }
                                    counter++;
                                }
                                if (resultsPrices.Text != string.Empty)
                                {
                                    if (resultsPrices != null)
                                    {
                                        //if (re.IsMatch(priceOnConteiner.Text))
                                        //{
                                            MatchCollection matchedAuthors = re.Matches(resultsPrices.Text);
                                            lblCompare.Invoke(new Action(() => lblCompare.Text = matchedAuthors[0].Value.ToString()));
                                        //}
                                    }
                                }
                                else
                                {
                                    lblCompare.Invoke(new Action(() => lblCompare.Text = "Cannot be found..."));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            Logger.Instance.WriteError("Error from price compare with bestprice.", ex);
                        }
                        finally
                        {
                            driver.Close();
                        }
                    }
                }
            });
            return tsk;
        }

        private Task ComparePriceWithSkroutzPrice()
        {
            var tsk = Task.Run(() =>
            {
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddArgument("--start-minimized");
                var chromeDriverService = ChromeDriverService.CreateDefaultService();
                chromeDriverService.HideCommandPromptWindow = true;

                ChromeDriver driver = new ChromeDriver(chromeDriverService, chromeOptions);
                try
                {
                    driver.Navigate().GoToUrl(@"https://www.skroutz.gr/");
                    WebElement form = (WebElement)driver.FindElement(By.ClassName("search-bar-input-wrapper"));
                    driver.Manage().Window.Position = new System.Drawing.Point(0, -2000);
                    form.FindElement(By.Id("search-bar-input")).SendKeys(lblProductTitle.Text);
                    form.Submit();

                    var searchResults = driver.FindElements(By.XPath("//section[@class='main-content']/ol/li")).FirstOrDefault();
                    IList<IWebElement> elements = driver.FindElements(By.XPath("//a[starts-with(@data-e2e-testid, 'sku-price-link')]"));
                    Regex re = new Regex(@"[0-9]{1,},[0-9]{0,2} €");

                    string val = elements.FirstOrDefault().Text;
                    //string numberOnly = Regex.Replace(val, "[^0-9.,€]", "");
                    string numberOnly = Regex.Replace(val, "από", "").Replace("απο", "");
                    if (elements != null)
                    {
                        if (re.IsMatch(numberOnly))
                        {
                            MatchCollection matchedAuthors = re.Matches(numberOnly);
                            lblCompare.Invoke(new Action(() => lblCompare.Text = matchedAuthors[0].Value.ToString()));
                        }
                    }
                    else
                    {
                        lblCompare.Invoke(new Action(() => lblCompare.Text = "Cannot be found..."));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Logger.Instance.WriteError("Error from compare skroutz price.", ex);
                }
                finally
                {
                    driver.Close();
                }
            });
            return tsk;
        }

        #endregion "Price Compare"

        #region "String Comparison"
        public static double CompareStrings(string str1, string str2)
        {
            List<string> pairs1 = WordLetterPairs(str1.ToUpper());
            List<string> pairs2 = WordLetterPairs(str2.ToUpper());

            int intersection = 0;
            int union = pairs1.Count + pairs2.Count;

            for (int i = 0; i < pairs1.Count; i++)
            {
                for (int j = 0; j < pairs2.Count; j++)
                {
                    if (pairs1[i] == pairs2[j])
                    {
                        intersection++;
                        pairs2.RemoveAt(j);
                        break;
                    }
                }
            }
            return (2.0 * intersection * 100) / union; 
        }
        private static List<string> WordLetterPairs(string str)
        {
            List<string> AllPairs = new List<string>();
            string[] Words = Regex.Split(str, @"\s");
            for (int w = 0; w < Words.Length; w++)
            {
                if (!string.IsNullOrEmpty(Words[w]))
                {
                    String[] PairsInWord = LetterPairs(Words[w]);

                    for (int p = 0; p < PairsInWord.Length; p++)
                    {
                        AllPairs.Add(PairsInWord[p]);
                    }
                }
            }
            return AllPairs;
        }
        private static string[] LetterPairs(string str)
        {
            int numPairs = str.Length - 1;
            string[] pairs = new string[numPairs];

            for (int i = 0; i < numPairs; i++)
            {
                pairs[i] = str.Substring(i, 2);
            }
            return pairs;
        }

        #endregion "String Comparison"

        private async void btnLoad_Click(object sender, EventArgs e)=> await LoadGraph();
        
        private void button2_Click_1(object sender, EventArgs e)
        {
            Order order = new Order();
            order.ShowDialog();
            order.FormClosed += Order_FormClosed;
        }

        private void materialRaisedButton3_Click(object sender, EventArgs e)
        {
            ExportToPng();
        }
        private void ExportToPng()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                sfd.Title = "Save chart to image";
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)cartesianChart1.ActualWidth, (int)cartesianChart1.ActualHeight);
                elementHost1.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(System.IO.Path.GetFullPath(sfd.FileName), System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void materialRaisedButton4_Click(object sender, EventArgs e)
        {
            if (dgvProductsForCheck.SelectedRows.Count > 0)
            {
                int id = Int32.Parse(dgvProductsForCheck.SelectedRows[0].Cells["Id"].Value.ToString());
                dgvProductsForCheck.Invoke(new Action(() => cartesianChart1.Series.Clear()));
                using (var context = new Data.StalkerEntities())
                {
                    var data = context.PriceHistory.Where(x => x.PId == id).OrderByDescending(x => x.Date).ToList();
                    var jsonresult = JsonConvert.SerializeObject(data);
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.DefaultExt = ".json";
                    sfd.Title = "Save json file";
                    sfd.InitialDirectory = "C:\\";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        System.IO.File.WriteAllText(System.IO.Path.GetFullPath(sfd.FileName), jsonresult);
                    }
                }
            }
        }
    }
    public class ComboboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }
        public override string ToString() => Text;
    }
}