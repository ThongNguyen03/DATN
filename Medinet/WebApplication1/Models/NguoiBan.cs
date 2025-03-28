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
    public class NguoiBan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNguoiBan { get; set; }

        [Required]
        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        [Required]
        [StringLength(255)]
        public string TenCuaHang { get; set; }

        public string MoTaCuaHang { get; set; }

        public string DiaChiCuaHang { get; set; }

        [StringLength(20)]
        public string SoDienThoaiCuaHang { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public decimal SoDuVi { get; set; } = 0;

        // Navigation properties
        public virtual NguoiDung NguoiDung { get; set; }
        public virtual ICollection<AnhChungChi> AnhChungChis { get; set; }
        public virtual ICollection<SanPham> SanPhams { get; set; }
        public virtual ICollection<DonHang> DonHangs { get; set; }
        public virtual ICollection<ChuongTrinhKhuyenMai> ChuongTrinhKhuyenMais { get; set; }
        public virtual ICollection<Escrow> Escrows { get; set; }
        public virtual ICollection<LichSuGiaoDichVi> LichSuGiaoDichVis { get; set; }

        //update ngày 27/3/2025
        public virtual ICollection<GhiChepVi> GhiChepVis { get; set; }
    }

}