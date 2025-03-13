using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using WebApplication1.Models;

namespace WebApplication1.Models
{
    [Table("NguoiDung")]
    public partial class NguoiDung
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNguoiDung { get; set; }
        public string TenNguoiDung { get; set; }
        public string Email { get; set; }
        public string VaiTro { get; set; } // Admin, Seller, Buyer
        public string MatKhauMaHoa { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public DateTime? NgayThangNamSinh { get; set; }
        public string AnhDaiDien { get; set; }
        public string GioiTinh { get; set; }
        public string SoDienThoai { get; set; }
        public DateTime? DangNhapCuoiCung { get; set; }
        public string TrangThai { get; set; }
        public bool XetDuyetThanhNguoiBan { get; set; }
        public bool BiTuChoiNangCap { get; set; }
        public DateTime? NgayTuChoiNangCap { get; set; }
        public string MaXacNhan { get; set; }
        public DateTime? ThoiGianHetHan { get; set; }
        public virtual ICollection<GioHang> GioHangs { get; set; }
        public virtual ICollection<DonHang> DonHangs { get; set; }
        public virtual ICollection<DanhGiaSanPham> DanhGiaSanPhams { get; set; }
        public virtual ICollection<ThongBao> ThongBaos { get; set; }
    }

}