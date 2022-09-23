using AptekaParsing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptekaParsing
{
    public class ApplicationContext:DbContext
    {
        public DbSet<DrugStore> Stores => Set<DrugStore>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductInStore> ProductInStores => Set<ProductInStore>();
        public static string connectionString;
        public ApplicationContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}
