using System;
using System.Collections.Generic;
using System.Text;

namespace Blynk.Tester.Model
{
    public class NhaMay
    {
        public string TenClientNhaMay { get; set; }
        public int IdClientTenNhaMay { get; set; }
        public int IdClientValueTotal { get; set; }
        public List<ToMay> listToMay { get; set; }
    }
}
