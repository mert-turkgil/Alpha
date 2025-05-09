using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha.Models
{
    public class ResourceOperationResult
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
    }
}