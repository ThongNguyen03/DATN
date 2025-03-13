using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("AnhSanPham")]
    public partial class AnhSanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaAnh { get; set; }
        public int MaSanPham { get; set; }
        public string DuongDanAnh { get; set; }

        // Quan hệ với sản phẩm
        public virtual SanPham SanPham { get; set; }
    }

}