// See https://aka.ms/new-console-template for more information
using AptekaParsing;
using AptekaParsing.Entities;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using System.Text.RegularExpressions;




public static class Program
{
    public static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }
    public static async Task MainAsync(string[] args)
    {
        if (args.Length == 0) return;
        foreach(var arg in args)
        {
            if (arg.Contains("clearStores"))
            {
                ClearStores();
            }
            else if (arg.Contains("clearProducts"))
            {
                ClearProducts();
            }
            else if (arg.Contains("clearAll"))
            {
                ClearStores();
                ClearProducts();
            }
            else if (arg.Contains("refreshAll"))
            {
                ClearStores();
                ClearProducts();

                await ParceAndSaveStores();
                await MainParce();
            }
            else if (arg.Contains("refreshProducts"))
            {
                ClearProducts();
                await MainParce();
            }
            else if (arg.Contains("parceStores"))
            {
                await ParceAndSaveStores();
            }
            else if (arg.Contains("parceProducts"))
            {
                await MainParce();
            }
        }
        Console.WriteLine("Completed!");
    }

    static void ClearStores()
    {
        using (var context = new ApplicationContext())
        {
            if (context.Stores.Count() <= 0) return;
            context.Stores.RemoveRange(context.Stores);
            context.SaveChanges();
        }
    }
    static void ClearProducts()
    {
        using (var context = new ApplicationContext())
        {
            if (context.Products.Count() <= 0) return;
            context.Products.RemoveRange(context.Products);
            context.ProductInStores.RemoveRange(context.ProductInStores);
            context.SaveChanges();
        }
    }

    async static void ParceAndSaveAll()
    {
        await ParceAndSaveStores();
        await MainParce();
    }

    async static Task ParceAndSaveStores()
    {
        WebScraper webScraper = new WebScraper();
        var drugStoreChainsUrl = "https://mypharmacy.com.ua/chain/";
        var html = await webScraper.GetHtml(drugStoreChainsUrl);
        var chainsHtmlDock = new HtmlDocument();
        chainsHtmlDock.LoadHtml(html);
        var chainNodes = chainsHtmlDock.DocumentNode.SelectNodes("//a[@class='chains-items__list-link']");

        List<DrugStore> stores = new List<DrugStore>();
        foreach (var chainNode in chainNodes)
        {
            var chainStoresHtml = await webScraper.GetHtml(chainNode.Attributes["href"].Value);
            var chainHtmlDock = new HtmlDocument();

            chainHtmlDock.LoadHtml(chainStoresHtml);


            var StoreNodes = chainHtmlDock.DocumentNode.SelectNodes("//li[@class='chain-sublist__item']");
            string? chainSite = null;
            foreach (var storeNode in StoreNodes)
            {
                var singleStoreNode = HtmlNode.CreateNode(storeNode.OuterHtml);
                var fullAdress = singleStoreNode.SelectSingleNode("//a[@class='chain-sublist__item-link']").InnerText;

                var adress = String.Join(',', fullAdress.Split(',').Skip(1)).Trim();
                var city = fullAdress.Split(',')[1].Trim();
                var name = fullAdress.Split(',')[0].Trim();

                var storeLink = singleStoreNode.SelectSingleNode("//a[@class='chain-sublist__item-link']").Attributes["href"].Value;
                var pattern = @"drugstore\/(\d+)\/";
                var stringId = Regex.Match(storeLink, pattern).Groups[1].Value;
                var id = Convert.ToInt32(stringId);

                var phoneNumber = singleStoreNode.SelectSingleNode("//p[@class='chain-sublist__item-element chain-sublist__item-element--second']")?.InnerText.Trim();

                if (chainSite == null)
                {
                    var storeHtml = await webScraper.GetHtml(storeLink);
                    var storeHtmlDock = new HtmlDocument();
                    storeHtmlDock.LoadHtml(storeHtml);
                    chainSite = storeHtmlDock.DocumentNode.SelectSingleNode("//a[@class='drugstore-contacts__item-link']")?.Attributes["href"].Value ?? "";
                }

                DrugStore store = new DrugStore { Adress = adress, City = city, Name = name, Site = chainSite, Id = id, PhoneNumber = phoneNumber };
                stores.Add(store);
            }
        }


        using (var context = new ApplicationContext())
        {
            context.Stores.AddRange(stores);
            context.SaveChanges();

        }

    }

    async static Task MainParce()
    {
        WebScraper webScraper = new WebScraper();
        string patternUrls = @"https://mypharmacy\.com\.ua/catalogue.*?(?="")";
        string patternCount = @"(?<=\ for\ \(var\ i\ =\ 1;\ i\ <=).*?(?=\ \*\ 28)";
        var str = await webScraper.GetHtml("https://mypharmacy.com.ua/catalogue/all/");

        var ListUrls = Regex.Matches(str, patternUrls).Cast<Match>()
                        .Select(m => m.Value).Distinct()
                        .ToList();
        string count = "";
        foreach (var listUrl in ListUrls)
        {
            if (listUrl == "https://mypharmacy.com.ua/catalogue/all/") continue;

            int countSymbol = Extensions.CountByCharacter(listUrl, '/');
            if (countSymbol > 5) continue;
            str = await webScraper.GetHtml(listUrl);
            count = Regex.Match(str, patternCount).Value;

            bool flag = Extensions.IsDigit(count, count.Length - 1);
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(str);
            HtmlAgilityPack.HtmlNodeCollection nodesProduct = htmlDoc.DocumentNode.SelectNodes("//a[@class='products-list__item-link-wrapper']");
            if (flag)
            {
                for (int i = 1; i < Convert.ToInt32(count);)
                {

                    var allLinksOnPage = nodesProduct.Select(node => node.Attributes["href"].Value.Replace("instruction/", "")).ToList();

                    i++;
                    str = await webScraper.GetHtml(listUrl + $"page={i}/");
                    htmlDoc.LoadHtml(str);
                    nodesProduct = htmlDoc.DocumentNode.SelectNodes("//a[@class='products-list__item-link-wrapper']");

                    var tasks = new List<Task<Tuple<Product, Dictionary<int, string>>>>();
                    using (var context = new ApplicationContext())
                    {
                        var idsInDb = context.Products.Select(p => p.Id).ToList();

                        allLinksOnPage = allLinksOnPage.Where(link =>
                        {
                            var productIdPattern = @".ua\/.+\/(\d+)\/";
                            var productId = Convert.ToInt32(Regex.Match(link, productIdPattern).Groups[1].Value);
                            return !idsInDb.Contains(productId);
                        }).ToList();
                    }
                    if (allLinksOnPage.Count <= 0) continue;
                    foreach (var link in allLinksOnPage)
                        {
                            

                            tasks.Add(ParceProductAndStoresCoordinates(link));
                        }
                    

                    const int tasksCount = 30;
                    while (tasks.Count > 0)
                    {
                        var currentTasks = tasks.Take(tasksCount);
                        tasks = tasks.Skip(tasksCount).ToList();

                        var tasksData = await Task.WhenAll(currentTasks);
                        var products = tasksData.Select(x => x.Item1).ToList();

                        var coordinates = new Dictionary<int, string>();
                        foreach (var dict in tasksData.Select(x => x.Item2).ToList())
                        {
                            foreach (var item in dict)
                            {
                                if (!coordinates.ContainsKey(item.Key)) coordinates[item.Key] = item.Value;
                            }
                        }



                        using (var context = new ApplicationContext())
                        {
                            var storesWithoutCords = context.Stores
                                .Where(x => x.Сoordinates == null)
                                .ToList()
                                .Where(x => coordinates.ContainsKey(x.Id))
                                .ToList();
                            foreach (var store in storesWithoutCords)
                            {
                                store.Сoordinates = coordinates[store.Id];
                            }
                            context.Products.AddRange(products);
                            context.SaveChanges();
                        }
                    }



                }
            }
        }
    }

    async static Task<Tuple<Product, Dictionary<int, string>>> ParceProductAndStoresCoordinates(string link)
    {
        WebScraper webScraper = new WebScraper();
        var html = await webScraper.GetHtml(link);

        var productIdPattern = @".ua\/.+\/(\d+)\/";

        string productName = "";

        var productId = Convert.ToInt32(Regex.Match(link, productIdPattern).Groups[1].Value);
        var organizationPointsPattern = @"organizationPoints = (\{.+\}),";
        var organizationIdListPattern = @"organizationIdList = (\[.+\]),";

        var organizationPointsJson = Regex.Match(html, organizationPointsPattern).Groups[1].Value;
        var organizationIdListJson = Regex.Match(html, organizationIdListPattern).Groups[1].Value;
        if (organizationIdListJson == "" && organizationPointsJson == "")
        {
            Console.WriteLine($"There isn't product {productId} in stores");

            var htmlDock = new HtmlDocument();
            htmlDock.LoadHtml(html);
            productName = htmlDock.DocumentNode.SelectSingleNode("//div[@class='search-result-head__title-wrapper']/h1").InnerText.Trim();

            return new Tuple<Product, Dictionary<int, string>>(new Product { Id = productId, Producer = null, ProductName = productName, ProductInStores = new List<ProductInStore>() }, new Dictionary<int, string>());
        }
        dynamic pointsData = JsonConvert.DeserializeObject<ExpandoObject>(organizationPointsJson, new ExpandoObjectConverter());
        var storeCoords = new Dictionary<int, string>();
        var productInStores = new List<ProductInStore>();

        foreach (var pointItem in pointsData)
        {
            var value = pointItem.Value;

            var latitude = value.latitude;
            var longitude = value.longitude;
            string cords = latitude + " " + longitude;

            int storeId = Convert.ToInt32(value.id);

            storeCoords.Add(storeId, cords);

            if (value.ordersOn == false) continue;
            var price = value.price;
            int countLeft = 1;

            var productInStore = new ProductInStore { Price = price, CountLeft = countLeft, StoreId = storeId, ProductId = productId };
            productInStores.Add(productInStore);



        }

        var productDataPattern = "{\"id\":" + productId + ",\"name\":\"(.+?)\",\"producer\":\"(.+?)\"}";

        var productData = Regex.Match(html, productDataPattern).Groups;
        productName = productData[1].Value.Replace("\\\"", "\"").Replace("\\\'", "\'");
        var productProducer = productData[2].Value.Replace("\\\"", "\"").Replace("\\\'", "\'");

        var product = new Product { Id = productId, ProductInStores = productInStores, ProductName = productName, Producer = productProducer };

        return new Tuple<Product, Dictionary<int, string>>(product, storeCoords);
    }


}


