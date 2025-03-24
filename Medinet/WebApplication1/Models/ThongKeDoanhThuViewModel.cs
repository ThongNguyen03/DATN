using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    // ViewModel chính cho trang thống kê doanh thu
    public class ThongKeDoanhThuViewModel
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }

        public decimal TongDoanhThu { get; set; }
        public decimal TongPhiNenTang { get; set; }
        public decimal TongTienChuyenChoNguoiBan { get; set; }

        public List<ThongKeNgayViewModel> ThongKeTheoNgay { get; set; }
    }
}