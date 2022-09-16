using AptekaParsing.Entities;
using Microsoft.EntityFrameworkCore;
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
        public ApplicationContext()
        {
            Database.EnsureCreated();
            
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string options;
            if (File.Exists("psqlConnection.txt"))
            {
                options = File.ReadAllText("psqlConnection.txt").Trim();
            }
            else
            {
                throw new FileNotFoundException("psqlConnection.txt not found");
            }
            optionsBuilder.UseNpgsql(options);
            //optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=AptekaParsing;Username=postgres;Password=admin;");
        }
    }
}
