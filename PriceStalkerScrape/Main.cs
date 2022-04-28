using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Validation;
using System.Drawing;
using System.Linq;
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
using Color = System.Windows.Media.Color;

namespace PriceStalkerScrape
{
    public partial class Main : MaterialForm
    {
        public Main()
        {
            InitializeComponent();
            LoadData();
            FillComboBox();
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
            try
            {
                using (var context = new Data.StalkerEntities())
                {
                    Data.tblProducts product = new Data.tblProducts();

                    if (context.tblProducts.Any(x => x.Link == txtLink.Text)) 
                    {
                        System.Windows.MessageBox.Show("Record already exists..","Warning",MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Warning); 
                        return; 
                    }
                    product.Link = txtLink.Text;
                    product.Title = lblProductTitle.Text;
                    string ignoreSign = lblProductPrice.Text.ToString().Replace("€", "").Trim();
                    float price = (float)Math.Round(float.Parse(ignoreSign), 2); 
                    product.Price =price;
                    string rating = lblProductRating.Text.Replace(".", ",");
                    float rate = (float)Math.Round(float.Parse(rating), 2);
                    product.Rating = rate;
                    product.Description = txtDescription.Text;
                    context.tblProducts.Add(product);

                    context.SaveChanges();
                    int pid = context.tblProducts.Max(x => x.Id);
                    Data.PriceHistory priceHistory = new Data.PriceHistory() 
                    {
                        PId= product.Id,
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

        private void btnScrape_Click(object sender, EventArgs e)
        {
        }

        private void materialFlatButton1_Click_1(object sender, EventArgs e)
        {
            InsertIntoDb();
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

        private void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtLink.Text.StartsWith("https://www.skroutz.gr/"))
                {
                    // From Web 
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
                // From Web 
                var url = txtLink.Text;
                var web = new HtmlWeb();
                var doc = web.Load(url);
                var title = doc?.DocumentNode?.SelectSingleNode("//div[@class='hgroup']/h1")?.InnerText;
                var prices = doc?.DocumentNode?.SelectNodes("//div[@class='prices__price']/a")?.ToList();
                string price = prices?.FirstOrDefault().InnerText.ToString();
                var rating = doc?.DocumentNode?.SelectSingleNode("//span[contains(@class,'Header__StarRating')]")?.InnerText; //actual-rating 
                                                                                                                              //var summary = doc?.DocumentNode?.SelectNodes("//div[contains(@class,'simple-description')]/ul/li").ToList();
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
            if (title != null && price != null)
            {
                Initialize(title, price.ToString(), rating.ToString(), description);
            }
        }
        public void FillComboBox()
        {
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
            int id = Int32.Parse((cbProducts.SelectedItem as ComboboxItem).Value.ToString());
            cartesianChart1.Series.Clear();
            using (var context = new Data.StalkerEntities())
            {
                var data = context.PriceHistory.Where(x => x.PId == id).OrderBy(x => x.Date).ToList();
                int counter = 0;
                LiveCharts.Wpf.ColumnSeries[] columnSeries = new ColumnSeries[data.Count];
                foreach (var item in data)
                {
                    double[] ys2 = {item.Price};
                    columnSeries[counter] = new LiveCharts.Wpf.ColumnSeries()
                    {
                        Title = item.Date.ToString(),
                        DataLabels =true,
                        ColumnPadding = 15,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(10, 10, 10, 10),
                        
                        Values = new LiveCharts.ChartValues<double>(ys2),
                    };
                    cartesianChart1.LegendLocation = LegendLocation.Right;
                    
                    cartesianChart1.Series.Add(columnSeries[counter]);
                    counter++;
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
                
                foreach (var link in data)
                {
                    var url = link.Link;
                    var web = new HtmlWeb();
                    var doc = web.Load(url);
                    var prices = doc?.DocumentNode?.SelectNodes("//strong[@class='dominant-price']")?.ToList();
                    var test = link.Price.ToString();
                    string newprice = prices?.FirstOrDefault().InnerText.ToString().Replace("€", "");
                    var testlink = link.Id;
                    float saveprice = (float)Math.Round(float.Parse(link.Price.ToString()), 2);
                    var joinprice = context.PriceHistory.Select(i => new { i.PId, i.Price, i.Date }).Where(x=>x.PId == link.Id).OrderByDescending(x => x.Date).FirstOrDefault();
                    Console.WriteLine(joinprice.Price);
                    float compPrice = (float)Math.Round(float.Parse(newprice.ToString()), 2);
                    if (compPrice != joinprice.Price)
                    {
                        CheckPrices(testlink, float.Parse(newprice), compPrice);
                        Data.tblProducts updProduct = context.tblProducts.Where(x=>x.Id == link.Id).FirstOrDefault();
                        updProduct.Id = link.Id;
                        updProduct.Price = compPrice;
                        context.SaveChanges();
                    }
                }
                
            }
        }
        private void CheckPrices(int pid , float newprice,float oldprice)
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
            }
            
        }
        private System.Drawing.Point _mouseLoc;

        private void Main_MouseDown(object sender, MouseEventArgs e)
        {
            _mouseLoc = e.Location;
        }

        private void Main_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int dx = e.Location.X - _mouseLoc.X;
                int dy = e.Location.Y - _mouseLoc.Y;
                this.Location = new System.Drawing.Point(this.Location.X + dx, this.Location.Y + dy);
            }
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
