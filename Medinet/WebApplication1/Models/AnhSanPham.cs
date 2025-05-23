﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    [Table("AnhSanPham")]
    public class AnhSanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaAnh { get; set; }

        [ForeignKey("SanPham")]
        public int MaSanPham { get; set; }

        [StringLength(500)]
        public string DuongDanAnh { get; set; }

        // Navigation property
        public virtual SanPham SanPham { get; set; }
    }


}