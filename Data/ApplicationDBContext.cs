using Microsoft.EntityFrameworkCore;
using testASPWebAPI.Models;

namespace testASPWebAPI.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions dbOptions) : base(dbOptions)
        {
            
        }

        public DbSet<API_Auth_User> API_Auth_User { get; set; }
    }
}
