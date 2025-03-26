using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("ChiTietDonHang")]
    public class ChiTietDonHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaChiTietDonHang { get; set; }

        [Required]
        [ForeignKey("DonHang")]
        public int MaDonHang { get; set; }

        [Required]
        [ForeignKey("SanPham")]
        public int MaSanPham { get; set; }

        [Required]
        public int SoLuong { get; set; }

        [Required]
      //  [Column(TypeName = "decimal(10,2)")]
        public decimal Gia { get; set; }

        // Navigation properties
        public virtual DonHang DonHang { get; set; }
        public virtual SanPham SanPham { get; set; }
    }


}