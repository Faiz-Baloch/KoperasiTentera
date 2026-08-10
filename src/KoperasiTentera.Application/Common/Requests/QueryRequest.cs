using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

namespace KoperasiTentera.Application.Common.Requests
{
    public class QueryRequest : PagedRequest
    {
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string SortDirection { get; set; } = "asc";
    }
}
