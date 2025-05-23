﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;


namespace WebApplication1.Models
{
    public class MedinetDATN : DbContext
    {
        public MedinetDATN() : base("MedinetDATN")
        {
        }

        public virtual DbSet<NguoiDung> NguoiDungs { get; set; }
        public virtual DbSet<NguoiBan> NguoiBans { get; set; }
        public virtual DbSet<AnhChungChi> AnhChungChis { get; set; }
        public virtual DbSet<DanhMucSanPham> DanhMucSanPhams { get; set; }
        public virtual DbSet<SanPham> SanPhams { get; set; }
        public virtual DbSet<AnhSanPham> AnhSanPhams { get; set; }
        public virtual DbSet<GioHang> GioHangs { get; set; }
        public virtual DbSet<DonHang> DonHangs { get; set; }
        public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public virtual DbSet<GiaoDich> GiaoDichs { get; set; }
        public virtual DbSet<DanhGiaSanPham> DanhGiaSanPhams { get; set; }
        public virtual DbSet<ThongBao> ThongBaos { get; set; }
        public virtual DbSet<ChuongTrinhKhuyenMai> ChuongTrinhKhuyenMais { get; set; }
        public virtual DbSet<KhuyenMaiSanPham> KhuyenMaiSanPhams { get; set; }
        public virtual DbSet<KhuyenMaiDonHang> KhuyenMaiDonHangs { get; set; }
        public virtual DbSet<Escrow> Escrows { get; set; }
        public virtual DbSet<LichSuGiaoDichVi> LichSuGiaoDichVis { get; set; }
        public virtual DbSet<ThongTinHoanTien> ThongTinHoanTiens { get; set; }
        public virtual DbSet<HangDoiHoanTienVNPay> HangDoiHoanTienVNPays { get; set; }
        //27/3/2025
        //2/4/2025
        public DbSet<PhanHoiDanhGia> PhanHoiDanhGias { get; set; }
        //2/4/2025

