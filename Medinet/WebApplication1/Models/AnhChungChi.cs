using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("AnhChungChi")]
    public partial class AnhChungChi
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaAnhChungChi { get; set; }
        public int MaNguoiBan { get; set; }
        [Required]
        [StringLength(2000)]
        public string TenChungChi { get; set; }
        public string TrangThai { get; set; }
        [Required]
        public string DuongDanAnh { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public DateTime? NgayPheDuyet { get; set; }

        public virtual NguoiBan NguoiBan { get; set; }
    }

}