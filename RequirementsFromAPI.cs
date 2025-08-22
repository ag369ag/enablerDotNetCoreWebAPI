using IniParser;
using IniParser.Model;
using ITL.Enabler.API;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Identity.Client;
using Microsoft.PointOfService;
using Newtonsoft.Json;
using RestSharp;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using testASPWebAPI.Data;

namespace testASPWebAPI
{
    public static class RequirementsFromAPI
    {
        public static List<DiscountPresets> discountPresets { get; set; }
        public static ReceiptHeaderFooter receiptHeaderFooter { get; set; }
        public static List<Tax> taxes { get; set; }
        public static string cashierName { get; set; }
        public static string cashierID { get; set; }

        public static PosPrinter posPrinter { get; set; }
        //public static 

      /*  private IniData data;
        private string APIBaseRoute;
        private string getDiscountsRoute;
        private string getReceiptLayoutRoute;
        string taxListRoute;
        private int POSID;*/
        

        public static void GetPrerequisiteData()
        {
            var configParser = new FileIniDataParser();
            IniData data = configParser.ReadFile("config.ini");

            string APIBaseRoute = data["ConnectedAPI"]["APIBaseRoute"];
            string getDiscountsRoute = "http://" + APIBaseRoute + data["ConnectedAPI"]["GetDiscountRoute"];
            string getReceiptLayoutRoute = "http://" + APIBaseRoute + data["ConnectedAPI"]["GetReceiptLayout"];
            string taxListRoute = "http://" + APIBaseRoute + data["ConnectedAPI"]["GetTaxList"];

            int POSID = int.Parse(data["Configuration"]["POS_ID"]);


            string lgnRoute = data["ConnectedAPI"]["LoginRoute"];
            string loginRoute = $"http://{APIBaseRoute}{lgnRoute}";

            string cashierID = data["CashierCreds"]["ID"];
            string cashierPass = data["CashierCreds"]["Password"];

            cashierName = "";

            discountPresets = new List<DiscountPresets>();
            receiptHeaderFooter = new ReceiptHeaderFooter();
            taxes = new List<Tax>();

            //LoginCashier(loginRoute, cashierID, cashierPass);
            GetTaxes(taxListRoute);
            GetDiscountPresets(getDiscountsRoute);
            GetReceiptLayout(getReceiptLayoutRoute, POSID);

            

            //InitializePrinter();
        }

        public static void TestPrint(List<string> receiptLines)
        {
            var configParser = new FileIniDataParser();
            IniData configData = configParser.ReadFile("config.ini");

            string printerIP = configData["Printer"]["printerIP"];
            int printerPort = int.Parse(configData["Printer"]["printerPort"]);

            Socket socket = new Socket(SocketType.Stream, ProtocolType.IP);
            socket.SendTimeout = 1000;
            socket.Connect(printerIP, printerPort);
            byte[] normalprintingCommand = { 0x1B, 0x00 };
            socket.Send(normalprintingCommand);

            List<byte> data = new List<byte>();
            foreach (string text in receiptLines)
            {
                data.AddRange(System.Text.Encoding.ASCII.GetBytes(text));
                data.Add(0x0A);
            }

            socket.Send(data.ToArray());
            List<byte> cutter = new List<byte>()
            {
                0x1D, 0x56, 0x42, 0x00
            };
            socket.Send(cutter.ToArray());
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket.Dispose();
        }

        private static void InitializePrinter()
        {
            PosExplorer posExplorer = new PosExplorer();
            DeviceInfo deviceInfo = null;
            try
            {
                deviceInfo = posExplorer.GetDevice(Microsoft.PointOfService.DeviceType.PosPrinter, "PosPrinter");
                posPrinter = (PosPrinter)posExplorer.CreateInstance(deviceInfo);
                posPrinter.Open();

                ClaimPrinter();
            }
            catch
            {

            }
        }

        private static async Task<bool> ClaimPrinter()
        {
            try
            {
                if (!posPrinter.Claimed)
                {
                    posPrinter.Claim(1000);
                    posPrinter.DeviceEnabled = true;
                    posPrinter.MapMode = MapMode.Metric;
                }

            }
            catch
            {
                return false;
            }
            return true;
        }

