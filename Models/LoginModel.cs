using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Alpha.Models
{
    public class LoginModel
    {
        #nullable disable
        [Required(ErrorMessage = "Email alanı gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir Email adresi girin.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre alanı gereklidir.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }

        public IEnumerable<Category> Categories { get; set; }

        public IEnumerable<Product> Products { get; set; }
        public string Culture { get; set; }

        public string RecaptchaSiteKey { get; set; }
        public string TurnstileSiteKey { get; set; }
        public string Honey { get; set; }
    }
}


