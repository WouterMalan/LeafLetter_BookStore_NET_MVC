using Bulky.DataAccess.Data;
using Bulky.DataAccess.DbInitializer;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.DBInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;

        public DbInitializer(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }

        public void Initialize()
        {
            //migrations if they are not applied
            try
            {
                if (_dbContext.Database.GetPendingMigrations().Count() > 0)
                {
                    _dbContext.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            //create roles if they are not created
            if (!_roleManager.RoleExistsAsync(SD.RoleCustomer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.RoleCustomer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.RoleAdmin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.RoleEmployee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.RoleCompany)).GetAwaiter().GetResult();

                //if roles are not created, create admin user
                _userManager.CreateAsync(new ApplicationUser
                    {
                        UserName = "admin@email.com",
                        Email = "admin@email.com",
                        Name = "Wouter Malan",
                        EmailConfirmed = true,
                        PhoneNumber = "1234567890",
                        City = "Pretoria",
                        Province = "Gauteng",
                        StreetAddress = "Street 1",
                        PostalAddress = "0001"
                    }, "Admin123*")
                    .GetAwaiter()
                    .GetResult();

                ApplicationUser user = _dbContext
                    .ApplicationUsers
                    .Where(u => u.Email == "admin@email.com")
                    .FirstOrDefault();

                _userManager.AddToRoleAsync(user, SD.RoleAdmin).GetAwaiter().GetResult();
            }

            return;
        }
    }
}