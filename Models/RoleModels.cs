using System.ComponentModel.DataAnnotations;
using Alpha.Entity;
using Microsoft.AspNetCore.Identity;
using Alpha.Identity;

namespace Alpha.Models
{
    public class RoleModels
    {      
        #nullable disable
        [Required]
        public string name { get; set; }

        public class RoleDetails
        {
            public IdentityRole Role { get; set; }

            public IEnumerable<User> Members { get; set; }
            public IEnumerable<User> NonMembers { get; set; }
        }

        public class RoleEditModel
        {
            public string RoleId { get; set; }
            public string RoleName { get; set; }
            public string[] IdsToAdd { get; set; }
            public string[] IdsToDelete { get; set; }
        }
    }
}