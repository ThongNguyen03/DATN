using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("DonHang")]
    public partial class DonHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDonHang { get; set; }
        public int MaNguoiDung { get; set; }
        public int MaNguoiBan { get; set; }
        public decimal TongSoTien { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public string TrangThaiDonHang { get; set; }
        public string GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayGiaoHang { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public DateTime NgayHuy { get; set; }
        public string LyDoHuy { get; set; }

        // Thêm trường để theo dõi việc buyer đã xác nhận nhận hàng hay chưa
        public bool DaXacNhanNhanHang { get; set; }

        // Thêm trường để theo dõi việc đã giải ngân cho seller hay chưa
        public bool DaGiaiNganChoSeller { get; set; }


        public virtual NguoiDung NguoiDung { get; set; }
        public virtual NguoiBan NguoiBan { get; set; }
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public virtual ICollection<GiaoDich> GiaoDiches { get; set; }
    }

}