using WebApplication1.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    [Table("NguoiBan")]
    public partial class NguoiBan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNguoiBan { get; set; }
        public int MaNguoiDung { get; set; }
        public string TenCuaHang { get; set; }
        public string MoTaCuaHang { get; set; }
        public string DiaChiCuaHang { get; set; }
        public string SoDienThoaiCuaHang { get; set; }
        public DateTime NgayTao { get; set; }

        public virtual NguoiDung NguoiDung { get; set; }

        public virtual ICollection<AnhChungChi> DanhSachChungChi { get; set; }

        public NguoiBan()
        {
            DanhSachChungChi = new HashSet<AnhChungChi>();
        }
    }

}