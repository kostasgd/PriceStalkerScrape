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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HtmlAgilityPack;
using iTextSharp.text;
using iTextSharp.text.pdf;
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
            //SplitTiff(@"C:\source2.tiff");
            //Export();
            exportJpgToPdf();
        }
        public static void SplitTiff(string filepath)
        {
            int activePage;
            int pages;

            var dest = @"c:\Tiffs";

            System.Drawing.Image image = System.Drawing.Image.FromFile(filepath);
            pages = image.GetFrameCount(System.Drawing.Imaging.FrameDimension.Page);

            for (int index = 0; index < pages; index++)
            {
                activePage = index + 1;
                image.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Page, index);
                image.Save(dest + @"\file_" + activePage.ToString() + ".tiff", System.Drawing.Imaging.ImageFormat.Tiff);
            }
            image.Dispose();
        }
        public static string[] ConvertTiffToJpeg(string fileName)
        {
            using (System.Drawing.Image imageFile = System.Drawing.Image.FromFile(fileName))
            {
                FrameDimension frameDimensions = new FrameDimension(
                    imageFile.FrameDimensionsList[0]);

                // Gets the number of pages from the tiff image (if multipage) 
                int frameNum = imageFile.GetFrameCount(frameDimensions);
                string[] jpegPaths = new string[frameNum];

                for (int frame = 0; frame < frameNum; frame++)
                {
                    // Selects one frame at a time and save as jpeg. 
                    imageFile.SelectActiveFrame(frameDimensions, frame);
                    using (Bitmap bmp = new Bitmap(imageFile))
                    {
                        jpegPaths[frame] = String.Format("{0}\\{1}{2}.jpg",
                            Path.GetDirectoryName(fileName),
                            Path.GetFileNameWithoutExtension(fileName),
                            frame);
                        bmp.Save(jpegPaths[frame], ImageFormat.Jpeg);
                    }
                }

                return jpegPaths;
            }
        }
        public static String[] GetFilesFrom(String searchFolder, String[] filters, bool isRecursive)
        {
            List<String> filesFound = new List<String>();
            var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var filter in filters)
            {
                filesFound.AddRange(Directory.GetFiles(searchFolder, String.Format("*.{0}", filter), searchOption));
            }
            return filesFound.ToArray();
        }
        private static void exportJpgToPdf()
        {
            String searchFolder = @"C:\";
            var filters = new String[] { "jpg", "jpeg",};
            var files = GetFilesFrom(searchFolder, filters, false);
            Document document = new Document();
            using (var stream = new FileStream(@"C:\test.pdf", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                PdfWriter.GetInstance(document, stream);
                document.Open();
                foreach(var i in files)
                {
                    var image = System.Drawing.Image.FromFile(i.ToString());
                    document.Add((IElement)image);
                }
                document.Close();
            }
        }
        private static void Export()
        {
            using (var stream = new FileStream(@"C:\result.pdf", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                Document document = new Document();
                var writer = PdfWriter.GetInstance(document, stream);
                var bitmap = new System.Drawing.Bitmap(@"C:\source2.tiff");
                var pages = bitmap.GetFrameCount(System.Drawing.Imaging.FrameDimension.Page);

                document.Open();
                iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;
                for (int i = 0; i < pages; ++i)
                {
                    bitmap.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Page, i);
                    iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(bitmap, System.Drawing.Imaging.ImageFormat.Jpeg);
                    // scale the image to fit in the page 
                    if(img.Width > img.Height)
                    {
                        document.SetPageSize(PageSize.A4);
                    }
                    else if (img.Width < img.Height)
                    {
                        document.SetPageSize(PageSize.A4.Rotate());
                    }
                    img.ScalePercent(72f / img.DpiX * 100);
                    img.SetAbsolutePosition(0, 0);
                    cb.AddImage(img);
                    document.NewPage();
                }
                document.Close();
            }
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

        private void btnScrape_Click(object sender, EventArgs e)
        {
        }

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
