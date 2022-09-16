using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceStalkerScrape
{
    public class tblProducts
    {
        public int PId;
        public double Price;
        public DateTime Date;
    }
    public class ListOfProducts
    {
        public List<tblProducts> ListOftblproducts;
    }
}
