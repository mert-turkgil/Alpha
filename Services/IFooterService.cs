using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Models;

namespace Alpha.Services
{
    public interface IFooterService 
    {
        FooterViewModel GetFooterViewModelAsync();
    }
}