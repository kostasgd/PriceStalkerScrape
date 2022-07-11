using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PriceStalkerScrape
{
    public partial class Order : Form
    {
        public Order()
        {
            InitializeComponent();
            FillComboBoxProducts();
            FillComboBoxCustomers();
            SelectIndexes();
        }
        private void SelectIndexes()
        {
            if (cbCustomer.Items.Count > 0)
                cbCustomer.SelectedIndex = 0;
        }
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }
        public void FillComboBoxProducts()
        {
            cbProduct.Items.Clear();
            cbProduct.DisplayMember = "Text";
            cbProduct.ValueMember = "Value";
            using (var context = new Data.StalkerEntities())
            {
                var data = context.tblProducts.ToList().Distinct();
                foreach (var item in data)
                {
                    cbProduct.Items.Add(new ComboboxItem() { Text = item.Title.ToString(), Value = item.Id });
                }
            }
        }
        public void FillComboBoxCustomers()
        {
            cbCustomer.Items.Clear();
            cbCustomer.DisplayMember = "Text";
            cbCustomer.ValueMember = "Value";
            using (var context = new Data.StalkerEntities())
            {
                var data = context.Customer.ToList().Distinct();
                foreach (var item in data)
                {
                    cbCustomer.Items.Add(new ComboboxItem() { Text = item.Name.ToString(), Value = item.Id });
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void Calculate()
        {
            using (var context = new Data.StalkerEntities())
            {
                string customername = cbCustomer.SelectedItem.ToString();
                var customer = context.Customer.Where(x => x.Name == customername).FirstOrDefault();
                //MessageBox.Show("Customer id ->" + customer.Id);
                string productTitle = cbProduct.SelectedItem.ToString();
                var product = context.tblProducts.Where(x => x.Title == productTitle).FirstOrDefault();
                //MessageBox.Show("Prodcut price ->" +product.Price);
                float totalPrice = (float)(product.Price * Int32.Parse(nudQty.Value.ToString()));
                lblTotalPrice.Text = totalPrice + "€";
            }
        }
        private void nudQty_ValueChanged(object sender, EventArgs e)
        {
            Calculate();
        }
        private void ClearFields()
        {
            txtAddress.Text = "";
            cbCustomer.SelectedIndex = 0;
            cbProduct.SelectedIndex = 0;
            nudQty.Value = 1;
            lblTotalPrice.Text = "";
        }
        private void button1_Click(object sender, EventArgs e)
        {
            using (var context = new Data.StalkerEntities())
            {
                string customername = cbCustomer.SelectedItem.ToString();
                var customer = context.Customer.Where(x => x.Name == customername).FirstOrDefault();
                string productTitle = cbProduct.SelectedItem.ToString();
                var product = context.tblProducts.Where(x => x.Title == productTitle).FirstOrDefault();
                Data.Orders order = new Data.Orders()
                {
                    CustomerId = customer.Id,
                    ProductId = product.Id,
                    Address = txtAddress.Text,
                    Qty = Int32.Parse(nudQty.Value.ToString()),
                    TotalPrice = float.Parse(lblTotalPrice.Text.Replace("€",""))
                };
                context.Orders.Add(order);
                context.SaveChanges();
                MessageBox.Show("Record saved successfully!","Success!",MessageBoxButtons.OK,MessageBoxIcon.Information);
                ClearFields();
            }
        }

        private void cbProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            Calculate();
            nudQty.Value =1;
        }

        private void Order_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }
    }
}
