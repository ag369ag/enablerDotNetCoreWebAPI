using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testASPWebAPI
{
    public class Finalisations
    {
        [Key]
        public int MOP_ID { get; set; }
        [Column(TypeName = "char(20)")]
        public string MOP_Name { get; set; }

        //[Column(TypeName = "varchar(10)")]
        [Column(TypeName = "varchar(10)")]
        public string? MOP_Code { get; set; }
    }
}
