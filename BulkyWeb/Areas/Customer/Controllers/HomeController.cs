using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Bulky.Utility;

namespace BulkyWeb.Areas.Customer.Controllers
{
[Area("Customer")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork unitOfWork;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        this.unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        IEnumerable<Product> productList = this.unitOfWork.Product.GetAll(includeProperties: "Category, ProductImages");

        return View(productList);
    }

    public IActionResult Details(int productId)
    {
        ShoppingCart cart = new ShoppingCart()
        {
            Product = this.unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Category, ProductImages"),
            Count = 1,
            ProductId = productId
        };

        return View(cart);
    }

    [HttpPost]
    [Authorize]
    public IActionResult Details(ShoppingCart shoppingCart)
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        
        shoppingCart.ApplicationUserId = userId;

        ShoppingCart cartFromDb = this.unitOfWork.ShoppingCart.Get(x => x.ApplicationUserId == userId && x.ProductId == shoppingCart.ProductId);

        if (cartFromDb != null)
        {
            //shopping cart exist
            cartFromDb.Count += shoppingCart.Count;
            this.unitOfWork.ShoppingCart.Update(cartFromDb);
            unitOfWork.Save();
        }
        else
        {
            //add cart record
            this.unitOfWork.ShoppingCart.Add(shoppingCart);
            unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.SessionCart, unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count());
        }

        TempData["Success"] = "Product added to cart successfully";

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
}
