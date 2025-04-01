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