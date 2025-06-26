using ITL.Enabler.API;
using Microsoft.PointOfService;

namespace testASPWebAPI
{
    public class PumpClass
    {
        public int pumpNumber {  get; set; }
        public string pumpName { get; set; }
        public int pumpStack { get; set; }

        public List<TransactionClass> transactions { get; set; }

        public static Forecourt forecourt = new Forecourt();

        public static PosPrinter posPrinter { get;set; }

        
       
       
    }
}
