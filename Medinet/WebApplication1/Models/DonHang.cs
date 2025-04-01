using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("DonHang")]
    public class DonHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDonHang { get; set; }

        [Required]
        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        [Required]
        [ForeignKey("NguoiBan")]
        public int MaNguoiBan { get; set; }

        [Required]
       // [Column(TypeName = "decimal(10,2)")]
        public decimal TongSoTien { get; set; }

        [Required]
        [StringLength(10)]
        public string PhuongThucThanhToan { get; set; }

        [StringLength(50)]
        public string TrangThaiDonHang { get; set; } = "Đang chờ xử lý";

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayGiaoHang { get; set; }

        public DateTime? NgayCapNhat { get; set; }

        public DateTime? NgayHuy { get; set; }

        public string LyDoHuy { get; set; }

        public string GhiChu { get; set; }

        [Required]
        public bool DaXacNhanNhanHang { get; set; } = false;

        [Required]
        public bool DaGiaiNganChoSeller { get; set; } = false;

        public bool DaDanhGia { get; set; } = false;


        //30/3/2025
        public string AnhXacNhanGiaoHang { get; set; }
        public DateTime? ThoiGianTuDongXacNhan { get; set; }

        //30/3/2025

        // Navigation properties
        public virtual NguoiDung NguoiDung { get; set; }
        public virtual NguoiBan NguoiBan { get; set; }
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public virtual ICollection<GiaoDich> GiaoDichs { get; set; }
        public virtual ICollection<KhuyenMaiDonHang> KhuyenMaiDonHangs { get; set; }
        public virtual ICollection<DanhGiaSanPham> DanhGiaSanPhams { get; set; }
        public virtual ICollection<Escrow> Escrows { get; set; }
        public virtual ICollection<LichSuGiaoDichVi> LichSuGiaoDichVis { get; set; }
        public virtual ICollection<ThongTinHoanTien> ThongTinHoanTiens { get; set; }
        public virtual ICollection<HangDoiHoanTienVNPay> HangDoiHoanTienVNPays { get; set; }
    }

}
