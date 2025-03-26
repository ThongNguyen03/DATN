using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("AnhChungChi")]
    public class AnhChungChi
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaAnhChungChi { get; set; }

        [ForeignKey("NguoiBan")]
        public int MaNguoiBan { get; set; }

        [Required]
        [StringLength(500)]
        public string TenChungChi { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = "Đang chờ xử lý";

        [StringLength(500)]
        public string DuongDanAnh { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        public DateTime? NgayPheDuyet { get; set; }

        // Navigation property
        public virtual NguoiBan NguoiBan { get; set; }
    }

}