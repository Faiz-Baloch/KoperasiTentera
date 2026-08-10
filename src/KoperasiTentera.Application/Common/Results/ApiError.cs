using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Application.Common.Results
{
    public class ApiError
    {
        public string? Field { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
