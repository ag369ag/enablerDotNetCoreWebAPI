using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace testASPWebAPI.Models
{
    [Keyless]
    public class API_Auth_User
    {
        //public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public byte isActive { get; set; }
    }
}
