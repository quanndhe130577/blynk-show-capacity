using Blynk.Tester.Common;
using Blynk.Tester.Model;
using Blynk.Virtual;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blynk.Tester.Service
{
    public class DataEnergyManage : IJob
    {
        public Client client;

        public DataEnergyManage(Client client)
        {
            this.client = client;
        }

        public DataEnergyManage() { }
        public Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            this.client = (Client)dataMap["client"];

            Task taskA = new Task(() => SetData());
            taskA.Start();
            return taskA;
        }

        public void SetData()
        {
            var defaultDate = DateTime.Now.AddDays(-1).Date;

            DateTime start_Month = new DateTime(defaultDate.Year, defaultDate.Month, 1);
            DateTime start_Year = new DateTime(defaultDate.Year, 1, 1);

            var total_Month_ThucHien_TrucThuoc = 0.0;
            var total_Year_ThucHien_TrucThuoc = 0.0;
            var total_Day_ThucHien_TrucThuoc = 0.0;

            var total_Month_ThucHien_LienKet = 0.0;
            var total_Year_ThucHien_LienKet = 0.0;
            var total_Day_ThucHien_LienKet = 0.0;

            var total_Month_KeHoach_TrucThuoc = 0.0;
            var total_Year_KeHoach_TrucThuoc = 0.0;
            var total_Day_KeHoach_TrucThuoc = 0.0;

            var total_Month_KeHoach_LienKet = 0.0;
            var total_Year_KeHoach_LienKet = 0.0;
            var total_Day_KeHoach_LienKet = 0.0;

            Boolean checkMonth = false;
            Boolean checkDay = false;

            // get data thuc hien
            for (var item = start_Year; item <= defaultDate; item = item.AddDays(1))
            {
                if (item >= start_Month && item <= defaultDate)
                {
                    checkMonth = true;
                }
                else
                {
                    checkMonth = false;
                }

                if (item.Date == defaultDate.Date)
                {
                    checkDay = true;
                }
                else
                {
                    checkDay = false;
                }

                List<LuyKeThucHien> dataThucHienTho = GetDataThucHienThoTheoNgay(item);
                var dataThucHienNMTrucThuoc = dataThucHienTho.Where(c => c.IdLoaiNhaMay == 1).Sum(c => c.GiaTri) / 1000000; //Chuyển đổi dữ liệu từ kwh ra triệu kwh
                var dataThucHienNMLienKet = dataThucHienTho.Where(c => c.IdLoaiNhaMay == 2 || c.IdLoaiNhaMay == 3).Sum(c => c.GiaTri) / 1000000;//Chuyển đổi dữ liệu từ kwh ra triệu kwh

                //Thực hiện năm
                total_Year_ThucHien_TrucThuoc += dataThucHienNMTrucThuoc;
                total_Year_ThucHien_LienKet += dataThucHienNMLienKet;

                if (checkMonth)
                {
                    //Thực hiện tháng
                    total_Month_ThucHien_TrucThuoc += dataThucHienNMTrucThuoc;
                    total_Month_ThucHien_LienKet += dataThucHienNMLienKet;
                }

                if (checkDay)
                {
                    //Thực hiện ngày
                    total_Day_ThucHien_TrucThuoc = dataThucHienNMTrucThuoc;
                    total_Day_ThucHien_LienKet = dataThucHienNMLienKet;
                }
            }

            // get data ke hoach
            for (var item = start_Year; item < start_Year.AddYears(1); item = item.AddMonths(1))
            {
                var dataKeHoachTho = GetDataKeHoachThoTheoThang(item);
                var dataKeHoachNMTrucThuoc = dataKeHoachTho.Where(c => c.IdLoaiNhaMay == 1).Sum(c => c.GiaTri);
                var datakeHoachNMLienKet = dataKeHoachTho.Where(c => c.IdLoaiNhaMay == 2 || c.IdLoaiNhaMay == 3).Sum(c => c.GiaTri);

                // Kế hoạch năm
                total_Year_KeHoach_TrucThuoc += dataKeHoachNMTrucThuoc;
                total_Year_KeHoach_LienKet += datakeHoachNMLienKet;

                if (item.Month == defaultDate.Month && item.Year == defaultDate.Year)
                {
                    // Kế hoạch tháng
                    total_Month_KeHoach_TrucThuoc = dataKeHoachNMTrucThuoc;
                    total_Month_KeHoach_LienKet = datakeHoachNMLienKet;

                    // Kế hoạch ngày
                    total_Day_KeHoach_TrucThuoc = dataKeHoachNMTrucThuoc / DateTime.DaysInMonth(item.Year, item.Month);// chia trung binh moi ngay
                    total_Day_KeHoach_LienKet = datakeHoachNMLienKet / DateTime.DaysInMonth(item.Year, item.Month);// chia trung binh moi ngay
                }
            }


            // Send data to client

            // Trực thuộc - Ngay
            client.WriteVirtualPin(IdClientEnergy.TrucThuoc.Ngay_TH, ConverDoubleToStringFormat(total_Day_ThucHien_TrucThuoc));
            client.WriteVirtualPin(IdClientEnergy.TrucThuoc.Ngay_KH, ConverDoubleToStringFormat(total_Day_KeHoach_TrucThuoc));
            client.WriteVirtualPin(IdClientEnergy.TrucThuoc.Ngay_TiLe, total_Day_ThucHien_TrucThuoc / total_Day_KeHoach_TrucThuoc * 100);

            // Trực thuộc - Thang
            client.WriteVirtualPin(IdClientEnergy.TrucThuoc.Thang_TH, ConverDoubleToStringFormat(total_Month_ThucHien_TrucThuoc));
            client.WriteVirtualPin(IdClientEnergy.TrucThuoc.Thang_KH, ConverDoubleToStringFormat(total_Month_KeHoach_TrucThuoc));
            client.WriteVirtualPin(IdClientEnergy.TrucThuoc.Thang_TiLe, total_Month_ThucHien_TrucThuoc / total_Month_KeHoach_TrucThuoc * 100);

            // Trực thuộc - Nam
            client.WriteVirtualPin(IdClientEnergy.TrucThuoc.Nam_TH, ConverDoubleToStringFormat(total_Year_ThucHien_TrucThuoc));
            client.WriteVirtualPin(IdClientEnergy.TrucThuoc.Nam_KH, ConverDoubleToStringFormat(total_Year_KeHoach_TrucThuoc));
            client.WriteVirtualPin(IdClientEnergy.TrucThuoc.Nam_TiLe, total_Year_ThucHien_TrucThuoc / total_Year_KeHoach_TrucThuoc * 100);

            // Liên kết - Ngay
            client.WriteVirtualPin(IdClientEnergy.LienKet.Ngay_TH, ConverDoubleToStringFormat(total_Day_ThucHien_LienKet));
            client.WriteVirtualPin(IdClientEnergy.LienKet.Ngay_KH, ConverDoubleToStringFormat(total_Day_KeHoach_LienKet));
            client.WriteVirtualPin(IdClientEnergy.LienKet.Ngay_TiLe, total_Day_ThucHien_LienKet / total_Day_KeHoach_LienKet * 100);

            // Liên kết - Thang
            client.WriteVirtualPin(IdClientEnergy.LienKet.Thang_TH, ConverDoubleToStringFormat(total_Month_ThucHien_LienKet));
            client.WriteVirtualPin(IdClientEnergy.LienKet.Thang_KH, ConverDoubleToStringFormat(total_Month_KeHoach_LienKet));
            client.WriteVirtualPin(IdClientEnergy.LienKet.Thang_TiLe, total_Month_ThucHien_LienKet / total_Month_KeHoach_LienKet * 100);

            // Liên kết - Nam
            client.WriteVirtualPin(IdClientEnergy.LienKet.Nam_TH, ConverDoubleToStringFormat(total_Year_ThucHien_LienKet));
            client.WriteVirtualPin(IdClientEnergy.LienKet.Nam_KH, ConverDoubleToStringFormat(total_Year_KeHoach_LienKet));
            client.WriteVirtualPin(IdClientEnergy.LienKet.Nam_TiLe, total_Year_ThucHien_LienKet / total_Year_KeHoach_LienKet * 100);

            // Genco 1 - Ngay
            client.WriteVirtualPin(IdClientEnergy.Genco_1.Ngay_TH, ConverDoubleToStringFormat(total_Day_ThucHien_TrucThuoc + total_Day_ThucHien_LienKet));
            client.WriteVirtualPin(IdClientEnergy.Genco_1.Ngay_KH, ConverDoubleToStringFormat(total_Day_KeHoach_TrucThuoc + total_Day_KeHoach_LienKet));
            client.WriteVirtualPin(IdClientEnergy.Genco_1.Ngay_TiLe, (total_Day_ThucHien_TrucThuoc + total_Day_ThucHien_LienKet) / (total_Day_KeHoach_TrucThuoc + total_Day_KeHoach_LienKet) * 100);

            // Genco 1 - Thang
            client.WriteVirtualPin(IdClientEnergy.Genco_1.Thang_TH, ConverDoubleToStringFormat(total_Month_ThucHien_TrucThuoc + total_Month_ThucHien_LienKet));
            client.WriteVirtualPin(IdClientEnergy.Genco_1.Thang_KH, ConverDoubleToStringFormat(total_Month_KeHoach_TrucThuoc + total_Month_KeHoach_LienKet));
            client.WriteVirtualPin(IdClientEnergy.Genco_1.Thang_TiLe, (total_Month_ThucHien_TrucThuoc + total_Month_ThucHien_LienKet) / (total_Month_KeHoach_TrucThuoc + total_Month_KeHoach_LienKet) * 100);

            // Genco 1 - Nam
            client.WriteVirtualPin(IdClientEnergy.Genco_1.Nam_TH, ConverDoubleToStringFormat(total_Year_ThucHien_TrucThuoc + total_Year_ThucHien_LienKet));
            client.WriteVirtualPin(IdClientEnergy.Genco_1.Nam_KH, ConverDoubleToStringFormat(total_Year_KeHoach_TrucThuoc + total_Year_KeHoach_LienKet));
            client.WriteVirtualPin(IdClientEnergy.Genco_1.Nam_TiLe, (total_Year_ThucHien_TrucThuoc + total_Year_ThucHien_LienKet) / (total_Year_KeHoach_TrucThuoc + total_Year_KeHoach_LienKet) * 100);
        }

        private string ConverDoubleToStringFormat(double n)
        {
            return String.Format("{0:0,0.0}", n);
        }

        private List<LuyKeKeHoach> GetDataKeHoachThoTheoThang(DateTime dt)
        {
            List<LuyKeKeHoach> listLKKH = new List<LuyKeKeHoach>();

            using (SqlConnection connection = new SqlConnection(ConnectionString.ConnectString_223))
            {
                string query = "select dk.Nam, dk.Thang, dk.SanLuongDauCucKeHoach, dk.UnitId, dv.IdLoaiDonVi " +
                    "from dbo.DoanhThuDuKien dk " +
                    "join ( select UnitIdDonVi, IdDonVi from dbo.HeThongNhaMay group by UnitIdDonVi, IdDonVi ) as nm on dk.UnitId = nm.UnitIdDonVi " +
                    "join dbo.HeThongDonVi dv on nm.IdDonVi = dv.MaDonVi " +
                    "where dk.Nam = " + dt.Year + " and dk.Thang = " + dt.Month + " and dk.LoaiChiTieu = 2 " +
                    "order by dk.Thang desc";
                SqlCommand command = new SqlCommand(query, connection);
                command.Connection.Open();

                SqlDataReader rdr = command.ExecuteReader();
                while (rdr.Read())
                {
                    LuyKeKeHoach lkkh = new LuyKeKeHoach();
                    lkkh.Nam = rdr.GetInt32(0);
                    lkkh.Thang = rdr.GetInt32(1);
                    var cs = rdr[2].ToString();
                    lkkh.GiaTri = (cs == "") ? 0 : double.Parse(cs);
                    lkkh.UnitId = rdr.GetInt32(3);
                    lkkh.IdLoaiNhaMay = rdr.GetInt32(4);

                    listLKKH.Add(lkkh);
                }

                command.Connection.Close();
                return listLKKH;
            }
        }
        private List<LuyKeThucHien> GetDataThucHienThoTheoNgay(DateTime dt)
        {
            List<LuyKeThucHien> listLKTH = new List<LuyKeThucHien>();

            using (SqlConnection connection = new SqlConnection(ConnectionString.ConnectString_223))
            {
                string query = "select sl.ChuKy, sl.GiaTri, sl.IdLoaiSanLuong, sl.IdToMay, dv.IdLoaiDonVi, sl.ThoiGian " +
                    "from dbo.TTDSanLuongToMayNgay sl join dbo.HeThongToMay tm on sl.IdToMay = tm.IdToMay " +
                    "join dbo.HeThongNhaMay nm on tm.IdNhaMay = nm.IdNhaMay " +
                    "join dbo.HeThongDonVi dv on nm.IdDonVi = dv.MaDonVi " +
                    "where sl.ThoiGian = '" + dt.Date.ToString("yyyy-MM-dd") + "' and sl.IdLoaiSanLuong = 1";
                SqlCommand command = new SqlCommand(query, connection);
                command.Connection.Open();

                SqlDataReader rdr = command.ExecuteReader();
                while (rdr.Read())
                {
                    LuyKeThucHien lkth = new LuyKeThucHien();
                    lkth.ChuKy = rdr.GetInt32(0);
                    var cs = rdr[1].ToString();
                    lkth.GiaTri = (cs == "") ? 0 : double.Parse(cs);
                    lkth.IdLoaiSanLuong = rdr.GetInt32(2);
                    lkth.IdToMay = rdr.GetInt32(3);
                    lkth.IdLoaiNhaMay = rdr.GetInt32(4);
                    lkth.ThoiGian = rdr.GetDateTime(5);

                    listLKTH.Add(lkth);
                }

                command.Connection.Close();
                return listLKTH;
            }
        }
    }
}
