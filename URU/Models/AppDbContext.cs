using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace URU.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }

        public AppDbContext(DbContextOptions<DbContext> options) : base(options)
        { }

        public DbSet<Contact> Contacts { get; set; }
    }
}