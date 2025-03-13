using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("SanPham")]
    public partial class SanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSanPham { get; set; }
        public int MaNguoiBan { get; set; }
        public int MaDanhMuc { get; set; }
        public string TenSanPham { get; set; }
        public string MoTaSanPham { get; set; }
        public decimal GiaSanPham { get; set; }
        public int SoLuongTonKho { get; set; }
        public string TrangThai { get; set; }
        public int SoLuotMua { get; set; }
        public string ThuongHieu { get; set; }
        public int SoLuongMoiHop { get; set; }
        public string ThanhPhan { get; set; }
        public string DoiTuongSuDung { get; set; }
        public string HuongDanSuDung { get; set; }
        public int KhoiLuong { get; set; }
        public string BaoQuan { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }

        public virtual NguoiBan NguoiBan { get; set; }
        public virtual DanhMucSanPham DanhMucSanPham { get; set; }
        public virtual ICollection<AnhSanPham> DanhSachAnhSanPham { get; set; }
        public SanPham()
        {
            DanhSachAnhSanPham = new HashSet<AnhSanPham>();
        }
    }

}