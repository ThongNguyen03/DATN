using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Data.Entity;
using System.Data.SqlClient;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Services
{
    public class OrderAutoConfirmationService : IRegisteredObject
    {
        private readonly object _lock = new object();
        private bool _shuttingDown;

        public OrderAutoConfirmationService()
        {
            // Register this task with the hosting environment
            HostingEnvironment.RegisterObject(this);
        }

        public void Start()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(CheckOrders, null);
        }

        private async void CheckOrders(object state)
        {
            try
            {
                using (var db = new MedinetDATN())
                {
                    // Tìm các đơn hàng ở trạng thái "Đã giao" mà đã quá thời gian tự động xác nhận
                    var donHangs = await db.DonHangs
                        .Where(d => d.TrangThaiDonHang == "Đã giao"
                               && d.ThoiGianTuDongXacNhan.HasValue
                               && d.ThoiGianTuDongXacNhan <= DateTime.Now)
                        .ToListAsync();

                    if (donHangs.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Tìm thấy {donHangs.Count} đơn hàng cần tự động xác nhận");
                    }

                    foreach (var donHang in donHangs)
                    {
                        try
                        {
                            // Cập nhật trạng thái đơn hàng
                            donHang.TrangThaiDonHang = "Đã hoàn thành";
                            donHang.DaXacNhanNhanHang = true;
                            donHang.NgayCapNhat = DateTime.Now;

                            db.Entry(donHang).State = EntityState.Modified;

                            // Cập nhật trạng thái giao dịch
                            string updateGiaoDichSql = @"
                            UPDATE GiaoDich 
                            SET TrangThaiGiaoDich = N'Đã hoàn thành' 
                            WHERE MaDonHang = @MaDonHang";

                            await db.Database.ExecuteSqlCommandAsync(
                                updateGiaoDichSql,
                                new SqlParameter("@MaDonHang", donHang.MaDonHang)
                            );

                            // Giải ngân tiền cho người bán
                            var escrowService = new EscrowService();
                            await escrowService.ReleaseEscrow(donHang.MaDonHang);

                            // Thêm thông báo cho người mua
                            var thongBaoNguoiMua = new ThongBao
                            {
                                MaNguoiDung = donHang.MaNguoiDung,
                                LoaiThongBao = "DonHang",
                                TieuDe = "Đơn hàng đã được tự động xác nhận",
                                TinNhan = $"Đơn hàng #{donHang.MaDonHang} đã được hệ thống tự động xác nhận nhận hàng sau thời gian chờ.",
                                MucDoQuanTrong = 1, // Thông báo thông thường
                                DuongDanChiTiet = "/DonHang/ChiTiet/" + donHang.MaDonHang,
                                NgayTao = DateTime.Now
                            };
                            db.ThongBaos.Add(thongBaoNguoiMua);

                            var nguoiban = db.NguoiBans.Find(donHang.MaNguoiBan);
                            if(nguoiban != null)
                            {
                                // Thêm thông báo cho người bán
                                var thongBaoNguoiBan = new ThongBao
                                {
                                    MaNguoiDung = nguoiban.MaNguoiDung,
                                    LoaiThongBao = "DonHang",
                                    TieuDe = "Đơn hàng đã được tự động xác nhận",
                                    TinNhan = $"Đơn hàng #{donHang.MaDonHang} đã được hệ thống tự động xác nhận nhận hàng. Thanh toán đã được chuyển vào tài khoản của bạn.",
                                    MucDoQuanTrong = 2, // Thông báo quan trọng
                                    DuongDanChiTiet = "/DonHang/ChiTietDonHangNguoiMua/" + donHang.MaDonHang,
                                    NgayTao = DateTime.Now
                                };
                                db.ThongBaos.Add(thongBaoNguoiBan);
                            }


                            // Thêm thông báo đặc biệt cho đơn hàng COD
                            if (donHang.PhuongThucThanhToan == "COD")
                            {
                                // Thông báo cho người mua về giao dịch COD
                                var thongBaoCODNguoiMua = new ThongBao
                                {
                                    MaNguoiDung = donHang.MaNguoiDung,
                                    LoaiThongBao = "ThanhToan",
                                    TieuDe = "Giao dịch COD đã hoàn thành tự động",
                                    TinNhan = $"Giao dịch thanh toán khi nhận hàng (COD) cho đơn hàng #{donHang.MaDonHang} đã được tự động hoàn thành sau thời gian chờ xác nhận.",
                                    MucDoQuanTrong = 1,
                                    DuongDanChiTiet = "/DonHang/ChiTiet/" + donHang.MaDonHang,
                                    NgayTao = DateTime.Now
                                };
                                db.ThongBaos.Add(thongBaoCODNguoiMua);

                                // Thông báo cho người bán về giao dịch COD
                                var thongBaoCODNguoiBan = new ThongBao
                                {
                                    MaNguoiDung = nguoiban.MaNguoiDung,
                                    LoaiThongBao = "ThanhToan",
                                    TieuDe = "Giao dịch COD đã hoàn thành tự động",
                                    TinNhan = $"Giao dịch thanh toán khi nhận hàng (COD) cho đơn hàng #{donHang.MaDonHang} đã được tự động hoàn thành. Số tiền sẽ được giải ngân vào ví của bạn sau khi trừ phí dịch vụ.",
                                    MucDoQuanTrong = 2,
                                    DuongDanChiTiet = "/DonHang/ChiTietDonHangNguoiMua/" + donHang.MaDonHang,
                                    NgayTao = DateTime.Now
                                };
                                db.ThongBaos.Add(thongBaoCODNguoiBan);
                            }

                            System.Diagnostics.Debug.WriteLine($"Đã tự động xác nhận đơn hàng #{donHang.MaDonHang}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý đơn hàng #{donHang.MaDonHang}: {ex.Message}");
                        }
                    }

                    if (donHangs.Any())
                    {
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi trong dịch vụ tự động xác nhận đơn hàng: " + ex.Message);
            }
            finally
            {
                // Schedule the next check
                lock (_lock)
                {
                    if (!_shuttingDown)
                    {
                        // Check again in 15 minutes
                        System.Threading.Timer timer = null;
                        timer = new System.Threading.Timer(
                            o => { timer.Dispose(); CheckOrders(null); },
                            null,
                            TimeSpan.FromMinutes(15),
                            TimeSpan.FromMilliseconds(-1));
                    }
                }
            }
        }

        public void Stop(bool immediate)
        {
            // Locking here will wait for the check to complete
            lock (_lock)
            {
                _shuttingDown = true;
            }

            HostingEnvironment.UnregisterObject(this);
        }
    }

    public static class OrderAutoConfirmationStarter
    {
        private static OrderAutoConfirmationService _service;

        public static void Start()
        {
            _service = new OrderAutoConfirmationService();
            _service.Start();
        }
    }
}