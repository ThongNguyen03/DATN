using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("HangDoiHoanTienVNPay")]
    public class HangDoiHoanTienVNPay
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaHangDoi { get; set; }

        [Required]
        [ForeignKey("DonHang")]
        public int MaDonHang { get; set; }

        [Required]
        [StringLength(50)]
        public string MaGiaoDichVNPay { get; set; }

        [Required]
       // [Column(TypeName = "decimal(10,2)")]
        public decimal SoTienHoan { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayXuLy { get; set; }

        public string KetQuaXuLy { get; set; }

        [StringLength(255)]
        public string GhiChu { get; set; }

        public int SoLanThuXuLy { get; set; } = 0;

        // Navigation property
        public virtual DonHang DonHang { get; set; }
    }
}