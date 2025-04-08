using IniParser;
using IniParser.Model;
using ITL.Enabler.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Registration;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

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

        private readonly ILogger<PumpController> _logger;

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
                if(selectedPump == null)
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
                            deliveryLockStatus = trans.IsLocked
                        });
                    }
                }

                if(selectedPump.CurrentTransaction != null)
                {
                    if(!selectedPump.CurrentTransaction.IsLocked)
                    {
                        pumpTransactions.Add(new TransactionClass
                        {
                            deliveryID = selectedPump.CurrentTransaction.DeliveryData.DeliveryID,
                            deliveryGrade = selectedPump.CurrentTransaction.DeliveryData.Grade.ToString(),
                            deliveryUnitPrice = selectedPump.CurrentTransaction.DeliveryData.UnitPrice,
                            deliveryQuantity = selectedPump.CurrentTransaction.DeliveryData.Quantity,
                            deliveryAmount = selectedPump.CurrentTransaction.DeliveryData.Money,
                            deliveryLockStatus = selectedPump.CurrentTransaction.IsLocked
                        });
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
            if(selectedPump.CurrentTransaction != null && selectedPump.CurrentTransaction.DeliveryData.DeliveryID == deliveryID)
            {
               return selectedPump.CurrentTransaction;
            }
            else
            {
                Transaction eqivalentTrans = null;
                foreach (Transaction trans in selectedPump.TransactionStack)
                {
                    if(!(trans.DeliveryData.DeliveryID == deliveryID))
                    {
                        continue;
                    }
                     eqivalentTrans = trans;
                }
                if(eqivalentTrans != null)
                {
                    return eqivalentTrans;
                }
                return null;

            }

        }


        //[Route("[action]")]
        [HttpPost (Name = "LockTransaction")]
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
            if(equivalentTransaction != null)
            {
                if (equivalentTransaction.IsLocked)
                {
                    result = false;
                    message = "Transaction is locked already.";
                    if(equivalentTransaction.LockedById != terminalID)
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

            if(result)
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
                if(equivalentTransaction != null)
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
                    deliveryLockStatus = equivalentTransaction.IsLocked
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

            if(!System.IO.File.Exists(path))
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
