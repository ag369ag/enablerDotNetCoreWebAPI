using IniParser;
using IniParser.Model;
using ITL.Enabler.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace testASPWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PumpController : ControllerBase
    {
        private Forecourt fore = PumpClass.forecourt;

        private string server;
        private int terminalID;
        private string password;
        private int pumpStart;
        private int pumpEnd;

        public PumpController()
        {
            var configParser = new FileIniDataParser();
            IniData data = configParser.ReadFile("config.ini");
            server = data["Configuration"]["Server"];
            terminalID = int.Parse(data["Configuration"]["TerminalID"]);
            password = data["Configuration"]["Password"];
            pumpStart = int.Parse(data["Configuration"]["PumpStartID"]);
            pumpEnd = int.Parse(data["Configuration"]["PumpEndID"]);
            if (!fore.IsConnected)
            {
                fore.Connect(server, terminalID, "", password, true);
            }
        }

       
        [Route("[action]")]
        [HttpGet(Name = "Getting pump data")]
        public JsonResult GetPumpData()
        {
            while (!fore.IsConnected)
            {
            }
            
            List<PumpClass> pumpList = new List<PumpClass>();
            foreach (Pump item in fore.Pumps)
            {
                List<TransactionClass> transList = new List<TransactionClass>();
                foreach (Transaction trans in item.TransactionStack)
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

                if(pumpStart <= item.Number && pumpEnd >= item.Number)
                {
                    pumpList.Add(new PumpClass
                    {
                        pumpName = item.Name,
                        pumpNumber = item.Number,
                        pumpStack = item.TransactionStack.Count,
                        transactions = transList
                    });
                }
            }
            return new JsonResult(pumpList);
        }

        [Route("[action]")]
        [HttpPost(Name = "GetTransactionInfo")]
        public JsonResult GetTransactionInfo(JsonDocument json)
        {
            while (!fore.IsConnected)
            {
            }

            int pumpID = json.RootElement.GetProperty("pumpID").GetInt32();
            int deliveryID = json.RootElement.GetProperty("deliveryID").GetInt32();
            

            Pump selectedPump = fore.Pumps[pumpID];
            TransactionClass selectedTransaction;
            Transaction equivalentTransaction;
            selectedPump.TransactionStack.TryGetValue(deliveryID, out equivalentTransaction);

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

                return new JsonResult(new { Status = "Success", Pumpid = pumpID, DeliveryID = deliveryID, SelectedTransaction = selectedTransaction });
            }

            else
            {
                JsonResult response = new JsonResult(new { Status = "Failed", Message = "No such transaction exist." });
                response.StatusCode = 500;
                return response;
            }

            
        }



        [Route("[action]")]
        [HttpPost(Name = "VoidTransaction")]
        public JsonResult VoidTransaction(JsonDocument json)
        {
            while (!fore.IsConnected)
            {
            }

            int pumpID = json.RootElement.GetProperty("pumpID").GetInt32();
            int deliveryID = json.RootElement.GetProperty("deliveryID").GetInt32();
           

            Pump selectedPump = fore.Pumps[pumpID];
            TransactionClass selectedTransaction;
            Transaction equivalentTransaction;
            selectedPump.TransactionStack.TryGetValue(deliveryID, out equivalentTransaction);
            bool result = false;
            string message = "";
            try
            {
                if(equivalentTransaction != null)
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
           
            if(result)
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

                return new JsonResult(new { Status = "Success", Pumpid = pumpID, DeliveryID = deliveryID, SelectedTransaction = selectedTransaction });
            }
            else
            {
                JsonResult response = new JsonResult(new { Status = "Failed", Message = message ?? "Error voiding transaction" });
                response.StatusCode = 500;
                return response;
            }
            
        }



        [Route("[action]")]
        [HttpPost(Name = "LockTransaction")]
        public JsonResult LockTransaction(JsonDocument json)
        {

            while (!fore.IsConnected)
            {
            }

            int pumpID = json.RootElement.GetProperty("pumpID").GetInt32();
            int deliveryID = json.RootElement.GetProperty("deliveryID").GetInt32();

            Pump selectedPump = fore.Pumps[pumpID];

            Transaction equivalentTransaction;
            selectedPump.TransactionStack.TryGetValue(deliveryID, out equivalentTransaction);

            bool result = false;
            string message = "";
            bool lockStatus = false;
            if(equivalentTransaction != null)
            {
                if (equivalentTransaction.IsLocked)
                {
                    result = false;
                    message = "Transaction is locked already.";
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
                    }
                }
            }
            else
            {
                result = false;
                message = "Transaction does not exist.";
            }
            

            Transaction transaction;
            selectedPump.TransactionStack.TryGetValue(deliveryID, out transaction);
            if(transaction != null)
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

            if(result)
            {
                return new JsonResult(new { Status = "Success", LockStatus = lockStatus });
            }
            else
            {
                return new JsonResult(new { Status = "Failed", LockStatus = lockStatus });
            }
        }



        [Route("[action]")]
        [HttpPost(Name = "ClearTransaction")]
        public JsonResult ClearTransaction(JsonDocument json)
        {
            while (!fore.IsConnected)
            {
            }

            int pumpID = json.RootElement.GetProperty("pumpID").GetInt32();
            int deliveryID = json.RootElement.GetProperty("deliveryID").GetInt32();
            //json.RootElement.GetProperty("transactionType").ToString()
            //TransactionClearTypes type = TransactionClearTypes.Normal;

            Pump selectedPump = fore.Pumps[pumpID];
            TransactionClass selectedTransaction;
            Transaction equivalentTransaction;
            selectedPump.TransactionStack.TryGetValue(deliveryID, out equivalentTransaction);
            bool result = false;
            string message = "";
            try
            {
                if(equivalentTransaction != null)
                {
                    equivalentTransaction.Clear(TransactionClearTypes.Normal);     
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
                selectedTransaction = new TransactionClass
                {
                    deliveryID = equivalentTransaction.DeliveryData.DeliveryID,
                    deliveryGrade = equivalentTransaction.DeliveryData.Grade.ToString(),
                    deliveryUnitPrice = equivalentTransaction.DeliveryData.UnitPrice,
                    deliveryQuantity = equivalentTransaction.DeliveryData.Quantity,
                    deliveryAmount = equivalentTransaction.DeliveryData.Money,
                    deliveryLockStatus = equivalentTransaction.IsLocked
                };
                return new JsonResult(new { Status = "Success", Pumpid = pumpID, DeliveryID = deliveryID, SelectedTransaction = selectedTransaction });
            }
            else
            {
                JsonResult response = new JsonResult(new { Status = "Failed", Message = message ?? "Error" });
                response.StatusCode = 500;
                return response;
            }
            
        }

    }

    
}
