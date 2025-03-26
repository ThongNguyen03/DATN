using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("KhuyenMaiDonHang")]
    public class KhuyenMaiDonHang
    {
        [Key]
        [Column(Order = 0)]
        [ForeignKey("ChuongTrinhKhuyenMai")]
        public int MaKM { get; set; }

        [Key]
        [Column(Order = 1)]
        [ForeignKey("DonHang")]
        public int MaDonHang { get; set; }

        // Navigation properties
        public virtual ChuongTrinhKhuyenMai ChuongTrinhKhuyenMai { get; set; }
        public virtual DonHang DonHang { get; set; }
    }
}