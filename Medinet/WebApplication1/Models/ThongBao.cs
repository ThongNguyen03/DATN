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

        // Navigation property
        public virtual NguoiDung NguoiDung { get; set; }
    }


}