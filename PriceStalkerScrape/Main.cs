using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
            //var .
            dgvProducts.DataSource = stalkerEntities.tblProducts.ToList();
        }
        private void InsertIntoDb()
        {
            try
            {
                using (var context = new Data.StalkerEntities())
                {
                    Data.tblProducts product = new Data.tblProducts();

                    if (context.tblProducts.Any(x => x.Link == txtLink.Text)) return;
                    product.Link = txtLink.Text;
                    product.Title = lblProductTitle.Text;
                    string ignoreSign = lblProductPrice.Text.ToString().Replace("€", "").Trim();
                    product.Price = float.Parse(ignoreSign);
                    string rating = lblProductRating.Text.Replace(".", ",");
                    product.Rating = float.Parse(rating);
                    product.Description = txtDescription.Text;
                    context.tblProducts.Add(product);
                    context.SaveChanges();
                    new ToastContentBuilder()
                    .AddArgument("action", "viewConversation")
                    .AddArgument("conversationId", 123)
                    .AddText("Record with title " + lblProductTitle.Text + " Successfully Saved")
                    .Show();
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
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
                            description += s.InnerText;
                        }
                    }
                    if (title != null && price != null && rating != null & summary != null)
                    {
                        Initialize(title, price.ToString(), rating.ToString(), description);
                    }
                }
            }
            catch (System.NullReferenceException ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
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
            chart1.Series.Clear();
            chart1.Series.Add("test");
            //chart1.Series.Add("test2");
            //chart1.Series["test"].Points.AddXY("test1",12);
            //chart1.Series["test2"].Points.AddXY("test2", 16);
            
            using (var context = new Data.StalkerEntities())
            {
                var data = context.PriceHistory.Where(x=>x.PId==7).ToList();
                foreach (var item in data)
                {
                    chart1.Series["test"].Points.AddXY(item.Date.ToString(), item.Price);
                }
            }
        }

        private void cbProducts_SelectedIndexChanged(object sender, EventArgs e)
        {
            //ComboBoxItem selectedCar = (ComboBoxItem)cbProducts.SelectedItem;
            //int selecteVal = Convert.ToInt32(selectedCar.Content);
            System.Windows.Forms.MessageBox.Show((cbProducts.SelectedItem as ComboboxItem).Value.ToString());
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
