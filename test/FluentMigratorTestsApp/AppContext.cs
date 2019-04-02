using FluentMigratorTestsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FluentMigratorTestsApp
{
    public class AppContext : DbContext
    {
        public DbSet<Orchard> Orchards { get; set; }
    }
}