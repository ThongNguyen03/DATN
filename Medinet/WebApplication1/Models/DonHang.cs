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
        public DateTime NgayTao { get; set; }

        public virtual NguoiDung NguoiDung { get; set; }
        public virtual NguoiBan NguoiBan { get; set; }
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public virtual ICollection<GiaoDich> GiaoDiches { get; set; }
    }

}