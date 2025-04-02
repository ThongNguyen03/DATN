using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("PhanHoiDanhGia")]
    public class PhanHoiDanhGia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaPhanHoi { get; set; }

        [Required]
        [ForeignKey("DanhGiaSanPham")]
        public int MaDanhGiaSanPham { get; set; }

        [Required]
        [ForeignKey("NguoiBan")]
        public int MaNguoiBan { get; set; }

        [Required]
        public string NoiDungPhanHoi { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual DanhGiaSanPham DanhGiaSanPham { get; set; }
        public virtual NguoiBan NguoiBan { get; set; }
    }
}