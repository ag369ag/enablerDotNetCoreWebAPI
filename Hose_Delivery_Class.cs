using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace testASPWebAPI
{
    //[Keyless]
    public class Hose_Delivery
    {
        [Key]
        public int Delivery_ID { get; set; }
        public int Hose_ID { get; set; }
        public decimal Hose_Meter_Value { get; set; }
       /* public int? Attendant_ID { get; set; }
        public int Price_Level { get; set; }
        public DateTime Completed_TS { get; set; }
        public DateTime? Cleared_Date_Time { get; set; }
        public int Delivery_Type { get;set; }
        public int Delivery_State { get; set; }
        public decimal Delivery_Volume { get; set; }
        public decimal Delivery_Value { get; set; }
        public decimal Del_Sell_Price { get; set; }
        public decimal Del_Cost_Price { get; set; }
        public int? Cleared_By { get; set; }
        public int? Reserved_By { get; set; }
        public int? Transaction_ID { get; set; }
        public int? Del_Item_Number { get; set; }
        public decimal? Delivery2_Volume { get; set; }
        
        public decimal Hose_Meter_Value { get; set; }
        public decimal Hose_Meter_Volume2 { get; set; }
        public decimal Hose_Meter_Value2 { get; set; }
        public decimal Blend_Ratio { get; set; }
        public int Previous_Delivery_Type { get; set; }
        public int? Auth_Ref { get; set; }
        public decimal Delivery1_Volume { get; set; }
        public decimal Delivery1_Value { get; set; }
        public decimal Delivery2_Value { get; set; }
        public decimal Hose_Meter_Volume1 { get; set; }
        public decimal Hose_Meter_Value1 { get; set; }
        public decimal Grade1_Price { get; set; }
        public decimal Grade2_Price { get; set; }*/
    }
}
