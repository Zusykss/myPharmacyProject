// See https://aka.ms/new-console-template for more information
using AptekaParsing;
using AptekaParsing.Entities;
using CsvHelper;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using CsvHelper.Configuration;
using System.Text;
using System.Data.SqlClient;
using System.IO.Compression;

public static class Program
{
    public static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }
    private static string logMessages="";
    private static LogWriter logger;
    private static string databasePath="";

    public static async Task MainAsync(string[] args)
    {
        try
        {

            if (args.Length < 2)
            {
                args = new string[2] { args[0], "" };
            }
            databasePath = args[1];
            var arg = args[0];

            logger = new LogWriter(args[1] + "\\" + "logs.txt");
            logger.LogInformation(DateTime.Now.ToLongTimeString());
            if (arg.Contains("mainParce"))
            {

                Console.WriteLine("Start Deleting");
                ClearProducts();
                using (var context = new ApplicationContext(databasePath))
                {
                    if (context.Stores.Count() <= 0) await ParceAndSaveStores();
                }
                Console.WriteLine("Start Parsing");
                await MainParce();
                exportCsv(args[1]);

                return;
            };


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
                Console.WriteLine("Start Deleting");
                ClearProducts();
                Console.WriteLine("Start Parsing");
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
            else if (arg.Contains("fastExport"))
            {
                exportCsv(args[1]);
            }
            else if (arg.Contains("testExport"))
            {
                exportCsv(args[1], 10);
            }
            logger.LogInformation(DateTime.Now.ToLongTimeString());
            Console.WriteLine("Completed!");

        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            logger.LogCritical(ex);
        }
    }
    private class ProductRecordCSV 
    {
        [CsvHelper.Configuration.Attributes.Index(0)]
        public string? StoreId { get; set; }
        [CsvHelper.Configuration.Attributes.Index(1)]
        public string? Addres{ get; set; }
        [CsvHelper.Configuration.Attributes.Index(2)]
        public string? City { get; set; }
        [CsvHelper.Configuration.Attributes.Index(3)]
        public string? ProductId { get; set; }
        [CsvHelper.Configuration.Attributes.Index(4)]
        public string? ProductName { get; set; }
        [CsvHelper.Configuration.Attributes.Index(5)]
        public string? Producer { get; set; }
        [CsvHelper.Configuration.Attributes.Index(6)]
        public string? CountLeft { get; set; }
        [CsvHelper.Configuration.Attributes.Index(7)]
        public string? Price { get; set; }
        [CsvHelper.Configuration.Attributes.Index(8)]
        public string ? Coordinat { get; set; }
        [CsvHelper.Configuration.Attributes.Index(9)]
        public string? StoreNet { get; set; }
        [CsvHelper.Configuration.Attributes.Index(10)]
        public string? RequestDate{ get; set; }
    }

    public static string UTF8toASCII(string text)
    {
        System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
        Byte[] encodedBytes = utf8.GetBytes(text);
        Byte[] convertedBytes =
                Encoding.Convert(Encoding.UTF8, Encoding.ASCII, encodedBytes);
        System.Text.Encoding ascii = System.Text.Encoding.ASCII;

        return ascii.GetString(convertedBytes);
    }
    static void exportCsv(string path =",", int count=-1)
    {
        var fileName = "MyApteka";
        //if (File.Exists(fileName+".csv"))
        //{
        //    int i = 1;
        //    for (; File.Exists($"{fileName}({i}).csv"); i++) ;
        //    fileName += $"({i})";
        //}
        Directory.GetCurrentDirectory();
        fileName += ".csv";
        fileName = path +"\\"+ fileName; 
        Console.WriteLine(fileName);
        using (var context = new ApplicationContext(databasePath))
        {
            IEnumerable<Product> products;
            if(count == -1)
            {
                products = context.Products.Include(p => p.ProductInStores).ToList();
            }
            else
            {
                products = context.Products.Include(p => p.ProductInStores).Take(count).ToList();
            }
            
            var dataProducts = new List<ProductRecordCSV>();
            var stores = context.Stores.ToList();
            foreach (var product in products)
            {
                if (product.ProductInStores.Count <= 0)
                {
                    continue;
                    //var dataProduct = new ProductRecordCSV
                    //{
                    //    StoreId = "-1",
                    //    Addres = "unknown",
                    //    City = "unknown",
                    //    ProductId = product.Id.ToString(),
                    //    ProductName = product.ProductName,
                    //    Producer = "",
                    //    CountLeft = "0",
                    //    Price = "",
                    //};
                    //dataProducts.Add(dataProduct);
                }
                    foreach (var productInStore in product.ProductInStores)
                {
                    var stor = stores.Find(s => s.Id == productInStore.StoreId);
                    var cords = String.Join(",", stor?.Сoordinates.Split(" ").Select(el => el.Replace(",", ".")) ?? new string[0]);
                    var dataProduct = new ProductRecordCSV
                    {
                        StoreId = productInStore.StoreId.ToString(),
                        Addres = stor?.Adress ?? "",
                        City = stor?.City ?? "",
                        ProductId = product.Id.ToString(),
                        ProductName = product.ProductName,
                        Producer = product.Producer,
                        CountLeft = "1",
                        Price = productInStore.Price.ToString().Replace(".", ","),
                        Coordinat = cords,
                        StoreNet = stor?.Name,
                        RequestDate = productInStore?.RequestDate.ToString("yyyy-MM-dd HH:mm:ss")??""
                    };
                    dataProducts.Add(dataProduct);
                }
            }
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var config = new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture) { Delimiter = ";", Encoding = Encoding.UTF8 };
            using (var writer = new StreamWriter(
                new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite),
                  Encoding.UTF8))
            using(var csv = new CsvWriter(writer, config))
            {
                csv.WriteRecords(dataProducts);
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ConvertFileEncoding(fileName, fileName.Replace(".csv", "1251.csv"), Encoding.UTF8, Encoding.GetEncoding(1251));


            File.Delete(fileName);
            var currentMothDay = DateTime.Now.ToString("dd");
            if(File.Exists(fileName.Replace(".csv", $"-{currentMothDay}.csv")))
            {
                File.Delete(fileName.Replace(".csv", $"-{currentMothDay}.csv"));
            }
            File.Move(fileName.Replace(".csv", "1251.csv"), fileName.Replace(".csv", $"-{currentMothDay}.csv"));
            fileName = fileName.Replace(".csv", $"-{currentMothDay}.csv");

            var archiveName = $"{path}\\MyApteka-{currentMothDay}.zip";
            if (File.Exists(archiveName))
            {
                File.Delete(archiveName);
            }
            using (var archive = ZipFile.Open(archiveName, ZipArchiveMode.Create))
            {
                    archive.CreateEntryFromFile(fileName, Path.GetFileName(fileName));
            }

        }


    }

    /// <summary>
    /// Converts a file from one encoding to another.
    /// </summary>
    /// <param name=”sourcePath”>the file to convert</param>
    /// <param name=”destPath”>the destination for the converted file</param>
    /// <param name=”sourceEncoding”>the original file encoding</param>
    /// <param name=”destEncoding”>the encoding to which the contents should be converted</param>
    public static void ConvertFileEncoding(String sourcePath, String destPath,
                                           Encoding sourceEncoding, Encoding destEncoding)
    {
        // If the destination’s parent doesn’t exist, create it.
        String parent = Path.GetDirectoryName(Path.GetFullPath(destPath));
        if (!Directory.Exists(parent))
        {
            Directory.CreateDirectory(parent);
        }
        // If the source and destination encodings are the same, just copy the file.
        if (sourceEncoding == destEncoding)
        {
            File.Copy(sourcePath, destPath, true);
            return;
        }
        // Convert the file.
        String tempName = null;
        try
        {
            tempName = Path.GetTempFileName();
            using (StreamReader sr = new StreamReader(sourcePath, sourceEncoding, false))
            {
                using (StreamWriter sw = new StreamWriter(tempName, false, destEncoding))
                {
                    int charsRead;
                    char[] buffer = new char[128 * 1024];
                    while ((charsRead = sr.ReadBlock(buffer, 0, buffer.Length)) > 0)
                    {
                        sw.Write(buffer, 0, charsRead);
                    }
                }
            }
            File.Delete(destPath);
            File.Move(tempName, destPath);
        }
        finally
        {
            File.Delete(tempName);
        }
    }
    static void ClearStores()
    {
        using (var context = new ApplicationContext(databasePath))
        {
            context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"Stores\"");
        }
    }
    static void ClearProducts()
    {
        using (var context = new ApplicationContext(databasePath))
        {
            context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"Products\" CASCADE");
            context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"ProductInStores\" CASCADE");
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


        using (var context = new ApplicationContext(databasePath))
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
                    try
                    {
                        var allLinksOnPage = nodesProduct.Select(node => node.Attributes["href"].Value.Replace("instruction/", "")).ToList();

                        i++;
                        str = await webScraper.GetHtml(listUrl + $"page={i}/");
                        htmlDoc.LoadHtml(str);
                        nodesProduct = htmlDoc.DocumentNode.SelectNodes("//a[@class='products-list__item-link-wrapper']");

                        var tasks = new List<Task<Tuple<Product, Dictionary<int, string>>>>();
                        using (var context = new ApplicationContext(databasePath))
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

                            if (logMessages != "")
                            {
                                logger.LogInformation(logMessages);
                                logMessages = "";
                            }

                            var products = tasksData.Select(x => x.Item1).Where(x => x is not null).ToList();

                            var coordinates = new Dictionary<int, string>();
                            foreach (var dict in tasksData.Select(x => x.Item2).ToList())
                            {
                                foreach (var item in dict)
                                {
                                    if (!coordinates.ContainsKey(item.Key)) coordinates[item.Key] = item.Value;
                                }
                            }



                            using (var context = new ApplicationContext(databasePath))
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
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex);
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
            logMessages += $"There isn't product {productId} in stores\n";
            var htmlDock = new HtmlDocument();
            htmlDock.LoadHtml(html);
            productName = htmlDock.DocumentNode.SelectSingleNode("//div[@class='search-result-head__title-wrapper']/h1").InnerText.Trim();

            return new Tuple<Product, Dictionary<int, string>>(null, new Dictionary<int, string>());
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

            var productInStore = new ProductInStore { Price = price, CountLeft = countLeft, StoreId = storeId, ProductId = productId, RequestDate=DateTime.Now};
            productInStores.Add(productInStore);

        }
        
        var productDataPattern = "{\"id\":" + productId + ",\"name\":\"(.+?)\",\"producer\":\"(.+?)\"}";

        var productData = Regex.Match(html, productDataPattern).Groups;
        productName = productData[1].Value.Replace("\\\"", "\"").Replace("\\\'", "\'");
        var productProducer = productData[2].Value.Replace("\\\"", "\"").Replace("\\\'", "\'");

        var product = new Product { Id = productId, ProductInStores = productInStores, ProductName = productName, Producer = productProducer };

        return new Tuple<Product, Dictionary<int, string>>(product, storeCoords);
    }
    //private static async Task<string> PostAsyncJson(string uri, string data)
    //{
    //    byte[] dataBytes = Encoding.UTF8.GetBytes(data);

    //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
    //    request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
    //    request.ContentLength = dataBytes.Length;
    //    request.ContentType = "application/json";
    //    request.Method = "POST";
    //    request.CookieContainer = new CookieContainer();
    //    request.CookieContainer.Add(new Cookie("SESSION", "SESSION =M2RiMjU1YTQtZWJiOS00YTRlLTk0YzItZTc0MzI0YWE3ZTE3"));


    //    using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
    //    using (Stream stream = response.GetResponseStream())
    //    using (StreamReader reader = new StreamReader(stream))
    //    {
    //        return await reader.ReadToEndAsync();
    //    }
    //}

}


