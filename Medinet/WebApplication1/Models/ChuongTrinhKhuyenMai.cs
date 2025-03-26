using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("ChuongTrinhKhuyenMai")]
    public class ChuongTrinhKhuyenMai
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaKM { get; set; }

        [Required]
        [StringLength(255)]
        public string TenKhuyenMai { get; set; }

        [Required]
        [StringLength(100)]
        public string LoaiKhuyenMai { get; set; }

        [Required]
       // [Column(TypeName = "decimal(10,2)")]
        public decimal GiaTriGiam { get; set; }

        [Required]
        public DateTime NgayBatDau { get; set; }

        [Required]
        public DateTime NgayKetThuc { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; }

        [Required]
        [ForeignKey("NguoiBan")]
        public int MaNguoiBan { get; set; }

        // Navigation properties
        public virtual NguoiBan NguoiBan { get; set; }
        public virtual ICollection<KhuyenMaiSanPham> KhuyenMaiSanPhams { get; set; }
        public virtual ICollection<KhuyenMaiDonHang> KhuyenMaiDonHangs { get; set; }
    }
}