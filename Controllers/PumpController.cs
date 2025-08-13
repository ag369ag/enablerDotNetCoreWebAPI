using Azure;
using IniParser;
using IniParser.Model;
using ITL.Enabler.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.PointOfService;
using Newtonsoft.Json;
using Registration;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using testASPWebAPI.Data;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace testASPWebAPI.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize]

    public class PumpController : ControllerBase
    {
        private Forecourt fore = PumpClass.forecourt;

        private IniData data;

        private string server;
        private int terminalID;
        private string password;
        private int pumpStart;
        private int pumpEnd;
        private int POSID = 0;
        private int transactionNum = 0;
        private string siNum = "";

        private string APIBaseRoute;
        private string addNewTransactionRoute;
        private string saveToLogRoute;
        private string saveToJournalRoute;
        private bool autoPrint;
        //private string getDiscountsRoute;
        //private string getReceiptLayoutRoute;


        private int ReceiptCharLength;
        private string posPrinterLogicalName = "PosPrinter";
        const int MAX_LINE_WIDTHS = 2;

        private string cashierName;
        private string cshrID;

        private string[] mopLastValues;

        private PosPrinter posPrinter = PumpClass.posPrinter;

        private readonly ILogger<PumpController> _logger;

        private readonly ApplicationDBContext _dbContext;

        List<TransactionItemClass> transactionItems = new List<TransactionItemClass>();
        List<MopCardInfo> transactionMOPS = new List<MopCardInfo>();
        CustomerInformations customerInfo = new CustomerInformations();
        List<DiscountPresets> discountPresets = new List<DiscountPresets>();
        List<string> receiptString = new List<string>();
        List<Tax> taxes = new List<Tax>();
        ReceiptHeaderFooter receiptHeaderFooter = new ReceiptHeaderFooter();


        public PumpController(ILogger<PumpController> logger, ApplicationDBContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            var configParser = new FileIniDataParser();
            data = configParser.ReadFile("config.ini");

            // from configuration
            server = data["Configuration"]["Server"];
            terminalID = int.Parse(data["Configuration"]["TerminalID"]);
            password = data["Configuration"]["Password"];
            pumpStart = int.Parse(data["Configuration"]["PumpStartID"]);
            pumpEnd = int.Parse(data["Configuration"]["PumpEndID"]);
            POSID = int.Parse(data["Configuration"]["POS_ID"]);

            // from printer
            ReceiptCharLength = int.Parse(data["Printer"]["ReceiptCharLength"]);

            // from connected API
            APIBaseRoute = data["ConnectedAPI"]["APIBaseRoute"];
            addNewTransactionRoute = "http://" + APIBaseRoute + data["ConnectedAPI"]["AddToTransactionRoute"];
            saveToLogRoute = "http://" + APIBaseRoute + data["ConnectedAPI"]["SaveToLogRoute"];
            saveToJournalRoute = "http://" + APIBaseRoute + data["ConnectedAPI"]["SaveToJournalRoute"];
            autoPrint = bool.Parse(data["Printer"]["AutoPrint"].ToString());
            //getDiscountsRoute = "http://" + APIBaseRoute + data["ConnectedAPI"]["GetDiscountRoute"];
            //getReceiptLayoutRoute = "http://" + APIBaseRoute + data["ConnectedAPI"]["GetReceiptLayout"];

            cshrID = "";


            if (!fore.IsConnected)
            {
                fore.Connect(server, terminalID, "", password, true);
            }


        }





        /*  private async Task<bool> loginCashier()
          {
              try
              {
                  string cashierID = data["CashierCreds"]["ID"];
                  string cashierPass = data["CashierCreds"]["Password"];
                  string lgnRoute = data["ConnectedAPI"]["LoginRoute"];
                  string loginRoute = $"http://{APIBaseRoute}{lgnRoute}";
                  object cashierData = new
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
                      return false;
                  }

                  //JsonElement jsonResponseElement = response.RootElement.GetProperty("data");
                  CashierLoginData cashierInfo = new CashierLoginData();
                  cashierInfo = JsonConvert.DeserializeObject<CashierLoginData>(response["data"].ToString());
                  cashierName = cashierInfo.name.Trim();

                  Console.WriteLine($"Cashier name is: {cashierName}");

                  return true;

              }
              catch
              {
                  return false;
              }
          }*/

        /// <summary>
        /// Getting Pump Data
        /// </summary>
        /// <returns>Pump data</returns>
        //[Route("[action]")]
        // [Tags("THIS ROUTE IS FOR GETTING PUMP DATA")]
        //[EndpointDescription("This route doesn't need parameter")]
        //[FromBody]
        [HttpGet(Name = "Getting pump data")]
        public async Task<JsonResult> GetPumpData()
        {
            /* while (!fore.IsConnected)
             {
             }*/

            /*bool isLoggedIn = await loginCashier();
            if (!isLoggedIn)
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "Cashier is not logged in" });
                notRegisteredResponse.StatusCode = 401;
                return notRegisteredResponse;
            }*/

            if (!IsRegistered())
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "API is not yet registered" });
                notRegisteredResponse.StatusCode = 401;
                return notRegisteredResponse;
            }

            if (!isForecourtConnected())
            {
                isForecourtConnected();
            }

            List<PumpDataClass> pumpList = new List<PumpDataClass>();
            foreach (Pump item in fore.Pumps)
            {
                /*List<TransactionClass> transList = new List<TransactionClass>();
                /foreach (Transaction trans in item.TransactionStack)
                {

                    transList.Add(new TransactionClass
                    {
                        deliveryID = trans.DeliveryData.DeliveryID,
                        deliveryGrade = trans.DeliveryData.Grade.ToString(),
                        deliveryUnitPrice = trans.DeliveryData.UnitPrice,
                        deliveryQuantity = trans.DeliveryData.Quantity,
                        deliveryAmount = trans.DeliveryData.Money,
                        deliveryLockStatus = trans.IsLocked
                    });
                }
                if(item.CurrentTransaction != null)
                {
                    
                        transList.Add(new TransactionClass
                        {
                            deliveryID = item.CurrentTransaction.DeliveryData.DeliveryID,
                            deliveryGrade = item.CurrentTransaction.DeliveryData.Grade.ToString(),
                            deliveryUnitPrice = item.CurrentTransaction.DeliveryData.UnitPrice,
                            deliveryQuantity = item.CurrentTransaction.DeliveryData.Quantity,
                            deliveryAmount = item.CurrentTransaction.DeliveryData.Money,
                            deliveryLockStatus = item.CurrentTransaction.IsLocked
                        });
                    
                    
                }*/

                if (pumpStart <= item.Number && pumpEnd >= item.Number)
                {
                    pumpList.Add(new PumpDataClass
                    {
                        pumpName = item.Name,
                        pumpNumber = item.Number
                    });
                }
            }

            SaveQueryLog($"Get all pump data.\nResult: {Newtonsoft.Json.JsonConvert.SerializeObject(pumpList)}");
            return new JsonResult(pumpList);
        }

        //[Route("[action]")]
        [HttpPost(Name = "getTransactionByPumpID")]
        public async Task<JsonResult> getTransactionByPumpID(JsonDocument json)
        {

            /*while (!fore.IsConnected)
            {

            }*/

            /*bool isLoggedIn = await loginCashier();
            if (!isLoggedIn)
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "Cashier is not logged in" });
                notRegisteredResponse.StatusCode = 401;
                return notRegisteredResponse;
            }*/

            if (!IsRegistered())
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "API is not yet registered" });
                notRegisteredResponse.StatusCode = 401;
                return notRegisteredResponse;
            }

            if (!isForecourtConnected())
            {
                isForecourtConnected();
            }

            int pumpID = json.RootElement.GetProperty("pumpID").GetInt32();
            List<TransactionClass> pumpTransactions = new List<TransactionClass>();

            try
            {
                Pump selectedPump = fore.Pumps[pumpID];
                if (selectedPump == null)
                {
                    JsonResult nullResponse = new JsonResult(new { status = "Failed", data = "Pump does not exist" });
                    SaveQueryLog($"Get transaction by pump {pumpID}\n Result: Failed, Data: Pump does not exist");
                    nullResponse.StatusCode = 404;
                    return nullResponse;
                }
                if (selectedPump.TransactionStack.Count > 0)
                {
                    foreach (Transaction trans in selectedPump.TransactionStack)
                    {
                        if (trans.IsLocked)
                        {
                            continue;
                        }

                        pumpTransactions.Add(new TransactionClass
                        {
                            deliveryID = trans.DeliveryData.DeliveryID,
                            deliveryGrade = trans.DeliveryData.Grade.ToString(),
                            deliveryUnitPrice = trans.DeliveryData.UnitPrice,
                            deliveryQuantity = trans.DeliveryData.Quantity,
                            deliveryAmount = trans.DeliveryData.Money,
                            deliveryLockStatus = trans.IsLocked,
                            isCurrentTransaction = false
                        });
                    }
                }

                if (selectedPump.CurrentTransaction != null && !selectedPump.CurrentTransaction.IsLocked && selectedPump.State != PumpState.Delivering)
                {
                    try
                    {
                        pumpTransactions.Add(new TransactionClass
                        {
                            deliveryID = selectedPump.CurrentTransaction.DeliveryData.DeliveryID,
                            deliveryGrade = selectedPump.CurrentTransaction.DeliveryData.Grade.ToString(),
                            deliveryUnitPrice = selectedPump.CurrentTransaction.DeliveryData.UnitPrice,
                            deliveryQuantity = selectedPump.CurrentTransaction.DeliveryData.Quantity,
                            deliveryAmount = selectedPump.CurrentTransaction.DeliveryData.Money,
                            deliveryLockStatus = selectedPump.CurrentTransaction.IsLocked,
                            isCurrentTransaction = true
                        });

                    }
                    catch (Exception ex)
                    {
                        JsonResult result = new JsonResult(new { status = "Failed", data = ex.Message });
                        SaveQueryLog($"Get transaction by pump {pumpID}\n Result: Failed, Data: {ex.Message}");
                        result.StatusCode = 404;
                        var file = Assembly.GetExecutingAssembly().Location;
                        Process.Start(file);
                        //return result;


                    }


                }

                PumpClass pumpData = new PumpClass
                {
                    pumpName = selectedPump.Name,
                    pumpNumber = pumpID,
                    pumpStack = selectedPump.TransactionStack.Count,
                    transactions = pumpTransactions
                };

                JsonResult response = new JsonResult(new { status = "Success", data = pumpData });
                SaveQueryLog($"Get transaction by pump {pumpID}\n Result: Success, Data: {Newtonsoft.Json.JsonConvert.SerializeObject(pumpData)}");

                return response;
            }
            catch (Exception ex)
            {
                JsonResult result = new JsonResult(new { status = "Failed", data = ex.Message });
                SaveQueryLog($"Get transaction by pump {pumpID}\n Result: Failed, Data: {ex.Message}");
                result.StatusCode = 404;
                return result;
            }
        }



        /*//[Route("[action]")]
        [HttpPost(Name = "GetTransactionInfo")]
        public JsonResult GetTransactionInfo(JsonDocument json)
        {
            if (!IsRegistered())
            {
                return new JsonResult(new { Message = "API is not yet registered" });
            }

            while (!fore.IsConnected)
            {
            }

            int pumpID = json.RootElement.GetProperty("pumpID").GetInt32();
            int deliveryID = json.RootElement.GetProperty("deliveryID").GetInt32();
            

            Pump selectedPump = fore.Pumps[pumpID];
            TransactionClass selectedTransaction;
            Transaction equivalentTransaction = getTransactionByID(pumpID, deliveryID);
            //selectedPump.TransactionStack.TryGetValue(deliveryID, out equivalentTransaction);

            if (equivalentTransaction != null)
            {
                selectedTransaction = new TransactionClass
                {
                    deliveryID = equivalentTransaction.DeliveryData.DeliveryID,
                    deliveryGrade = equivalentTransaction.DeliveryData.Grade.ToString(),
                    deliveryUnitPrice = equivalentTransaction.DeliveryData.UnitPrice,
                    deliveryQuantity = equivalentTransaction.DeliveryData.Quantity,
                    deliveryAmount = equivalentTransaction.DeliveryData.Money,
                    deliveryLockStatus = equivalentTransaction.IsLocked
                };
                JsonResult result = new JsonResult(new { Status = "Success", Pumpid = pumpID, DeliveryID = deliveryID, SelectedTransaction = selectedTransaction });

                SaveQueryLog($"Get transaction info from pump {pumpID} with deliveryID {deliveryID}\n Result: Success, Data: {Newtonsoft.Json.JsonConvert.SerializeObject(selectedTransaction)}");
                return result;
            }

            else
            {
                JsonResult response = new JsonResult(new { Status = "Failed", Message = "No such transaction exist." });
                SaveQueryLog($"Get transaction info from pump {pumpID} with deliveryID {deliveryID}\n Result: Failed. Message: No such transaction exist.");
                response.StatusCode = 500;
                return response;
            }

            
        }*/

        [HttpPost(Name = "UpdateActiveCashier")]
        public async Task<JsonResult> UpdateActiveCashier(JsonDocument json)
        {

            try
            {
                Dictionary<string, string> request = new Dictionary<string, string>();
                request = JsonConvert.DeserializeObject<Dictionary<string, string>>(json.RootElement.GetRawText());

                int? cashierID = int.Parse(request["CashierID"]);
                string? cashierName = request["CashierName"];
                string desc = request["Description"];

                //Console.WriteLine(id);
                Console.WriteLine(cashierID);
                Console.WriteLine(cashierName);
                Console.WriteLine(desc);

                API_Active_Cashier activeCashier = new API_Active_Cashier()
                {
                    ID = 1,
                    CashierName = cashierName,
                    Description = desc,
                    CashierID = cashierID
                };

                _dbContext.API_Active_Cashier.Update(activeCashier);
                await _dbContext.SaveChangesAsync();
                
            }
            catch (Exception ex)
            { 
                JsonResult response = new JsonResult(new {  statusCode = 0, statusDescription = "Failed"  });
                response.StatusCode = 401;
                SaveQueryLog($"Updating active cashier info.\nResult: Failed. Message: {ex.Message}");
                return response;
            }

            JsonResult resp = new JsonResult(new { statusCode = 1, statusDescription = "Success", data = "Updating active cashier successful." });
            resp.StatusCode = 200;
            SaveQueryLog($"Updating active cashier info.\nResult: Success. Message: Updating active cashier successful.");
            return resp;
        }



        //[Route("[action]")]
        [HttpPost(Name = "VoidTransaction")]
        public async Task<JsonResult> VoidTransaction(JsonDocument json)
        {
            /*while (!fore.IsConnected)
            {
            }*/

            /*bool isLoggedIn = await loginCashier();
            if (!isLoggedIn)
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "Cashier is not logged in" });
                notRegisteredResponse.StatusCode = 401;
                return notRegisteredResponse;
            }*/

            if (!IsRegistered())
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "API is not yet registered" });
                notRegisteredResponse.StatusCode = 401;
                return notRegisteredResponse;
            }

            if (!isForecourtConnected())
            {
                isForecourtConnected();
            }

            int pumpID = json.RootElement.GetProperty("pumpID").GetInt32();
            int deliveryID = json.RootElement.GetProperty("deliveryID").GetInt32();


            Pump selectedPump = fore.Pumps[pumpID];
            TransactionClass selectedTransaction;
            Transaction equivalentTransaction = getTransactionByID(pumpID, deliveryID);
            //Transaction equivalentTransaction;
            //selectedPump.TransactionStack.TryGetValue(deliveryID, out equivalentTransaction);




            bool result = false;
            string message = "";
            try
            {
                if (equivalentTransaction != null)
                {
                    equivalentTransaction.ReleaseLock();
                    result = true;
                }
                else
                {
                    message = "Transaction does not exist.";
                    result = false;
                }
            }
            catch (Exception ex)
            {
                result = false;
                message = ex.Message;
            }

            if (result)
            {
                selectedTransaction = new TransactionClass
                {
                    deliveryID = equivalentTransaction.DeliveryData.DeliveryID,
                    deliveryGrade = equivalentTransaction.DeliveryData.Grade.ToString(),
                    deliveryUnitPrice = equivalentTransaction.DeliveryData.UnitPrice,
                    deliveryQuantity = equivalentTransaction.DeliveryData.Quantity,
                    deliveryAmount = equivalentTransaction.DeliveryData.Money,
                    deliveryLockStatus = equivalentTransaction.IsLocked,
                    isCurrentTransaction = fore.Pumps[pumpID].CurrentTransaction == equivalentTransaction
                };
                JsonResult response = new JsonResult(new { Status = "Success", Pumpid = pumpID, DeliveryID = deliveryID, SelectedTransaction = selectedTransaction });
                SaveQueryLog($"Void transaction from pump {pumpID} with deliveryID {deliveryID}\nResult: {Newtonsoft.Json.JsonConvert.SerializeObject(selectedTransaction)}");
                return response;
            }
            else
            {
                JsonResult response = new JsonResult(new { Status = "Failed", Message = message ?? "Error voiding transaction" });
                SaveQueryLog($"Void transaction from pump {pumpID} with deliveryID {deliveryID}\nResult: Failed. Message: {message ?? "Error voiding transaction"}");
                response.StatusCode = 404;
                return response;
            }

        }

        private Transaction getTransactionByID(int pumpID, int deliveryID)
        {
            Pump selectedPump = fore.Pumps[pumpID];
            
            if (selectedPump.CurrentTransaction != null && selectedPump.CurrentTransaction.DeliveryData.DeliveryID == deliveryID)
            {
                return selectedPump.CurrentTransaction;
            }
            else
            {
                Transaction eqivalentTrans = null;
                foreach (Transaction trans in selectedPump.TransactionStack)
                {
                    if (!(trans.DeliveryData.DeliveryID == deliveryID))
                    {
                        continue;
                    }
                    eqivalentTrans = trans;
                }
                if (eqivalentTrans != null)
                {
                    return eqivalentTrans;
                }
                return null;

            }

        }


        //[Route("[action]")]
        [HttpPost(Name = "LockTransaction")]
        public async Task<JsonResult> LockTransaction(JsonDocument json)
        {
            /*bool isLoggedIn = await loginCashier();
            if (!isLoggedIn)
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "Cashier is not logged in" });
                notRegisteredResponse.StatusCode = 401;
                return notRegisteredResponse;
            }*/

            if (!IsRegistered())
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "API is not yet registered" });
                notRegisteredResponse.StatusCode = 401;
                return notRegisteredResponse;
            }

            if (!isForecourtConnected())
            {
                isForecourtConnected();
            }

            int pumpID = json.RootElement.GetProperty("pumpID").GetInt32();
            int deliveryID = json.RootElement.GetProperty("deliveryID").GetInt32();

            Pump selectedPump = fore.Pumps[pumpID];

            Transaction equivalentTransaction = getTransactionByID(pumpID, deliveryID);
            //Transaction equivalentTransaction;

            //selectedPump.TransactionStack.TryGetValue(deliveryID, out equivalentTransaction);



            bool result = false;
            string message = "";
            bool lockStatus = false;
            if (equivalentTransaction != null)
            {
                if (equivalentTransaction.IsLocked)
                {
                    result = false;
                    message = "Transaction is locked already.";
                    if (equivalentTransaction.LockedById != terminalID)
                    {
                        message = $"Transaction is locked by {equivalentTransaction.LockedById}";
                    }
                }
                else
                {
                    try
                    {
                        equivalentTransaction.GetLock();
                        result = true;
                    }
                    catch (EnablerException e)
                    {
                        result = false;
                        message = e.Message;
                    }
                }
            }
            else
            {
                result = false;
                message = "Transaction does not exist.";
            }


            Transaction transaction = getTransactionByID(pumpID, deliveryID);
            //Transaction transaction; 
            //selectedPump.TransactionStack.TryGetValue(deliveryID, out transaction);



            if (transaction != null)
            {
                if (transaction.IsLocked)
                {
                    lockStatus = true;
                }
                else
                {
                    lockStatus = false;
                }
            }

            if (result)
            {
                JsonResult response = new JsonResult(new { Status = "Success", LockStatus = lockStatus });
                SaveQueryLog($"Lock transaction from pump {pumpID} with deliveryID {deliveryID}.\nResult: Success, LockStatus: {lockStatus}");
                return response;
            }
            else
            {
                JsonResult response = new JsonResult(new { Status = "Failed", LockStatus = lockStatus, Message = message });
                response.StatusCode = 404;
                SaveQueryLog($"Lock transaction from pump {pumpID} with deliveryID {deliveryID}.\nResult: Failed. Message: {message}");
                return response;
            }
        }



        /* //[Route("[action]")]
         [HttpPost(Name = "ClearTransaction")]
         public async Task<JsonResult> ClearTransaction(JsonDocument json)
         {
             bool isLoggedIn = await loginCashier();
             if (!isLoggedIn)
             {
                 JsonResult notRegisteredResponse = new JsonResult(new { Message = "Cashier is not logged in" });
                 notRegisteredResponse.StatusCode = 401;
                 return notRegisteredResponse;
             }

             if (!IsRegistered())
             {
                 JsonResult notRegisteredResponse = new JsonResult(new { Message = "API is not yet registered" });
                 notRegisteredResponse.StatusCode = 401;
                 return notRegisteredResponse;
             }

             if (!isForecourtConnected())
             {
                 isForecourtConnected();
             }

             int pumpID = json.RootElement.GetProperty("pumpID").GetInt32();
             int deliveryID = json.RootElement.GetProperty("deliveryID").GetInt32();
             //json.RootElement.GetProperty("transactionType").ToString()
             //TransactionClearTypes type = TransactionClearTypes.Normal;

             Pump selectedPump = fore.Pumps[pumpID];
             TransactionClass DeliveryInformation;
             Transaction equivalentTransaction = getTransactionByID(pumpID, deliveryID);
             //Transaction equivalentTransaction;
             //selectedPump.TransactionStack.TryGetValue(deliveryID, out equivalentTransaction);




             bool result = false;
             string message = "";
             try
             {
                 if (equivalentTransaction != null)
                 {
                     equivalentTransaction.Clear(TransactionClearTypes.Normal);
                     result = true;
                 }
                 else
                 {
                     message = "Transaction does not exist.";
                 }

             }
             catch (Exception ex)
             {
                 message = ex.Message;
             }

             if (result)
             {
                 DeliveryInformation = new TransactionClass
                 {
                     deliveryID = equivalentTransaction.DeliveryData.DeliveryID,
                     deliveryGrade = equivalentTransaction.DeliveryData.Grade.ToString(),
                     deliveryUnitPrice = equivalentTransaction.DeliveryData.UnitPrice,
                     deliveryQuantity = equivalentTransaction.DeliveryData.Quantity,
                     deliveryAmount = equivalentTransaction.DeliveryData.Money,
                     deliveryLockStatus = equivalentTransaction.IsLocked,
                     isCurrentTransaction = fore.Pumps[pumpID].CurrentTransaction == equivalentTransaction
                 };

                 JsonResult response = new JsonResult(new { Status = "Success", Pumpid = pumpID, DeliveryID = deliveryID, SelectedTransaction = DeliveryInformation });
                 SaveQueryLog($"Clear transaction from pump {pumpID} with deliveryID {deliveryID}.\nResult: {Newtonsoft.Json.JsonConvert.SerializeObject(DeliveryInformation)}");
                 return response;
             }
             else
             {
                 JsonResult response = new JsonResult(new { Status = "Failed", Message = message ?? "Error" });
                 SaveQueryLog($"Clear transaction from pump {pumpID} with deliveryID {deliveryID}.\nResult: Failed. Message: {message ?? "Error"}");
                 response.StatusCode = 404;
                 return response;
             }
         }
 */

        private void ClearTransaction(int pumpID, int deliveryID, out bool isCompleted, out string resultMessage)
        {
            Pump selectedPump = fore.Pumps[pumpID];
            TransactionClass DeliveryInformation;
            Transaction equivalentTransaction = getTransactionByID(pumpID, deliveryID);

            bool result = false;
            resultMessage = "";
            try
            {
                if (equivalentTransaction != null)
                {
                    equivalentTransaction.Clear(TransactionClearTypes.Normal);
                    result = true;
                }
                else
                {
                    resultMessage = "Transaction does not exist.";
                }

            }
            catch (Exception ex)
            {
                resultMessage = ex.Message;
            }

            if (result)
            {
                DeliveryInformation = new TransactionClass
                {
                    deliveryID = equivalentTransaction.DeliveryData.DeliveryID,
                    deliveryGrade = equivalentTransaction.DeliveryData.Grade.ToString(),
                    deliveryUnitPrice = equivalentTransaction.DeliveryData.UnitPrice,
                    deliveryQuantity = equivalentTransaction.DeliveryData.Quantity,
                    deliveryAmount = equivalentTransaction.DeliveryData.Money,
                    deliveryLockStatus = equivalentTransaction.IsLocked
                };

                SaveQueryLog($"Clear transaction from pump {pumpID} with deliveryID {deliveryID}.\nResult: {Newtonsoft.Json.JsonConvert.SerializeObject(DeliveryInformation)}");
                isCompleted = true;
            }
            else
            {

                SaveQueryLog($"Clear transaction from pump {pumpID} with deliveryID {deliveryID}.\nResult: Failed. Message: {resultMessage ?? "Error"}");
                isCompleted = false;
            }


        }

        [HttpPost(Name = "SaveToDatabase")]
        public async Task<JsonResult> SaveToDatabase(JsonDocument document)
        {
            try
            {

           

            cashierName = _dbContext.API_Active_Cashier.ToList().First().CashierName;
            cshrID = _dbContext.API_Active_Cashier.ToList().First().CashierID.ToString();

            transactionItems = new List<TransactionItemClass>();
            transactionMOPS = new List<MopCardInfo>();
            customerInfo = new CustomerInformations();

            //cashierName = RequirementsFromAPI.cashierName;
            taxes = RequirementsFromAPI.taxes;
            discountPresets = RequirementsFromAPI.discountPresets;
            receiptHeaderFooter = RequirementsFromAPI.receiptHeaderFooter;

            CFFLS cffls = new CFFLS();

            /* bool isLoggedIn = await loginCashier();
             if (!isLoggedIn)
             {
                 JsonResult notRegisteredResponse = new JsonResult(new { Message = "Cashier is not logged in" });
                 notRegisteredResponse.StatusCode = 401;
                 return notRegisteredResponse;
             }*/

            if (!IsRegistered())
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "API is not yet registered" });
                notRegisteredResponse.StatusCode = 401;
                return notRegisteredResponse;
            }

            if (!isForecourtConnected())
            {
                isForecourtConnected();
            }



            /*
                        InitializePrinter();

                        bool isPrinterClaimed = await ClaimPrinter();
                        if (!isPrinterClaimed)
                        {
                            JsonResult notRegisteredResponse = new JsonResult(new { Message = "Error claiming printer/printer not found." });
                            notRegisteredResponse.StatusCode = 404;
                            return notRegisteredResponse;
                        }
            */
            /*bool gettingDiscounts = await GetDiscounts();

            if (!gettingDiscounts)
            {
                JsonResult result = new JsonResult(new { result = "Failed", Message = "Error in getting discounts" });
                return result;
            }*/



            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, object>>(document.RootElement.GetRawText());

            int itemNum = 1;
            int newItemNum = getDeliveries(keyValuePairs["deliveries"].ToString(), itemNum);

            if(newItemNum == 0)
            {
                JsonResult invalidDeliveryID = new JsonResult(new { Message = "Delivery does not exist" });
                invalidDeliveryID.StatusCode = 404;
                return invalidDeliveryID;
            }


            mopLastValues = getMops(keyValuePairs["mop"].ToString(), newItemNum);

            /*JsonResult result1 = new JsonResult(new { result = "test", Message = "test" });
            return result1;*/

            bool getCustomerInfo = getCustomerDetails(keyValuePairs["customerInfo"].ToString());

            string rptToPrint = keyValuePairs["rptToPrint"].ToString();

            cffls = JsonConvert.DeserializeObject<CFFLS>(keyValuePairs["cffls"].ToString());

            string vehicleType = keyValuePairs["VehicleTypeID"].ToString();
            string attendantID = keyValuePairs["attendantID"].ToString();
            string pgCardNumber = keyValuePairs["pgCardNumber"].ToString();

            if (!getCustomerInfo)
            {
                JsonResult result = new JsonResult(new { result = "Failed", Message = "Failed getting value" });
                return result;
            }

            List<object> DataArray = new List<object>();

            string data = Newtonsoft.Json.JsonConvert.SerializeObject(DataArray);

            string transItems = Newtonsoft.Json.JsonConvert.SerializeObject(transactionItems);

            double taxtotalValue = transactionItems.Where(a => a.itemType == 2).Sum(a => a.itemTaxAmount);
            double subtotalValue = transactionItems.Where(a => a.itemType == 2 || a.itemType == 52).Sum(a => a.itemValue);

            object PaymentData = new
            {
                cashierID = cshrID,
                subAccID = "",
                accountID = "",
                posID = POSID,
                num = "",
                periodID = "",
                taxTotal = taxtotalValue,
                saleTotal = subtotalValue,
                isManual = "0",
                isZeroRated = "0",
                customerName = customerInfo.name ?? mopLastValues[3],
                address = customerInfo.add,
                TIN = customerInfo.TINNumber,
                businessStyle = customerInfo.busStyle,
                cardNumber = mopLastValues[2],
                approvalCode = mopLastValues[0],
                bankCode = mopLastValues[1],
                type = "1",
                itemsString = transItems,
                isRefund = "0",
                transaction_type = "1",
                isRefundOrigTransNum = "",
                transaction_resetter = "",
                birReceiptType = "2",
                birTransNum = "",
                poNum = "",
                plateNum = "",
                odometer = "",
                transRefund = "0",
                grossRefund = "0",
                subAccAmt = "",
                vehicleTypeID = vehicleType,
                isNormalTrans = "1",
                attendantID = "",
                items = transactionItems,
                scData = data,
                pwdData = data,
                spData = data,
                naacData = data,
                movData = data,
                serviceAmount = 0
            };




            string doneSaving = "";

            try
            {
                doneSaving = await SendToAPI(PaymentData);
            }
            catch (Exception ex)
            {
                var file = Assembly.GetExecutingAssembly().Location;
                Process.Start(file);
            }

            if (string.IsNullOrEmpty(doneSaving))
            {

                JsonResult result = new JsonResult(new { result = "Failed", Message = doneSaving, task = "Send to api" });
                return result;

            }

            // done saving sample
            // {"resetter":0,"or_num":17,"transID":"28"}

            AddToTransResponse transNums = new AddToTransResponse();
            transNums = JsonConvert.DeserializeObject<AddToTransResponse>(doneSaving);

            string receiptOrNumber = transNums.or_num.ToString();
            string receiptResetter = transNums.resetter.ToString();
            string receiptTransID = transNums.transID.ToString();


            bool receipt = true;
            receipt = await createReceipt(transactionItems, receiptOrNumber, receiptResetter, receiptTransID, rptToPrint);

            if (!receipt)
            {
                JsonResult result = new JsonResult(new { result = "Failed", Message = "Failed creating receipt" });
                return result;
            }

            bool XMLCreated = await CreateXML(receiptOrNumber, receiptResetter, taxtotalValue, subtotalValue, vehicleType, attendantID, pgCardNumber, PaymentData, cffls);

            if (!XMLCreated)
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "Error generating XML" });
                notRegisteredResponse.StatusCode = 500;
                return notRegisteredResponse;
            }

            // Clearing transactions
            foreach (TransactionItemClass fuelItems in transactionItems.Where(a => a.itemType == 2))
            {

                ClearTransaction((int)fuelItems.pumpID, fuelItems.deliveryID, out bool isCompleted, out string errorMessage);

                if (!isCompleted)
                {
                    JsonResult result = new JsonResult(new { result = "Failed", Message = errorMessage });
                    return result;
                }
            }

            bool receiptPrinted = true;
            receiptPrinted = await PrintReceipt();
            if (!receiptPrinted)
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = "Error printing receipt" });
                notRegisteredResponse.StatusCode = 500;
                return notRegisteredResponse;
            }


            JsonResult res = new JsonResult(new { result = "Success" });
            return res;
            
            }
            catch (Exception e)
            {
                JsonResult notRegisteredResponse = new JsonResult(new { Message = e.Message });
                notRegisteredResponse.StatusCode = 500;
                return notRegisteredResponse;
            }


        }

        private async Task<bool> CreateXML(string orNum, string orResetter, double taxtotalValue, double subtotalValue, string vehicleType, string attendantID, string pgCardNumber, object paymentData, CFFLS cffls)
        {
            /* try
             {*/
            DateTime nowDate = DateTime.Now;                            //System date
            DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();   //Date Format
            dateFormat.MonthDayPattern = "MM";
            string strDate = nowDate.ToString("MM/dd/yyyy hh:mm tt", dateFormat);
            DateTime dt = Convert.ToDateTime(strDate);

            string fileName = data["XMLData"]["XMLDir"] + "trans_req.xml";
            string corpCode = data["XMLData"]["CorpCode"];
            string siteCode = data["XMLData"]["SiteCode"];
            string pstid = data["XMLData"]["PSTID"];
            string priceCode = data["XMLData"]["PriceCode"];
            string formattedORNum = FormatInvoiceNum(orNum.Trim(), orResetter.Trim());

            string paymentDataString = JsonConvert.SerializeObject(paymentData);
            Dictionary<string, object> pd = new Dictionary<string, object>();
            pd = JsonConvert.DeserializeObject<Dictionary<string, object>>(paymentDataString);


            using (XmlWriter wr = XmlWriter.Create(fileName))
            {
                wr.WriteStartDocument();
                wr.WriteStartElement("xml");
                wr.WriteAttributeString("CORPCODE", corpCode);
                wr.WriteAttributeString("SITECODE", siteCode);
                wr.WriteAttributeString("PSTID", pstid);
                wr.WriteAttributeString("PRICECODE", priceCode);
                wr.WriteAttributeString("PROPERTYTAG", "EXPOSE19720818");
                wr.WriteAttributeString("FLD001", "001");

                wr.WriteStartElement("TICKET");

                wr.WriteStartElement("HEADER");
                wr.WriteAttributeString("YEAR", dt.Year.ToString());    //yy
                wr.WriteAttributeString("MONTH", dt.Month.ToString());     //mm
                wr.WriteAttributeString("DAY", dt.Day.ToString());       //dd
                wr.WriteAttributeString("TIME", nowDate.ToShortTimeString());


                Periods prds = new Periods();
                prds = _dbContext.Periods.ToList().Where(a => a.Period_State == 1 && a.Period_Type == 1).First();
                //string activeShiftDesc = prds.Shift_number > 4? "US" : 
                API_Active_Cashier activeCashier = new API_Active_Cashier();
                activeCashier = _dbContext.API_Active_Cashier.ToList().First();

                wr.WriteAttributeString("BATCH", prds.Period_ID.ToString());   // X

                wr.WriteAttributeString("TRANSACTION", formattedORNum);
                wr.WriteAttributeString("CID", activeCashier.ID.ToString());
                wr.WriteAttributeString("ONAME", activeCashier.CashierName);   //CASHIER NAME

                wr.WriteAttributeString("PID", prds.Period_ID.ToString()); // X
                wr.WriteAttributeString("SHIFT", prds.Shift_number.ToString() + activeCashier.Description); // X

                bool cfflsEmpty = string.IsNullOrEmpty(cffls.HEADER.REFNO);

                if (!cfflsEmpty)
                {
                    wr.WriteAttributeString("DESCRIPTION", cffls.DETAIL.ITEM.PRODUCT.Trim());
                }
                else
                {
                    if (transactionItems[0].itemType == 2 || transactionItems[0].itemType == 53)
                    {
                        string grade = transactionItems[0].itemDesc.Substring(3);
                        wr.WriteAttributeString("DESCRIPTION", grade.Trim().ToUpper());
                    }
                    else
                    {
                        wr.WriteAttributeString("DESCRIPTION", transactionItems[0].itemDesc.Trim().ToUpper());
                    }
                }

                double totAmt = transactionItems.Where(a => a.itemType == 2).Sum(a => a.itemValue);
                double totQty = transactionItems.Where(a => a.itemType == 2).Sum(a => a.itemQTY);
                double totDisc = Math.Abs(transactionItems.Where(a => a.itemType == 2).Sum(a => a.itemDiscTotal));

                wr.WriteAttributeString("PRICE", transactionItems[0].itemPrice.ToString());
                wr.WriteAttributeString("TOTALAMOUNT", subtotalValue.ToString());
                wr.WriteAttributeString("TOTALQUANTITY", totQty.ToString());
                wr.WriteAttributeString("ACCTNO", !cfflsEmpty ? cffls.DETAIL.ITEM.ACCOUNTNO : "");
                wr.WriteAttributeString("VIPNO", !cfflsEmpty ? cffls.HEADER.CARDSERIALNO : "");
                wr.WriteAttributeString("PUMP_PROD_REFNO", !cfflsEmpty ? cffls.HEADER.REFNO : "");
                wr.WriteAttributeString("VEHICLETYPE", vehicleType);
                wr.WriteAttributeString("ENTITYCODE", !cfflsEmpty ? cffls.DETAIL.ITEM.ENTITYCODE : "");
                wr.WriteAttributeString("ENTITYNAME", !cfflsEmpty ? cffls.DETAIL.ITEM.ENTITYNAME : "");
                wr.WriteAttributeString("DISCOUNTAMOUNT", totDisc == 0 ? "" : totDisc.ToString());
                wr.WriteAttributeString("PAEMPID", attendantID);
                wr.WriteAttributeString("PANAME", "");

                wr.WriteEndElement();


                wr.WriteStartElement("DETAIL");
                List<TransactionItemClass> mops = new List<TransactionItemClass>();
                mops = transactionItems.Where(a => a.itemType == 7 || a.itemType == 8).ToList();
                foreach (TransactionItemClass mop in mops)
                {
                    wr.WriteStartElement("MOP");

                    Finalisations mopData = new Finalisations();
                    mopData = _dbContext.Finalisations.ToList().Where(a => a.MOP_ID == mop.itemID).First();

                    string mopdesc = mopData.MOP_Code!;
                    wr.WriteAttributeString("MOP_DESCRIPTION", mopdesc ?? "");
                    wr.WriteAttributeString("REFNO", mopLastValues[0]);
                    wr.WriteAttributeString("AMOUNTDUE", mop.itemValue.ToString());
                    wr.WriteAttributeString("GCSERIALNUMBER", mop.gcNumber ?? "");

                    wr.WriteEndElement();

                }

                wr.WriteStartElement("FUNCTION");
                wr.WriteAttributeString("TASKCODE", "003-VAT");
                wr.WriteAttributeString("DESCRIPTION", "VAT 12%");
                wr.WriteAttributeString("TYPE", "");

                if (!(int.Parse(pd["isZeroRated"].ToString()) == 1))
                {
                    wr.WriteAttributeString("AMOUNTDUE", taxtotalValue.ToString("F4"));
                    wr.WriteAttributeString("TOTALAMOUNT", taxtotalValue.ToString("F4"));
                    wr.WriteAttributeString("TOTALAMOUNTDUE", taxtotalValue.ToString("F4"));
                }
                else
                {
                    wr.WriteAttributeString("AMOUNTDUE", "");
                    wr.WriteAttributeString("TOTALAMOUNT", "");
                    wr.WriteAttributeString("TOTALAMOUNTDUE", "");
                }

                wr.WriteEndElement();

                wr.WriteStartElement("FUNCTION");

                List<TransactionItemClass> discs = new List<TransactionItemClass>();
                discs = transactionItems.Where(a => a.itemType == 52).ToList();
                double discsTotalValue = Math.Abs(discs.Sum(a => a.itemValue));

                if (discs.Count == 1 && discs[0].itemDesc == "Puregold Discount")
                {
                    wr.WriteAttributeString("TASKCODE", pgCardNumber);
                    wr.WriteAttributeString("DESCIPRIONT", "PUREGOLD DISCOUNT");
                }
                else
                {
                    wr.WriteAttributeString("TASKCODE", "003-VAT");
                    wr.WriteAttributeString("DESCIPRIONT", "DISCOUNT");
                }

                wr.WriteAttributeString("TYPE", "DISCOUNT");
                wr.WriteAttributeString("AMOUNTDUE", discsTotalValue.ToString("F4"));
                wr.WriteAttributeString("TOTALAMOUNT", discsTotalValue.ToString("F4"));
                wr.WriteAttributeString("TOTALAMOUNTDUE", discsTotalValue.ToString("F4"));

                wr.WriteEndElement();

                List<TransactionItemClass> trans = new List<TransactionItemClass>();
                trans = transactionItems.Where(a => a.itemType == 2).ToList();
                foreach (TransactionItemClass item in trans)
                {


                    Hose_Delivery Closing_Hose_Delivery = new Hose_Delivery();
                    Closing_Hose_Delivery = _dbContext.Hose_Delivery.ToList().Where(a => a.Delivery_ID == item.deliveryID).First();

                    Hose_Delivery Opening_Hose_Delivery = null;
                    int openingCount = _dbContext.Hose_Delivery.ToList().Where(a => a.Hose_ID == Closing_Hose_Delivery.Hose_ID && a.Delivery_ID < a.Delivery_ID).Count();
                    if (openingCount > 0)
                    {
                        Opening_Hose_Delivery = _dbContext.Hose_Delivery.ToList().Where(a => a.Hose_ID == Closing_Hose_Delivery.Hose_ID && a.Delivery_ID < a.Delivery_ID).Last();

                    }

                    Transaction trn = getTransactionByID((int)item.pumpID, item.deliveryID);
                    int tankNum = trn.Hose.Tank1.Id;


                    Console.WriteLine(Closing_Hose_Delivery.Hose_Meter_Value.ToString() + "        THIS IS CLOSE PUMP READING");
                    Console.WriteLine(Closing_Hose_Delivery.Hose_ID);
                    Console.WriteLine(Closing_Hose_Delivery.Delivery_ID);

                    wr.WriteStartElement("ITEM");
                    wr.WriteAttributeString("PUMPID", item.pumpID.ToString());
                    wr.WriteAttributeString("OPENPUMPREADING", Opening_Hose_Delivery == null ? "0" : Opening_Hose_Delivery.Hose_Meter_Value.ToString()); // X
                    wr.WriteAttributeString("CLOSEPUMPREADING", Closing_Hose_Delivery.Hose_Meter_Value.ToString()); // X
                    wr.WriteAttributeString("QUANTITY", item.itemQTY.ToString());
                    wr.WriteAttributeString("TANKNO", tankNum.ToString()); // X
                    wr.WriteEndElement();
                }

                wr.WriteEndElement();   //DETAIL
                wr.WriteEndElement();   //ticket
                wr.WriteEndElement();   //</xml>
                wr.WriteEndDocument();  //close xml

            }
            /* }
             catch
             {
                 Console.WriteLine();
                 return false;
             }*/

            return true;
        }

        private async Task<bool> PrintReceipt()
        {
            string receiptToDB = "";
            foreach (string line in receiptString)
            {
                Console.WriteLine(line);
                receiptToDB += $"{line}\n";
            }

            object toPrintLog = new
            {
                posID = POSID,
                printedData = receiptToDB
            };

            object toEJournal = new
            {
                posID = POSID,
                data = receiptToDB.Replace(' ', '='),
                Transaction_ID = transactionNum,
                si_number = siNum
            };
            
            APIQuery(toEJournal, saveToJournalRoute);

            if (!autoPrint)
            {
                return true;
            }
            
            APIQuery(toPrintLog, saveToLogRoute);
            
            try
            {
                RequirementsFromAPI.TestPrint(receiptString);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
            try
            {
                posPrinter.AsyncMode = true;
                posPrinter.TransactionPrint(PrinterStation.Receipt, PrinterTransactionControl.Transaction);

                if (data["PRINTER"]["printertype"] == "dotmatrix")
                {
                    int[] RecLineChars = new int[MAX_LINE_WIDTHS] { 0, 0 };
                    long lRecLineCharsCount;

                    Console.WriteLine("RECLINECHARS 1 {0}", RecLineChars[1]);
                    Console.WriteLine("RECLINECHARS 0 {0}", RecLineChars[0]);

                    lRecLineCharsCount = GetRecLineChars(ref RecLineChars);
                    if (lRecLineCharsCount >= 2)
                    {
                        posPrinter.RecLineChars = RecLineChars[1];
                        //56
                        posPrinter.RecLineSpacing = 350;
                    }
                    else
                    {
                        posPrinter.RecLineChars = RecLineChars[0];
                    }
                }
                else
                {
                    posPrinter.RecLineSpacing = 350;
                }

                foreach (string receiptLine in receiptString)
                {
                    posPrinter.PrintImmediate(PrinterStation.Receipt, receiptLine);
                }

                posPrinter.PrintImmediate(PrinterStation.Receipt, "\u001b|fP");

                //posPrinter.TransactionPrint(PrinterStation.Receipt, PrinterTransactionControl.Normal);

                posPrinter.AsyncMode = false;

            }
            catch
            {
                return false;
            }

            return true;
        }

        private long GetRecLineChars(ref int[] RecLineChars)
        {
            long lRecLineChars = 0;
            long lCount;
            int i;

            // Calculate the element count.
            lCount = posPrinter.RecLineCharsList.GetLength(0);

            if (lCount == 0)
            {
                lRecLineChars = 0;
            }
            else
            {
                if (lCount > MAX_LINE_WIDTHS)
                {
                    lCount = MAX_LINE_WIDTHS;
                }

                for (i = 0; i < lCount; i++)
                {
                    RecLineChars[i] = posPrinter.RecLineCharsList[i];
                }

                lRecLineChars = lCount;
            }

            return lRecLineChars;
        }

        private async Task<bool> createReceipt(List<TransactionItemClass> items, string orNum, string orResetter, string transID, string rpttoprint)
        {
            receiptString = new List<string>();

            object receiptPOS = new
            {
                posID = POSID
            };

            /* Dictionary<string, object> jsonDoc = await APIQuery(receiptPOS, getReceiptLayoutRoute);



             try
             {

                 //JsonElement jsonResponseElement = jsonDoc.RootElement.GetProperty("data");
                 string jsonResponseElement = jsonDoc["data"].ToString();
                 //receiptHeaderFooter = JsonConvert.DeserializeObject<ReceiptHeaderFooter>(jsonResponseElement.ToString());
                 receiptHeaderFooter = JsonConvert.DeserializeObject<ReceiptHeaderFooter>(jsonResponseElement);

             }
             catch
             {
                 return false;
             }*/

            foreach (string headerLine in receiptHeaderFooter.headerL1.Split("\\n"))
            {
                ReceiptCenteredString(headerLine.Trim());
            }

            foreach (string headerLine in receiptHeaderFooter.headerL2.Split("\\n"))
            {
                ReceiptCenteredString(headerLine.Trim());
            }

            foreach (string headerLine in receiptHeaderFooter.headerL3.Split("\\n"))
            {
                ReceiptCenteredString(headerLine.Trim());
            }

            foreach (string headerLine in receiptHeaderFooter.headerL4.Split("\\n"))
            {
                ReceiptCenteredString(headerLine.Trim());
            }

            foreach (string headerLine in receiptHeaderFooter.headerL5.Split("\\n"))
            {
                ReceiptCenteredString(headerLine.Trim());
            }



            /* ReceiptCenteredString(receiptHeaderFooter.headerL2.Trim());
             ReceiptCenteredString(receiptHeaderFooter.headerL3.Trim());
             ReceiptCenteredString(receiptHeaderFooter.headerL4.Trim());
             ReceiptCenteredString(receiptHeaderFooter.headerL5.Trim());*/

            receiptString.Add("");

            siNum = FormatInvoiceNum(orNum.Trim(), orResetter.Trim());
            transactionNum = int.Parse(transID.Trim());
            

            DateTime nowDate = DateTime.Now;                            //System date
            DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();   //Date Format
            dateFormat.MonthDayPattern = "MM";
            string strDate = nowDate.ToString("MM/dd/yyyy hh:mm tt", dateFormat);

            ReceiptSideString(strDate.Trim(), $"POS #{POSID}");
            ReceiptSideString(cashierName, siNum);

            receiptString.Add("");
            receiptString.Add("");
            ReceiptCenteredString("*** INVOICE ***");
            receiptString.Add("");

            double unitPrice = 0;
            double quantity = 0;
            double value = 0;
            foreach (TransactionItemClass item in items)
            {

                if (item.itemType != 2 && item.itemType != 52)
                    continue;

                unitPrice = item.itemPrice;
                quantity = item.itemQTY;
                value = item.itemValue;
                //Transaction fuelTrans = getTransactionByID((int)item.pumpID, item.deliveryID);

                if (item.itemType == 2)
                {
                    ReceiptSideString($" {item.itemDesc.Trim()}", "");
                    ReceiptThreeString($"  {item.itemQTY}L x P{item.itemPrice}", "VAT", $"P{item.itemValue.ToString("N2")}");
                }

                if (item.itemType == 52)
                {
                    AddDiscountToReceipt(item, unitPrice, quantity, value);
                    continue;
                }

            }

            receiptString.Add("");
            double saleTotal = items.Where(a => a.itemType == 2).Sum(a => a.itemValue - a.itemDiscTotal);
            ReceiptSideString("Sales Total", $"P{saleTotal.ToString("N2")}");


            foreach (TransactionItemClass mop in items)
            {
                if (mop.itemType != 7 || mop.itemType != 8)
                    continue;

                ReceiptSideString(mop.itemDesc.Trim(), $"P{mop.itemValue.ToString("N2")}");

            }

            receiptString.Add("");
            ReceiptSideString("TOTAL INVOICE", $"P{saleTotal.ToString("N2")}");


            double vatable = items.Where(a => a.itemType == 2 && a.itemTaxID == 1).Sum(a => a.itemValue - a.itemDiscTotal);
            double nonVat = items.Where(a => a.itemType == 2 && a.itemTaxID == 2).Sum(a => a.itemValue - a.itemDiscTotal);
            double zeroRated = items.Where(a => a.itemType == 2 && a.itemTaxID == 3).Sum(a => a.itemValue - a.itemDiscTotal);

            receiptString.Add("");
            ReceiptSideString("VATable Sales", $"P{(vatable / 1.12).ToString("N2")}");
            ReceiptSideString("VAT Amount", $"P{(vatable - (vatable / 1.12)).ToString("N2")}");
            ReceiptSideString("VAT-Exempt Sales", $"P{nonVat.ToString("N2")}");
            ReceiptSideString("Zero Rated Sales", $"P{zeroRated.ToString("N2")}");



            if (transactionMOPS.Count() > 0)
            {
                foreach (MopCardInfo mopinfo in transactionMOPS)
                {
                    receiptString.Add("");
                    int numberLength = mopinfo.CardNumber.Length;
                    string maskedCardNumber = "XXXX-XXXX-XXXX-" + mopinfo.CardNumber.Substring(numberLength - 4);

                    ReceiptSideString($"Account Name:  {mopinfo.CardHolderName}", "");
                    ReceiptSideString($"Card#:         {maskedCardNumber}", "");
                    ReceiptSideString($"Approval Code: {mopinfo.BankCode}", "");
                    ReceiptSideString($"Sign:           ______________________", "");

                }
            }

            receiptString.Add("");

            foreach (string line in receiptHeaderFooter.footerL1.Split("\\n"))
            {
                string f1WithoutLine = line.Replace("_", "");

                if (line.Contains("NAME") && !string.IsNullOrEmpty(customerInfo.name))
                {
                    f1WithoutLine += $": {customerInfo.name}";
                }
                else if (line.Contains("ADDRESS") && !string.IsNullOrEmpty(customerInfo.add))
                {
                    f1WithoutLine += $": {customerInfo.add}";
                }
                else if (line.Contains("TIN") && !string.IsNullOrEmpty(customerInfo.TINNumber))
                {
                    f1WithoutLine += $": {customerInfo.TINNumber}";
                }
                else if (line.Contains("BUSINESS STYLE") && !string.IsNullOrEmpty(customerInfo.busStyle))
                {
                    f1WithoutLine += $": {customerInfo.busStyle}";
                }
                else
                {
                    int lineCharCount = ReceiptCharLength - f1WithoutLine.Length;
                    f1WithoutLine += new string('_', lineCharCount - 1);
                }

                ReceiptSideString(f1WithoutLine, "");
            }

            receiptString.Add("");
            foreach (string f1line in receiptHeaderFooter.footerL2.Split("\\n"))
            {
                ReceiptCenteredString(f1line.Trim().Replace("\\", ""));
            }

            receiptString.Add("");
            foreach (string f1line in receiptHeaderFooter.footerL3.Split("\\n"))
            {
                ReceiptCenteredString(f1line.Trim().Replace("\\", ""));
            }

            receiptString.Add("");
            foreach (string f1line in receiptHeaderFooter.footerL4.Split("\\n"))
            {
                ReceiptCenteredString(f1line.Trim().Replace("\\", ""));
            }

            receiptString.Add("");
            foreach (string f1line in receiptHeaderFooter.footerL5.Split("\\n"))
            {
                ReceiptCenteredString(f1line.Trim().Replace("\\", ""));
            }

            if (rpttoprint.Length > 0)
            {
                receiptString.Add("");
                foreach (string rptLine in rpttoprint.Split("\\n"))
                {
                    ReceiptCenteredString(rptLine);
                }
            }


            return true;
        }


        private void AddDiscountToReceipt(TransactionItemClass discount, double itemPrice, double itemQuantity, double itemValue)
        {
            ReceiptSideString($"   {discount.itemDesc}", "");
            DiscountPresets appliedDiscount = discountPresets.Where(a => a.presetId == discount.discountPresetID).First();
            if (appliedDiscount.discountType == 1)
            {
                int percent = Convert.ToInt32(appliedDiscount.presetValue);

                ReceiptSideString($"    {Math.Abs(itemValue).ToString("N2")} X {Math.Abs(percent)}%", $"(P{Math.Abs(discount.itemValue).ToString("N2")})");
            }

            if (appliedDiscount.discountType == 2)
            {
                ReceiptSideString($"    {Math.Abs(discount.itemPrice).ToString("N2")} X {Math.Abs(discount.itemQTY)} P/L", $"(P{Math.Abs(discount.itemValue).ToString("N2")})");
            }

            if (appliedDiscount.discountType == 3)
            {
                ReceiptSideString($"    P{Math.Abs(discount.itemPrice).ToString("N2")}", $"(P{Math.Abs(discount.itemValue).ToString("N2")})");
            }
        }

        private void ReceiptThreeString(string text1, string text2, string text3)
        {
            int spaceCount = (ReceiptCharLength - (text1.Length + text2.Length + text3.Length)) / 2;
            string formattedString = text1 + new string(' ', spaceCount) + text2 + new string(' ', spaceCount) + text3;
            receiptString.Add(formattedString);
        }
        private void ReceiptSideString(string text1, string text2)
        {


            int spaceCount = ReceiptCharLength - (text1.Length + text2.Length);
            string formattedString = text1 + new string(' ', spaceCount) + text2;
            receiptString.Add(formattedString);
        }

        private string FormatInvoiceNum(string orNum, string resetter)
        {
            string formattedString = "INV #:" + resetter.PadLeft(3, '0') + "-" + orNum.PadLeft(9, '0');
            return formattedString;
        }

        private void ReceiptCenteredString(string text)
        {
            Console.WriteLine(text);
            if (text.Length <= 0)
            {
                return;
            }

            int sideLength = (ReceiptCharLength - text.Length) / 2;
            string formattedString = new string(' ', sideLength) + text + new string(' ', sideLength);
            receiptString.Add(formattedString);
        }



        private int getDeliveries(string deliveries, int itemNum)
        {
            List<Dictionary<string, object>> deliveriesDict = new List<Dictionary<string, object>>();
            deliveriesDict = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(deliveries);
            //Console.WriteLine(JsonConvert.SerializeObject(deliveries) + "         this");
            Console.WriteLine(deliveriesDict[0]["deliveryID"].ToString() + "         this");

            foreach (Dictionary<string, object> delivery in deliveriesDict)
            {
                Transaction trs = getTransactionByID(int.Parse(delivery["pumpID"].ToString()), int.Parse(delivery["deliveryID"].ToString()));

                if(trs == null)
                {
                    return 0;
                }

                transactionItems.Add(new TransactionItemClass(
                    itemNum,
                    2,
                    trs.DeliveryData.Grade.ToString(),
                    //trans.DeliveryData.Grade.ToString(),
                    double.Parse(delivery["price"].ToString()),
                    double.Parse(delivery["volume"].ToString()),
                    double.Parse(delivery["value"].ToString()),
                    int.Parse(delivery["deliveryID"].ToString()),
                    double.Parse(delivery["value"].ToString()) / 1.12,
                    int.Parse(delivery["deliveryID"].ToString()),
                    1,
                    "",
                    "",
                    null,
                    0,
                    0,
                    0,
                    1,
                    "",
                    null,
                    "",
                    0,
                    int.Parse(delivery["pumpID"].ToString())
                    ));
                itemNum++;

                Dictionary<string, object> discounts = new Dictionary<string, object>();
                discounts = JsonConvert.DeserializeObject<Dictionary<string, object>>(delivery["discount"].ToString());

                if (!discounts.IsNullOrEmpty())
                {
                    int discPresetID = int.Parse(discounts["ID"].ToString());
                    int discType = int.Parse(discounts["Type"].ToString());
                    double discValue = double.Parse(discounts["Value"].ToString());




                    /* foreach (var discountDesc in delivery.GetProperty("discount").EnumerateObject())
                     {
                         switch (discountDesc.Name)
                         {
                             case "ID":
                                 {
                                     discPresetID = int.Parse(discountDesc.Value.ToString());
                                 }
                                 break;
                             case "Type":
                                 {
                                     discType = int.Parse(discountDesc.Value.ToString());
                                 }
                                 break;
                             case "Value":
                                 {
                                     discValue = double.Parse(discountDesc.Value.ToString());
                                 }
                                 break;
                             default:
                                 { }
                                 break;
                         }
                     }*/

                    


                    DiscountPresets appliedDiscount = discountPresets.Where(a => a.presetId == discPresetID).First();
                    string discountPresetName = appliedDiscount.presetName;
                    int discountID = appliedDiscount.presetDiscountId;

                    transactionItems[itemNum - 2].ApplyDiscount(discPresetID, discType, discValue);
                    getDiscountInfo(itemNum - 2, discType, discValue, out double transPrice, out double transVolume, out double transValue);

                    Console.WriteLine(discPresetID);
                    Console.WriteLine(discType);
                    Console.WriteLine(discValue);
                    Console.WriteLine(transValue);
                    Console.WriteLine(transactionItems[itemNum - 2].itemDiscTotal);
                    transactionItems.Add(new TransactionItemClass(itemNum,
                        52,
                        discountPresetName,
                        transPrice,
                        transVolume,
                        transValue,
                        discountID,
                        transValue / 1.12,
                        discountID,
                        1,
                        "",
                        "",
                        null,
                        0,
                        0,
                        0,
                        0,
                        "",
                        null,
                        "",
                        discPresetID,
                        null
                        ));

                    itemNum++;
                }

            }

            /* foreach (JsonElement delivery in deliveries.EnumerateArray())
             {
                 //Transaction trans = fore.GetTransactionById(int.Parse(delivery.GetProperty("deliveryID").ToString()));
                 Console.WriteLine(JsonConvert.SerializeObject(delivery) + "         this");
                 transactionItems.Add(new TransactionItemClass(
                     itemNum,
                     2,
                     "test",
                     //trans.DeliveryData.Grade.ToString(),
                     double.Parse(delivery.GetProperty("price").ToString()),
                     double.Parse(delivery.GetProperty("volume").ToString()),
                     double.Parse(delivery.GetProperty("value").ToString()),
                     int.Parse(delivery.GetProperty("deliveryID").ToString()),
                     double.Parse(delivery.GetProperty("value").ToString()) / 1.12,
                     int.Parse(delivery.GetProperty("deliveryID").ToString()),
                     1,
                     "",
                     "",
                     null,
                     0,
                     0,
                     0,
                     1,
                     "",
                     null,
                     "",
                     0,
                     int.Parse(delivery.GetProperty("pumpID").ToString())
                     ));
                 itemNum++;
                 if (delivery.GetProperty("discount").EnumerateObject().Count() > 0)
                 {
                     int discPresetID = 0;
                     int discType = 0;
                     double discValue = 0;

                     *//*double transPrice = 0;
                     double transVolume = 0;
                     double transValue = 0;*//*

                     foreach (var discountDesc in delivery.GetProperty("discount").EnumerateObject())
                     {
                         switch (discountDesc.Name)
                         {
                             case "ID":
                                 {
                                     discPresetID = int.Parse(discountDesc.Value.ToString());
                                 }
                                 break;
                             case "Type":
                                 {
                                     discType = int.Parse(discountDesc.Value.ToString());
                                 }
                                 break;
                             case "Value":
                                 {
                                     discValue = double.Parse(discountDesc.Value.ToString());
                                 }
                                 break;
                             default:
                                 { }
                                 break;
                         }
                     }

                     DiscountPresets appliedDiscount = discountPresets.Where(a => a.presetId == discPresetID).First();
                     string discountPresetName = appliedDiscount.presetName;
                     int discountID = appliedDiscount.presetDiscountId;

                     transactionItems[itemNum - 2].ApplyDiscount(discPresetID, discType, discValue);
                     getDiscountInfo(itemNum - 2, discType, discValue, out double transPrice, out double transVolume, out double transValue);

                     transactionItems.Add(new TransactionItemClass(itemNum,
                         52,
                         discountPresetName,
                         transPrice,
                         transVolume,
                         transValue,
                         discountID,
                         transValue / 1.12,
                         discountID,
                         1,
                         "",
                         "",
                         null,
                         0,
                         0,
                         0,
                         0,
                         "",
                         null,
                         "",
                         discPresetID,
                         null
                         ));

                     itemNum++;
                 }
             }*/
            return itemNum;
        }

        private string[] getMops(string mops, int itemNum)
        {
            string lastAprovalCode = "";
            string lastBankCode = "";
            string lastCardNumber = "";
            string lastCardholderName = "";
            int mopCount = 1;

            List<Dictionary<string, object>> mopList = new List<Dictionary<string, object>>();
            mopList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(mops);

            int multipleMOPcount = mopList.Count();

            foreach (Dictionary<string, object> mop in mopList)
            {
                int itemType = 8;
                if (multipleMOPcount == mopCount)
                {
                    itemType = 7;
                }
                multipleMOPcount--;

                transactionItems.Add(new TransactionItemClass(itemNum,
                    itemType,
                    mop["name"].ToString(),
                    double.Parse(mop["value"].ToString()),
                    1,
                    double.Parse(mop["value"].ToString()),
                    int.Parse(mop["mopID"].ToString()),
                    0,
                    int.Parse(mop["mopID"].ToString()),
                    0,
                    "",
                    "",
                    null,
                    0,
                    0,
                    0,
                    0,
                    "",
                    null,
                    "",
                    0,
                    null
                    ));

                string cardNum = "";
                string cardholderName = "";
                string bankCode = "";

                Dictionary<string, object> infoDict = new Dictionary<string, object>();
                infoDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(mop["info"].ToString());

                if (!infoDict.IsNullOrEmpty())
                {

                    cardNum = infoDict["cardNumber"].ToString();
                    cardholderName = infoDict["cardholderName"].ToString();
                    bankCode = infoDict["bankCode"].ToString();

                    transactionMOPS.Add(new MopCardInfo(cardNum, cardholderName, bankCode));

                    lastAprovalCode = mop["approvalCode"].ToString();
                    lastBankCode = bankCode;
                    lastCardNumber = cardNum;
                    lastCardholderName = cardholderName;
                }

                itemNum++;
            }


            List<string> lastValues = new List<string>()
            {
                lastAprovalCode,
                lastBankCode,
                lastCardNumber,
                lastCardholderName

            };
            return lastValues.ToArray();
        }

        // private bool getCustomerDetails(JsonElement customerInformations)
        private bool getCustomerDetails(string customerInformations)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();
            info = JsonConvert.DeserializeObject<Dictionary<string, string>>(customerInformations);

            customerInfo.name = info["name"];
            customerInfo.add = info["address"];
            customerInfo.TINNumber = info["TIN"];
            customerInfo.busStyle = info["busStyle"];

            /* foreach (var info in customerInformations.EnumerateObject())
             {
                 switch (info.Name)
                 {
                     case "name":
                         {
                             customerInfo.name = info.Value.ToString();
                         }
                         break;
                     case "address":
                         {
                             customerInfo.add = info.Value.ToString();
                         }
                         break;
                     case "TIN":
                         {
                             customerInfo.TINNumber = info.Value.ToString();
                         }
                         break;
                     case "busStyle":
                         {
                             customerInfo.busStyle = info.Value.ToString();
                         }
                         break;
                     default:
                         { }
                         break;
                 }
             }*/
            return true;
        }

        private async Task<Dictionary<string, object>> APIQuery(object obj, string route)
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

        private async Task<string> SendToAPI(object obj)
        {
            //JsonElement response = new JsonElement();
            string response = "";
            try
            {
                //JsonDocument decodedResponse = await APIQuery(obj, addNewTransactionRoute);
                //JsonElement responseValue = decodedResponse.RootElement.GetProperty("data");
                Dictionary<string, object> decodedResponse = new Dictionary<string, object>();
                decodedResponse = await APIQuery(obj, addNewTransactionRoute);
                Console.WriteLine($"send to api status code is {decodedResponse["statusCode"]}");
                Console.WriteLine($"send to api status code is {decodedResponse["statusDescription"]}");
                //string responseValue = decodedResponse["data"].ToString();
                string responseValue = decodedResponse["data"].ToString();


                response = responseValue;
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
            return response;
        }


        /*private async Task<bool> GetDiscounts()
        {
            try
            {
                discountPresets.Clear();
                object emptyObject = new { };
                //JsonDocument decodedResponse = await APIQuery(emptyObject, getDiscountsRoute);
                //JsonElement jsonResponseElement = decodedResponse.RootElement.GetProperty("data");
                Dictionary<string, object> decodedResponse = await APIQuery(emptyObject, getDiscountsRoute);
                string jsonResponseElement = decodedResponse["data"].ToString();
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
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }*/

        private void getDiscountInfo(int index, int type, double value, out double price, out double volume, out double discValue)
        {
            discValue = 0;
            volume = 1;
            price = 1;
            switch (type)
            {
                case 1:
                    {
                        double percentDiscValue = value / 100;
                        discValue = Math.Abs(transactionItems[index].itemValue * percentDiscValue) * -1;
                        price = discValue;

                    }
                    break;
                case 2:
                    {
                        discValue = Math.Abs(transactionItems[index].itemQTY * value) * -1;
                        volume = discValue;
                        // price = value;
                    }
                    break;
                case 3:
                    {
                        discValue = Math.Abs(value) * -1;
                        price = discValue;
                    }
                    break;
                default:
                    {

                    }
                    break;
            }


        }

        private bool isForecourtConnected()
        {

            /*// Checking registration
            string volumeSerial = Registration.RegKey.GetVolumeSerial();
            string baseboardSerial = Registration.RegKey.GetBaseBoardSerial();
            string biosSerial = Registration.RegKey.GetBIOSID();
            string procSerial = Registration.RegKey.GetProcessorID();

            string hardware = volumeSerial + baseboardSerial + biosSerial + procSerial;

            var posKey = Registration.RegKey.ComputeSha256Hash(hardware);

            string configRegKey = data["Registration"]["KEY"];

            if (posKey != configRegKey)
                return false;*/


            if (fore.IsConnected)
            {
                return true;
            }
            else
            {
                try
                {
                    int connectAttempt = 0;
                    fore.Connect(server, terminalID, "", password, true);
                    while (!fore.IsConnected)
                    {
                        Console.WriteLine("Connecting ...");
                        Task.Delay(1000);
                        connectAttempt++;
                        if (connectAttempt >= 3)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private void SaveQueryLog(string data)
        {
            string path = "logs\\QueryLogs.txt";

            if (!System.IO.File.Exists(path))
            {
                using (StreamWriter sw = System.IO.File.CreateText(path))
                {
                    sw.WriteLine(data);
                }
            }
            else
            {
                using (StreamWriter sw = System.IO.File.AppendText(path))
                {
                    sw.WriteLine(new string('=', 42));
                    sw.WriteLine(data);

                }
            }
        }

        private bool IsRegistered()
        {
            try
            {
                var configParser = new FileIniDataParser();
                IniData data = configParser.ReadFile("config.ini");
                string regKey = data["REGISTRATION"]["KEY"];

                string volumeSerial = RegKey.GetVolumeSerial();
                string baseboardSerial = RegKey.GetBaseBoardSerial();
                string biosSerial = RegKey.GetBIOSID();
                string procSerial = RegKey.GetProcessorID();

                string hardwareID = volumeSerial + baseboardSerial + biosSerial + procSerial;
                string APIKey = RegKey.ComputeSha256Hash(hardwareID);

                return regKey == APIKey;
            }
            catch (Exception ex)
            {
                SaveQueryLog(ex.Message);
                return false;
            }

        }

    }


}
