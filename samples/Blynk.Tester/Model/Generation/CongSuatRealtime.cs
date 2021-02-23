using System;
using System.Collections.Generic;
using System.Text;

namespace Blynk.Tester.Model
{
    public class CongSuatRealtime
    {
        public decimal? CongSuat { get; set; }
        public DateTime? ThoiGian { get; set; }
        public string StrThoiGian { get; set; }
        public string TagScada { get; set; }
        public string TenTatNM { get; set; }
        public string TenToMay { get; set; }
        public int IdToMay { get; set; }
        public string MaToMay { get; set; }
        public int IdNhaMay { get; set; }
        public int? QualityDetail { get; set; }
    }
}
