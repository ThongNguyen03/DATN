using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    // ViewModel cho thống kê theo người bán
    public class ThongKeNguoiBanViewModel
    {
        public int MaNguoiBan { get; set; }
        public string TenCuaHang { get; set; }
        public int SoDonHang { get; set; }
        public decimal TongDoanhThu { get; set; }
        public decimal TongPhiNenTang { get; set; }
        public decimal TongTienNhan { get; set; }
    }
}