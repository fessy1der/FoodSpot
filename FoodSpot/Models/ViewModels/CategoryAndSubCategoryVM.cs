using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodSpot.Models
{
    public class CategoryAndSubCategoryVM
    {
        public IEnumerable<Category> CategoryList { get; set; }
        public SubCategory SubCategory { get; set; }
        public List<string> SubCategoryList { get; set; }
        public string StatusMessage { get; set; }
    }
}
