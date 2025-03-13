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
    public partial class GioHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaGioHang { get; set; }
        public int MaNguoiDung { get; set; }
        public int MaSanPham { get; set; }
        public int SoLuong { get; set; }
        public DateTime NgayTao { get; set; }

        public virtual NguoiDung NguoiDung { get; set; }
        public virtual SanPham SanPham { get; set; }
    }

}