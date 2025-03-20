using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public class MedinetDATN : DbContext
    {
        public MedinetDATN() : base("MedinetDATN")
        { 
            
        }
        // Định nghĩa các DbSet tương ứng với các Model
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<DonHang> DonHangs { get; set; }
        public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public DbSet<DanhGiaSanPham> DanhGiaSanPhams { get; set; }
        public DbSet<GiaoDich> GiaoDichs { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<DanhMucSanPham> DanhMucSanPhams { get; set; }
        public DbSet<NguoiBan> NguoiBans { get; set; }
        public DbSet<AnhSanPham> AnhSanPhams { get; set; }
        public DbSet<AnhChungChi> AnhChungChis { get; set; }
        public DbSet<GioHang> GioHangs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Thiết lập mối quan hệ giữa SanPham và DanhMucSanPham
            modelBuilder.Entity<SanPham>()
                .HasRequired(s => s.DanhMucSanPham)
                .WithMany(d => d.SanPhams)
                .HasForeignKey(s => s.MaDanhMuc);

            // Thiết lập mối quan hệ giữa SanPham và NguoiBan
            modelBuilder.Entity<SanPham>()
                .HasRequired(s => s.NguoiBan)
                .WithMany()
                .HasForeignKey(s => s.MaNguoiBan);

            // Thiết lập mối quan hệ tự tham chiếu trong DanhMucSanPham
            modelBuilder.Entity<DanhMucSanPham>()
                .HasOptional(d => d.DanhMucCha)
                .WithMany()
                .HasForeignKey(d => d.MaDanhMucCha);

            base.OnModelCreating(modelBuilder);
        }
    }
}