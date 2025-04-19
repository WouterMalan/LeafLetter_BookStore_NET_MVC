using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Bulky.Utility;

namespace BookStoreWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList =
                _unitOfWork
                    .Product
                    .GetAll(includeProperties: "Category,ProductImages");

            return View(productList);
        }

        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new ShoppingCart()
            {
                Product =
                    _unitOfWork
                        .Product
                        .Get(u => u.Id == productId, includeProperties: "Category,ProductImages"),
                Count = 1,
                ProductId = productId
            };

            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                shoppingCart.ApplicationUserId = userId;

                var cartFromDb = _unitOfWork.ShoppingCart
                    .Get(x => x.ApplicationUserId == userId &&
                              x.ProductId == shoppingCart.ProductId);

                if (cartFromDb != null)
                {
                    cartFromDb.Count += shoppingCart.Count;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }
                else
                {
                    _unitOfWork.ShoppingCart.Add(shoppingCart);
                }

                _unitOfWork.Save();
                
                UpdateCartItemCount(userId);

                TempData["Success"] = "Product added to cart successfully";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                TempData["Error"] = "Failed to add product to cart";

                return RedirectToAction(nameof(Details), new { productId = shoppingCart.ProductId });
            }
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
        
        private void UpdateCartItemCount(string userId)
        {
            var cartItemCount = _unitOfWork
                .ShoppingCart
                .GetAll(u => u.ApplicationUserId == userId)
                .Count();
            
            HttpContext.Session.SetInt32(SD.SessionCart, cartItemCount);
        }
    }
}