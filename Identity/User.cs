using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Alpha.Identity
{
    public class User:IdentityUser
    {
        #nullable disable
        public string FirstName{get;set;}
        public string LastName { get; set; }
         
    }
}