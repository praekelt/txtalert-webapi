using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TxtAlert.API.Models
{
    public class PatientList
    {
        public string Facility_name { get; set; }
        public IEnumerable<Patient> Patients { get; set; }
    }
}