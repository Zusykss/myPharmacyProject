using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptekaParsing.Entities
{
    public class Product
    {
        [Column("ProductId")]
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? Producer { get; set; }
        public List<ProductInStore> ProductInStores { get; set; } = new();
        
    }
}
