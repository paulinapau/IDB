using System;
using System.Collections.Generic;

namespace Org.Quickstart.API.Models
{
    public class Profile
    {
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

        /*
        private string _password;
        public string Password {
            get
            {
                return _password;
            }
            set
            {
                _password = BCrypt.Net.BCrypt.HashPassword(value);
            }
         }

        public void TransferTo(Profile to, decimal amount)
        {
            // TODO: logic to make sure amount can't go negative
            // and that amount isn't negative, etc

            this.OnBoardCredit -= amount;
            to.OnBoardCredit += amount;
        }
        */
        public string GenerateNextUserId(string lastUserId)
        {
            if (string.IsNullOrEmpty(lastUserId))
            {
                return "u1";
            }

            int lastNumber = int.Parse(lastUserId.Substring(1)); // Extract the numeric part
            int nextNumber = lastNumber + 1;

            return "u" + nextNumber;
        }

    }
}
