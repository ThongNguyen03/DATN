using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    // ViewModel cho thống kê doanh thu theo ngày
    public class ThongKeNgayViewModel
    {
        public DateTime Ngay { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal PhiNenTang { get; set; }
        public decimal TienChuyenChoNguoiBan { get; set; }
    }


}