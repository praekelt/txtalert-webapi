using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data;
using MySql.Data.MySqlClient;
using TxtAlert.API.Models;
using System.Collections.Specialized;
using System.Configuration;

namespace TxtAlert.API.Controllers
{
    public class AppadController : ApiController
    {
        private class ViewObject
        {
            public string Clinic { get; set; }
            public string View { get; set; }
        }

        List<ViewObject> tables = new List<ViewObject>();

        private AppadController()
        {
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            string[] keys = appSettings.AllKeys;

            foreach (string key in keys)
            {
                int index = key.IndexOf("Clinic_");
                if (index >= 0)
                {
                    string name = key.Replace("Clinic_", "");
                    tables.Add(new ViewObject
                    {
                        Clinic = name,
                        View = appSettings[key]
                    });
                }
            }
        }

        readonly string connString = Properties.Settings.Default.ConnectionString;

        [HttpGet, ActionName("DefaultCall")]
        public IEnumerable<Appad> Get()
        {
            if (tables.Count() > 0)
            {
                string query = GenerateQuery("", "");
                return ExecuteQuery(query, null, null);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> PatientList()
        {
            if (tables.Count() > 0)
            {
                string filter = @" GROUP BY Ptd_No";

                string query = GenerateQuery(filter, "");
                return ExecuteQuery(query, null, null);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> ComingVisits(string dateFrom, string dateTo)
        {
            if (tables.Count() > 0)
            {
                string filter = @" WHERE 
                                       NOT ISNULL(Next_tcb)
                                   AND
                                       Next_tcb
                                           BETWEEN 
                                               @dateFrom
                                           AND 
                                               @dateTo
                                   GROUP BY 
                                       Ptd_No";
                string order = @" ORDER BY 
                                    Next_tcb DESC";

                string query = GenerateQuery(filter, order);
                return ExecuteQuery(query, dateFrom, dateTo);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> MissedVisits(string dateFrom, string dateTo)
        {
            if (tables.Count() > 0)
            {
                string filter = @" WHERE 
                                       Status = 'M'
                                   AND 
                                       NOT ISNULL(Return_date)
                                   AND
                                       Return_date
                                           BETWEEN 
                                               @dateFrom
                                           AND 
                                               @dateTo";
                string order = @" ORDER BY
                                    Return_date DESC";

                string query = GenerateQuery(filter, order);
                return ExecuteQuery(query, dateFrom, dateTo);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> DoneVisits(string dateFrom, string dateTo)
        {
            if (tables.Count() > 0)
            {
                string filter = @" WHERE 
                                       (Status = 'AE' OR Status = 'A')
                                   AND 
                                       NOT ISNULL(Return_date)
                                   AND
                                       Return_date
                                           BETWEEN 
                                               @dateFrom
                                           AND 
                                               @dateTo";
                string order = @" ORDER BY
                                    Return_date DESC";

                string query = GenerateQuery(filter, order);
                return ExecuteQuery(query, dateFrom, dateTo);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> RescheduledVisits(string dateFrom, string dateTo)
        {
            string query = @"SELECT
                                * 
                            FROM 
                                p_appad
                            WHERE
                                Visit_date < Return_date
                            AND 
                                Status <> 'M'
                            AND
                                Return_date
                                    BETWEEN
                                        @dateFrom
                                    AND 
                                        @dateTo
                            ORDER BY 
                                Ptd_No, 
                                Next_tcb DESC";

            return ExecuteQuery(query, dateFrom, dateTo);
        }

        [HttpGet]
        public IEnumerable<Appad> DeletedVisits(string dateFrom, string dateTo)
        {
            string query = @"SELECT
                                * 
                            FROM 
                                p_appad
                            WHERE
                                ISNULL(Return_date)
                            AND 
                                Status <> 'M'
                            AND
                                Visit_date
                                    BETWEEN
                                        @dateFrom
                                    AND 
                                        @dateTo
                            ORDER BY 
                                Ptd_No, 
                                Next_tcb DESC";

            return ExecuteQuery(query, dateFrom, dateTo);
        }

        private string GenerateQuery(string filter, string order)
        {
            string query = @"SELECT 
                                    *,
                                    '" + tables[0].Clinic + @"' AS Facility_name
                                FROM " + tables[0].View + filter;

            for (int i = 1; i < tables.Count(); i++)
            {
                query += @" UNION ALL SELECT 
                                    *,
                                    '" + tables[i].Clinic + @"' AS Facility_name
                                FROM " + tables[i].View + filter;
            }

            query += order;

            return query;
        }

        private IEnumerable<Appad> ExecuteQuery(string query, string dateFrom, string dateTo)
        {
            MySqlConnection connection = new MySqlConnection(connString);
            MySqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = query;

                cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                cmd.Parameters.AddWithValue("@dateTo", dateTo);

                MySqlDataAdapter adap = new MySqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adap.Fill(ds);

                IEnumerable<Appad> results = ds.Tables[0].AsEnumerable().Select(x => new Appad
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

                return results;
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
