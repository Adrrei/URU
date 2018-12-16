using Microsoft.EntityFrameworkCore;

namespace URU.Models
{
    public interface IAppDbContext
    {
        DbSet<Contact> Contacts { get; set; }
    }
}