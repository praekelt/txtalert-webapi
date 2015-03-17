using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TxtAlert.API.Models
{
    public class Appad
    {
        public string Ptd_No { get; set; }
        public double? Visit { get; set; }
        public DateTime? Return_date { get; set; }
        public DateTime? Visit_date { get; set; }
        public string Status { get; set; }
        public string Received_sms { get; set; }
        public string Data_Extraction { get; set; }
        public DateTime? Next_tcb { get; set; }
        public string File_No { get; set; }
        public string Cellphone_number { get; set; }
        public string Facility_name { get; set; }
    }
}