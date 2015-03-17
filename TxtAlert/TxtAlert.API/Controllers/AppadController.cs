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
using System.Data.SqlClient;

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
        readonly bool useMySQL = true;

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

            useMySQL = appSettings["Use_MySQL"].ToLower() == "y";
        }

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
                string query = GeneratePatientListQuery();
                return ExecuteQuery(query, null, null);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> ComingVisits(string dateFrom, string dateTo)
        {
            if (tables.Count() > 0)
            {
                string nullCheck = (useMySQL) ? "NOT ISNULL(Next_tcb)" : "Next_tcb IS NOT NULL";
                string filter = @" WHERE 
                                       " + nullCheck + @"
                                   AND
                                       Next_tcb
                                           BETWEEN 
                                               @dateFrom
                                           AND 
                                               @dateTo";
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
                string nullCheck = (useMySQL) ? "NOT ISNULL(Return_date)" : "Return_date IS NOT NULL";
                string filter = @" WHERE 
                                       " + nullCheck + @" 
                                   AND
                                       Status = 'M'
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
                string nullCheck = (useMySQL) ? "NOT ISNULL(Return_date)" : "Return_date IS NOT NULL";
                string filter = @" WHERE 
                                       " + nullCheck + @"
                                   AND
                                       (Status = 'AE' OR Status = 'A')
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
            if (tables.Count() > 0)
            {
                string filter = @" WHERE
                                       Visit_date < Return_date
                                   AND 
                                       Status <> 'M'
                                   AND
                                       Return_date
                                           BETWEEN
                                               @dateFrom
                                           AND 
                                               @dateTo";
                string order = @" ORDER BY 
                                    Ptd_No, 
                                    Next_tcb DESC";

                string query = GenerateQuery(filter, order);
                return ExecuteQuery(query, dateFrom, dateTo);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> DeletedVisits(string dateFrom, string dateTo)
        {
            if (tables.Count() > 0)
            {
                string nullCheck = (useMySQL) ? "NOT ISNULL(Return_date)" : "Return_date IS NOT NULL";
                string filter = @" WHERE
                                       " + nullCheck + @"
                                   AND 
                                       Status <> 'M'
                                   AND
                                       Visit_date
                                           BETWEEN
                                               @dateFrom
                                           AND 
                                               @dateTo";
                string order = @" ORDER BY 
                                    Ptd_No, 
                                    Next_tcb DESC";

                string query = GenerateQuery(filter, order);
                return ExecuteQuery(query, dateFrom, dateTo);
            }

            return null;
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

        private string GeneratePatientListQuery()
        {
            string query = @"SELECT DISTINCT
                                Ptd_No,
                                File_No,
                                Cellphone_number,
                                '" + tables[0].Clinic + @"' AS [Facility_name] 
                             FROM " + tables[0].View;

            for (int i = 1; i < tables.Count(); i++)
            {
                query += @" UNION ALL SELECT DISTINCT
                                Ptd_No,
                                File_No,
                                Cellphone_number,
                                '" + tables[i].Clinic + @"' AS [Facility_name] 
                             FROM " + tables[i].View;
            }

            return query;
        }

        private DataSet MSSQL_GetDataSet(string query, string dateFrom, string dateTo)
        {
            string connString = Properties.Settings.Default.MSSQLConnectionString;
            SqlConnection connection = new SqlConnection(connString);
            SqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = query;

                if (dateFrom != null)
                    cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                if (dateTo != null)
                    cmd.Parameters.AddWithValue("@dateTo", dateTo);

                SqlDataAdapter adap = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adap.Fill(ds);

                return ds;
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

        private DataSet MySQL_GetDataSet(string query, string dateFrom, string dateTo)
        {
            string connString = Properties.Settings.Default.MySQLConnectionString;
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

                return ds;
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

        private IEnumerable<Appad> ExecuteQuery(string query, string dateFrom, string dateTo)
        {
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            DataSet ds;

            if (useMySQL)
                ds = MySQL_GetDataSet(query, dateFrom, dateTo);
            else
                ds = MSSQL_GetDataSet(query, dateFrom, dateTo);

            IEnumerable<Appad> results = ds.Tables[0].AsEnumerable().Select(x => new Appad
            {
                Ptd_No = (x.Table.Columns.Contains("Ptd_No") ? x.Field<string>("Ptd_No") : null),
                Visit = (x.Table.Columns.Contains("Visit") ? x.Field<double>("Visit") : 0),
                Return_date = (x.Table.Columns.Contains("Return_date") ? x.Field<DateTime?>("Return_date") : null),
                Visit_date = (x.Table.Columns.Contains("Visit_date") ? x.Field<DateTime?>("Visit_date") : null),
                Status = (x.Table.Columns.Contains("Status") ? x.Field<string>("Status") : null),
                Received_sms = (x.Table.Columns.Contains("Received_sms") ? x.Field<string>("Received_sms") : null),
                Data_Extraction = (x.Table.Columns.Contains("Data_Extraction") ? x.Field<string>("Data_Extraction") : null),
                Next_tcb = (x.Table.Columns.Contains("Next_tcb") ? x.Field<DateTime?>("Next_tcb") : null),
                File_No = (x.Table.Columns.Contains("File_No") ? x.Field<string>("File_No") : null),
                Cellphone_number = (x.Table.Columns.Contains("Cellphone_number") ? x.Field<string>("Cellphone_number") : null),
                Facility_name = (x.Table.Columns.Contains("Facility_name") ? x.Field<string>("Facility_name") : null)
            });

            return results;
        }
    }
}
