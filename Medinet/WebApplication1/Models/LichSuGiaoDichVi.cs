using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("LichSuGiaoDichVi")]
    public class LichSuGiaoDichVi
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaGiaoDichVi { get; set; }

        [Required]
        [ForeignKey("NguoiBan")]
        public int MaNguoiBan { get; set; }

        [ForeignKey("DonHang")]
        public int? MaDonHang { get; set; }

        [Required]
      //  [Column(TypeName = "decimal(10,2)")]
        public decimal SoTien { get; set; }

        [Required]
        [StringLength(50)]
        public string LoaiGiaoDich { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; }

        [StringLength(255)]
        public string NoiDung { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayGiaoDich { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual NguoiBan NguoiBan { get; set; }
        public virtual DonHang DonHang { get; set; }
    }

}