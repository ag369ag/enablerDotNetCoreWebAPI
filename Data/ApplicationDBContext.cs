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

        public DbSet<Hose_Delivery> Hose_Delivery { get; set; }

        public DbSet<Finalisations> Finalisations { get; set; }
        public DbSet<Periods> Periods { get; set; }

    }
}
