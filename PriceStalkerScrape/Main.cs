using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Validation;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HtmlAgilityPack;
using LiveCharts;
using LiveCharts.Wpf;
using MaterialSkin;
using MaterialSkin.Controls;
using Microsoft.Toolkit.Uwp.Notifications;
using Application = System.Windows.Forms.Application;
using Color = System.Windows.Media.Color;

namespace PriceStalkerScrape
{
    public partial class Main : MaterialForm
    {
        public Main()
        {
            InitializeComponent();
            //LoadData();
            //FillComboBox();
            SetImpression();
        }
        private void Initialize(string title, string price, string rating, string summary)
        {
            lblProductPrice.Text = price;
            lblProductTitle.Text = title;
            lblProductRating.Text = rating;
            txtDescription.Text = summary;
        }
        private void LoadData()
        {
            var stalkerEntities = new Data.StalkerEntities();
            //stalkerEntities.Configuration.ProxyCreationEnabled = false;
            var ProductQuery =from t in stalkerEntities.tblProducts
                        select new { t.Id, t.Title, t.Price, t.Rating,t.Link,t.Description };
            dgvProducts.DataSource = ProductQuery.ToList();
            var OrderQuery = from x in stalkerEntities.Orders
                               select new { x.Id, x.CustomerId, x.Customer.Name, x.ProductId, x.tblProducts.Title, x.Address };
            dgvOrders.DataSource = OrderQuery.ToList();
        }
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
                            System.Windows.MessageBox.Show("Something went wrong, please try again..", "Warning", MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Warning);
                            return;
                        }
                        product.Link = txtLink.Text;
                        product.Title = lblProductTitle.Text;
                        string ignoreSign = lblProductPrice.Text.ToString().Replace("€", "").Trim();
                        float price = (float)Math.Round(float.Parse(ignoreSign), 2);
                        product.Price = price;
                        string rating = lblProductRating.Text.Replace(".", ",");
                        float rate = (float)Math.Round(float.Parse(rating), 2);
                        product.Rating = rate;
                        product.Description = txtDescription.Text;
                        context.tblProducts.Add(product);

