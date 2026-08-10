using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;
namespace KoperasiTentera.Application.Common.Requests
{
public class PagedRequest
    {
        private int _page = 1;
        private int _pageSize = 10;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
        }
    } 
}
