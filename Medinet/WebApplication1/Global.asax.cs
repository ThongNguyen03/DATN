using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using WebApplication1.Models;

namespace WebApplication1
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);


            // Cấu hình connection pooling
            string connString = ConfigurationManager.ConnectionStrings["MedinetDATN"].ConnectionString;
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connString);

            // Giới hạn số kết nối tối đa trong pool
            builder.MaxPoolSize = 100;

            // Thời gian timeout kết nối (giây)
            builder.ConnectTimeout = 30;  // Sửa từ ConnectionTimeout thành ConnectTimeout

            Database.SetInitializer<MedinetDATN>(null);

            // Thiết lập command timeout mặc định
            var objectContext = ((IObjectContextAdapter)new MedinetDATN()).ObjectContext;
            objectContext.CommandTimeout = 300; // 5 phút
        }
    }
}
