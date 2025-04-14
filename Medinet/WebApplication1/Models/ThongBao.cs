using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("ThongBao")]
    public class ThongBao
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaThongBao { get; set; }

        [Required]
        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        [Required]
        public string TinNhan { get; set; }

        [StringLength(10)]
        public string TrangThai { get; set; } = "Chưa đọc";

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;
        //11/4/2025
        [Required]
        [StringLength(100)]
        public string LoaiThongBao { get; set; } = "Chung";

        [Required]
        public string TieuDe { get; set; } = "Thông báo";

        public string DuongDanChiTiet { get; set; }

        public int MucDoQuanTrong { get; set; } = 0;

        public DateTime? NgayDoc { get; set; }
        //11/4/2025

        // Navigation property
        public virtual NguoiDung NguoiDung { get; set; }
    }


}