        //27/4/2025
        public DbSet<BaoCaoDonHang> BaoCaoDonHangs { get; set; }
        //27/4/2025
        public DbSet<GhiChepVi> GhiChepVis { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // 1. KHÓA CHÍNH COMPOSITE
            // Cấu hình khóa chính cho bảng KhuyenMaiSanPham
            modelBuilder.Entity<KhuyenMaiSanPham>()
                .HasKey(k => new { k.MaKM, k.MaSanPham });

            // Cấu hình khóa chính cho bảng KhuyenMaiDonHang
            modelBuilder.Entity<KhuyenMaiDonHang>()
                .HasKey(k => new { k.MaKM, k.MaDonHang });

            // 2. MỐI QUAN HỆ NguoiDung - NguoiBan (1-1)
            // Xác định khóa chính
            modelBuilder.Entity<NguoiBan>()
                .HasKey(nb => nb.MaNguoiBan);

            // Đảm bảo MaNguoiDung là bắt buộc
            modelBuilder.Entity<NguoiBan>()
                .Property(nb => nb.MaNguoiDung)
                .IsRequired();

            // 3. MỐI QUAN HỆ TỰ THAM CHIẾU DanhMucSanPham (cha-con)
            modelBuilder.Entity<DanhMucSanPham>()
                .HasOptional(d => d.DanhMucCha)
                .WithMany(d => d.DanhMucCons)
                .HasForeignKey(d => d.MaDanhMucCha)
                .WillCascadeOnDelete(false);

            // 4. MỐI QUAN HỆ NguoiDung - GioHang (1-n)
            modelBuilder.Entity<GioHang>()
                .HasRequired(g => g.NguoiDung)
                .WithMany(nd => nd.GioHangs)
                .HasForeignKey(g => g.MaNguoiDung)
                .WillCascadeOnDelete(true);

            // 5. MỐI QUAN HỆ SanPham - GioHang (1-n)
            modelBuilder.Entity<GioHang>()
                .HasRequired(g => g.SanPham)
                .WithMany(sp => sp.GioHangs)
                .HasForeignKey(g => g.MaSanPham)
                .WillCascadeOnDelete(false);

            // 6. MỐI QUAN HỆ NguoiDung - DonHang (1-n)
            modelBuilder.Entity<DonHang>()
                .HasRequired(dh => dh.NguoiDung)
                .WithMany(nd => nd.DonHangs)
                .HasForeignKey(dh => dh.MaNguoiDung)
                .WillCascadeOnDelete(true);

            // 7. MỐI QUAN HỆ NguoiBan - DonHang (1-n)
            modelBuilder.Entity<DonHang>()
                .HasRequired(dh => dh.NguoiBan)
                .WithMany(nb => nb.DonHangs)
                .HasForeignKey(dh => dh.MaNguoiBan)
                .WillCascadeOnDelete(false);

            // 8. MỐI QUAN HỆ DonHang - ChiTietDonHang (1-n)
            modelBuilder.Entity<ChiTietDonHang>()
                .HasRequired(ct => ct.DonHang)
                .WithMany(dh => dh.ChiTietDonHangs)
                .HasForeignKey(ct => ct.MaDonHang)
                .WillCascadeOnDelete(true);

            // 9. MỐI QUAN HỆ SanPham - ChiTietDonHang (1-n)
            modelBuilder.Entity<ChiTietDonHang>()
                .HasRequired(ct => ct.SanPham)
                .WithMany(sp => sp.ChiTietDonHangs)
                .HasForeignKey(ct => ct.MaSanPham)
                .WillCascadeOnDelete(false);

            // 10. MỐI QUAN HỆ DonHang - GiaoDich (1-n)
            modelBuilder.Entity<GiaoDich>()
                .HasRequired(gd => gd.DonHang)
                .WithMany(dh => dh.GiaoDichs)
                .HasForeignKey(gd => gd.MaDonHang)
                .WillCascadeOnDelete(true);

            // 11. MỐI QUAN HỆ NguoiBan - SanPham (1-n)
            modelBuilder.Entity<SanPham>()
                .HasRequired(sp => sp.NguoiBan)
                .WithMany(nb => nb.SanPhams)
                .HasForeignKey(sp => sp.MaNguoiBan)
                .WillCascadeOnDelete(true);

            // 12. MỐI QUAN HỆ DanhMucSanPham - SanPham (1-n)
            modelBuilder.Entity<SanPham>()
                .HasRequired(sp => sp.DanhMucSanPham)
                .WithMany(dm => dm.SanPhams)
                .HasForeignKey(sp => sp.MaDanhMuc)
                .WillCascadeOnDelete(false);

            // 13. MỐI QUAN HỆ SanPham - AnhSanPham (1-n)
            modelBuilder.Entity<AnhSanPham>()
                .HasRequired(a => a.SanPham)
                .WithMany(sp => sp.AnhSanPhams)
                .HasForeignKey(a => a.MaSanPham)
                .WillCascadeOnDelete(true);

            // 14. MỐI QUAN HỆ NguoiBan - AnhChungChi (1-n)
            modelBuilder.Entity<AnhChungChi>()
                .HasRequired(a => a.NguoiBan)
                .WithMany(nb => nb.AnhChungChis)
                .HasForeignKey(a => a.MaNguoiBan)
                .WillCascadeOnDelete(true);

            // 15. MỐI QUAN HỆ DanhGiaSanPham
            modelBuilder.Entity<DanhGiaSanPham>()
                .HasRequired(d => d.NguoiDung)
                .WithMany(nd => nd.DanhGiaSanPhams)
                .HasForeignKey(d => d.MaNguoiDung)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DanhGiaSanPham>()
                .HasRequired(d => d.SanPham)
                .WithMany(sp => sp.DanhGiaSanPhams)
                .HasForeignKey(d => d.MaSanPham)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<DanhGiaSanPham>()
                .HasOptional(d => d.NguoiBan)
                .WithMany()
                .HasForeignKey(d => d.MaNguoiBan)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DanhGiaSanPham>()
                .HasOptional(d => d.DonHang)
                .WithMany(dh => dh.DanhGiaSanPhams)
                .HasForeignKey(d => d.MaDonHang)
                .WillCascadeOnDelete(false);
            //2/4/2025
            // Cấu hình cho PhanHoiDanhGia
            modelBuilder.Entity<PhanHoiDanhGia>()
                .HasRequired(p => p.DanhGiaSanPham)
                .WithMany()
                .HasForeignKey(p => p.MaDanhGiaSanPham)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PhanHoiDanhGia>()
                .HasRequired(p => p.NguoiBan)
                .WithMany()
                .HasForeignKey(p => p.MaNguoiBan)
                .WillCascadeOnDelete(false);
            //2/4/2025
            // 16. MỐI QUAN HỆ NguoiDung - ThongBao (1-n)
            modelBuilder.Entity<ThongBao>()
                .HasRequired(tb => tb.NguoiDung)
                .WithMany(nd => nd.ThongBaos)
                .HasForeignKey(tb => tb.MaNguoiDung)
                .WillCascadeOnDelete(true);

            // 17. MỐI QUAN HỆ NguoiBan - ChuongTrinhKhuyenMai (1-n)
            modelBuilder.Entity<ChuongTrinhKhuyenMai>()
                .HasRequired(km => km.NguoiBan)
                .WithMany(nb => nb.ChuongTrinhKhuyenMais)
                .HasForeignKey(km => km.MaNguoiBan)
                .WillCascadeOnDelete(true);

            // 18. MỐI QUAN HỆ KhuyenMaiSanPham
            modelBuilder.Entity<KhuyenMaiSanPham>()
                .HasRequired(km => km.ChuongTrinhKhuyenMai)
                .WithMany(ct => ct.KhuyenMaiSanPhams)
                .HasForeignKey(km => km.MaKM)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<KhuyenMaiSanPham>()
                .HasRequired(km => km.SanPham)
                .WithMany(sp => sp.KhuyenMaiSanPhams)
                .HasForeignKey(km => km.MaSanPham)
                .WillCascadeOnDelete(false);

            // 19. MỐI QUAN HỆ KhuyenMaiDonHang
            modelBuilder.Entity<KhuyenMaiDonHang>()
                .HasRequired(km => km.ChuongTrinhKhuyenMai)
                .WithMany(ct => ct.KhuyenMaiDonHangs)
                .HasForeignKey(km => km.MaKM)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<KhuyenMaiDonHang>()
                .HasRequired(km => km.DonHang)
                .WithMany(dh => dh.KhuyenMaiDonHangs)
                .HasForeignKey(km => km.MaDonHang)
                .WillCascadeOnDelete(false);

            // 20. MỐI QUAN HỆ Escrow
            modelBuilder.Entity<Escrow>()
                .HasRequired(e => e.DonHang)
                .WithMany(dh => dh.Escrows)
                .HasForeignKey(e => e.MaDonHang)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Escrow>()
                .HasRequired(e => e.NguoiBan)
                .WithMany(nb => nb.Escrows)
                .HasForeignKey(e => e.MaNguoiBan)
                .WillCascadeOnDelete(false);

            // 21. MỐI QUAN HỆ LichSuGiaoDichVi
            modelBuilder.Entity<LichSuGiaoDichVi>()
                .HasRequired(ls => ls.NguoiBan)
                .WithMany(nb => nb.LichSuGiaoDichVis)
                .HasForeignKey(ls => ls.MaNguoiBan)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<LichSuGiaoDichVi>()
                .HasOptional(ls => ls.DonHang)
                .WithMany(dh => dh.LichSuGiaoDichVis)
                .HasForeignKey(ls => ls.MaDonHang)
                .WillCascadeOnDelete(false);

            // 22. MỐI QUAN HỆ ThongTinHoanTien
            modelBuilder.Entity<ThongTinHoanTien>()
                .HasRequired(ht => ht.DonHang)
                .WithMany(dh => dh.ThongTinHoanTiens)
                .HasForeignKey(ht => ht.MaDonHang)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ThongTinHoanTien>()
                .HasRequired(ht => ht.NguoiDung)
                .WithMany(nd => nd.ThongTinHoanTiens)
                .HasForeignKey(ht => ht.MaNguoiDung)
                .WillCascadeOnDelete(false);

            // 23. MỐI QUAN HỆ HangDoiHoanTienVNPay
            modelBuilder.Entity<HangDoiHoanTienVNPay>()
                .HasRequired(hd => hd.DonHang)
                .WithMany(dh => dh.HangDoiHoanTienVNPays)
                .HasForeignKey(hd => hd.MaDonHang)
                .WillCascadeOnDelete(false);




            // Cấu hình precision và scale cho các trường decimal
            // NguoiBan
            modelBuilder.Entity<NguoiBan>()
                .Property(nb => nb.SoDuVi)
                .HasPrecision(10, 2);

            // SanPham
            modelBuilder.Entity<SanPham>()
                .Property(sp => sp.GiaSanPham)
                .HasPrecision(10, 2);

            // DonHang
            modelBuilder.Entity<DonHang>()
                .Property(dh => dh.TongSoTien)
                .HasPrecision(10, 2);

            // ChiTietDonHang
            modelBuilder.Entity<ChiTietDonHang>()
                .Property(ct => ct.Gia)
                .HasPrecision(10, 2);

            // GiaoDich
            modelBuilder.Entity<GiaoDich>()
                .Property(gd => gd.TongTien)
                .HasPrecision(10, 2);

            // ChuongTrinhKhuyenMai
            modelBuilder.Entity<ChuongTrinhKhuyenMai>()
                .Property(km => km.GiaTriGiam)
                .HasPrecision(10, 2);

            // Escrow
            modelBuilder.Entity<Escrow>()
                .Property(e => e.TongTien)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Escrow>()
                .Property(e => e.PhiNenTang)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Escrow>()
                .Property(e => e.TienChuyenChoNguoiBan)
                .HasPrecision(10, 2);

            // LichSuGiaoDichVi
            modelBuilder.Entity<LichSuGiaoDichVi>()
                .Property(ls => ls.SoTien)
                .HasPrecision(10, 2);

            // ThongTinHoanTien
            modelBuilder.Entity<ThongTinHoanTien>()
                .Property(ht => ht.SoTienHoan)
                .HasPrecision(10, 2);

            // HangDoiHoanTienVNPay
            modelBuilder.Entity<HangDoiHoanTienVNPay>()
                .Property(hd => hd.SoTienHoan)
                .HasPrecision(10, 2);


            //27/4/2025
            // Cấu hình các quan hệ cho BaoCaoDonHang
            modelBuilder.Entity<BaoCaoDonHang>()
                .HasRequired(b => b.DonHang)
                .WithMany()
                .HasForeignKey(b => b.MaDonHang)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BaoCaoDonHang>()
                .HasRequired(b => b.NguoiDung)
                .WithMany()
                .HasForeignKey(b => b.MaNguoiDung)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BaoCaoDonHang>()
                .HasRequired(b => b.NguoiBan)
                .WithMany()
                .HasForeignKey(b => b.MaNguoiBan)
                .WillCascadeOnDelete(false);
            //27/4/2025

            //27/3/2025
            // Thiết lập cơ bản
            modelBuilder.Entity<GhiChepVi>()
                .ToTable("GhiChepVi");

            // Cấu hình primary key
            modelBuilder.Entity<GhiChepVi>()
                .HasKey(g => g.MaGhiChep)
                .Property(g => g.MaGhiChep)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            // Cấu hình precision và scale cho các trường decimal
            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.SoTien)
                .HasPrecision(10, 2)
                .IsRequired();

