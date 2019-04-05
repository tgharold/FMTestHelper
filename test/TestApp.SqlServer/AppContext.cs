using Microsoft.EntityFrameworkCore;
using TestApp.SqlServer.Models;

namespace TestApp.SqlServer
{
    public class AppContext : DbContext
    {
        public DbSet<Orchard> Orchards { get; set; }
    }
}