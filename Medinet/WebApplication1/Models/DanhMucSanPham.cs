using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("DanhMucSanPham")]
    public class DanhMucSanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDanhMuc { get; set; }

        [Required]
        [StringLength(255)]
        public string TenDanhMuc { get; set; }

        [ForeignKey("DanhMucCha")]
        public int? MaDanhMucCha { get; set; }

        public string AnhDanhMucSanPham { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual DanhMucSanPham DanhMucCha { get; set; }
        public virtual ICollection<DanhMucSanPham> DanhMucCons { get; set; }
        public virtual ICollection<SanPham> SanPhams { get; set; }
    }

}