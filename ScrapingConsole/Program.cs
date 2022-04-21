using HtmlAgilityPack;
using System;
using System.Linq;

namespace ScrapingConsole
{
    class Program
    {
        static void Main(string[] args)
        {

        }
        void ScrapeBestPrice()
        {
            try
            {
                // From Web 
                var url = "https://www.bestprice.gr/item/2156987532/apple-ipad-mini-83-2021-wifi-64gb.html";
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
            }
            catch (System.NullReferenceException ex)
            {
                Console.Write(ex.Message);
            }
        }
    }
}
