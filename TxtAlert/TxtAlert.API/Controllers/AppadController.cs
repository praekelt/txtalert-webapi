using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data;
using MySql.Data.MySqlClient;
using TxtAlert.API.Models;

namespace TxtAlert.API.Controllers
{
    public class AppadController : ApiController
    {
        readonly string connString = Properties.Settings.Default.ConnectionString;

        [HttpGet, ActionName("DefaultCall")]
        public IEnumerable<Appad> Get()
        {
            MySqlConnection connection = new MySqlConnection(connString);
            MySqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT * FROM txtalertdb.p_appad";

                MySqlDataAdapter adap = new MySqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adap.Fill(ds);

                IEnumerable<Appad> patients = ds.Tables[0].AsEnumerable().Select(x => new Appad
                {
                    Ptd_No = x.Field<string>("Ptd_No"),
                    Visit = x.Field<double>("Visit"),
                    Return_date = x.Field<DateTime?>("Return_date"),
                    Visit_date = x.Field<DateTime?>("Visit_date"),
                    Status = x.Field<string>("Status"),
                    Received_sms = x.Field<string>("Received_sms"),
                    Data_Extraction = x.Field<string>("Data_Extraction"),
                    Next_tcb = x.Field<DateTime?>("Next_tcb"),
                    File_No = x.Field<string>("File_No"),
                    Cellphone_number = x.Field<string>("Cellphone_number"),
                    Facility_name = x.Field<string>("Facility_name")
                });

                return patients;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }
}
