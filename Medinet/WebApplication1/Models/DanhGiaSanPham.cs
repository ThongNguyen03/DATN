using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("DanhGiaSanPham")]
    public class DanhGiaSanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDanhGiaSanPham { get; set; }

        [Required]
        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        [Required]
        [ForeignKey("SanPham")]
        public int MaSanPham { get; set; }

        [ForeignKey("NguoiBan")]
        public int? MaNguoiBan { get; set; }

        [ForeignKey("DonHang")]
        public int? MaDonHang { get; set; }

        public int? DanhGia { get; set; }

        public string BinhLuan { get; set; }

        public bool DaDanhGia { get; set; } = false;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual NguoiDung NguoiDung { get; set; }
        public virtual SanPham SanPham { get; set; }
        public virtual NguoiBan NguoiBan { get; set; }
        public virtual DonHang DonHang { get; set; }
    }

}