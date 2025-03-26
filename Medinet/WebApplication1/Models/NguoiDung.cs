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
    public class NguoiDung
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNguoiDung { get; set; }

        [Required]
        [StringLength(255)]
        public string TenNguoiDung { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string VaiTro { get; set; } = "Buyer";

        [Required]
        [StringLength(255)]
        public string MatKhauMaHoa { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        public DateTime? NgayThangNamSinh { get; set; }

        public string AnhDaiDien { get; set; }

        [StringLength(50)]
        public string GioiTinh { get; set; }

        [StringLength(50)]
        public string SoDienThoai { get; set; }

        public DateTime? DangNhapCuoiCung { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = "Active";

        [Required]
        public bool XetDuyetThanhNguoiBan { get; set; } = false;

        public bool BiTuChoiNangCap { get; set; } = false;

        public DateTime? NgayTuChoiNangCap { get; set; }

        [StringLength(10)]
        public string MaXacNhan { get; set; }

        public DateTime? ThoiGianHetHan { get; set; }

        [StringLength(255)]
        public string DiaChi { get; set; }

        // Navigation properties
        //public virtual NguoiBan NguoiBan { get; set; }
        public virtual ICollection<GioHang> GioHangs { get; set; }
        public virtual ICollection<DonHang> DonHangs { get; set; }
        public virtual ICollection<DanhGiaSanPham> DanhGiaSanPhams { get; set; }
        public virtual ICollection<ThongBao> ThongBaos { get; set; }
        public virtual ICollection<ThongTinHoanTien> ThongTinHoanTiens { get; set; }
    }

}