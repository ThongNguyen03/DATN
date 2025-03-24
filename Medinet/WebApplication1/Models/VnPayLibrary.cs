using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WebApplication1.Models
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string queryString = data.ToString();

            // Loại bỏ dấu & cuối cùng trước khi tạo URL
            if (queryString.Length > 0)
            {
                queryString = queryString.Substring(0, queryString.Length - 1);
            }

            baseUrl += "?" + queryString;
            string signData = queryString;

            string vnp_SecureHash = VnPayUtil.HmacSHA512(vnp_HashSecret, signData);
            baseUrl += "&vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            try
            {
                string rspRaw = GetResponseData();
                string myChecksum = VnPayUtil.HmacSHA512(secretKey, rspRaw);

                // Kiểm tra log
                System.Diagnostics.Debug.WriteLine("Expected hash: " + inputHash);
                System.Diagnostics.Debug.WriteLine("Calculated hash: " + myChecksum);

                return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Error in ValidateSignature: " + ex.Message);
                return false;
            }
        }

        private string GetResponseData()
        {
            StringBuilder data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }
            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }

            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            // Loại bỏ dấu & cuối cùng
            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }

    public class VnPayUtil
    {
        public static string HmacSHA512(string key, string inputData)
        {
            try
            {
                var hash = new StringBuilder();
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
                using (var hmac = new HMACSHA512(keyBytes))
                {
                    byte[] hashValue = hmac.ComputeHash(inputBytes);
                    foreach (var theByte in hashValue)
                    {
                        hash.Append(theByte.ToString("x2"));
                    }
                }

                return hash.ToString();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Error in HmacSHA512: " + ex.Message);
                throw; // Ném lại lỗi để xử lý ở cấp cao hơn
            }
        }

        public static string GetIpAddress()
        {
            string ipAddress = "127.0.0.1"; // Giá trị mặc định
            try
            {
                ipAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (string.IsNullOrEmpty(ipAddress) || (ipAddress.ToLower() == "unknown") || ipAddress.Length > 45)
                {
                    ipAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }

                // Nếu chuỗi IP chứa nhiều địa chỉ (cách nhau bởi dấu phẩy), lấy địa chỉ đầu tiên
                if (!string.IsNullOrEmpty(ipAddress) && ipAddress.Contains(","))
                {
                    ipAddress = ipAddress.Split(',')[0].Trim();
                }

                // Kiểm tra định dạng IP hợp lệ
                if (string.IsNullOrEmpty(ipAddress) || !IsValidIpAddress(ipAddress))
                {
                    ipAddress = "127.0.0.1";
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Error in GetIpAddress: " + ex.Message);
                ipAddress = "127.0.0.1";
            }

            return ipAddress;
        }

        private static bool IsValidIpAddress(string ipAddress)
        {
            // Kiểm tra định dạng IPv4
            IPAddress ip;
            return IPAddress.TryParse(ipAddress, out ip);
        }
    }
}