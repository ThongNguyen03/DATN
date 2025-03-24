using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class EscrowService
    {
        private MedinetDATN db = new MedinetDATN();

        // Tạo ký quỹ mới khi thanh toán thành công
        public async Task<Escrow> CreateEscrow(int maDonHang)
        {
            try
            {
                // Lấy thông tin đơn hàng
                var donHang = await db.DonHangs
                    .Include(d => d.NguoiBan)
                    .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);

                if (donHang == null)
                {
                    throw new Exception("Không tìm thấy đơn hàng");
                }

                // Tính phí nền tảng (10%)
                decimal marketplaceFeePercent = Convert.ToDecimal(ConfigurationManager.AppSettings["MarketplaceFeePercent"] ?? "10");
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

                db.Escrows.Add(escrow);
                await db.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Đã tạo ký quỹ cho đơn hàng {maDonHang} với số tiền {donHang.TongSoTien}");
                System.Diagnostics.Debug.WriteLine($"Phí nền tảng: {phiNenTang}, Tiền chuyển cho người bán: {tienChuyenChoNguoiBan}");

                return escrow;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tạo ký quỹ: " + ex.Message);
                throw;
            }
        }

        // Giải ngân tiền cho seller sau khi buyer xác nhận nhận hàng
        public async Task<bool> ReleaseEscrow(int maDonHang)
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

                // Cập nhật trạng thái đơn hàng
                var donHang = escrow.DonHang;
                donHang.DaGiaiNganChoSeller = true;
                donHang.TrangThaiDonHang = "Đã hoàn thành";

                // TODO: Thực hiện chuyển tiền thực tế cho người bán
                // Đây là nơi bạn sẽ tích hợp API chuyển khoản ngân hàng hoặc
                // gửi thông báo cho admin để xử lý chuyển khoản thủ công

                await db.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Đã giải ngân {escrow.TienChuyenChoNguoiBan} cho người bán từ đơn hàng {maDonHang}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi giải ngân: " + ex.Message);
                throw;
            }
        }

        // Hoàn tiền cho buyer nếu hủy đơn hàng
        public async Task<bool> RefundEscrow(int maDonHang)
        {
            try
            {
                var escrow = await db.Escrows
                    .Include(e => e.DonHang)
                    .FirstOrDefaultAsync(e => e.MaDonHang == maDonHang && e.TrangThai == "Đang giữ");

                if (escrow == null)
                {
                    return false;
                }

                escrow.TrangThai = "Đã hoàn tiền";
                escrow.NgayGiaiNgan = DateTime.Now;

                var donHang = escrow.DonHang;
                donHang.TrangThaiDonHang = "Đã hủy";

                // TODO: Thực hiện hoàn tiền thực tế cho người mua
                // Đây là nơi bạn sẽ tích hợp API hoàn tiền của VNPay hoặc
                // gửi thông báo cho admin để xử lý hoàn tiền thủ công

                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi hoàn tiền: " + ex.Message);
                throw;
            }
        }

        // Lấy thông tin ký quỹ
        public async Task<Escrow> GetEscrow(int maDonHang)
        {
            return await db.Escrows
                .Include(e => e.DonHang)
                .Include(e => e.NguoiBan)
                .FirstOrDefaultAsync(e => e.MaDonHang == maDonHang);
        }
    }
}