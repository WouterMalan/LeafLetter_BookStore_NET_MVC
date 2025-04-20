using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace BookStoreWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;

        [BindProperty] public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(ILogger<CartController> logger, IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            try
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;

                ShoppingCartVM = LoadShoppingCart(claimsIdentity);

                return View(ShoppingCartVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shopping cart");
                TempData["Error"] = "Failed to load shopping cart";

                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Summary()
        {
            try
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                ShoppingCartVM = LoadOrderSummary(claimsIdentity);

                return View(ShoppingCartVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order summary");
                TempData["Error"] = "Failed to load order summary";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            try
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User not found");
                }


                var applicationUser = _unitOfWork.ApplicationUser
                                          .Get(x => x.Id == userId, tracked: false)
                                      ?? throw new InvalidOperationException("User details not found");

                InitializeOrder(userId);

                // if (!ModelState.IsValid)
                // {
                //     ShoppingCartVM = LoadOrderSummary(claimsIdentity);
                //     return View(nameof(Summary), ShoppingCartVM);
                // }

                var cartItems = _unitOfWork.ShoppingCart
                    .GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product");

                if (!cartItems.Any())
                {
                    throw new InvalidOperationException("Shopping cart is empty");
                }

                CalculateOrderTotal();
                SetOrderStatus(applicationUser.CompanyId);
                SaveOrderHeader();
                SaveOrderDetails();

                if (IsRegularCustomer(applicationUser.CompanyId))
                {
                    return ProcessStripePayment();
                }

                return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                TempData["Error"] = "Failed to process order";
                return RedirectToAction(nameof(Summary));
            }
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork
                .OrderHeader
                .Get(u => u.Id == id, includeProperties: "ApplicationUser");

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                //Order by customer
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                // Check if the session is paid (From Stripe)
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);

                    _unitOfWork.Save();
                }

                HttpContext.Session.Clear();
            }

            _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order Created",
                $"<p>Order - {orderHeader.Id} has been submitted successfully</p>");

            List<ShoppingCart> shoppingCartsList = _unitOfWork
                .ShoppingCart
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCart.DeleteRange(shoppingCartsList);

            _unitOfWork.Save();

            return View(id);
        }

        public IActionResult Plus(int cartId)
        {
            var cart = _unitOfWork
                .ShoppingCart
                .Get(x => x.Id == cartId, includeProperties: "Product");
            cart.Count += 1;

            _unitOfWork.ShoppingCart.Update(cart);

            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cart = _unitOfWork
                .ShoppingCart
                .Get(x => x.Id == cartId, includeProperties: "Product", tracked: true);

            if (cart.Count == 1)
            {
                HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).Count() - 1);

                _unitOfWork.ShoppingCart.Delete(cart);
            }
            else
            {
                cart.Count -= 1;

                _unitOfWork.ShoppingCart.Update(cart);
            }

            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cart = _unitOfWork
                .ShoppingCart
                .Get(x => x.Id == cartId, tracked: true);

            _unitOfWork.ShoppingCart.Delete(cart);

            HttpContext.Session.SetInt32(SD.SessionCart,
                _unitOfWork
                    .ShoppingCart
                    .GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).Count() - 1);

            _unitOfWork.Save();

            TempData["Success"] = "Product removed from cart successfully";

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        private ShoppingCartVM LoadShoppingCart(ClaimsIdentity claimsIdentity)
        {
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID not found");
            }

            var cartVm = new ShoppingCartVM
            {
                ShoppingCartList = _unitOfWork.ShoppingCart
                    .GetAll(x => x.ApplicationUserId == userId,
                        includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };

            // Load all product images in one query
            var productIds = cartVm.ShoppingCartList.Select(x => x.ProductId).ToList();
            var productImages = _unitOfWork.ProductImage
                .GetAll(x => productIds.Contains(x.ProductId))
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Calculate totals
            foreach (var cart in cartVm.ShoppingCartList)
            {
                cart.Product.ProductImages = productImages.GetValueOrDefault(cart.ProductId, new List<ProductImage>());
                cart.Price = GetPriceBasedOnQuantity(cart);
                cartVm.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return cartVm;
        }

        private decimal GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }

            if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }

            return shoppingCart.Product.Price100;
        }

        private ShoppingCartVM LoadOrderSummary(ClaimsIdentity claimsIdentity)
        {
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID not found");
            }

            var user = _unitOfWork
                           .ApplicationUser
                           .Get(x => x.Id == userId, tracked: false)
                       ?? throw new InvalidOperationException("User not found");

            var cartVm = new ShoppingCartVM
            {
                ShoppingCartList = _unitOfWork.ShoppingCart
                    .GetAll(x => x.ApplicationUserId == userId,
                        includeProperties: "Product"),
                OrderHeader = new OrderHeader
                {
                    ApplicationUser = user,
                    Name = user.Name,
                    PhoneNumber = user.PhoneNumber,
                    StreetAddress = user.StreetAddress,
                    City = user.City,
                    State = user.Province,
                    PostalAddress = user.PostalAddress,
                    OrderTotal = 0
                }
            };

            cartVm.OrderHeader.OrderTotal = cartVm.ShoppingCartList.Sum(cart =>
                GetPriceBasedOnQuantity(cart) * cart.Count);

            foreach (var cart in cartVm.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
            }

            return cartVm;
        }

        private void InitializeOrder(string userId)
        {
            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart
                .GetAll(x => x.ApplicationUserId == userId,
                    includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
        }

        private void CalculateOrderTotal()
        {
            ShoppingCartVM.OrderHeader.OrderTotal = ShoppingCartVM.ShoppingCartList.Sum(cart =>
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                return cart.Price * cart.Count;
            });
        }

        private void SetOrderStatus(int? companyId)
        {
            if (IsRegularCustomer(companyId))
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
        }

        private void SaveOrderHeader()
        {
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();
        }

        private void SaveOrderDetails()
        {
            var orderDetails = ShoppingCartVM.ShoppingCartList.Select(cart => new OrderDetail
            {
                ProductId = cart.ProductId,
                OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            }).ToList();

            _unitOfWork.OrderDetail.AddRange(orderDetails);
            _unitOfWork.Save();
        }

        private IActionResult ProcessStripePayment()
        {
            var domain = $"{Request.Scheme}://{Request.Host.Value}/";
            var options = CreateStripeSessionOptions(domain);
            var service = new SessionService();
            Session session = service.Create(options);

            _unitOfWork.OrderHeader.UpdateStripePaymentId(
                ShoppingCartVM.OrderHeader.Id,
                session.Id,
                session.PaymentIntentId);

            _unitOfWork.Save();

            Response.Headers.Append("Location", session.Url);

            // Redirect to Stripe Checkout
            return StatusCode(303);
        }

        private SessionCreateOptions CreateStripeSessionOptions(string domain)
        {
            return new SessionCreateOptions
            {
                SuccessUrl = $"{domain}Customer/Cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                CancelUrl = $"{domain}Customer/Cart/Index",
                LineItems = ShoppingCartVM.ShoppingCartList.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "zar",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                }).ToList(),
                Mode = "payment"
            };
        }

        private static bool IsRegularCustomer(int? companyId) =>
            companyId.GetValueOrDefault() == 0;
    }
}