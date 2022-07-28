using HtmlAgilityPack;
using LiveCharts;
using LiveCharts.Wpf;
using MaterialSkin;
using MaterialSkin.Controls;
using Microsoft.Toolkit.Uwp.Notifications;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;

namespace PriceStalkerScrape
{
    public partial class Main : MaterialForm
    {
        public Main()
        {
            InitializeComponent();
            LoadData();
            FillDatagridStats();
        }

        #region "Initialize & load"

        public void FillDatagridStats()
        {
            using (var context = new Data.StalkerEntities())
            {
                var data = context.tblProducts.ToList();
                dgvProductsForCheck.DataSource = data.Select(x => new { x.Id, x.Title }).ToList();
                dgvProductsForCheck.Columns[0].Width = 80;
                dgvProductsForCheck.Columns[1].Width = 550;
            }
        }

        private void Main_Load_1(object sender, EventArgs e)
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey600, Primary.BlueGrey900, Primary.BlueGrey700, Accent.Blue100, TextShade.WHITE);
            dgvProducts.Columns[0].Width = 100;
            dgvProducts.Columns[1].Width = 670;
            dgvProducts.Columns[3].Width = 140;
            dgvProducts.Columns[5].Width = 500;
            dgvProductsForCheck.Columns[1].Width = 650;
            dgvOrders.Columns[4].Width = 650;
        }

        public void RemoveOldPrices()
        {
            using (var context = new Data.StalkerEntities())
            {
                var data = context.PriceHistory.ToList();
                foreach (var d in data)
                {
                    DateTime now = DateTime.Now;
                    DateTime priceHistory = d.Date;
                    uint totalDays = (uint)now.Subtract(priceHistory).TotalDays;
                    if (totalDays > 54)
                    {
                        var deletedRecord = context.PriceHistory.FirstOrDefault(x => x.Id == d.Id);
                        context.PriceHistory.Remove(deletedRecord);
                        context.SaveChanges();
                    }
                }
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
            var stalkerEntities = new Data.StalkerEntities();
            var ProductQuery = from t in stalkerEntities.tblProducts
                               select new { t.Id, t.Title, t.Price, t.Rating, t.Link, t.Description };
            dgvProducts.DataSource = ProductQuery.ToList();
            var OrderQuery = from x in stalkerEntities.Orders
                             select new { x.Id, x.CustomerId, x.Customer.Name, x.ProductId, x.tblProducts.Title, x.Address };
            dgvOrders.DataSource = OrderQuery.ToList();
            dgvOrders.Update();
            dgvOrders.Refresh();
        }

        #endregion "Initialize & load"

        #region "Insert"

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
                    throw;
                }
            }
        }

        #endregion "Insert"

        private bool Check()
        {
            if (lblProductPrice.Text.Length > 0 && lblProductPrice.Text.Length > 0 && lblProductRating.Text.Length > 0)
            {
                return true;
            }
            return false;
        }

        private void materialFlatButton1_Click_1(object sender, EventArgs e)
        {
            InsertIntoDb();
            LoadData();
            FillDatagridStats();
        }

        private void dgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
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

        private void InitBrowser(ChromeOptions options)
        {
            options.AddArgument("--disable-gpu");
            options.AddArgument("log-level=3");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("window-size=1920x1080");
            options.AddArgument("--disable-extensions");
            options.AddArgument("no-sandbox");
            options.AddUserProfilePreference("profile.default_content_setting_values.cookies", 2);
            options.AddUserProfilePreference("profile.cookie_controls_mode", 1);
        }

        [STAThread]
        private Task SetImpression()
        {
            var tsk = Task.Run(() =>
            {
                var url = txtLink.Text;
                var web = new HtmlWeb();
                var doc = web.Load(url);
                rtbImpressions.Invoke(new Action(() => rtbImpressions.Text = ""));
                List<string> listpros = new List<string>(), listsoso = new List<string>(), listcons = new List<string>();
                var chromeOptions = new ChromeOptions();
                InitBrowser(chromeOptions);
                
                var chromeDriverService = ChromeDriverService.CreateDefaultService();
                chromeDriverService.HideCommandPromptWindow = true;
                try
                {
                    using (var browser = new ChromeDriver(chromeDriverService, chromeOptions))
                    {
                        browser.Manage().Cookies.DeleteAllCookies();
                        browser.Url = url;
                        browser.Manage().Window.Position = new System.Drawing.Point(0, -2000);
                        var wait = new WebDriverWait(browser, TimeSpan.FromSeconds(120));
                        Thread.Sleep(500);
                        var btn = wait.Until(x => x.FindElement(By.Id("accept-all")));
                        btn.Click();
                        var myElement = wait.Until(x => x.FindElements(By.XPath("//ul[contains(@class,'pros')]/li"))).ToList();
                        var chars = wait.Until(x=> x.FindElements(By.XPath("//ul[@class='sku-reviews-aggregation']/li"))).ToList();
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
  
                        GenerateRatingText(joinpros, "+");
                        GenerateRatingText(joinsoso, "^");
                        GenerateRatingText(joincons, "-");
                        GenerateRatingText(common, "+-");

                        var countPositives = doc?.DocumentNode?.SelectNodes("//ul[contains(@class,'pros')]")?.ToList();
                        var countSoso = doc?.DocumentNode?.SelectNodes("//ul[contains(@class,'so-so')]")?.ToList();
                        var countNegatives = doc?.DocumentNode?.SelectNodes("//ul[contains(@class,'cons')]")?.ToList();
                        List<int> ListLengths = new List<int>();
                        
                        InsertLengths(safepros.Value,ListLengths);
                        InsertLengths(safesoso.Value, ListLengths);
                        InsertLengths(safecons.Value, ListLengths);

                        List<string> Labels = new List<string>();
                        InsertLabels(safepros.Value, Labels, "Θετικά");
                        InsertLabels(safesoso.Value, Labels, "Ετσι και έτσι");
                        InsertLabels(safecons.Value, Labels, "Αρνητικά");

                        List<IWebElement> e = new List<IWebElement>();
                        e.AddRange(wait.Until(x => x.FindElements(By.XPath("//*[text()='To προϊόν δεν υπάρχει πλέον στο Skroutz']"))));

                        if (e.Count < 1)
                        {
                            Thread.Sleep(250);
                            var title = wait?.Until(x => x.FindElement(By.XPath("//h1[@class='page-title']")));
                            var prices = wait?.Until(x => x.FindElements(By.ClassName("dominant-price")))?.FirstOrDefault();
                            var defaultprices = wait?.Until(x => x.FindElements(By.XPath("//span[@class='default']/span/strong")))?.FirstOrDefault();

                            var rating = wait?.Until(x => x.FindElement(By.XPath("//span[@itemprop='ratingValue']")))?.Text;
                            var summary = wait?.Until(x => x.FindElement(By.XPath("//div[contains(@class,'summary')]")))?.Text;
                            if (title != null && prices != null)
                            {
                                Initialize(title.Text.ToString(), defaultprices.Text.ToString(), rating.ToString(), summary);
                            }
                            if (common.Length > 0)
                            {
                                FillStatsChart(ListLengths, Labels);
                            }
                        }
                        browser.Close();
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
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
        private void GenerateRatingText(string lst , string RatingOperator)
        {
            if (lst.Length > 0)
            {
                rtbImpressions.Invoke(new Action(() => rtbImpressions.Text += RatingOperator + lst.ToString() + "\n"));
                rtbImpressions.Invoke(new Action(() => rtbImpressions.Text += "\n"));
            }
        }
        private async void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            try
            {
                RemoveOldPrices();
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
            }
            catch (System.NullReferenceException ex)
            {
                MessageBox.Show(ex.Message);
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
                    //var summary = doc?.DocumentNode?.SelectNodes("//div[contains(@class,'simple-description')]/ul/li").ToList();
                    var summary = doc?.DocumentNode?.SelectNodes("//div[contains(@class,'item-header__specs-list')]/ul/li")?.ToList(); //summary
                    string description = "";
                    WebClient wc = new WebClient();

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
                }
            });
            return tsk;
        }

        #endregion "Scrape"

        private async void materialRaisedButton2_Click(object sender, EventArgs e) => await GetPriceHistoryInfo();
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
                                Thread.Sleep(1500);
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
                                    Data.tblProducts updProduct = context.tblProducts.Where(x => x.Id == link.Id).FirstOrDefault();
                                    updProduct.Id = link.Id;
                                    updProduct.Price = (double)compPrice;
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
                }
            });  
            return tsk;
        }
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
                if (deservedDifference >= 4)
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
                            context.SaveChanges();
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

        #region "Price Comparators"

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
                    chromeOptions.AddArguments(
                       "--disable-gpu", "--disable-extensions", "--start-minimized", "--headless", "--no-sandbox"
                    );
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

                            Thread.Sleep(1250);
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
                                if (priceOnConteiner.Text != string.Empty)
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

                                var resultsPrices = driver?.FindElements(By.ClassName("prices__price"));
                                var searchTitle = driver?.FindElements(By.ClassName("product__title"));//
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
                                if (priceOnConteiner.Text != string.Empty)
                                {
                                    var searchResult = resultsPrices.FirstOrDefault();
                                    if (resultsPrices != null)
                                    {
                                        if (re.IsMatch(priceOnConteiner.Text))
                                        {
                                            MatchCollection matchedAuthors = re.Matches(priceOnConteiner.Text);
                                            lblCompare.Invoke(new Action(() => lblCompare.Text = matchedAuthors[0].Value.ToString()));
                                        }
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
                }
                finally
                {
                    driver.Close();
                }
            });
            return tsk;
        }

        #endregion "Price Comparators"

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
        private Task LoadGraph()
        {
            var tsk = Task.Run(() =>
            {
                try
                {
                    if (dgvProductsForCheck.SelectedRows.Count > 0)
                    {
                        int id = Int32.Parse(dgvProductsForCheck.SelectedRows[0].Cells["Id"].Value.ToString());
                        dgvProductsForCheck.Invoke(new Action(()=> cartesianChart1.Series.Clear()));
                        using (var context = new Data.StalkerEntities())
                        {
                            var data = context.PriceHistory.Where(x => x.PId == id).OrderBy(x => x.Date).ToList();
                            int counter = 0;
                            LiveCharts.Wpf.ColumnSeries[] columnSeries = new ColumnSeries[data.Count];
                            foreach (var item in data)
                            {
                                double[] ys2 = { item.Price };
                                dgvProductsForCheck.Invoke(new Action(() =>
                                    columnSeries[counter] = new LiveCharts.Wpf.ColumnSeries()
                                    {
                                        Title = item.Date.ToString(),
                                        DataLabels = true,
                                        ColumnPadding = 15,
                                        VerticalAlignment = VerticalAlignment.Stretch,
                                        Margin = new Thickness(10, 10, 10, 10),
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
                }
            });
            return tsk;
        }
        private void button2_Click_1(object sender, EventArgs e)
        {
            Order order = new Order();
            order.ShowDialog();
            order.FormClosed += Order_FormClosed;
        }
    }
    public class ComboboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }
        public override string ToString() => Text;
    }
}