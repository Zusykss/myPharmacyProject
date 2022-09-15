using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptekaParsing.Entities
{
    public class DrugStore
    {
        [Key]
        [Column("StoreId")]
        public int Id { get; set; }

        public string Name { get; set; }
        public string ?City { get; set; }
        public string ?Adress { get; set; }
        public string ?PhoneNumber { get; set; }
        public string ?Сoordinates { get; set; }
        public string ?Site { get; set; }
    }
}
