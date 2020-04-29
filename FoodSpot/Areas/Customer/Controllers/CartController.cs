using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodSpot.Data;
using FoodSpot.Models;
using FoodSpot.Models.ViewModels;
using FoodSpot.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace FoodSpot.Areas.Customer.Controllers
{
    [Area("customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        [BindProperty]
        public OrderCart orderCart { get; set; }

        public CartController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {

            orderCart = new OrderCart()
            {
                Order = new Models.Order()
            };

            orderCart.Order.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _db.Cart.Where(c => c.ApplicationUserId == claim.Value);
            if (cart != null)
            {
                orderCart.listCart = cart.ToList();
            }

            foreach (var list in orderCart.listCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                orderCart.Order.OrderTotal = orderCart.Order.OrderTotal + (list.MenuItem.Price * list.Count);
                list.MenuItem.Description = StaticDetails.ConvertToRawHtml(list.MenuItem.Description);
                if (list.MenuItem.Description.Length > 100)
                {
                    list.MenuItem.Description = list.MenuItem.Description.Substring(0, 99) + "...";
                }
            }
            orderCart.Order.OrderTotalOriginal = orderCart.Order.OrderTotal;

            if (HttpContext.Session.GetString(StaticDetails.ssCouponCode) != null)
            {
                orderCart.Order.CouponCode = HttpContext.Session.GetString(StaticDetails.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == orderCart.Order.CouponCode.ToLower()).FirstOrDefaultAsync();
                orderCart.Order.OrderTotal = StaticDetails.DiscountedPrice(couponFromDb, orderCart.Order.OrderTotalOriginal);
            }


            return View(orderCart);

        }


        public async Task<IActionResult> Summary()
        {

            orderCart = new OrderCart()
            {
                Order = new Models.Order()
            };

            orderCart.Order.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            AppUser appUser = await _db.AppUser.Where(c => c.Id == claim.Value).FirstOrDefaultAsync();
            var cart = _db.Cart.Where(c => c.ApplicationUserId == claim.Value);
            if (cart != null)
            {
                orderCart.listCart = cart.ToList();
            }

            foreach (var list in orderCart.listCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                orderCart.Order.OrderTotal = orderCart.Order.OrderTotal + (list.MenuItem.Price * list.Count);

            }
            orderCart.Order.OrderTotalOriginal = orderCart.Order.OrderTotal;
            orderCart.Order.PickupName = appUser.Name;
            orderCart.Order.PhoneNumber = appUser.PhoneNumber;
            orderCart.Order.PickUpTime = DateTime.Now;


            if (HttpContext.Session.GetString(StaticDetails.ssCouponCode) != null)
            {
                orderCart.Order.CouponCode = HttpContext.Session.GetString(StaticDetails.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == orderCart.Order.CouponCode.ToLower()).FirstOrDefaultAsync();
                orderCart.Order.OrderTotal = StaticDetails.DiscountedPrice(couponFromDb, orderCart.Order.OrderTotalOriginal);
            }


            return View(orderCart);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);


            orderCart.listCart = await _db.Cart.Where(c => c.ApplicationUserId == claim.Value).ToListAsync();

            orderCart.Order.PaymentStatus = StaticDetails.PaymentStatusPending;
            orderCart.Order.OrderDate = DateTime.Now;
            orderCart.Order.UserId = claim.Value;
            orderCart.Order.Status = StaticDetails.PaymentStatusPending;
            orderCart.Order.PickUpTime = Convert.ToDateTime(orderCart.Order.PickUpDate.ToShortDateString() + " " + orderCart.Order.PickUpTime.ToShortTimeString());

            List<OrderDetails> orderDetailsList = new List<OrderDetails>();
            _db.Order.Add(orderCart.Order);
            await _db.SaveChangesAsync();

            orderCart.Order.OrderTotalOriginal = 0;


            foreach (var item in orderCart.listCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                OrderDetails orderDetails = new OrderDetails
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = orderCart.Order.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                orderCart.Order.OrderTotalOriginal += orderDetails.Count * orderDetails.Price;
                _db.OrderDetails.Add(orderDetails);

            }

            if (HttpContext.Session.GetString(StaticDetails.ssCouponCode) != null)
            {
                orderCart.Order.CouponCode = HttpContext.Session.GetString(StaticDetails.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == orderCart.Order.CouponCode.ToLower()).FirstOrDefaultAsync();
                orderCart.Order.OrderTotal = StaticDetails.DiscountedPrice(couponFromDb, orderCart.Order.OrderTotalOriginal);
            }
            else
            {
                orderCart.Order.OrderTotal = orderCart.Order.OrderTotalOriginal;
            }
            orderCart.Order.CouponCodeDiscount = orderCart.Order.OrderTotalOriginal - orderCart.Order.OrderTotal;

            _db.Cart.RemoveRange(orderCart.listCart);
            HttpContext.Session.SetInt32(StaticDetails.ssShoppingCartCount, 0);
            await _db.SaveChangesAsync();

            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(orderCart.Order.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID : " + orderCart.Order.Id,
                Source = stripeToken

            };
            var service = new ChargeService();
            Charge charge = service.Create(options);

            if (charge.BalanceTransactionId == null)
            {
                orderCart.Order.PaymentStatus = StaticDetails.PaymentStatusRejected;
            }
            else
            {
                orderCart.Order.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status.ToLower() == "succeeded")
            {
                orderCart.Order.PaymentStatus = StaticDetails.PaymentStatusApproved;
                orderCart.Order.Status = StaticDetails.StatusSubmitted;
            }
            else
            {
                orderCart.Order.PaymentStatus = StaticDetails.PaymentStatusRejected;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Index", "Home");

        }


        public IActionResult AddCoupon()
        {
            if (orderCart.Order.CouponCode == null)
            {
                orderCart.Order.CouponCode = "";
            }
            HttpContext.Session.SetString(StaticDetails.ssCouponCode, orderCart.Order.CouponCode);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {

            HttpContext.Session.SetString(StaticDetails.ssCouponCode, string.Empty);

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _db.Cart.FirstOrDefaultAsync(c => c.Id == cartId);
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _db.Cart.FirstOrDefaultAsync(c => c.Id == cartId);
            if (cart.Count == 1)
            {
                _db.Cart.Remove(cart);
                await _db.SaveChangesAsync();

                var cnt = _db.Cart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(StaticDetails.ssShoppingCartCount, cnt);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _db.Cart.FirstOrDefaultAsync(c => c.Id == cartId);

            _db.Cart.Remove(cart);
            await _db.SaveChangesAsync();

            var cnt = _db.Cart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(StaticDetails.ssShoppingCartCount, cnt);


            return RedirectToAction(nameof(Index));
        }
    }
}