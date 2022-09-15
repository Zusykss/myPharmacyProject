using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AptekaParsing
{
    public class WebScraper
    {
        public WebScraper()
        {

        }
        private LaunchOptions _options = null;
        private async Task<string> HttpClientLoader(string url)
        {
            HttpResponseMessage response = null;
            try
            {
                //var proxy = _proxyGenerator.GetRandomProxy();
                var cookieContainer = new CookieContainer();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                using (var client = new HttpClient(handler))
                {
                    //if (_useProxy) handler.Proxy = proxy._webProxy;
                    client.BaseAddress = new Uri(url);

                
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.0.0 Safari/537.36");

                    response = await client.GetAsync(url);

                    Console.WriteLine(response.StatusCode + " - " + url);
                    var text = await response.Content.ReadAsStringAsync();
                    return text;
                }
            }
            catch (Exception ex)
            {
            }
            return "";
        }
        private void CreateOptionForBrowser()
        {
            List<string> _argsForOption = new()
            {
                    "--disable-gpu",
                    "--disable-dev-shm-usage",
                    "--disable-setuid-sandbox",
                    "--no-first-run",
                    "--no-sandbox",
                    "--no-zygote",
                    "--deterministic-fetch",
                    "--disable-features=IsolateOrigins",
                    "--disable-site-isolation-trials",
                    "--start-maximized"
            };
            //if (_useProxy)
            //{
            //    _argsForOption.Add($"--proxy-server=http://{_currentProxy._host}:{_currentProxy._port}");
            //}

            _options = new LaunchOptions
            {
                Headless = true,
                Args = _argsForOption.ToArray(),
                IgnoredDefaultArgs = new string[]
                {
                    "--enable-automation"
                },
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
            };
        }
      
        public async Task<string> GetHtml(string url)
        {
            string str = "";
            if (!url.Contains("rozetka") && !url.Contains("eldorado"))
            {
                str = await HttpClientLoader(url);
            }
            if (str == "" || str.Contains("ERROR HTTPCLIENT") || str.Contains("Please Wait... | Cloudflare") || str.Contains("403 Forbidden")|| str.Contains(":443"))
            {
                str = await BrowserLoader(url, 0);
            }

            if (str.Contains("Please Wait... | Cloudflare") || str.Contains("Error 404")
            || str.Contains("403 Forbidden")
            || str.Contains("404 Not Found")
            || str.Contains("503 Service Unavailable")
            || str.Contains("502 Bad Gateway")
            || str.Contains("500 Internal Server Error")
            || str.Contains("400 Bad Request")
            || str.Contains(":443"))
            {
                return str;
            }
            return str;
        }
        private async Task<string> BrowserLoader(string url, int count)
        {
            string text = "";
            Page page;
            var uri = new Uri(url);
            var browser = await InitBrowserLocalAsync();
            try
            {
                page = browser.PagesAsync().Result.First();

                //if (_useProxy)
                //await page.AuthenticateAsync(new PuppeteerSharp.Credentials() { Username = proxy._userName, Password = proxy._passwordProxy });
                //var responce = _page.GoToAsync(url, WaitUntilNavigation.DOMContentLoaded).Result;
                var responce = await page.GoToAsync(url);
                //Thread.Sleep(10000);
                text = await responce.TextAsync();
                await browser.CloseAsync();
            }
            catch (Exception ex)
            {
                await browser.CloseAsync();
            }
            return text;
        }
        public async Task<Browser> InitBrowserLocalAsync()
        {
            CreateOptionForBrowser();
            return await Puppeteer.LaunchAsync(_options);
        }
    }
}