            // Cấu hình các trường required
            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.MaNguoiBan)
                .IsRequired();

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.LoaiGiaoDich)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.NgayGiaoDich)
                .IsRequired();

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.TrangThai)
                .IsRequired()
                .HasMaxLength(20);

            // Cấu hình các trường tùy chọn
            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.MoTa)
                .HasMaxLength(255)
                .IsOptional();

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.MaGiaoDichNgoai)
                .HasMaxLength(50)
                .IsOptional();

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.IP)
                .HasMaxLength(100)
                .IsOptional();

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.MaDonHang)
                .IsOptional();

            // Cấu hình mối quan hệ giữa GhiChepVi và NguoiBan
            modelBuilder.Entity<GhiChepVi>()
                .HasRequired(g => g.NguoiBan)
                .WithMany(n => n.GhiChepVis)
                .HasForeignKey(g => g.MaNguoiBan)
                .WillCascadeOnDelete(false);

            // Cấu hình mối quan hệ giữa GhiChepVi và DonHang (nếu có)
            modelBuilder.Entity<GhiChepVi>()
                .HasOptional(g => g.DonHang)
                .WithMany()
                .HasForeignKey(g => g.MaDonHang)
                .WillCascadeOnDelete(false);

            // Index để tối ưu hiệu suất truy vấn
            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.MaNguoiBan)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_GhiChepVi_MaNguoiBan")));

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.NgayGiaoDich)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_GhiChepVi_NgayGiaoDich")));

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.MaDonHang)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_GhiChepVi_MaDonHang")));

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.LoaiGiaoDich)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_GhiChepVi_LoaiGiaoDich")));

            // Index tổng hợp cho báo cáo
            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.MaNguoiBan)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_GhiChepVi_MaNguoiBan_NgayGiaoDich", 1) { IsUnique = false }));

            modelBuilder.Entity<GhiChepVi>()
                .Property(g => g.NgayGiaoDich)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_GhiChepVi_MaNguoiBan_NgayGiaoDich", 2) { IsUnique = false }));

            // Tắt quy ước cascade delete mặc định để tránh xung đột
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();



            base.OnModelCreating(modelBuilder);
        }
    }
}