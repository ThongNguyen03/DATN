using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("GiaoDich")]
    public partial class GiaoDich
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaGiaoDich { get; set; }
        public int MaDonHang { get; set; }
        public string MaGiaoDichVNPay { get; set; }
        public string ThongTinGiaoDich { get; set; }
        public string TrangThaiGiaoDich { get; set; } // "Đang chờ xử lý", "Đã hoàn thành", "Không thành công"
        public string PhuongThucThanhToan { get; set; } // "COD", "VNPAY"
        public decimal TongTien { get; set; }
        public DateTime NgayGiaoDich { get; set; }

        // Quan hệ với Đơn Hàng
        public virtual DonHang DonHang { get; set; }
    }

}