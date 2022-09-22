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
        private string databasePath;
        public ApplicationContext()
        {
            Database.EnsureCreated();
        }
        public ApplicationContext(string databasePath)
        {
            this.databasePath = databasePath;
            Database.EnsureCreated();
            
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string options;
            if (File.Exists(databasePath+"\\"+"psqlConnection.txt"))
            {
                options = File.ReadAllText(databasePath + "\\" + "psqlConnection.txt").Trim();
            }
            else
            {
                throw new FileNotFoundException(databasePath+"\\"+"psqlConnection.txt not found");
            }
            optionsBuilder.UseNpgsql(options);
            //optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=AptekaParsing;Username=postgres;Password=admin;");
        }
    }
}
