using System.ComponentModel.DataAnnotations;

namespace testASPWebAPI
{
    public class Periods
    {
        [Key]
        public int Period_ID {  get; set; }
        public int Period_Type { get; set; }
        public byte Period_State { get; set; }
        public int Shift_number { get; set; }
    }
}
