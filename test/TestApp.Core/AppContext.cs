using Microsoft.EntityFrameworkCore;
using TestApp.Core.Models;

namespace TestApp.Core
{
    public class AppContext : DbContext
    {
        public DbSet<Orchard> Orchards { get; set; }
    }
}