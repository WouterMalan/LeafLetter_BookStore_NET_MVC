using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ApplicationDbContext dbContext;

        public DbInitializer(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext dbContext)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.dbContext = dbContext;
        }

        public void Initialize()
        {


            //migrations if they are not applied
            try
            {
                if (this.dbContext.Database.GetPendingMigrations().Count() > 0)
                {
                    this.dbContext.Database.Migrate();
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            //create roles if they are not created
            if (!this.roleManager.RoleExistsAsync(SD.RoleCustomer).GetAwaiter().GetResult())
            {
                this.roleManager.CreateAsync(new IdentityRole(SD.RoleCustomer)).GetAwaiter().GetResult();
                this.roleManager.CreateAsync(new IdentityRole(SD.RoleAdmin)).GetAwaiter().GetResult();
                this.roleManager.CreateAsync(new IdentityRole(SD.RoleEmployee)).GetAwaiter().GetResult();
                this.roleManager.CreateAsync(new IdentityRole(SD.RoleCompany)).GetAwaiter().GetResult();

                //if roles are not created, then we wil create admin user as well.
                userManager.CreateAsync(new ApplicationUser
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
                }, "Admin123*").GetAwaiter().GetResult();

                ApplicationUser user = this.dbContext.ApplicationUsers.Where(u => u.Email == "admin@email.com").FirstOrDefault();
                this.userManager.AddToRoleAsync(user, SD.RoleAdmin).GetAwaiter().GetResult();
            }

            return ;
        }
    }
}