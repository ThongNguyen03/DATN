using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("Escrow")]
    public class Escrow
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaKyQuy { get; set; }

        [Required]
        [ForeignKey("DonHang")]
        public int MaDonHang { get; set; }

        [Required]
        [ForeignKey("NguoiBan")]
        public int MaNguoiBan { get; set; }

        [Required]
        //[Column(TypeName = "decimal(10,2)")]
        public decimal TongTien { get; set; }

        [Required]
        //[Column(TypeName = "decimal(10,2)")]
        public decimal PhiNenTang { get; set; }

        [Required]
        //[Column(TypeName = "decimal(10,2)")]
        public decimal TienChuyenChoNguoiBan { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayGiaiNgan { get; set; }

        // Navigation properties
        public virtual DonHang DonHang { get; set; }
        public virtual NguoiBan NguoiBan { get; set; }
    }


}