using System.ComponentModel.DataAnnotations;

namespace testASPWebAPI
{
    public class API_Active_Cashier
    {
        [Key]
        public int ID { get; set; }
        public int? CashierID { get; set; }
        public string? CashierName { get; set; }

        public string Description { get; set; }
    }
}
