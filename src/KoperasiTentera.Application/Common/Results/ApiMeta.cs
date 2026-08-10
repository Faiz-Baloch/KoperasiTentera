using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

namespace KoperasiTentera.Application.Common.Results
{
    public class ApiMeta
    {
        public string? TraceId { get; set; }
        public string? CorrelationId { get; set; }
        //public int? Page { get; set; }
        //public int? PageSize { get; set; }
        //public long? TotalCount { get; set; }
        //public int? TotalPages { get; set; }
    }
}
