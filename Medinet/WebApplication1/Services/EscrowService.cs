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

                        // Tính phí nền tảng
                        decimal marketplaceFeePercent = 10;
                        decimal.TryParse(ConfigurationManager.AppSettings["MarketplaceFeePercent"], out marketplaceFeePercent);

                        decimal phiNenTang = Math.Round(donHang.TongSoTien * (marketplaceFeePercent / 100), 0);
                        decimal tienChuyenChoNguoiBan = donHang.TongSoTien - phiNenTang;

                        // Tạo escrow
                        var newEscrow = new Escrow  // Đổi tên biến từ escrow thành newEscrow
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
                    // Kiểm tra và cải thiện kết nối database - THÊM ĐOẠN NÀY
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
                        .Select(d => new { d.MaDonHang, d.MaNguoiBan, d.TongSoTien })
                        .FirstOrDefaultAsync();

                    if (donHang == null)
                    {
                        var errorMsg = $"Không tìm thấy đơn hàng {maDonHang}";
                        System.Diagnostics.Debug.WriteLine(errorMsg);
                        throw new Exception(errorMsg);
                    }

                    System.Diagnostics.Debug.WriteLine($"Đã tìm thấy đơn hàng {maDonHang}, tổng tiền: {donHang.TongSoTien}");

                    // Tính phí nền tảng
                    decimal marketplaceFeePercent = 10;
                    decimal.TryParse(ConfigurationManager.AppSettings["MarketplaceFeePercent"], out marketplaceFeePercent);

                    decimal phiNenTang = Math.Round(donHang.TongSoTien * (marketplaceFeePercent / 100), 0);
                    decimal tienChuyenChoNguoiBan = donHang.TongSoTien - phiNenTang;

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
        /// Tạo ký quỹ mới khi thanh toán thành công
        /// </summary>
        /// <param name="maDonHang">Mã đơn hàng cần tạo ký quỹ</param>
        /// <returns>Đối tượng ký quỹ đã tạo</returns>
        //public async Task<Escrow> CreateEscrow(int maDonHang)
        //{
        //    // Tạo một context mới cho mỗi lần gọi
        //    // Tạo một context mới cho mỗi lần gọi
        //    using (var db = new MedinetDATN())
        //    {
        //        try
        //        {
        //            // Thêm debug log
        //            System.Diagnostics.Debug.WriteLine($"===== Bắt đầu tạo ký quỹ cho đơn hàng {maDonHang} =====");

        //            // Kiểm tra nếu đã có ký quỹ cho đơn hàng này
        //            var existingEscrow = await db.Escrows.FirstOrDefaultAsync(e => e.MaDonHang == maDonHang);
        //            if (existingEscrow != null)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"Đã tồn tại ký quỹ cho đơn hàng {maDonHang}, mã ký quỹ: {existingEscrow.MaKyQuy}");
        //                return existingEscrow;
        //            }

        //            // Lấy thông tin đơn hàng
        //            var donHang = await db.DonHangs
        //                .Include(d => d.NguoiBan)
        //                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);

        //            if (donHang == null)
        //            {
        //                throw new Exception($"Không tìm thấy đơn hàng {maDonHang}");
        //            }

        //            System.Diagnostics.Debug.WriteLine($"Đã tìm thấy đơn hàng {maDonHang}, tổng tiền: {donHang.TongSoTien}");

        //            // Tính phí nền tảng
        //            decimal marketplaceFeePercent = 10;
        //            decimal.TryParse(ConfigurationManager.AppSettings["MarketplaceFeePercent"], out marketplaceFeePercent);

        //            decimal phiNenTang = Math.Round(donHang.TongSoTien * (marketplaceFeePercent / 100), 0);
        //            decimal tienChuyenChoNguoiBan = donHang.TongSoTien - phiNenTang;

        //            // Tạo bản ghi ký quỹ mới
        //            var escrow = new Escrow
        //            {
        //                MaDonHang = maDonHang,
        //                MaNguoiBan = donHang.MaNguoiBan,
        //                TongTien = donHang.TongSoTien,
        //                PhiNenTang = phiNenTang,
        //                TienChuyenChoNguoiBan = tienChuyenChoNguoiBan,
        //                TrangThai = "Đang giữ",
        //                NgayTao = DateTime.Now
        //            };

        //            System.Diagnostics.Debug.WriteLine($"Đã tạo đối tượng escrow, chuẩn bị lưu vào DB");

        //            db.Escrows.Add(escrow);
        //            await db.SaveChangesAsync();

        //            System.Diagnostics.Debug.WriteLine($"Đã lưu escrow vào DB thành công, ID: {escrow.MaKyQuy}");
        //            System.Diagnostics.Debug.WriteLine($"Đã tạo ký quỹ cho đơn hàng {maDonHang}:");
        //            System.Diagnostics.Debug.WriteLine($"- Tổng tiền: {donHang.TongSoTien:N0}đ");
        //            System.Diagnostics.Debug.WriteLine($"- Phí nền tảng ({marketplaceFeePercent}%): {phiNenTang:N0}đ");
        //            System.Diagnostics.Debug.WriteLine($"- Tiền chuyển cho người bán: {tienChuyenChoNguoiBan:N0}đ");
        //            System.Diagnostics.Debug.WriteLine($"===== Kết thúc tạo ký quỹ cho đơn hàng {maDonHang} =====");

        //            return escrow;
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"===== LỖI tạo ký quỹ cho đơn hàng {maDonHang}: {ex.Message} =====");
        //            if (ex.InnerException != null)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
        //            }

        //            // QUAN TRỌNG: Log ra stack trace
        //            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

        //            throw; // Re-throw để caller biết có lỗi
        //        }
        //    }

        //}

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

                    // Cập nhật ví tiền của người bán (nếu có)
                    try
                    {
                        var nguoiBan = escrow.NguoiBan;
                        if (nguoiBan != null)
                        {
                            // Nếu chưa có SoDuVi, khởi tạo = 0
                            nguoiBan.SoDuVi = nguoiBan.SoDuVi;

                            // Cập nhật số dư ví
                            nguoiBan.SoDuVi += escrow.TienChuyenChoNguoiBan;
                            db.Entry(nguoiBan).State = EntityState.Modified;

                            System.Diagnostics.Debug.WriteLine($"Đã cập nhật số dư ví người bán ID {nguoiBan.MaNguoiBan}: " +
                                $"Cũ={nguoiBan.SoDuVi - escrow.TienChuyenChoNguoiBan:N0}đ, " +
                                $"Thêm={escrow.TienChuyenChoNguoiBan:N0}đ, " +
                                $"Mới={nguoiBan.SoDuVi:N0}đ");
                        }
                    }
                    catch (Exception walletEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi cập nhật ví người bán: {walletEx.Message}");
                        // Tiếp tục xử lý, không dừng giải ngân
                    }

                    // Ghi lịch sử giao dịch ví
                    try
                    {
                        var lichSuGiaoDich = new LichSuGiaoDichVi
                        {
                            MaNguoiBan = escrow.MaNguoiBan,
                            MaDonHang = escrow.MaDonHang,
                            SoTien = escrow.TienChuyenChoNguoiBan,
                            LoaiGiaoDich = "Nhận tiền từ đơn hàng",
                            TrangThai = "Thành công",
                            NoiDung = $"Nhận tiền từ đơn hàng #{escrow.MaDonHang} sau khi trừ phí nền tảng",
                            NgayGiaoDich = DateTime.Now
                        };

                        db.LichSuGiaoDichVis.Add(lichSuGiaoDich);
                        System.Diagnostics.Debug.WriteLine($"Đã thêm lịch sử giao dịch ví cho người bán {escrow.MaNguoiBan}");
                    }
                    catch (Exception historyEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi ghi lịch sử giao dịch: {historyEx.Message}");
                        // Tiếp tục xử lý, không dừng giải ngân
                    }

                    // Lưu tất cả thay đổi
                    await db.SaveChangesAsync();

                    // Commit transaction
                    dbTransaction.Commit();

                    System.Diagnostics.Debug.WriteLine($"Đã giải ngân {escrow.TienChuyenChoNguoiBan:N0}đ cho người bán ID {escrow.MaNguoiBan} từ đơn hàng {maDonHang}");
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