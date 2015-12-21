using Newtonsoft.Json.Linq;
using CsQuery;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using UpdateCheckCS;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Market_Watcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Delcare Variables Start
        double GBPUSD;
        double EURUSD;
        double CADUSD;
        double BRLUSD;
        double RUBUSD;
        double KRWUSD;
        double NZDUSD;

        double lfp = 0;
        int interval = 1;
        string marketURL = "";

        DispatcherTimer intervalTimer = new DispatcherTimer();
        bool watching = false;

        JObject dataCollection = new JObject();
        //Declare Variables End

        public MainWindow()
        {
            var timeStart = DateTime.Now;
            InitializeComponent();
            var version = "1.0";
            using (UpdateCheck check = new UpdateCheck())
            {
                Title = Title + ": v" + version;
                //check.tryCheck(version, 4, "na");
                tConsole("Ran update");
            }
            //Load Currency Rates..Currently supported: USD, GBP, EUR
            //Rates will be used to automatically convert currencies to USD
            try
            {
                WebRequest getVersion = WebRequest.Create("https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.xchange%20where%20pair%20in%20(%22EURUSD%22%2C%20%22GBPUSD%22%2C%20%22CADUSD%22%2C%20%22BRLUSD%22%2C%20%22RUBUSD%22%2C%20%22KRWUSD%22%2C%20%22NZDUSD%22)&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&format=json&callback=&callback=");
                using (Stream responseVersion = getVersion.GetResponse().GetResponseStream())
                {
                    using (StreamReader responseStream = new StreamReader(responseVersion))
                    {
                        string jsonData = responseStream.ReadToEnd();
                        JObject jsonRead = JObject.Parse(jsonData);
                        EURUSD = (double)jsonRead["query"]["results"]["rate"][0]["Rate"];
                        GBPUSD = (double)jsonRead["query"]["results"]["rate"][1]["Rate"];
                        CADUSD = (double)jsonRead["query"]["results"]["rate"][2]["Rate"];
                        BRLUSD = (double)jsonRead["query"]["results"]["rate"][3]["Rate"];
                        RUBUSD = (double)jsonRead["query"]["results"]["rate"][4]["Rate"];
                        KRWUSD = (double)jsonRead["query"]["results"]["rate"][5]["Rate"];
                        NZDUSD = (double)jsonRead["query"]["results"]["rate"][6]["Rate"];
                        tConsole(string.Format("EUR/USD Ratio: {0}", EURUSD));
                        tConsole(string.Format("GBP/USD Ratio: {0}", GBPUSD));
                        tConsole(string.Format("CAD/USD Ratio: {0}", CADUSD));
                        tConsole(string.Format("BRL/USD Ratio: {0}", BRLUSD));
                        tConsole(string.Format("RUB/USD Ratio: {0}", RUBUSD));
                        tConsole(string.Format("KRW/USD Ratio: {0}", KRWUSD));
                        tConsole(string.Format("NZD/USD Ratio: {0}", NZDUSD));
                    }
                }
            }
            catch (Exception ex)
            {
                tConsole(ex.Message);
            }
            var timeend = DateTime.Now - timeStart;
            tConsole(string.Format("App took {0} to load", timeend));
            tConsole("App fully loaded");
        }

        string consoleString = "";
        public void tConsole(object message)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                var newLine = Environment.NewLine;
                string timestamp = "<" + DateTime.Now.ToString() + "> ";
                string newText;
                if (consoleString == "")
                {
                    newText = timestamp + message.ToString();
                }
                else
                {
                    newText = timestamp + message.ToString() + newLine + consoleString;
                }
                consoleString = newText;
                consoleTB.Text = consoleString;
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(
                  DispatcherPriority.Background,
                  new Action(() =>
                  {
                      var newLine = Environment.NewLine;
                      string timestamp = "<" + DateTime.Now.ToString() + "> ";
                      string newText;
                      if (consoleString == "")
                      {
                          newText = timestamp + message;
                      }
                      else
                      {
                          newText = timestamp + message + newLine + consoleString;
                      }
                      consoleString = newText;
                      consoleTB.Text = consoleString;
                  }
                 )
               );
            }
        }
        private double EURUSDc(double eur)
        {
            return eur * EURUSD;
        }
        private double GBPUSDc(double gbp)
        {
            return gbp * GBPUSD;
        }
        private double CADUSDc(double cad)
        {
            return cad * CADUSD;
        }
        private double BRLUSDc(double brl)
        {
            return brl * BRLUSD;
        }
        private double RUBUSDc(double rub)
        {
            return rub * RUBUSD;
        }        
        private double KRWUSDc(double krw)
        {
            return krw * KRWUSD;
        }
        private double NZDUSDc(double nzd)
        {
            return nzd * NZDUSD;
        }
        private void intervalTB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //Keep cursor at same position
            int cp = intervalTB.SelectionStart;

            string str = intervalTB.Text;
            string bstr = str;
            Regex digitsOnly = new Regex(@"[^\d]");
            str = digitsOnly.Replace(str, "");
            intervalTB.Text = str;
            //Take first branch if the original string contained non-numeric, take second otherwise
            if (str != bstr)
            {
                intervalTB.SelectionStart = (cp - 1);
            }
            else
            {
                intervalTB.SelectionStart = cp;
            }
        }
        private void intervalTB_LostFocus(object sender, RoutedEventArgs e)
        {
            if (intervalTB.Text.Length != 0)
            {
                if (Convert.ToInt64(intervalTB.Text) < 10)
                {
                    intervalTB.Text = "10";
                    intervalTB.SelectionStart = 1;
                }
            }
            else if (intervalTB.Text.Length >= 10)
            {
                intervalTB.Text = intervalTB.Text.Remove(intervalTB.Text.Length - 1, 1);
            }
        }

        private string parseHTML (string URL)
        {
            var dom = new CQ();
            try
            {
                dom = CQ.CreateFromUrl(URL);
            }
            catch (WebException)
            {
                return null;
            }
            var rows = dom[".market_listing_row"];

            string htmlEuro = "&#8364;";
            string htmlPound = "&#163;";
            string htmlRuble = "p&#1091;&#1073;.";
            string htmlKRW = "&#8361;";
            string htmlCDN = "CDN$";
            string htmlReal = "R$";
            string htmlNZD = "NZ$";

            JArray parsedPrices = new JArray();
            foreach (var row in rows)
            {
                var id = row.GetAttribute("id");
                var price = dom["#" + id + " > div.market_listing_price_listings_block > div.market_listing_right_cell.market_listing_their_price > span > span.market_listing_price.market_listing_price_with_fee"].Html().Trim();
                double priceF;
                if (price.Contains(htmlEuro))
                {
                    priceF = EURUSDc(Convert.ToDouble(price.Remove(price.IndexOf('&'), 7).Replace(',','.')));
                }
                else if (price.Contains(htmlRuble))
                {
                    priceF = RUBUSDc(Convert.ToDouble(price.Remove(price.IndexOf('p') - 1, 17).Replace(',', '.')));
                }
                else if (price.Contains(htmlKRW))
                {
                    priceF = KRWUSDc(Convert.ToDouble(price.Substring(price.IndexOf(';') + 1).Replace(",", "")));
                }
                else if (price.Contains(htmlPound)) 
                {
                    priceF = GBPUSDc(Convert.ToDouble(price.Substring(price.IndexOf(';') + 1)));
                }
                else if (price.Contains(htmlCDN))
                {
                    priceF = CADUSDc(Convert.ToDouble(price.Substring(price.IndexOf('$') + 2)));
                }
                else if (price.Contains(htmlReal))
                {
                    priceF = BRLUSDc(Convert.ToDouble(price.Substring(price.IndexOf('$') + 2).Replace(',', '.')));
                }
                else if (price.Contains(htmlNZD))
                {
                    priceF = NZDUSDc(Convert.ToDouble(price.Substring(price.IndexOf('$') + 2)));
                }
                else if (price == "Sold!")
                {
                    priceF = 0;
                }
                else
                {
                    try
                    {
                        Regex regex = new Regex(@"^\$?[0-9]+\.?[0-9]*$");
                        if (regex.Match(price).Success)
                        {
                            priceF = Convert.ToDouble(price.Remove(0, 1));
                        }
                        else
                        {
                            throw new FormatException();
                        }
                    }
                    catch (FormatException)
                    {
                        tConsole("Unsupported currency detected: " + price);
                        priceF = 0;
                    }
                }
                if (price != "Sold!")
                {
                    parsedPrices.Add(priceF);
                }
            }
            JObject toJson = new JObject();
            toJson["prices"] = parsedPrices;
            return toJson.ToString();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (watching)
            {
                intervalTimer.Stop();

                watchBtn.Content = "Start Watching";

                dataCollection = new JObject();

                urlTB.IsEnabled = true;
                intervalTB.IsEnabled = true;
                priceTB.IsEnabled = true;

                watching = false;
            }
            else
            {
                tConsole("URL set to: " + urlTB.Text);
                tConsole("Interval set to: " + intervalTB.Text);
                tConsole("Price set to: " + priceTB.Text);
                lfp = Convert.ToDouble(priceTB.Text);
                marketURL = urlTB.Text;
                interval = Convert.ToInt32(intervalTB.Text);

                urlTB.IsEnabled = false;
                intervalTB.IsEnabled = false;
                priceTB.IsEnabled = false;

                watchBtn.Content = "Stop Watching";

                intervalTimer.Interval = TimeSpan.FromSeconds(interval);
                intervalTimer.Tick += intervalTimer_Tick;
                intervalTimer.Start();
                watching = true;
            }
        }

        void intervalTimer_Tick(object sender, EventArgs e)
        {
            var parsedPrices = parseHTML(marketURL);
            JObject jsonData;
            try
            {
                jsonData = JObject.Parse(parsedPrices);
            }
            catch
            {
                watchBtn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                MessageBox.Show("Watching stopped due to steam error. Most likely caused by sending too many requests in too little time.");
                return;
            }
            double avg = 0;
            double median = 0;
            double low = 0;
            int count = 0;
            //Create average
            foreach (var val in jsonData["prices"])
            {
                double price = Convert.ToDouble(val);
                if (price != 0)
                {
                    avg += price;
                    count++;
                }
            }
            //Get real low
            for (int i = 0; i < count; i++)
            {
                if ((double)jsonData["prices"][i] != 0)
                {
                    low = (double)jsonData["prices"][i];
                    break;
                }
            }
            double high = (double)jsonData["prices"][count - 1];
            avg = avg / count;
            int medval = (int)(Math.Ceiling((decimal)(count / 2)));
            median = ((double)jsonData["prices"][medval]);
            tConsole("Median: " + median);
            tConsole("Average: " + avg);
            tConsole("Low: " + low);
            tConsole("High: " + high);

            //Create current data set
            JObject dcContent = new JObject();
            dcContent["high"] = high;
            dcContent["low"] = low;
            dcContent["average"] = avg;
            dcContent["median"] = median;
            dcContent["count"] = count;

            //Add to data collection
            dataCollection[Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()] = dcContent;

            //Populate Treeview
            dcTv.Items.Clear();
            foreach (var row in dataCollection)
            {
                var key = row.Key;
                TreeViewItem item = new TreeViewItem();
                string lfpt;
                if ((double)dataCollection[key]["low"] < lfp)
                {
                    lfpt = " [LOWER]";
                }
                else
                {
                    lfpt = "";
                }
                item.Header = key + " (" + dataCollection[key]["count"] + " prices figured)" + lfpt;
                item.Items.Add("Highest: $" + string.Format("{0:0.00}", dataCollection[key]["high"]));
                item.Items.Add("Lowest: $" + string.Format("{0:0.00}", dataCollection[key]["low"]));
                item.Items.Add("Average: $" + string.Format("{0:0.00}", dataCollection[key]["average"]));
                item.Items.Add("Median: $" + string.Format("{0:0.00}", dataCollection[key]["median"]));
                dcTv.Items.Add(item);
            }
        }
    }
}
