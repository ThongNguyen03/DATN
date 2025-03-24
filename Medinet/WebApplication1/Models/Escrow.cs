using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    // Thêm mô hình Escrow (ký quỹ) vào Models

    public class Escrow
    {
        [Key]
        public int MaKyQuy { get; set; }

        [Required]
        public int MaDonHang { get; set; }

        [Required]
        public int MaNguoiBan { get; set; }

        [Required]
        public decimal TongTien { get; set; }

        [Required]
        public decimal PhiNenTang { get; set; } // Phí nền tảng (10%)

        [Required]
        public decimal TienChuyenChoNguoiBan { get; set; } // Số tiền sau khi trừ phí

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } // "Đang giữ", "Đã giải ngân", "Đã hoàn tiền"

        public DateTime NgayTao { get; set; }

        public DateTime? NgayGiaiNgan { get; set; }

        // Khóa ngoại
        [ForeignKey("MaDonHang")]
        public virtual DonHang DonHang { get; set; }

        [ForeignKey("MaNguoiBan")]
        public virtual NguoiBan NguoiBan { get; set; }
    }
}