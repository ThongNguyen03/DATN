using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("ThongTinHoanTien")]
    public class ThongTinHoanTien
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaHoanTien { get; set; }

        [Required]
        [ForeignKey("DonHang")]
        public int MaDonHang { get; set; }

        [Required]
        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        [Required]
      //  [Column(TypeName = "decimal(10,2)")]
        public decimal SoTienHoan { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; }

        [Required]
        [StringLength(20)]
        public string PhuongThucHoanTien { get; set; }

        [StringLength(255)]
        public string LyDoHoanTien { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayYeuCau { get; set; } = DateTime.Now;

        public DateTime? NgayHoanTien { get; set; }

        // Navigation properties
        public virtual DonHang DonHang { get; set; }
        public virtual NguoiDung NguoiDung { get; set; }
    }


}