using Blynk.Tester.Common;
using Blynk.Tester.Model;
using Blynk.Virtual;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace Blynk.Tester.Service
{
    public class DataGenerationManage
    {
        public Client client;

        public DataGenerationManage(Client client)
        {
            this.client = client;
        }
        private List<NhaMay> GetListNhaMay()
        {
            try
            {
                List<NhaMay> listNhaMay = new List<NhaMay>();
                using (StreamReader sr = File.OpenText("Config.json"))
                {
                    var obj = sr.ReadToEnd();
                    listNhaMay = JsonConvert.DeserializeObject<List<NhaMay>>(obj);
                }
                return listNhaMay;
            }
            catch
            {
                return null;
            }
        }

        public void SetData()
        {
            List<NhaMay> listNhaMay = GetListNhaMay();
            List<CongSuatRealtime> listCSLT = GetDataRealTime();
            var congSuatTong = 0.0;
            foreach (var item in listNhaMay)
            {
                var tongCsNhaMay = 0.0;
                client.WriteVirtualPin(item.IdClientTenNhaMay, item.TenClientNhaMay);
                foreach (var subItem in item.listToMay)
                {
                    client.WriteVirtualPin(subItem.IdClientTenToMay, subItem.TenClientToMay);
                    var congSuat = listCSLT.Where(x => x.IdToMay == subItem.IdToMayDB).Select(x => x.CongSuat).FirstOrDefault();
                    var rs = 0.0;
                    if (congSuat != null)
                    {
                        rs = double.Parse(congSuat.ToString());
                    }
                    else
                    {
                        var maToMay = GetMaToMayByIdToMay(subItem.IdToMayDB);
                        if (maToMay != null)
                        {
                            var dataLenhDiem = GetCongSuatLenhDim(subItem.IdToMayDB, maToMay);
                            rs = dataLenhDiem != -1 ? dataLenhDiem : 0;
                        }
                    }
                    tongCsNhaMay += rs >= 0 ? rs : 0;
                    client.WriteVirtualPin(subItem.IdClientValueToMay, rs >= 0 ? rs : 0);
                }
                client.WriteVirtualPin(item.IdClientValueTotal, tongCsNhaMay);
                congSuatTong += tongCsNhaMay;
            }
            client.WriteVirtualPin(0, congSuatTong);
        }


        

        private string GetListTagName()
        {
            string listTagName = "";

            using (SqlConnection connection = new SqlConnection(ConnectionString.ConnectString_223))
            {
                string query = "Select DISTINCT nm.TenTatNM+'_'+tm.TagScada+'_P' from HeThongNhaMay nm join HeThongToMay tm " +
                    "on nm.IdNhaMay = tm.IdNhaMay and tm.TagScada is not null";
                /*string query = "Select DISTINCT nm.TenTatNM, tm.TagScada, tm.TenToMay " +
                    "from HeThongNhaMay nm join HeThongToMay tm " +
                    "on nm.IdNhaMay = tm.IdNhaMay where tm.TenToMay is not null or tm.TagScada is not null";*/
                SqlCommand command = new SqlCommand(query, connection);
                command.Connection.Open();

                SqlDataReader rdr = command.ExecuteReader();
                while (rdr.Read())
                {
                    listTagName += "('" + rdr.GetString(0) + "'),";
                }
                listTagName = listTagName.Substring(0, listTagName.Length - 1);
                command.Connection.Close();
            }

            return listTagName;
        }

        private List<CongSuatRealtime> GetDataRealTime()
        {
            string listTagName = GetListTagName();
            List<CongSuatRealtime> listCSLT = new List<CongSuatRealtime>();
            List<NhaMayToMay> listNhaMayToMay = GetListNhaMayToMay();

            string query = "SET NOCOUNT ON DECLARE @TempTable TABLE(Seq INT IDENTITY, tempTagName NVARCHAR(256))" +
                "INSERT @TempTable(tempTagName) VALUES " + listTagName + @"
                SELECT v_AnalogLive.TagName, DateTime, Value, MinRaw = ISNULL(Cast(AnalogTag.MinRaw as VarChar(20)), 'N/A'), MaxRaw = ISNULL(Cast(AnalogTag.MaxRaw as VarChar(20)), 'N/A'), Quality, QualityDetail = v_AnalogLive.QualityDetail, QualityString
                FROM v_AnalogLive
                LEFT JOIN @TempTable ON TagName = tempTagName
                LEFT JOIN AnalogTag ON AnalogTag.TagName = v_AnalogLive.TagName
                LEFT JOIN QualityMap ON QualityMap.QualityDetail = v_AnalogLive.QualityDetail
                WHERE v_AnalogLive.TagName IN(" + listTagName + @")
                ORDER BY Seq";

            using (SqlConnection connection = new SqlConnection(ConnectionString.ConnectString_98))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Connection.Open();

                SqlDataReader rdr = command.ExecuteReader();
                while (rdr.Read())
                {
                    var TDHT = rdr[1].ToString();
                    var tenNhaMayandTagName = rdr[0].ToString().Split('_')[0] + "_" + rdr[0].ToString().Split('_')[1];
                    var CS = rdr[2].ToString();
                    CongSuatRealtime csrt = new CongSuatRealtime();
                    csrt.CongSuat = (CS == "") ? (decimal?)null : decimal.Parse(CS);
                    csrt.TenTatNM = rdr[0].ToString().Split('_')[0];
                    csrt.ThoiGian = Convert.ToDateTime(TDHT);
                    csrt.StrThoiGian = Convert.ToDateTime(TDHT).ToString("dd/MM/yyyy HH:mm:ss");
                    csrt.QualityDetail = int.Parse(rdr[6].ToString());
                    csrt.IdToMay = listNhaMayToMay.Where(x => x.TenNhaMayandTagNameToMay == tenNhaMayandTagName).Select(x => x.IdToMay).FirstOrDefault();
                    csrt.IdNhaMay = listNhaMayToMay.Where(x => x.TenNhaMayandTagNameToMay == tenNhaMayandTagName).Select(x => x.IdNhaMay).FirstOrDefault();
                    csrt.TenToMay = listNhaMayToMay.Where(x => x.TenNhaMayandTagNameToMay == tenNhaMayandTagName).Select(x => x.TenToMay).FirstOrDefault();
                    csrt.MaToMay = listNhaMayToMay.Where(x => x.TenNhaMayandTagNameToMay == tenNhaMayandTagName).Select(x => x.MaToMay).FirstOrDefault();
                    csrt.TagScada = string.IsNullOrEmpty(rdr[0].ToString()) ? "" : rdr.GetString(0).Split('_')[0];

                    listCSLT.Add(csrt);
                }

                /*foreach (var item in listCSLT)
                {
                    if (item.CongSuat == null)
                    {
                        item.CongSuat = decimal.Parse(GetCongSuatLenhDim(item.IdToMay, item.MaToMay).ToString());
                    }
                }*/
                command.Connection.Close();
            }

            return listCSLT;
        }

        private List<NhaMayToMay> GetListNhaMayToMay()
        {
            List<NhaMayToMay> listNhaMayToMay = new List<NhaMayToMay>();
            using (SqlConnection connection = new SqlConnection(ConnectionString.ConnectString_223))
            {
                string query = "select nm.TenTatNM+'_'+tm.TagScada as TenNhaMayandTagNameToMay, tm.IdToMay, nm.IdNhaMay, tm.TenToMay, tm.MaToMay " +
                    "from dbo.HeThongNhaMay nm join dbo.HeThongToMay tm on nm.IdNhaMay = tm.IdNhaMay where tm.TagScada is not null";
                SqlCommand command = new SqlCommand(query, connection);
                command.Connection.Open();

                SqlDataReader rdr = command.ExecuteReader();
                while (rdr.Read())
                {
                    NhaMayToMay nmtm = new NhaMayToMay();
                    nmtm.TenNhaMayandTagNameToMay = rdr.GetString(0);
                    nmtm.IdToMay = rdr.GetInt32(1);
                    nmtm.IdNhaMay = rdr.GetInt32(2);
                    nmtm.TenToMay = rdr.GetString(3);
                    nmtm.MaToMay = rdr.GetString(4);

                    listNhaMayToMay.Add(nmtm);
                }
                command.Connection.Close();
            }
            return listNhaMayToMay;
        }

        private double GetCongSuatLenhDim(int idToMay, string maToMay)
        {
            var date_now = DateTime.Now.Date;
            TimeSpan timeOfDay = DateTime.Now.TimeOfDay;
            int hour = timeOfDay.Hours + 1;

            using (SqlConnection connection = new SqlConnection(ConnectionString.ConnectString_223))
            {
                var soChuKy = LayCauHinh(date_now.Date);
                var rs = hour;
                if (soChuKy != 24)
                {
                    rs = 2 * hour;
                }
                string query = "select top 1 ct.GiaTri " +
                    "from dbo.HeThongNhaMay nm join dbo.HeThongToMay tm on nm.IdNhaMay = tm.IdNhaMay " +
                    "join dbo.TTDKeHoachVanHanhGioToiTongQuat tq on tm.IdToMay = tq.IdToMay " +
                    "join dbo.TTDKeHoachVanHanhGioToiChiTiet ct on tq.Id = ct.IdKeHoachVanHanhGioToiTongQuat " +
                    "where tq.Ngay = '" + DateTime.Now.ToString("yyyy-MM-dd") + "' and tq.IdToMay = " + idToMay + " and ct.Chuky = " + rs;
                SqlCommand command = new SqlCommand(query, connection);
                command.Connection.Open();

                SqlDataReader rdr = command.ExecuteReader();
                if (!rdr.HasRows)
                {
                    var query_1 = "select top 1 a.Giatri_So_HoanThanh  " +
                        "from dbo.NK_Lenh a where a.IDTB = '" + maToMay + "' " +
                        "order by a.Thoidiem_Hoanthanh desc";
                    command.CommandText = query_1;
                    rdr = command.ExecuteReader();
                    while (rdr.Read())
                    {
                        return Math.Truncate(double.Parse(rdr.GetDecimal(0).ToString()) * 10) / 10;
                    }
                }
                while (rdr.Read())
                {
                    return Math.Truncate(double.Parse(rdr.GetDecimal(0).ToString()) * 10) / 10;
                }
                command.Connection.Close();
                return -1;
            }
        }

        private int LayCauHinh(DateTime ngayXet)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString.ConnectString_223))
                {
                    string query = "select x.SoChuKy from dbo.CauHinh x where x.NgayBatDau <= '" + ngayXet + "' and (x.NgayKetThuc is null or x.NgayKetThuc >= '" + ngayXet + "')";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Connection.Open();

                    SqlDataReader rdr = command.ExecuteReader();
                    while (rdr.Read())
                    {
                        return rdr.GetInt32(0);
                    }
                    command.Connection.Close();
                    return 24;
                }
            }
            catch (Exception ex)
            {
                return 24;
            }
        }
        private string GetMaToMayByIdToMay(int idToMay)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString.ConnectString_223))
            {
                string query = "select tm.MaToMay from dbo.HeThongToMay tm where tm.IdToMay = " + idToMay;
                SqlCommand command = new SqlCommand(query, connection);
                command.Connection.Open();

                SqlDataReader rdr = command.ExecuteReader();
                while (rdr.Read())
                {
                    return rdr.GetString(0);
                }
                command.Connection.Close();
                return null;
            }
        }
    }
}
