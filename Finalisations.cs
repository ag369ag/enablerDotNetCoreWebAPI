using System.ComponentModel.DataAnnotations;

namespace testASPWebAPI
{
    public class Finalisations
    {
        [Key]
        public int MOP_ID { get; set; }
        public string MOP_Name { get; set; }
        public string MOP_Code { get; set; }
    }
}
