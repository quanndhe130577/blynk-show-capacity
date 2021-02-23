using System;
using System.Collections.Generic;
using System.Text;

namespace Blynk.Tester.Model
{
    public class LuyKeThucHien
    {
        public int ChuKy { get; set; }
        public double GiaTri { get; set; }// kwh 
        public int IdLoaiSanLuong { get; set; }
        public int IdToMay { get; set; }
        public int IdLoaiNhaMay { get; set; }
        public DateTime ThoiGian { get; set; }
    }
}
