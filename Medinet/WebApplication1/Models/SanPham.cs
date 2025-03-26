using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("SanPham")]
    public class SanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSanPham { get; set; }

        [Required]
        [ForeignKey("NguoiBan")]
        public int MaNguoiBan { get; set; }

        [Required]
        [ForeignKey("DanhMucSanPham")]
        public int MaDanhMuc { get; set; }

        [Required]
        [StringLength(255)]
        public string TenSanPham { get; set; }

        public string MoTaSanPham { get; set; }

        [Required]
       // [Column(TypeName = "decimal(10,2)")]
        public decimal GiaSanPham { get; set; }

        [Required]
        public int SoLuongTonKho { get; set; } = 0;

        [StringLength(20)]
        public string TrangThai { get; set; } = "Đang chờ xử lý";

        [Required]
        public int SoLuotMua { get; set; } = 0;

        [Required]
        [StringLength(255)]
        public string ThuongHieu { get; set; } = "Khác";

        [Required]
        public int SoLuongMoiHop { get; set; } = 0;

        [Required]
        public string ThanhPhan { get; set; }

        [Required]
        public string DoiTuongSuDung { get; set; }

        [Required]
        public string HuongDanSuDung { get; set; }

        [Required]
        public int KhoiLuong { get; set; }

        [Required]
        public string BaoQuan { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual NguoiBan NguoiBan { get; set; }
        public virtual DanhMucSanPham DanhMucSanPham { get; set; }
        public virtual ICollection<AnhSanPham> AnhSanPhams { get; set; }
        public virtual ICollection<GioHang> GioHangs { get; set; }
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public virtual ICollection<DanhGiaSanPham> DanhGiaSanPhams { get; set; }
        public virtual ICollection<KhuyenMaiSanPham> KhuyenMaiSanPhams { get; set; }
    }

}