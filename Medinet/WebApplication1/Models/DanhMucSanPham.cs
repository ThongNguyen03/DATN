using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("DanhMucSanPham")]
    public partial class DanhMucSanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDanhMuc { get; set; }
        public string TenDanhMuc { get; set; }
        public int? MaDanhMucCha { get; set; }
        public string AnhDanhMucSanPham { get; set; }
        public DateTime NgayTao { get; set; }

        public virtual DanhMucSanPham DanhMucCha { get; set; }
        public virtual ICollection<SanPham> SanPhams { get; set; }
    }

}