                        context.SaveChanges();
                        int pid = context.tblProducts.Max(x => x.Id);
                        Data.PriceHistory priceHistory = new Data.PriceHistory()
                        {
                            PId = product.Id,
                            Price = price,
                            Date = DateTime.Now
                        };
                        //priceHistory.PId = pid;
                        //priceHistory.Price = float.Parse(ignoreSign);
                        //priceHistory.Date = DateTime.Now;
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
                        Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                            eve.Entry.Entity.GetType().Name, eve.Entry.State);
                        foreach (var ve in eve.ValidationErrors)
                        {
                            Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                                ve.PropertyName, ve.ErrorMessage);
                        }
                    }
                    throw;
                }
            }  
        }
        private bool Check()
        {
            if(lblProductPrice.Text.Length>0 && lblProductPrice.Text.Length>0&& lblProductRating.Text.Length > 0)
            {
                return true;
            }
            return false;
        }
        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            
        }
        private void Main_Load_1(object sender, EventArgs e)
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);   
        }

        private void btnScrape_Click(object sender, EventArgs e){}

        private void materialFlatButton1_Click_1(object sender, EventArgs e)
        {
            InsertIntoDb();
            LoadData();
            FillComboBox();
        }

        private void dgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DialogResult result = System.Windows.Forms.MessageBox.Show("Are you sure you wanna delete this record ?", "Delete ", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                string Link = dgvProducts.SelectedRows[0].Cells["Link"].Value.ToString();
                Console.WriteLine(Link);
                var stalkerEntities = new Data.StalkerEntities();
                var selected = stalkerEntities.tblProducts.FirstOrDefault(x => x.Link == Link);
                stalkerEntities.tblProducts.Remove(selected);
                stalkerEntities.SaveChanges();
                dgvProducts.DataSource = stalkerEntities.tblProducts.ToList();
            }
        }
        private void SetImpression()
        {
            var url = "https://www.skroutz.gr/s/24996052/AOC-C27G2AE-VA-Curved-Gaming-Monitor-27-FHD-1920x1080-165Hz-%CE%BC%CE%B5-%CF%87%CF%81%CF%8C%CE%BD%CE%BF-%CE%B1%CF%80%CF%8C%CE%BA%CF%81%CE%B9%CF%83%CE%B7%CF%82-4ms-GTG.html?from=comparison_table";
            var web = new HtmlWeb();
            var doc = web.Load(url);

            List<string> listpros = new List<string>();
            List<string> listsoso = new List<string>();
            List<string> listcons = new List<string>();
            var prosImpressions = doc?.DocumentNode?.SelectNodes("//ul[contains(@class,'pros')]/li")?.ToList();
            foreach (var pros in prosImpressions.GroupBy(x => x.InnerText))
            {
                listpros.Add(pros.Key.ToString());
                Console.WriteLine("pros " + pros.Key.ToString());
            }

            var soso = doc?.DocumentNode?.SelectNodes("//ul[contains(@class,'so-so')]/li")?.ToList();
            foreach (var so in soso.GroupBy(x => x.InnerText))
            {
                listsoso.Add(so.Key.ToString());
                Console.WriteLine("soso " + so.Key.ToString());
            }

            var cons = doc?.DocumentNode?.SelectNodes("//ul[contains(@class,'cons')]/li")?.ToList();
            if (cons != null)
            {
                foreach (var c in cons.GroupBy(x => x.InnerText))
                {
                    listcons.Add(c.Key.ToString());
                    Console.WriteLine("cons " + c.Key.ToString());
                }
            }
            int total = prosImpressions.Count() + soso.Count() + cons.Count();
            
            
            //var commons = prosImpressions.Intersect(soso);
            List<string> commons = prosImpressions.Select(s1 => s1.InnerText).ToList().Intersect(soso.Select(s2 => s2.InnerText).ToList()).ToList();
            foreach (var com in commons)
            {
                Console.WriteLine("Commons "+com.ToString());
            }

            var joinpros = String.Join(",", listpros.ToArray());
            var joinsoso = String.Join(",", listsoso.ToArray());
            var joincons = String.Join(",", listcons.ToArray());
            string common = "";
            foreach(var k in commons)
            {
                common += k+",";
            }
            common = common.Remove(common.Length-1);
            rtbImpressions.Text += "+" + joinpros.ToString() +"\n";
            rtbImpressions.Text += "\n";
            rtbImpressions.Text += "^" + joinsoso.ToString() + "\n";
            rtbImpressions.Text += "\n";
            rtbImpressions.Text += "-" + joincons.ToString() + "\n";
            rtbImpressions.Text += "\n";
            rtbImpressions.Text += "+-"+ common;
        }
        private void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtLink.Text.StartsWith("https://www.skroutz.gr/"))
                {
                    ScrapeSkroutz();
                }
                else if (txtLink.Text.StartsWith("https://www.bestprice.gr/"))
                {
                    ScrapeBestPrice();
                }
            }
            catch (System.NullReferenceException ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
        private void ScrapeBestPrice()
        {
            try
            {
                var url = txtLink.Text;
                var web = new HtmlWeb();
                var doc = web.Load(url);
                var title = doc?.DocumentNode?.SelectSingleNode("//div[@class='hgroup']/h1")?.InnerText;
                var prices = doc?.DocumentNode?.SelectNodes("//div[@class='prices__price']/a")?.ToList();
                string price = prices?.FirstOrDefault().InnerText.ToString();
                var rating = doc?.DocumentNode?.SelectSingleNode("//span[contains(@class,'Header__StarRating')]")?.InnerText; //actual-rating 
                var picture = doc.DocumentNode.SelectSingleNode("//img[@itemprop='image']").Attributes["src"].Value;                                                                                   //var summary = doc?.DocumentNode?.SelectNodes("//div[contains(@class,'simple-description')]/ul/li").ToList();
                var summary = doc?.DocumentNode?.SelectNodes("//div[contains(@class,'item-header__specs-list')]/ul/li")?.ToList(); //summary
                string description = "";
                Console.WriteLine("Title :" + title + " Price :" + price + " Rating :" + rating);
                foreach (var j in summary)
                {
                    Console.WriteLine(j.InnerText);
                }
                if (summary != null)
                {
                    foreach (var s in summary)
                    {
                        description += s.InnerText + "\n";
                    }
                }
                if (picture != null)
                {
                    pbLoadImage.Load("https:" + picture);
                }
                if (string.IsNullOrEmpty(rating))
                {
                    rating = "0";
                }
                if (title != null && price != null)
                {
                    Initialize(title, price.ToString(), rating.ToString(), description);
                }
            }
            catch (System.NullReferenceException ex)
            {
                Console.Write(ex.Message);
            }
        }
        private void ScrapeSkroutz()
        {
            var url = txtLink.Text;
            var web = new HtmlWeb();
            var doc = web.Load(url);
            var title = doc?.DocumentNode?.SelectSingleNode("//h1[@class='page-title']")?.InnerText;
            var prices = doc?.DocumentNode?.SelectNodes("//strong[@class='dominant-price']")?.ToList();
            string price = prices?.FirstOrDefault().InnerText.ToString();
            var picture = doc?.DocumentNode?.SelectSingleNode("//div[@class='image']//a//img")?.Attributes["src"].Value;
            var rating = doc?.DocumentNode?.SelectSingleNode("//span[@itemprop='ratingValue']")?.InnerText; //actual-rating 
                                                                                                            //var summary = doc?.DocumentNode?.SelectNodes("//div[contains(@class,'simple-description')]/ul/li").ToList();
            var summary = doc?.DocumentNode?.SelectNodes("//div[contains(@class,'summary')]")?.ToList(); //summary
            string description = "";
            if (summary != null)
            {
                foreach (var s in summary)
                {
                    description += s.InnerText + "\n";
                }
            }
            if (string.IsNullOrEmpty(rating))
            {
                rating = "0";
            }
            if (picture != null)
            {
                pbLoadImage.Load("https:" + picture.ToString());
            }
            if (title != null && price != null)
            {
                Initialize(title, price.ToString(), rating.ToString(), description);
            }
        }
        public void FillComboBox()
        {
            cbProducts.Items.Clear();
            cbProducts.DisplayMember = "Text";
            cbProducts.ValueMember = "Value";
            using (var context = new Data.StalkerEntities())
            {
                var data = context.tblProducts.ToList();
                foreach (var item in data)
                {
                    cbProducts.Items.Add(new ComboboxItem() {Text = item.Title.ToString(),Value =item.Id });
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (cbProducts.SelectedIndex >= 0)
            {
                int id = Int32.Parse((cbProducts.SelectedItem as ComboboxItem).Value.ToString());
                cartesianChart1.Series.Clear();
                using (var context = new Data.StalkerEntities())
                {
                    var data = context.PriceHistory.Where(x => x.PId == id).OrderBy(x => x.Date).ToList();
                    int counter = 0;
                    LiveCharts.Wpf.ColumnSeries[] columnSeries = new ColumnSeries[data.Count];
                    foreach (var item in data)
                    {
                        double[] ys2 = { item.Price };
                        columnSeries[counter] = new LiveCharts.Wpf.ColumnSeries()
                        {
                            Title = item.Date.ToString(),
                            DataLabels = true,
                            ColumnPadding = 15,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Margin = new Thickness(10, 10, 10, 10),
                            Values = new LiveCharts.ChartValues<double>(ys2),
                        };
                        cartesianChart1.LegendLocation = LegendLocation.Right;
                        cartesianChart1.FontStretch = new FontStretch();
                        cartesianChart1.Series.Add(columnSeries[counter]);
                        counter++;
                    }
                }
            }
            
        }

        private void cbProducts_SelectedIndexChanged(object sender, EventArgs e)
        {
            //ComboBoxItem selectedCar = (ComboBoxItem)cbProducts.SelectedItem;
            //int selecteVal = Convert.ToInt32(selectedCar.Content);
            //System.Windows.Forms.MessageBox.Show((cbProducts.SelectedItem as ComboboxItem).Value.ToString());
        }

        private void materialRaisedButton2_Click(object sender, EventArgs e)
        {
            //εδω θα υλοποιηθεί η λειτουργία ελέγχου για αλλαγή τιμών προιόντων
            using(var context = new Data.StalkerEntities())
            {
                var data = context.tblProducts.ToList().Select(i => new { i.Link, i.Id ,i.Price,i.Rating,i.Title}).ToList();
                //btnLoad.Invoke(new Action(() => btnLoad.Enabled = false));
                materialRaisedButton2.Invoke(new Action(() => materialRaisedButton2.Enabled=false));
                foreach (var link in data)
                {
                    Application.DoEvents();
                    var url = link.Link;
                    var web = new HtmlWeb();
                    var doc = web.Load(url);
                    var skroutzprices = doc?.DocumentNode?.SelectNodes("//strong[@class='dominant-price']")?.ToList();
                    var bestpprices = doc?.DocumentNode?.SelectNodes("//div[@class='prices__price']/a")?.ToList();
                    string bestpprice = bestpprices?.FirstOrDefault().InnerText.ToString().Replace("€", "");
                    var test = link.Price.ToString();
                    string newskroutzprice = skroutzprices?.FirstOrDefault().InnerText.ToString().Replace("€", "");
                    
                    var testlink = link.Id;
                    float saveprice = (float)Math.Round(float.Parse(link.Price.ToString()), 2);
                    var joinprice = context.PriceHistory.Select(i => new { i.PId, i.Price, i.Date }).Where(x=>x.PId == link.Id).OrderByDescending(x => x.Date).FirstOrDefault();
                    float compPrice = 0;
                    if (newskroutzprice != null)
                    {
                        compPrice = (float)Math.Round(float.Parse(newskroutzprice.ToString()), 2);
                    }
                    else
                    {
                        compPrice = (float)Math.Round(float.Parse(bestpprice.ToString()), 2);
                    }
                    
                    if (compPrice != joinprice.Price)
                    {
                        CheckPrices(link.Title,testlink, compPrice,(float) joinprice.Price);
                        Data.tblProducts updProduct = context.tblProducts.Where(x=>x.Id == link.Id).FirstOrDefault();
                        updProduct.Id = link.Id;
                        updProduct.Price = compPrice;
                        context.SaveChanges();
                    }
                }
                //'btnLoad.Invoke(new Action(() => btnLoad.Enabled = true));
                materialRaisedButton2.Invoke(new Action(() => materialRaisedButton2.Enabled = true));
            }
        }
        private void CheckPrices(string title , int pid , float newprice,float oldprice)
        {
            if (newprice != oldprice)
            {
                float deservedDifference = newprice - oldprice;
                if (deservedDifference >= 4)
                {
                    using (var context = new Data.StalkerEntities())
                    {
                        Data.PriceHistory priceHistory = new Data.PriceHistory()
                        {
                            PId = pid,
                            Price = Math.Round(newprice, 2),
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
        private System.Drawing.Point _mouseLoc;
        private void Main_MouseDown(object sender, MouseEventArgs e)
        {
            _mouseLoc = e.Location;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Order order = new Order();
            order.ShowDialog();
        }

    }
    public class ComboboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }
}
