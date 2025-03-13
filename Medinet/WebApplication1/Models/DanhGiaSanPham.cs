using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("DanhGiaSanPham")]
    public partial class DanhGiaSanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDanhGiaSanPham { get; set; }
        public int MaNguoiDung { get; set; }
        public int MaSanPham { get; set; }
        public int DanhGia { get; set; } // 1 - 5 sao
        public string BinhLuan { get; set; }
        public DateTime NgayTao { get; set; }

        // Quan hệ với Người Dùng và Sản Phẩm
        public virtual NguoiDung NguoiDung { get; set; }
        public virtual SanPham SanPham { get; set; }
    }

}