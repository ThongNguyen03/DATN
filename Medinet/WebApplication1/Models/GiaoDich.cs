using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("GiaoDich")]
    public class GiaoDich
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaGiaoDich { get; set; }

        [Required]
        [ForeignKey("DonHang")]
        public int MaDonHang { get; set; }

        [StringLength(20)]
        public string TrangThaiGiaoDich { get; set; } = "Đang chờ xử lý";

        [Required]
        [StringLength(10)]
        public string PhuongThucThanhToan { get; set; }

        [Required]
       // [Column(TypeName = "decimal(10,2)")]
        public decimal TongTien { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayGiaoDich { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string MaGiaoDichVNPay { get; set; }

        [StringLength(500)]
        public string ThongTinGiaoDich { get; set; }

        // Navigation property
        public virtual DonHang DonHang { get; set; }
    }

}