using WebApplication1.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    [Table("GioHang")]
    public class GioHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaGioHang { get; set; }

        [Required]
        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        [Required]
        [ForeignKey("SanPham")]
        public int MaSanPham { get; set; }

        [Required]
        public int SoLuong { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual NguoiDung NguoiDung { get; set; }
        public virtual SanPham SanPham { get; set; }
    }


}