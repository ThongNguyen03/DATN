using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("ThongBao")]
    public partial class ThongBao
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaThongBao { get; set; }
        public int MaNguoiDung { get; set; }
        public string TinNhan { get; set; }
        public string TrangThai { get; set; } // "Chưa đọc", "Đã đọc"
        public DateTime NgayTao { get; set; }

        // Quan hệ với Người Dùng
        public virtual NguoiDung NguoiDung { get; set; }
    }

}