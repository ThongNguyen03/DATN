using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("ChiTietDonHang")]
    public partial class ChiTietDonHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaChiTietDonHang { get; set; }
        public int MaDonHang { get; set; }
        public int MaSanPham { get; set; }
        public int SoLuong { get; set; }
        public decimal Gia { get; set; }

        // Quan hệ với Đơn Hàng và Sản Phẩm
        public virtual DonHang DonHang { get; set; }
        public virtual SanPham SanPham { get; set; }
    }

}