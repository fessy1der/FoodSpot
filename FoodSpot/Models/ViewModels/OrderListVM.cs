using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodSpot.Models.ViewModels
{
    public class OrderListVM
    {
        public IList<OrderDetailsVM> Orders { get; set; }
        public PagingInfo PagingInfo { get; set; }
    }
}
