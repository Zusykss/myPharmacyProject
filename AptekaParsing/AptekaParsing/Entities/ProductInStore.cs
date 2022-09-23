using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptekaParsing.Entities
{
    public class ProductInStore
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int StoreId { get; set; }
        public int CountLeft { get; set; }
        public double Price { get; set; }
        public DateTime RequestDate{ get; set; }
    }
}
