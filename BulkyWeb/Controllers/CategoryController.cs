using BulkyWeb.Data;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Controllers
{
    public class CategoryController : Controller
    {

    public CategoryController(ApplicationDbContext db)
    {
        dbContext = db;
    }

        public IActionResult Index()
        {
            List<Category> objCategoryList = dbContext.Categories.ToList();

            return View(objCategoryList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category obj)
        {
            if (ModelState.IsValid)
            {
                dbContext.Categories.Add(obj);
                dbContext.SaveChanges();
                return RedirectToAction("Index");
            }
            
            return View(obj);
          
        }

    private readonly ApplicationDbContext dbContext;

    }
}
