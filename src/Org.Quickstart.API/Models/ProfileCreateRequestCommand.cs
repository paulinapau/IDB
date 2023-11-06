using System;
using System.Collections.Generic;

namespace Org.Quickstart.API.Models
{
    public class ProfileCreateRequestCommand
    {
        /*
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public decimal OnBoardCredit { get; set; }
        */
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }

        public string phoneNumber { get; set; }
        public string gender { get; set; }
        public DateTime registrationDate { get; set; }
        public Address Address { get; set; }
        public ICollection<Order> Orders { get; set; }

        public Profile GetProfile()
        {
            return new Profile
            {
                username = this.username,
                phoneNumber = this.phoneNumber,
                firstName = this.firstName,
                lastName = this.lastName,
                email = this.email,
                password = this.password,
                gender = this.gender,
                registrationDate = this.registrationDate,
                Address = this.Address,
                Orders = this.Orders,
            };
        }
        
    }
}
