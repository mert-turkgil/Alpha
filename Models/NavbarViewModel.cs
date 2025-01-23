using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Alpha.Models
{
    public class NavbarViewModel
    {
        #nullable disable
            public string Home { get; set; }
            public string About { get; set; }
            public string Services { get; set; }
            public string Blog { get; set; }
            public string Privacy { get; set; }
            public string Contact { get; set; }
            public string NH1 { get; set; }
            public string NH2 { get; set; }
            public string NH3 { get; set; }
            public List<Category> Categories { get; set; }

            public string Culture { get; set; }
        

    }
}