        private static async void LoginCashier(string loginRoute, string cashierID, string cashierPass, ApplicationDBContext dbContext)
        {
            try
            {
                /*object cashierData = new
                {
                    number = cashierID,
                    password = cashierPass
                };

                //JsonDocument response = await APIQuery(cashierData, loginRoute);
                Dictionary<string, object> response = await APIQuery(cashierData, loginRoute);
                Console.WriteLine($"Login status code is {response["statusCode"]}");
                //int statusCode = int.Parse(response.RootElement.GetProperty("statusCode").ToString());
                int statusCode = int.Parse(response["statusCode"].ToString());
                Console.WriteLine($"Login route is {loginRoute}");
                Console.WriteLine($"Login status code is {statusCode}");
                if (statusCode == 0)
                {
                    return;
                }

                //JsonElement jsonResponseElement = response.RootElement.GetProperty("data");
                CashierLoginData cashierInfo = new CashierLoginData();
                cashierInfo = JsonConvert.DeserializeObject<CashierLoginData>(response["data"].ToString());
                cashierName = cashierInfo.name.Trim();*/
                cashierName = dbContext.API_Active_Cashier.ToList().First().CashierName;

                Console.WriteLine($"Cashier name is: {cashierName}");

                return;

            }
            catch
            {
                return;
            }
        }
        private static async void GetTaxes(string taxListRoute)
        {
            object blankObject = new { };
            Dictionary<string, object> taxesData = await APIQuery(blankObject, taxListRoute);
            if (int.Parse(taxesData["statusCode"].ToString()) == 0)
            {
                return;
            }
            taxes = JsonConvert.DeserializeObject<List<Tax>>(taxesData["data"].ToString());
        }

        private static async void GetReceiptLayout(string getReceiptLayoutRoute, int POSID)
        {
            object receiptPOS = new
            {
                posID = POSID
            };

            Dictionary<string, object> jsonDoc = await APIQuery(receiptPOS, getReceiptLayoutRoute);

            //ReceiptHeaderFooter receiptHeaderFooter = new ReceiptHeaderFooter();

            try
            {

                //JsonElement jsonResponseElement = jsonDoc.RootElement.GetProperty("data");
                string jsonResponseElement = jsonDoc["data"].ToString();
                //receiptHeaderFooter = JsonConvert.DeserializeObject<ReceiptHeaderFooter>(jsonResponseElement.ToString());
                receiptHeaderFooter = JsonConvert.DeserializeObject<ReceiptHeaderFooter>(jsonResponseElement);

            }
            catch
            {
               
            }
        }

        private static async void GetDiscountPresets(string getDiscountsRoute)
        {
            try
            {
                discountPresets.Clear();
                object emptyObject = new { };
                //JsonDocument decodedResponse = await APIQuery(emptyObject, getDiscountsRoute);
                //JsonElement jsonResponseElement = decodedResponse.RootElement.GetProperty("data");
                Dictionary<string, object> decodedResponse = await APIQuery(emptyObject, getDiscountsRoute);
                string jsonResponseElement = decodedResponse["data"].ToString();
                Console.WriteLine(jsonResponseElement);
                int discType = 0;
                foreach (var data in JsonDocument.Parse(jsonResponseElement).RootElement.EnumerateArray())
                {
                    discType = int.Parse(data.GetProperty("discount_type").ToString());
                    JsonElement presets = data.GetProperty("discPreset");
                    foreach (var presetItem in presets.EnumerateArray())
                    {
                        int presetID = int.Parse(presetItem.GetProperty("preset_id").ToString());
                        int discountID = int.Parse(data.GetProperty("discount_id").ToString());
                        string presetName = presetItem.GetProperty("preset_name").ToString();
                        double presetValue = double.Parse(presetItem.GetProperty("preset_value").ToString());
                        discountPresets.Add(new DiscountPresets(presetID, presetName, discountID, discType, presetValue));
                    }
                }
                

            }
            catch (Exception ex)
            {
            }
        }
        private static async Task<Dictionary<string, object>> APIQuery(object obj, string route)
        {
            HttpClient http = new HttpClient();
            HttpResponseMessage response = null;

            if (!(obj == null))
            {
                string content = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                StringContent contentString = new StringContent(content, Encoding.UTF8, "application/json");
                Console.WriteLine($"Post content:  {content}");
                response = await http.PostAsync(route, contentString);
            }
            else
            {
                response = await http.GetAsync(route);
            }

            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            JsonDocument responseJson = JsonDocument.Parse(responseString);

            Dictionary<string, object> responseDictionary = new Dictionary<string, object>();
            responseDictionary = responseJson.Deserialize<Dictionary<string, object>>();

            return responseDictionary;

        }

        
    }
}
