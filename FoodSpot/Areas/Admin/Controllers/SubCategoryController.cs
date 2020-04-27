using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoodSpot.Data;
using FoodSpot.Models;
using FoodSpot.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FoodSpot.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticDetails.ManagerUser)]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        [TempData]
        public string StatusMessage { get; set; }

        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            var subCategories = await _db.SubCategory.Include(s => s.Category).ToListAsync();
            return View(subCategories);
        }

        public async Task<IActionResult> Create()
        {
            CategoryAndSubCategoryVM model = new CategoryAndSubCategoryVM()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).Distinct().ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryAndSubCategoryVM model)
        {
            if (ModelState.IsValid)
            {
                var SubCategoryExists = _db.SubCategory.Include(s => s.Category).Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);

                if (SubCategoryExists.Count()>0)
                {
                    StatusMessage = "Error: Sub Category exists under " + SubCategoryExists.First().Category.Name + " category. Please add a different category or use another name.";
                }
                else
                {
                    _db.SubCategory.Add(model.SubCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            CategoryAndSubCategoryVM modelVM = new CategoryAndSubCategoryVM()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync(),
                StatusMessage = StatusMessage
            };
            return View(modelVM);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subCategory = await _db.SubCategory.SingleOrDefaultAsync(m => m.Id == id);

            if (subCategory == null)
            {
                return NotFound();
            }

            CategoryAndSubCategoryVM model = new CategoryAndSubCategoryVM()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryAndSubCategoryVM model)
        {
            if (ModelState.IsValid)
            {
                var SubCategoryExists = _db.SubCategory.Include(s => s.Category).Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);

                if (SubCategoryExists.Count() > 0)
                {
                    //Error
                    StatusMessage = "Error : Sub Category exists under " + SubCategoryExists.First().Category.Name + " category. Please use another name.";
                }
                else
                {
                    var subCatFromDb = await _db.SubCategory.FindAsync(model.SubCategory.Id);
                    subCatFromDb.Name = model.SubCategory.Name;

                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            CategoryAndSubCategoryVM modelVM = new CategoryAndSubCategoryVM()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(),
                StatusMessage = StatusMessage
            };
            //modelVM.SubCategory.Id = id;
            return View(modelVM);
        }

        [ActionName("GetSubCategory")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            List<SubCategory> subCategories = new List<SubCategory>();

            subCategories = await (from subCategory in _db.SubCategory
                                   where subCategory.CategoryId == id
                                   select subCategory).ToListAsync();
            return Json(new SelectList(subCategories, "Id", "Name"));
        }

        //GET Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var subCategory = await _db.SubCategory.Include(s => s.Category).SingleOrDefaultAsync(m => m.Id == id);
            if (subCategory == null)
            {
                return NotFound();
            }

            return View(subCategory);
        }

        //GET Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var subCategory = await _db.SubCategory.Include(s => s.Category).SingleOrDefaultAsync(m => m.Id == id);
            if (subCategory == null)
            {
                return NotFound();
            }

            return View(subCategory);
        }

        //POST Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subCategory = await _db.SubCategory.SingleOrDefaultAsync(m => m.Id == id);
            _db.SubCategory.Remove(subCategory);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

    }
}