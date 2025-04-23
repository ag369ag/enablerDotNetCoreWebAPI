using Azure;
using IniParser;
using IniParser.Model;
using ITL.Enabler.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Registration;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

namespace testASPWebAPI.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize]

    public class PumpController : ControllerBase
    {
        private Forecourt fore = PumpClass.forecourt;

        private string server;
        private int terminalID;
        private string password;
        private int pumpStart;
        private int pumpEnd;
        private string addNewTransactionRoute;
        private string getDiscountsRoute;

        private readonly ILogger<PumpController> _logger;

        List<TransactionItemClass> transactionItems = new List<TransactionItemClass>();
        List<MopCardInfo> transactionMOPS = new List<MopCardInfo>();
        CustomerInformations customerInfo = new CustomerInformations();
        List<DiscountPresets> discountPresets = new List<DiscountPresets>();

        public PumpController(ILogger<PumpController> logger)
        {
            _logger = logger;
            var configParser = new FileIniDataParser();
            IniData data = configParser.ReadFile("config.ini");
            server = data["Configuration"]["Server"];
            terminalID = int.Parse(data["Configuration"]["TerminalID"]);
            password = data["Configuration"]["Password"];
            pumpStart = int.Parse(data["Configuration"]["PumpStartID"]);
            pumpEnd = int.Parse(data["Configuration"]["PumpEndID"]);
            addNewTransactionRoute = "http://" + data["ConnectedAPI"]["AddToTransactionRoute"];
            getDiscountsRoute = "http://" + data["ConnectedAPI"]["GetDiscountRoute"];
            if (!fore.IsConnected)
            {
                fore.Connect(server, terminalID, "", password, true);
            }


        }

        /// <summary>
        /// Getting Pump Data
        /// </summary>
        /// <returns>Pump data</returns>
        //[Route("[action]")]
        // [Tags("THIS ROUTE IS FOR GETTING PUMP DATA")]
        //[EndpointDescription("This route doesn't need parameter")]
        //[FromBody]
        [HttpGet(Name = "Getting pump data")]
        public JsonResult GetPumpData()
        {
            /* while (!fore.IsConnected)
             {
             }*/

            /*if (!IsRegistered())
            {
                return new JsonResult(new { Message = "API is not yet registered" });
            }*/

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

                /*if (pumpStart <= item.Number && pumpEnd >= item.Number)
                {*/
                pumpList.Add(new PumpDataClass
                {
                    pumpName = item.Name,
                    pumpNumber = item.Number
                });
                //}
            }

            SaveQueryLog($"Get all pump data.\nResult: {Newtonsoft.Json.JsonConvert.SerializeObject(pumpList)}");
            return new JsonResult(pumpList);
        }

        //[Route("[action]")]
        [HttpPost(Name = "getTransactionByPumpID")]
        public JsonResult getTransactionByPumpID(JsonDocument json)
        {

            /*while (!fore.IsConnected)
            {

            }*/

            /*if (!IsRegistered())
            {
                return new JsonResult(new { Message = "API is not yet registered" });
            }*/

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



        //[Route("[action]")]
        [HttpPost(Name = "VoidTransaction")]
        public JsonResult VoidTransaction(JsonDocument json)
        {
            /*while (!fore.IsConnected)
            {
            }*/

            /*if (!IsRegistered())
            {
                return new JsonResult(new { Message = "API is not yet registered" });
            }*/

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
        public JsonResult LockTransaction(JsonDocument json)
        {
            /* if (!IsRegistered())
             {
                 return new JsonResult(new { Message = "API is not yet registered" });
             }*/

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



        //[Route("[action]")]
        [HttpPost(Name = "ClearTransaction")]
        public JsonResult ClearTransaction(JsonDocument json)
        {
            /* if (!IsRegistered())
             {
                 return new JsonResult(new { Message = "API is not yet registered" });
             }*/

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
            
                transactionItems = new List<TransactionItemClass>();
                transactionMOPS = new List<MopCardInfo>();
                customerInfo = new CustomerInformations();

                if (!isForecourtConnected())
                {
                    isForecourtConnected();
                }

                bool gettingDiscounts = await GetDiscounts();

                if (!gettingDiscounts)
                {
                    JsonResult result = new JsonResult(new { result = "Failed", Message = "Error in getting discounts" });
                    return result;
                }

                int itemNum = 1;
                JsonElement deliveries = document.RootElement.GetProperty("deliveries");
                int newItemNum = getDeliveries(deliveries, itemNum);

                JsonElement mops = document.RootElement.GetProperty("mop");
                string[] mopLastValues = getMops(mops, newItemNum);

                JsonElement customerInformations = document.RootElement.GetProperty("customerInfo");
                bool getCustomerInfo = getCustomerDetails(customerInformations);

                string rptToPrint = document.RootElement.GetProperty("rptToPrint").ToString();

                // Clearing transactions
                /*foreach(TransactionItemClass fuelItems in transactionItems.Where(a=>a.itemType == 2))
                {
                    ClearTransaction(1, fuelItems.deliveryID, out bool isCompleted, out string errorMessage);

                    if (!isCompleted)
                    {
                        JsonResult result = new JsonResult(new { result = "Failed", Message = errorMessage });
                        return result;
                    }
                }*/


                if (getCustomerInfo)
                {
                    List<object> DataArray = new List<object>();

                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(DataArray);

                    string transItems = Newtonsoft.Json.JsonConvert.SerializeObject(transactionItems);

                    double taxtotalValue = transactionItems.Where(a => a.itemType == 2).Sum(a => a.itemTaxAmount);
                    double subtotalValue = transactionItems.Where(a => a.itemType == 2 || a.itemType == 52).Sum(a => a.itemValue);

                    var PaymentData = new
                    {
                        cashierID = "1",
                        subAccID = "",
                        accountID = "",
                        posID = 1,
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
                        vehicleTypeID = "",
                        isNormalTrans = "1",
                        attendantID = "",
                        items = transactionItems,
                        scData = data,
                        pwdData = data,
                        spData = data,
                        naacData = data,
                        movData = data
                    };

                    /*JsonResult result1 = new JsonResult(new { result = transItems });
                    return result1;*/

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

                    if (!doneSaving.IsNullOrEmpty())
                    {
                        JsonResult result = new JsonResult(new { result = "Success" });
                        return result;
                    }
                    else
                    {
                        JsonResult result = new JsonResult(new { result = "Failed", Message = doneSaving });
                        return result;
                    }
                }





                JsonResult jresult = new JsonResult(new { Message = "Done" });
                return jresult;
            
        }

        private int getDeliveries(JsonElement deliveries, int itemNum)
        {
            foreach (JsonElement delivery in deliveries.EnumerateArray())
            {
                //Transaction trans = fore.GetTransactionById(int.Parse(delivery.GetProperty("deliveryID").ToString()));
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
                    0
                    ));
                itemNum++;
                if (delivery.GetProperty("discount").EnumerateObject().Count() > 0)
                {
                    int discPresetID = 0;
                    int discType = 0;
                    double discValue = 0;

                    /*double transPrice = 0;
                    double transVolume = 0;
                    double transValue = 0;*/

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
                        discPresetID
                        ));

                    itemNum++;
                }
            }
            return itemNum;
        }

        private string[] getMops(JsonElement mops, int itemNum)
        {
            string lastAprovalCode = "";
            string lastBankCode = "";
            string lastCardNumber = "";
            string lastCardholderName = "";
            int mopCount = 1;
            foreach (JsonElement mop in mops.EnumerateArray())
            {
                int itemType = 8;
                if (mopCount == mops.EnumerateArray().Count())
                    itemType = 7;

                transactionItems.Add(new TransactionItemClass(itemNum,
                    itemType,
                    mop.GetProperty("name").ToString(),
                    double.Parse(mop.GetProperty("value").ToString()),
                    1,
                    double.Parse(mop.GetProperty("value").ToString()),
                    int.Parse(mop.GetProperty("mopID").ToString()),
                    double.Parse(mop.GetProperty("value").ToString()) / 1.12,
                    int.Parse(mop.GetProperty("mopID").ToString()),
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
                    0
                    ));

                string cardNum = "";
                string cardholderName = "";
                string bankCode = "";




                /*if (mops.GetProperty("info").ToString() != null)
                {
                    foreach (var mopInf in mops.GetProperty("info").EnumerateArray())
                    {
                        Console.WriteLine(mopInf.ValueKind.ToString());
                    }
                }

                if (mops.GetProperty("info").ValueKind != null)
                {
                    foreach (var mopInf in mops.GetProperty("info").EnumerateArray())
                    {
                        Console.WriteLine(mopInf.ValueKind.ToString());
                    }
                }*/


                if (mop.GetProperty("info").EnumerateObject().Count() > 0)
                {
                    foreach (var mopInfo in mop.GetProperty("info").EnumerateObject())
                    {

                        switch (mopInfo.Name)
                        {
                            case "cardNumber":
                                {
                                    cardNum = mopInfo.Value.ToString();
                                }
                                break;
                            case "cardholderName":
                                {
                                    cardholderName = mopInfo.Value.ToString();
                                }
                                break;
                            case "bankCode":
                                {
                                    bankCode = mopInfo.Value.ToString();
                                }
                                break;
                            default:
                                { }
                                break;
                        }

                        transactionMOPS.Add(new MopCardInfo(cardNum, cardholderName, bankCode));

                        lastAprovalCode = mop.GetProperty("approvalCode").ToString();
                        lastBankCode = bankCode;
                        lastCardNumber = cardNum;
                        lastCardholderName = cardholderName;

                    }
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

        private bool getCustomerDetails(JsonElement customerInformations)
        {
            foreach (var info in customerInformations.EnumerateObject())
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
            }
            return true;
        }

        private async Task<JsonDocument> APIQuery(object obj, string route)
        {
            HttpClient http = new HttpClient();
            HttpResponseMessage response = null;
            
            if (!(obj == null))
            {
                string content = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                StringContent contentString = new StringContent(content, Encoding.UTF8, "application/json");
                response = await http.PostAsync(route, contentString);
            }
            else
            {
                response = await http.GetAsync(route);
            }

            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            JsonDocument responseJson = JsonDocument.Parse(responseString);
            return responseJson;

        }

        private async Task<string> SendToAPI(object obj)
        {
            string response = "";
            try
            {
                JsonDocument decodedResponse = await APIQuery(obj, addNewTransactionRoute);
                JsonElement responseValue = decodedResponse.RootElement.GetProperty("data");
                string receiptOrNumber = responseValue.GetProperty("or_num").ToString();
                string receiptResetter = responseValue.GetProperty("resetter").ToString();
                string receiptTransID = responseValue.GetProperty("transID").ToString();
                response = responseValue.ToString();
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
            return response;
        }


        private async Task<bool> GetDiscounts()
        {
            try
            {
                discountPresets.Clear();
                object emptyObject = new { };
                JsonDocument decodedResponse = await APIQuery(emptyObject, getDiscountsRoute);
                JsonElement jsonResponseElement = decodedResponse.RootElement.GetProperty("data");
                foreach (var data in jsonResponseElement.EnumerateArray())
                {
                    JsonElement presets = data.GetProperty("discPreset");
                    foreach (var presetItem in presets.EnumerateArray())
                    {
                        int presetID = int.Parse(presetItem.GetProperty("preset_id").ToString());
                        int discountID = int.Parse(data.GetProperty("discount_id").ToString());
                        string presetName = presetItem.GetProperty("preset_name").ToString();
                        discountPresets.Add(new DiscountPresets(presetID, presetName, discountID));
                    }
                }
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

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
                var APIKey = RegKey.ComputeSha256Hash(hardwareID);

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
