using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderPipeline.Models
{
    public class Order
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public decimal Discount { get; set; }
        public string Status { get; set; }
        public string Stage { get; set; }
    }
}
