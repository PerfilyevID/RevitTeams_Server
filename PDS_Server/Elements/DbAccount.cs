using MongoDB.Bson;
using System;

namespace PDS_Server.Elements
{
    public class DbAccount : DbElement
    {
        public DbAccount() { }

        public string Login { get; set; }
        public string Password { get; set; }
        public string VerifyLink { get; set; } = null;
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// Values: "Admin", "Owner", "User", "Expired", "NewUser".
        /// </summary>
        public string Role { get; set; } = "NewUser";

        // Personal
        public string ImageData { get; set; } = null;
        public string FirstName { get; set; } = null;
        public string SecondName { get; set; } = null;

        // Commercial
        public DateTime Access { get; set; } = DateTime.Now;

        // Team
        public ObjectId? Team { get; set; }
        public bool Owner { get; set; } = false;
        public ObjectId Department { get; set; }
    }

}
