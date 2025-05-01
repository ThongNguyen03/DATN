using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("BaoCaoDonHang")]
    public class BaoCaoDonHang
    {
        [Key]
        public int MaBaoCao { get; set; }

        public int MaDonHang { get; set; }
        public int MaNguoiDung { get; set; }
        public int MaNguoiBan { get; set; }

        [Required]
        [StringLength(50)]
        public string LoaiBaoCao { get; set; }

        [Required]
        [StringLength(500)]
        public string LyDoBaoCao { get; set; }

        [StringLength(1000)]
        public string ChiTietBaoCao { get; set; }

        [StringLength(20)]
        public string SoDienThoaiLienHe { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; }

        public DateTime NgayTao { get; set; }
        public DateTime? NgayXuLy { get; set; }

        [StringLength(500)]
        public string KetQuaXuLy { get; set; }

        // Quan hệ
        // Navigation properties
        [ForeignKey("MaDonHang")]
        public virtual DonHang DonHang { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung NguoiDung { get; set; }

        [ForeignKey("MaNguoiBan")]
        public virtual NguoiBan NguoiBan { get; set; }
    }
}