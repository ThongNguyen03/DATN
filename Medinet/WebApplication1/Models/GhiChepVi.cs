using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public class GhiChepVi
    {
        [Key]
        public int MaGhiChep { get; set; }

        [Required(ErrorMessage = "Mã người bán là bắt buộc")]
        public int MaNguoiBan { get; set; }

        [Required(ErrorMessage = "Số tiền là bắt buộc")]

        [Display(Name = "Số tiền")]
        public decimal SoTien { get; set; }

        [Required(ErrorMessage = "Loại giao dịch là bắt buộc")]
        [StringLength(50, ErrorMessage = "Loại giao dịch không được vượt quá 50 ký tự")]
        [Display(Name = "Loại giao dịch")]
        public string LoaiGiaoDich { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        [Display(Name = "Mô tả")]
        public string MoTa { get; set; }

        [Required(ErrorMessage = "Ngày giao dịch là bắt buộc")]
        [Display(Name = "Ngày giao dịch")]
        public DateTime NgayGiaoDich { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [StringLength(20, ErrorMessage = "Trạng thái không được vượt quá 20 ký tự")]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "Thành công";

        // Liên kết với người bán
        [ForeignKey("MaNguoiBan")]
        public virtual NguoiBan NguoiBan { get; set; }

        // Thuộc tính bổ sung cho tracking
        [StringLength(50)]
        public string MaGiaoDichNgoai { get; set; }

        [StringLength(100)]
        public string IP { get; set; }

        // Liên kết tùy chọn với đơn hàng (nếu giao dịch liên quan đến đơn hàng)
        public int? MaDonHang { get; set; }

        [ForeignKey("MaDonHang")]
        public virtual DonHang DonHang { get; set; }

        // Phương thức tiện ích để tạo ghi chép
        public static GhiChepVi TaoGhiChepNap(int maNguoiBan, decimal soTien, string phuongThuc, string moTa = null)
        {
            return new GhiChepVi
            {
                MaNguoiBan = maNguoiBan,
                SoTien = soTien,
                LoaiGiaoDich = "Nạp tiền",
                MoTa = string.IsNullOrEmpty(moTa) ? $"Nạp tiền vào ví ({phuongThuc})" : moTa,
                NgayGiaoDich = DateTime.Now,
                TrangThai = "Thành công"
            };
        }

        public static GhiChepVi TaoGhiChepDatCoc(int maNguoiBan, int maDonHang, decimal soTien)
        {
            return new GhiChepVi
            {
                MaNguoiBan = maNguoiBan,
                MaDonHang = maDonHang,
                SoTien = -soTien,
                LoaiGiaoDich = "Đặt cọc",
                MoTa = $"Đặt cọc cho đơn hàng #{maDonHang}",
                NgayGiaoDich = DateTime.Now,
                TrangThai = "Thành công"
            };
        }

        public static GhiChepVi TaoGhiChepPhiNenTang(int maNguoiBan, int maDonHang, decimal soTien)
        {
            return new GhiChepVi
            {
                MaNguoiBan = maNguoiBan,
                MaDonHang = maDonHang,
                SoTien = -soTien,
                LoaiGiaoDich = "Phí nền tảng",
                MoTa = $"Phí nền tảng cho đơn hàng #{maDonHang} (10%)",
                NgayGiaoDich = DateTime.Now,
                TrangThai = "Thành công"
            };
        }

        public static GhiChepVi TaoGhiChepHoanTraCoc(int maNguoiBan, int maDonHang, decimal soTien)
        {
            return new GhiChepVi
            {
                MaNguoiBan = maNguoiBan,
                MaDonHang = maDonHang,
                SoTien = soTien,
                LoaiGiaoDich = "Hoàn trả đặt cọc",
                MoTa = $"Hoàn trả tiền đặt cọc cho đơn hàng #{maDonHang}",
                NgayGiaoDich = DateTime.Now,
                TrangThai = "Thành công"
            };
        }

        public static GhiChepVi TaoGhiChepThanhToanDonHang(int maNguoiBan, int maDonHang, decimal soTien)
        {
            return new GhiChepVi
            {
                MaNguoiBan = maNguoiBan,
                MaDonHang = maDonHang,
                SoTien = soTien,
                LoaiGiaoDich = "Thanh toán đơn hàng",
                MoTa = $"Thanh toán tiền đơn hàng #{maDonHang}",
                NgayGiaoDich = DateTime.Now,
                TrangThai = "Thành công"
            };
        }
    }
}