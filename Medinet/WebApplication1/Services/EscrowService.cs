using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class EscrowService
    {
        private MedinetDATN db = new MedinetDATN();

        // Đặt tỷ lệ đặt cọc là 10% tổng giá trị đơn hàng
        private const decimal DEPOSIT_RATE = 0.1m;
        // Đặt tỷ lệ phí nền tảng là 10% tổng giá trị đơn hàng
        private const decimal PLATFORM_FEE_RATE = 0.1m;

        // Phương thức kiểm tra người bán có đủ tiền đặt cọc và phí nền tảng không
        public async Task<bool> CheckSellerBalanceAsync(int maNguoiBan, decimal orderAmount)
        {
            using (var db = new MedinetDATN())
            {
                var nguoiBan = await db.NguoiBans.FindAsync(maNguoiBan);
                if (nguoiBan == null)
                    return false;

                // Số tiền đặt cọc cần thiết cho đơn hàng
                decimal requiredDeposit = CalculateRequiredDeposit(orderAmount);

                // Phí nền tảng cần trừ trước
                decimal platformFee = orderAmount * PLATFORM_FEE_RATE;

                // Tổng số tiền cần có = tiền đặt cọc + phí nền tảng
                decimal totalRequired = requiredDeposit + platformFee;

                // Kiểm tra số dư ví của người bán
                return nguoiBan.SoDuVi >= totalRequired;
            }
        }

        // Tính số tiền đặt cọc cần thiết dựa trên tổng giá trị đơn hàng
        public decimal CalculateRequiredDeposit(decimal orderAmount)
        {
            return orderAmount * DEPOSIT_RATE;
        }

        // Tính tổng số tiền cần có trong ví (đặt cọc + phí nền tảng)
        public decimal CalculateTotalRequiredAmount(decimal orderAmount)
        {
            decimal depositAmount = CalculateRequiredDeposit(orderAmount);
            decimal platformFee = orderAmount * PLATFORM_FEE_RATE;
            return depositAmount + platformFee;
        }


        //28/03/2025
        // Thêm phương thức mới để kiểm tra số dư và trả về thông tin chi tiết
        public async Task<(bool DuTien, decimal SoTienYeuCau)> KiemTraSoDuNguoiBanChiTietAsync(int maNguoiBan, decimal soTien)
        {
            // Lấy thông tin số dư của người bán
            var nguoiBan = await db.NguoiBans.FindAsync(maNguoiBan);
            if (nguoiBan == null)
            {
                return (false, 0);
            }

            // Số tiền đặt cọc cần thiết
            decimal requiredDeposit = CalculateRequiredDeposit(soTien);

            // Phí nền tảng cần trừ trước
            decimal platformFee = soTien * PLATFORM_FEE_RATE;

            // Tổng số tiền cần có = tiền đặt cọc + phí nền tảng
            decimal soTienYeuCau = requiredDeposit + platformFee;

            return (nguoiBan.SoDuVi >= soTienYeuCau, soTienYeuCau);
        }

        // Phương thức xử lý đặt cọc cho đơn hàng - được gọi khi người bán cập nhật trạng thái
        public async Task<bool> XuLyDatCocDonHangAsync(int maDonHang)
        {
            using (var dbTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Lấy thông tin đơn hàng
                    var donHang = await db.DonHangs
                        .Include(d => d.NguoiBan)
                        .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);

                    if (donHang == null)
                    {
                        return false;
                    }

                    // Kiểm tra nếu đã có ký quỹ
                    var escrowExists = await db.Escrows.AnyAsync(e => e.MaDonHang == maDonHang);
                    if (escrowExists)
                    {
                        return true; // Đã xử lý ký quỹ trước đó
                    }

                    // Lấy thông tin người bán
                    var nguoiBan = await db.NguoiBans.FindAsync(donHang.MaNguoiBan);
                    if (nguoiBan == null)
                    {
                        return false;
                    }

                    // Tính số tiền đặt cọc và phí
                    decimal orderAmount = donHang.TongSoTien;
                    decimal requiredDeposit = CalculateRequiredDeposit(orderAmount);
                    decimal platformFee = orderAmount * PLATFORM_FEE_RATE;
                    decimal totalAmount = requiredDeposit + platformFee;

                    // Kiểm tra số dư
                    if (nguoiBan.SoDuVi < totalAmount)
                    {
                        return false;
                    }

                    // Trừ tiền từ ví người bán
                    nguoiBan.SoDuVi -= totalAmount;
                    db.Entry(nguoiBan).State = EntityState.Modified;

                    // Tạo ghi chép ví cho tiền đặt cọc
                    var ghiChepCoc = new GhiChepVi
                    {
                        MaNguoiBan = nguoiBan.MaNguoiBan,
                        MaDonHang = maDonHang,
                        SoTien = -requiredDeposit,
                        LoaiGiaoDich = "Đặt cọc",
                        MoTa = $"Đặt cọc cho đơn hàng #{maDonHang}",
                        NgayGiaoDich = DateTime.Now,
                        TrangThai = "Thành công"
                    };
                    db.GhiChepVis.Add(ghiChepCoc);

                    // Tạo ghi chép ví cho phí nền tảng
                    var ghiChepPhi = new GhiChepVi
                    {
                        MaNguoiBan = nguoiBan.MaNguoiBan,
                        MaDonHang = maDonHang,
                        SoTien = -platformFee,
                        LoaiGiaoDich = "Phí nền tảng",
                        MoTa = $"Phí nền tảng cho đơn hàng #{maDonHang} ({PLATFORM_FEE_RATE * 100}%)",
                        NgayGiaoDich = DateTime.Now,
                        TrangThai = "Thành công"
                    };
                    db.GhiChepVis.Add(ghiChepPhi);

                    // Tạo bản ghi ký quỹ
                    var escrow = new Escrow
                    {
                        MaDonHang = maDonHang,
                        MaNguoiBan = donHang.MaNguoiBan,
                        TongTien = orderAmount,
                        PhiNenTang = platformFee,
                        TienChuyenChoNguoiBan = orderAmount - platformFee,
                        TrangThai = "Đang giữ",
                        NgayTao = DateTime.Now
                    };
                    db.Escrows.Add(escrow);

                    // Lưu thay đổi và commit transaction
                    await db.SaveChangesAsync();
                    dbTransaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    dbTransaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"Lỗi xử lý đặt cọc: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    return false;
                }
            }
        }

        //28/03/2025
        public async Task<List<Escrow>> CreateMultipleEscrowsAsync(List<int> maDonHangList)
        {
            System.Diagnostics.Debug.WriteLine($"===== Bắt đầu tạo ký quỹ cho {maDonHangList.Count} đơn hàng =====");
            var escrows = new List<Escrow>();

            foreach (var maDonHang in maDonHangList)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Xử lý đơn hàng: MaDonHang = {maDonHang}");

                    // Tạo riêng DB context cho mỗi escrow để tránh các vấn đề về timeout
                    using (var db = new MedinetDATN())
                    {
                        db.Database.CommandTimeout = 300; // 5 phút

                        // Kiểm tra đã tồn tại
                        var existingEscrow = await db.Escrows.AnyAsync(e => e.MaDonHang == maDonHang);
                        if (existingEscrow)
                        {
                            var foundEscrow = await db.Escrows.Where(e => e.MaDonHang == maDonHang).FirstOrDefaultAsync();
                            escrows.Add(foundEscrow);
                            System.Diagnostics.Debug.WriteLine($"Đã tồn tại ký quỹ cho đơn hàng {maDonHang}");
                            continue;
                        }

                        // Lấy thông tin đơn hàng
                        var donHang = await db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);
                        if (donHang == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Không tìm thấy đơn hàng {maDonHang}");
                            continue;
                        }

                        // Lấy thông tin người bán
                        var nguoiBan = await db.NguoiBans.FindAsync(donHang.MaNguoiBan);
                        if (nguoiBan == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Không tìm thấy người bán với mã {donHang.MaNguoiBan}");
                            continue;
                        }

                        // Kiểm tra số dư và trừ tiền nếu là đơn hàng COD
                        if (donHang.PhuongThucThanhToan == "COD")
                        {
                            // Số tiền đặt cọc cần thiết
                            decimal requiredDeposit = CalculateRequiredDeposit(donHang.TongSoTien);

                            // Phí nền tảng cần trừ trước
                            decimal platformFee = donHang.TongSoTien * PLATFORM_FEE_RATE;

                            // Tổng số tiền cần trừ = tiền đặt cọc + phí nền tảng
                            decimal totalAmount = requiredDeposit + platformFee;

                            // Kiểm tra số dư ví của người bán
                            if (nguoiBan.SoDuVi < totalAmount)
                            {
                                System.Diagnostics.Debug.WriteLine($"Số dư ví không đủ để đặt cọc và trả phí nền tảng. Cần {totalAmount:N0} VNĐ, hiện có {nguoiBan.SoDuVi:N0} VNĐ");
                                continue;
                            }

                            // Trừ tiền đặt cọc và phí từ ví của người bán
                            nguoiBan.SoDuVi -= totalAmount;
                            db.Entry(nguoiBan).State = EntityState.Modified;

                            // Tạo ghi chép ví cho tiền đặt cọc
                            var ghiChepCoc = new GhiChepVi
                            {
                                MaNguoiBan = nguoiBan.MaNguoiBan,
                                MaDonHang = maDonHang,
                                SoTien = -requiredDeposit,
                                LoaiGiaoDich = "Đặt cọc",
                                MoTa = $"Đặt cọc cho đơn hàng #{maDonHang}",
                                NgayGiaoDich = DateTime.Now,
                                TrangThai = "Thành công"
                            };
                            db.GhiChepVis.Add(ghiChepCoc);

                            // Tạo ghi chép ví cho phí nền tảng
                            var ghiChepPhi = new GhiChepVi
                            {
                                MaNguoiBan = nguoiBan.MaNguoiBan,
                                MaDonHang = maDonHang,
                                SoTien = -platformFee,
                                LoaiGiaoDich = "Phí nền tảng",
                                MoTa = $"Phí nền tảng cho đơn hàng #{maDonHang} (10%)",
                                NgayGiaoDich = DateTime.Now,
                                TrangThai = "Thành công"
                            };
                            db.GhiChepVis.Add(ghiChepPhi);
                        }

                        // Tính phí nền tảng (vẫn cần dù đã trừ trực tiếp ở COD)
                        decimal phiNenTang = Math.Round(donHang.TongSoTien * PLATFORM_FEE_RATE, 0);
                        decimal tienChuyenChoNguoiBan = donHang.TongSoTien - phiNenTang;

                        // Tạo escrow
                        var newEscrow = new Escrow
                        {
                            MaDonHang = maDonHang,
                            MaNguoiBan = donHang.MaNguoiBan,
                            TongTien = donHang.TongSoTien,
                            PhiNenTang = phiNenTang,
                            TienChuyenChoNguoiBan = tienChuyenChoNguoiBan,
                            TrangThai = "Đang giữ",
                            NgayTao = DateTime.Now
                        };

                        db.Escrows.Add(newEscrow);
                        await db.SaveChangesAsync();

                        escrows.Add(newEscrow);
                        System.Diagnostics.Debug.WriteLine($"Đã tạo ký quỹ thành công cho đơn hàng {maDonHang}, ID: {newEscrow.MaKyQuy}");

                        // Delay một chút để tránh overload
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi tạo ký quỹ cho đơn hàng {maDonHang}: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"===== Kết thúc, đã tạo {escrows.Count}/{maDonHangList.Count} ký quỹ =====");
            return escrows;
        }

        public async Task<Escrow> CreateEscrowAsync(int maDonHang)
        {
            using (var db = new MedinetDATN())
            {
                // Tăng thời gian timeout của kết nối
                db.Database.CommandTimeout = 300; // Tăng lên 5 phút (300 giây)

                try
                {
                    // Kiểm tra và cải thiện kết nối database
                    if (db.Database.Connection.State != ConnectionState.Open)
                    {
                        System.Diagnostics.Debug.WriteLine($"Kết nối đang ở trạng thái {db.Database.Connection.State}, đang mở kết nối...");
                        await db.Database.Connection.OpenAsync();
                    }
                    System.Diagnostics.Debug.WriteLine($"Trạng thái kết nối DB: {db.Database.Connection.State}");

                    System.Diagnostics.Debug.WriteLine($"===== Bắt đầu tạo ký quỹ cho đơn hàng {maDonHang} =====");

                    // Tối ưu hóa: Sử dụng Any để kiểm tra tồn tại trước
                    bool escrowExists = await db.Escrows.AnyAsync(e => e.MaDonHang == maDonHang);

                    if (escrowExists)
                    {
                        // Chỉ khi đã biết tồn tại thì mới lấy chi tiết
                        var existingEscrow = await db.Escrows.Where(e => e.MaDonHang == maDonHang).FirstOrDefaultAsync();
                        System.Diagnostics.Debug.WriteLine($"Đã tồn tại ký quỹ cho đơn hàng {maDonHang}, mã ký quỹ: {existingEscrow.MaKyQuy}");
                        return existingEscrow;
                    }

                    // Lấy thông tin đơn hàng - không cần Include các bảng liên quan nếu không sử dụng
                    var donHang = await db.DonHangs
                        .Where(d => d.MaDonHang == maDonHang)
                        .FirstOrDefaultAsync();

                    if (donHang == null)
                    {
                        var errorMsg = $"Không tìm thấy đơn hàng {maDonHang}";
                        System.Diagnostics.Debug.WriteLine(errorMsg);
                        throw new Exception(errorMsg);
                    }

                    System.Diagnostics.Debug.WriteLine($"Đã tìm thấy đơn hàng {maDonHang}, tổng tiền: {donHang.TongSoTien}");

                    // Lấy thông tin người bán
                    var nguoiBan = await db.NguoiBans.FindAsync(donHang.MaNguoiBan);
                    if (nguoiBan == null)
                    {
                        throw new Exception($"Không tìm thấy người bán với mã {donHang.MaNguoiBan}");
                    }

                    // Kiểm tra số dư và trừ tiền nếu là đơn hàng COD
                    if (donHang.PhuongThucThanhToan == "COD")
                    {
                        // Số tiền đặt cọc cần thiết
                        decimal requiredDeposit = CalculateRequiredDeposit(donHang.TongSoTien);

                        // Phí nền tảng cần trừ trước
                        decimal platformFee = donHang.TongSoTien * PLATFORM_FEE_RATE;

                        // Tổng số tiền cần trừ = tiền đặt cọc + phí nền tảng
                        decimal totalAmount = requiredDeposit + platformFee;

                        // Kiểm tra số dư ví của người bán
                        if (nguoiBan.SoDuVi < totalAmount)
                        {
                            throw new Exception($"Số dư ví không đủ để đặt cọc và trả phí nền tảng. Cần {totalAmount:N0} VNĐ, hiện có {nguoiBan.SoDuVi:N0} VNĐ");
                        }

                        // Trừ tiền đặt cọc và phí từ ví của người bán
                        nguoiBan.SoDuVi -= totalAmount;
                        db.Entry(nguoiBan).State = EntityState.Modified;

                        // Tạo ghi chép ví cho tiền đặt cọc
                        var ghiChepCoc = new GhiChepVi
                        {
                            MaNguoiBan = nguoiBan.MaNguoiBan,
                            MaDonHang = maDonHang,
                            SoTien = -requiredDeposit,
                            LoaiGiaoDich = "Đặt cọc",
                            MoTa = $"Đặt cọc cho đơn hàng #{maDonHang}",
                            NgayGiaoDich = DateTime.Now,
                            TrangThai = "Thành công"
                        };
                        db.GhiChepVis.Add(ghiChepCoc);

                        // Tạo ghi chép ví cho phí nền tảng
                        var ghiChepPhi = new GhiChepVi
                        {
                            MaNguoiBan = nguoiBan.MaNguoiBan,
                            MaDonHang = maDonHang,
                            SoTien = -platformFee,
                            LoaiGiaoDich = "Phí nền tảng",
                            MoTa = $"Phí nền tảng cho đơn hàng #{maDonHang} (10%)",
                            NgayGiaoDich = DateTime.Now,
                            TrangThai = "Thành công"
                        };
                        db.GhiChepVis.Add(ghiChepPhi);
                    }

                    // Tính phí nền tảng cho Escrow
                    decimal phiNenTang = Math.Round(donHang.TongSoTien * PLATFORM_FEE_RATE, 0);
                    decimal tienChuyenChoNguoiBan = donHang.TongSoTien - phiNenTang;

                    System.Diagnostics.Debug.WriteLine($"Phí nền tảng: {phiNenTang:N0}đ, Tiền chuyển cho người bán: {tienChuyenChoNguoiBan:N0}đ");

                    // Tạo bản ghi ký quỹ mới
                    var escrow = new Escrow
                    {
                        MaDonHang = maDonHang,
                        MaNguoiBan = donHang.MaNguoiBan,
                        TongTien = donHang.TongSoTien,
                        PhiNenTang = phiNenTang,
                        TienChuyenChoNguoiBan = tienChuyenChoNguoiBan,
                        TrangThai = "Đang giữ",
                        NgayTao = DateTime.Now
                    };

                    System.Diagnostics.Debug.WriteLine($"Đã tạo đối tượng escrow, chuẩn bị lưu vào DB");

                    try
                    {
                        db.Escrows.Add(escrow);
                        await db.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine($"Đã lưu escrow vào DB thành công, ID: {escrow.MaKyQuy}");
                    }
                    catch (DbUpdateException dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"DbUpdateException khi lưu escrow: {dbEx.Message}");
                        if (dbEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner exception: {dbEx.InnerException.Message}");
                        }
                        throw;
                    }

                    System.Diagnostics.Debug.WriteLine($"===== Kết thúc tạo ký quỹ cho đơn hàng {maDonHang} =====");
                    return escrow;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"===== LỖI tạo ký quỹ cho đơn hàng {maDonHang}: {ex.Message} =====");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }
                finally
                {
                    // Đảm bảo connection được đóng
                    if (db.Database.Connection.State != ConnectionState.Closed)
                    {
                        db.Database.Connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Giải ngân tiền cho người bán sau khi người mua xác nhận nhận hàng
        /// </summary>
        /// <param name="maDonHang">Mã đơn hàng cần giải ngân</param>
        /// <returns>Kết quả giải ngân (true/false)</returns>
        public async Task<bool> ReleaseEscrow(int maDonHang)
        {
            using (var dbTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Tìm bản ghi ký quỹ
                    var escrow = await db.Escrows
                        .Include(e => e.DonHang)
                        .Include(e => e.NguoiBan)
                        .FirstOrDefaultAsync(e => e.MaDonHang == maDonHang && e.TrangThai == "Đang giữ");

                    if (escrow == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Không tìm thấy ký quỹ cho đơn hàng {maDonHang} hoặc đã giải ngân");
                        return false;
                    }

                    // Cập nhật trạng thái ký quỹ
                    escrow.TrangThai = "Đã giải ngân";
                    escrow.NgayGiaiNgan = DateTime.Now;
                    db.Entry(escrow).State = EntityState.Modified;

                    // Cập nhật trạng thái đơn hàng
                    var donHang = escrow.DonHang;
                    donHang.DaGiaiNganChoSeller = true;
                    donHang.TrangThaiDonHang = "Đã hoàn thành";
                    db.Entry(donHang).State = EntityState.Modified;

                    // Cập nhật ví tiền của người bán
                    var nguoiBan = escrow.NguoiBan;
                    if (nguoiBan != null)
                    {
                        // Xác định số tiền cần hoàn trả/thanh toán
                        decimal depositAmount = 0;
                        if (donHang.PhuongThucThanhToan == "COD")
                        {
                            // Đối với COD, hoàn trả tiền đặt cọc + thanh toán tiền đơn hàng
                            depositAmount = CalculateRequiredDeposit(donHang.TongSoTien);

                            // Hoàn trả tiền đặt cọc + thanh toán tiền đơn hàng
                            decimal totalPayment = depositAmount + donHang.TongSoTien;
                            nguoiBan.SoDuVi += totalPayment;
                            db.Entry(nguoiBan).State = EntityState.Modified;

                            System.Diagnostics.Debug.WriteLine($"COD - Hoàn trả đặt cọc: {depositAmount:N0}đ, Thanh toán đơn hàng: {donHang.TongSoTien:N0}đ");

                            // Tạo ghi chép hoàn trả đặt cọc
                            var ghiChepCoc = new GhiChepVi
                            {
                                MaNguoiBan = nguoiBan.MaNguoiBan,
                                MaDonHang = maDonHang,
                                SoTien = depositAmount,
                                LoaiGiaoDich = "Hoàn trả đặt cọc",
                                MoTa = $"Hoàn trả tiền đặt cọc cho đơn hàng #{maDonHang}",
                                NgayGiaoDich = DateTime.Now,
                                TrangThai = "Thành công"
                            };
                            db.GhiChepVis.Add(ghiChepCoc);

                            // Tạo ghi chép thanh toán đơn hàng
                            var ghiChepThanhToan = new GhiChepVi
                            {
                                MaNguoiBan = nguoiBan.MaNguoiBan,
                                MaDonHang = maDonHang,
                                SoTien = donHang.TongSoTien,
                                LoaiGiaoDich = "Thanh toán đơn hàng",
                                MoTa = $"Thanh toán tiền đơn hàng #{maDonHang}",
                                NgayGiaoDich = DateTime.Now,
                                TrangThai = "Thành công"
                            };
                            db.GhiChepVis.Add(ghiChepThanhToan);
                        }
                        else
                        {
                            // Đối với VNPAY, chỉ thanh toán tiền sau khi trừ phí
                            nguoiBan.SoDuVi += escrow.TienChuyenChoNguoiBan;
                            db.Entry(nguoiBan).State = EntityState.Modified;

                            System.Diagnostics.Debug.WriteLine($"VNPAY - Thanh toán sau khi trừ phí: {escrow.TienChuyenChoNguoiBan:N0}đ");

                            // Tạo ghi chép thanh toán
                            var ghiChep = new GhiChepVi
                            {
                                MaNguoiBan = nguoiBan.MaNguoiBan,
                                MaDonHang = maDonHang,
                                SoTien = escrow.TienChuyenChoNguoiBan,
                                LoaiGiaoDich = "Thanh toán đơn hàng",
                                MoTa = $"Thanh toán tiền đơn hàng #{maDonHang} (đã trừ phí nền tảng)",
                                NgayGiaoDich = DateTime.Now,
                                TrangThai = "Thành công"
                            };
                            db.GhiChepVis.Add(ghiChep);
                        }

                        System.Diagnostics.Debug.WriteLine($"Đã cập nhật số dư ví người bán ID {nguoiBan.MaNguoiBan}: {nguoiBan.SoDuVi:N0}đ");
                    }

                    // Lưu tất cả thay đổi
                    await db.SaveChangesAsync();

                    // Commit transaction
                    dbTransaction.Commit();

                    System.Diagnostics.Debug.WriteLine($"Đã giải ngân thành công cho đơn hàng {maDonHang}");
                    return true;
                }
                catch (Exception ex)
                {
                    // Rollback transaction nếu có lỗi
                    dbTransaction.Rollback();

                    System.Diagnostics.Debug.WriteLine($"Lỗi giải ngân cho đơn hàng {maDonHang}: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Xử lý hoàn thành đơn hàng và giải ngân ký quỹ
        /// </summary>
        /// <param name="maDonHang">Mã đơn hàng cần xử lý</param>
        /// <returns>Kết quả xử lý (true/false)</returns>
        public async Task<bool> ProcessOrderCompletionAsync(int maDonHang)
        {
            using (var db = new MedinetDATN())
            {
                using (var dbTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Lấy thông tin đơn hàng
                        var donHang = await db.DonHangs.FindAsync(maDonHang);
                        if (donHang == null)
                            throw new Exception($"Không tìm thấy đơn hàng với mã {maDonHang}");

                        // Lấy thông tin ký quỹ
                        var escrow = await db.Escrows
                            .FirstOrDefaultAsync(e => e.MaDonHang == maDonHang);

                        if (escrow == null)
                            throw new Exception($"Không tìm thấy ký quỹ cho đơn hàng {maDonHang}");

                        // Chỉ xử lý khi ký quỹ đang ở trạng thái "Đang giữ"
                        if (escrow.TrangThai != "Đang giữ")
                            throw new Exception($"Ký quỹ đã được xử lý trước đó, trạng thái hiện tại: {escrow.TrangThai}");

                        // Cập nhật trạng thái đơn hàng
                        donHang.TrangThaiDonHang = "Đã xác nhận nhận hàng";
                        donHang.DaXacNhanNhanHang = true;
                        donHang.NgayCapNhat = DateTime.Now;
                        db.Entry(donHang).State = EntityState.Modified;
                        await db.SaveChangesAsync();

                        // Gọi phương thức giải ngân có sẵn
                        bool result = await ReleaseEscrow(maDonHang);

                        if (!result)
                            throw new Exception("Không thể giải ngân ký quỹ");

                        System.Diagnostics.Debug.WriteLine($"Đã xử lý hoàn thành đơn hàng {maDonHang} thành công");

                        // Commit transaction
                        dbTransaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        dbTransaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý hoàn thành đơn hàng: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Hoàn tiền cho người mua nếu hủy đơn hàng và hoàn trả tiền đặt cọc cho người bán
        /// </summary>
        /// <param name="maDonHang">Mã đơn hàng cần hoàn tiền</param>
        /// <param name="lyDoHuy">Lý do hủy đơn hàng</param>
        /// <returns>Kết quả hoàn tiền (true/false)</returns>
        public async Task<bool> ProcessOrderCancellationAsync(int maDonHang, string lyDoHuy)
        {
            using (var db = new MedinetDATN())
            {
                using (var dbTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Lấy thông tin đơn hàng
                        var donHang = await db.DonHangs.FindAsync(maDonHang);
                        if (donHang == null)
                            throw new Exception($"Không tìm thấy đơn hàng với mã {maDonHang}");

                        // Lấy thông tin ký quỹ
                        var escrow = await db.Escrows
                            .FirstOrDefaultAsync(e => e.MaDonHang == maDonHang);

                        if (escrow == null)
                        {
                            // Nếu không có ký quỹ (có thể đơn hàng chưa được xử lý), vẫn tiếp tục xử lý hủy đơn hàng
                            System.Diagnostics.Debug.WriteLine($"Không tìm thấy ký quỹ cho đơn hàng {maDonHang}, tiếp tục xử lý hủy");
                        }
                        else if (escrow.TrangThai != "Đang giữ")
                        {
                            // Nếu ký quỹ đã được xử lý, cảnh báo nhưng vẫn tiếp tục
                            System.Diagnostics.Debug.WriteLine($"Ký quỹ đã được xử lý trước đó, trạng thái hiện tại: {escrow.TrangThai}");
                        }
                        else
                        {
                            // Cập nhật trạng thái ký quỹ
                            escrow.TrangThai = "Đã hoàn tiền";
                            escrow.NgayGiaiNgan = DateTime.Now;
                            db.Entry(escrow).State = EntityState.Modified;

                            // Nếu là COD, hoàn trả tiền đặt cọc cho người bán
                            if (donHang.PhuongThucThanhToan == "COD")
                            {
                                var nguoiBan = await db.NguoiBans.FindAsync(donHang.MaNguoiBan);
                                if (nguoiBan != null)
                                {
                                    decimal depositAmount = CalculateRequiredDeposit(donHang.TongSoTien);

                                    // Hoàn trả tiền đặt cọc
                                    nguoiBan.SoDuVi += depositAmount;
                                    db.Entry(nguoiBan).State = EntityState.Modified;

                                    // Tạo ghi chép ví
                                    var ghiChep = new GhiChepVi
                                    {
                                        MaNguoiBan = nguoiBan.MaNguoiBan,
                                        MaDonHang = maDonHang,
                                        SoTien = depositAmount,
                                        LoaiGiaoDich = "Hoàn trả đặt cọc",
                                        MoTa = $"Hoàn trả tiền đặt cọc cho đơn hàng #{maDonHang} (đã hủy)",
                                        NgayGiaoDich = DateTime.Now,
                                        TrangThai = "Thành công"
                                    };
                                    db.GhiChepVis.Add(ghiChep);

                                    System.Diagnostics.Debug.WriteLine($"Đã hoàn trả tiền đặt cọc {depositAmount:N0}đ cho người bán ID {nguoiBan.MaNguoiBan}");
                                }
                            }
                            // Nếu là VNPAY, thêm vào hàng đợi hoàn tiền cho người mua
                            else if (donHang.PhuongThucThanhToan == "VNPAY")
                            {
                                await RefundEscrow(maDonHang);
                            }
                        }

                        // Cập nhật trạng thái đơn hàng
                        donHang.TrangThaiDonHang = "Đã hủy";
                        donHang.NgayHuy = DateTime.Now;
                        donHang.LyDoHuy = lyDoHuy;
                        donHang.NgayCapNhat = DateTime.Now;

                        // Lưu thay đổi
                        await db.SaveChangesAsync();
                        dbTransaction.Commit();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        dbTransaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý hủy đơn hàng: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Hoàn tiền cho người mua nếu hủy đơn hàng
        /// </summary>
        /// <param name="maDonHang">Mã đơn hàng cần hoàn tiền</param>
        /// <returns>Kết quả hoàn tiền (true/false)</returns>
        public async Task<bool> RefundEscrow(int maDonHang)
        {
            using (var dbTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Tìm bản ghi ký quỹ
                    var escrow = await db.Escrows
                        .Include(e => e.DonHang)
                        .FirstOrDefaultAsync(e => e.MaDonHang == maDonHang && e.TrangThai == "Đang giữ");

                    if (escrow == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Không tìm thấy ký quỹ cho đơn hàng {maDonHang} hoặc đã xử lý");
                        return false;
                    }

                    // Cập nhật trạng thái ký quỹ
                    escrow.TrangThai = "Đã hoàn tiền";
                    escrow.NgayGiaiNgan = DateTime.Now;
                    db.Entry(escrow).State = EntityState.Modified;

                    // Cập nhật trạng thái đơn hàng
                    var donHang = escrow.DonHang;

                    // Chỉ cập nhật nếu đơn hàng chưa ở trạng thái Đã hủy
                    if (donHang.TrangThaiDonHang != "Đã hủy")
                    {
                        donHang.TrangThaiDonHang = "Đã hủy";
                        db.Entry(donHang).State = EntityState.Modified;
                    }

                    // Ghi thông tin hoàn tiền
                    try
                    {
                        var hoantien = new ThongTinHoanTien
                        {
                            MaDonHang = maDonHang,
                            MaNguoiDung = donHang.MaNguoiDung,
                            SoTienHoan = escrow.TongTien,
                            TrangThai = "Đã hoàn tiền",
                            PhuongThucHoanTien = donHang.PhuongThucThanhToan,
                            LyDoHoanTien = "Hủy đơn hàng",
                            NgayHoanTien = DateTime.Now
                        };

                        db.ThongTinHoanTiens.Add(hoantien);
                        System.Diagnostics.Debug.WriteLine($"Đã thêm thông tin hoàn tiền cho đơn hàng {maDonHang}");
                    }
                    catch (Exception refundEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi ghi thông tin hoàn tiền: {refundEx.Message}");
                        // Tiếp tục xử lý, không dừng quá trình hoàn tiền
                    }

                    // Nếu đơn hàng thanh toán qua VNPAY, thêm hàng đợi hoàn tiền qua API
                    if (donHang.PhuongThucThanhToan == "VNPAY")
                    {
                        // Tìm giao dịch VNPAY
                        var giaoDich = await db.GiaoDichs
                            .FirstOrDefaultAsync(g => g.MaDonHang == maDonHang);

                        if (giaoDich != null && !string.IsNullOrEmpty(giaoDich.MaGiaoDichVNPay))
                        {
                            try
                            {
                                var hangDoiHoanTien = new HangDoiHoanTienVNPay
                                {
                                    MaDonHang = maDonHang,
                                    MaGiaoDichVNPay = giaoDich.MaGiaoDichVNPay,
                                    SoTienHoan = escrow.TongTien,
                                    TrangThai = "Chờ xử lý",
                                    NgayTao = DateTime.Now,
                                    GhiChu = "Hoàn tiền tự động do hủy đơn hàng"
                                };

                                db.HangDoiHoanTienVNPays.Add(hangDoiHoanTien);
                                System.Diagnostics.Debug.WriteLine($"Đã thêm vào hàng đợi hoàn tiền VNPAY cho đơn hàng {maDonHang}");
                            }
                            catch (Exception queueEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Lỗi thêm hàng đợi hoàn tiền VNPAY: {queueEx.Message}");
                                // Tiếp tục xử lý, không dừng quá trình hoàn tiền
                            }
                        }
                    }

                    // Lưu tất cả thay đổi
                    await db.SaveChangesAsync();

                    // Commit transaction
                    dbTransaction.Commit();

                    System.Diagnostics.Debug.WriteLine($"Đã hoàn tiền {escrow.TongTien:N0}đ cho người mua từ đơn hàng {maDonHang}");
                    return true;
                }
                catch (Exception ex)
                {
                    // Rollback transaction nếu có lỗi
                    dbTransaction.Rollback();

                    System.Diagnostics.Debug.WriteLine($"Lỗi hoàn tiền cho đơn hàng {maDonHang}: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Lấy thông tin ký quỹ
        /// </summary>
        /// <param name="maDonHang">Mã đơn hàng cần lấy thông tin ký quỹ</param>
        /// <returns>Đối tượng ký quỹ</returns>
        public async Task<Escrow> GetEscrow(int maDonHang)
        {
            return await db.Escrows
                .Include(e => e.DonHang)
                .Include(e => e.NguoiBan)
                .FirstOrDefaultAsync(e => e.MaDonHang == maDonHang);
        }

        /// <summary>
        /// Kiểm tra trạng thái ký quỹ của đơn hàng
        /// </summary>
        /// <param name="maDonHang">Mã đơn hàng cần kiểm tra</param>
        /// <returns>Trạng thái ký quỹ (null nếu không tìm thấy)</returns>
        public async Task<string> GetEscrowStatus(int maDonHang)
        {
            var escrow = await db.Escrows
                .FirstOrDefaultAsync(e => e.MaDonHang == maDonHang);

            return escrow?.TrangThai;
        }
    }
}