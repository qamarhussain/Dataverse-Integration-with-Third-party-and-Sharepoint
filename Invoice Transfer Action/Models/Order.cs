using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Invoice_Transfer_Action.Models
{
    
    public class Order
    {
        public string number { get; set; }
        public Customer customer { get; set; } = new Customer();
        public string deliveryDate { get; set; }
        public string orderDate { get; set; }
        public List<OrderLine> orderLines { get; set; } = new List<OrderLine>();
    }
    public class Product
    {
        public string id { get; set; }
    }
    public class Customer
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class OrderLine
    {
        public Product product { get; set; } = new Product();
        public int count { get; set; }
        //public decimal unitPriceExcludingVatCurrency { get; set; }
        //public decimal discount { get; set; }
        public string description { get; set; }

    }

    public class MonthData
    {
        public string InvoiceId { get; set; }
        public string InvoiceName { get; set; }
        public string cr200_ordrelinjeid { get; set; }
        public string cr200_vasktype { get; set; }
        public string ProductId { get; set; }
        public decimal Price { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
    }
}
