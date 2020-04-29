using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodSpot.Models.ViewModels
{
    public class OrderCart
    {
        public List<Cart> listCart { get; set; }
        public Order Order { get; set; }
    }
}
