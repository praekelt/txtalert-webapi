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
                return ExecuteVisitQuery(query, null, null);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<PatientList> PatientList()
        {
            if (tables.Count() > 0)
            {
                List<PatientList> patients = new List<PatientList>();
                for (int i = 0; i < tables.Count; ++i)
                {
                    string query = GeneratePatientListQuery(i);
                    patients.Add(ExecutePatientQuery(query, tables[i].Clinic));
                }

                return patients;
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> ComingVisits(string dateFrom, string dateTo)
        {
            if (tables.Count() > 0)
            {
                string filter = @" WHERE 
                                       Next_tcb
                                           BETWEEN 
                                               @dateFrom
                                           AND 
                                               @dateTo
                                   OR 
                                       (
                                           Status IS NULL
                                       AND
                                           Visit_date
                                               BETWEEN
                                                   @dateFrom2
                                               AND
                                                   @dateTo2
                                       )";
                string order = @" ORDER BY 
                                    Ptd_No, 
                                    Visit";

                string query = GenerateQuery(filter, order);
                return ExecuteVisitQuery(query, dateFrom, dateTo);
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
                                       Visit_date
                                           BETWEEN 
                                               @dateFrom
                                           AND 
                                               @dateTo";
                string order = @" ORDER BY
                                    Ptd_No, 
                                    Visit";

                string query = GenerateQuery(filter, order);
                return ExecuteVisitQuery(query, dateFrom, dateTo);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> DoneVisits(string dateFrom, string dateTo)
        {
            if (tables.Count() > 0)
            {
                string filter = @" WHERE 
                                       (
                                            Status = 'A'
                                       AND 
                                            Visit_date
                                                BETWEEN 
                                                    @dateFrom
                                                AND 
                                                    @dateTo
                                       )
                                       OR
                                       (
                                            Status = 'AE'
                                       AND
                                            Return_date
                                                BETWEEN 
                                                    @dateFrom2
                                                AND 
                                                    @dateTo2
                                       )";
                string order = @" ORDER BY
                                    Ptd_No, 
                                    Visit";

                string query = GenerateQuery(filter, order);
                return ExecuteVisitQuery(query, dateFrom, dateTo);
            }

            return null;
        }

        [HttpGet]
        public IEnumerable<Appad> RescheduledVisits(string dateFrom, string dateTo)
        {
            if (tables.Count() > 0)
            {
                string filter = @" WHERE
                                       Visit_date < Next_tcb
                                   AND
                                       Status = 'M'
                                   AND
                                       Next_tcb
                                           BETWEEN
                                               @dateFrom
                                           AND 
                                               @dateTo";
                string order = @" ORDER BY 
                                    Ptd_No, 
                                    Visit";

                string query = GenerateQuery(filter, order);
                return ExecuteVisitQuery(query, dateFrom, dateTo);
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
                return ExecuteVisitQuery(query, dateFrom, dateTo);
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

        private string GeneratePatientListQuery(int index)
        {
            string query = @"SELECT DISTINCT
                                Ptd_No,
                                File_No,
                                Cellphone_number,
                                '" + tables[index].Clinic + @"' AS [Facility_name] 
                             FROM " + tables[index].View;

            return query;
        }

        private DataSet MSSQL_GetDataSet(string query, DateTime? dateFrom, DateTime? dateTo)
        {
            string connString = Properties.Settings.Default.MSSQLConnectionString;
            SqlConnection connection = new SqlConnection(connString);
            SqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = query;

                if (dateFrom.HasValue)
                {
                    cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                }

                if (dateTo.HasValue)
                {
                    cmd.Parameters.AddWithValue("@dateTo", dateTo);
                }

                // Some  queries have two between statements
                if (dateFrom.HasValue)
                {
                    cmd.Parameters.AddWithValue("@dateFrom2", dateFrom);
                }

                if (dateTo.HasValue)
                {
                    cmd.Parameters.AddWithValue("@dateTo2", dateTo);
                }

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

        private DataSet MySQL_GetDataSet(string query, DateTime? dateFrom, DateTime? dateTo)
        {
            string connString = Properties.Settings.Default.MySQLConnectionString;
            MySqlConnection connection = new MySqlConnection(connString);
            MySqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = query;

                if (dateFrom.HasValue)
                {
                    cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                }

                if (dateTo.HasValue)
                {
                    cmd.Parameters.AddWithValue("@dateTo", dateTo);
                }

                // Some  queries have two between statements
                if (dateFrom.HasValue)
                {
                    cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                }

                if (dateTo.HasValue)
                {
                    cmd.Parameters.AddWithValue("@dateTo", dateTo);
                }

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

        private DateTime? ConstructDate(string date, bool fillTime = false)
        {
            if(String.IsNullOrEmpty(date))
            {
                return null;
            }

            string[] parts = date.Split(new char[] { '_' });
            
            if(parts.Length != 3)
            {
                return null;
            }

            DateTime? dt = null;

            try
            {
                int month = 0;
                int year = 0;
                int day = 0;

                if( 
                    int.TryParse(parts[0], out year) &&
                    int.TryParse(parts[1], out month) &&
                    int.TryParse(parts[2], out day)
                    )
                {
                    if(fillTime)
                    {
                        dt = new DateTime(year, month, day, 23, 59, 59, 999);
                    }
                    else
                    {
                        dt = new DateTime(year, month, day);
                    }                    
                }                
            }
            catch(Exception)
            {
                dt = null;
            }

            return dt;
        }

        private IEnumerable<Appad> ExecuteVisitQuery(string query, string dateFrom, string dateTo)
        {
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            DataSet ds;

            DateTime? dFrom = ConstructDate(dateFrom);
            DateTime? dTo = ConstructDate(dateTo, true);

            if(!dFrom.HasValue || !dTo.HasValue)
            {
                return new List<Appad>().AsEnumerable();
            }

            if (useMySQL)
                ds = MySQL_GetDataSet(query, dFrom, dTo);
            else
                ds = MSSQL_GetDataSet(query, dFrom, dTo);

            IEnumerable<Appad> results = ds.Tables[0].AsEnumerable().Select(x => new Appad
            {
                Ptd_No = (x.Table.Columns.Contains("Ptd_No") ? x.Field<string>("Ptd_No") : null),
                Visit = (x.Table.Columns.Contains("Visit") ? x.Field<double?>("Visit") : null),
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

        private PatientList ExecutePatientQuery(string query, string clinic)
        {
            DataSet ds;

            if (useMySQL)
                ds = MySQL_GetDataSet(query, null, null);
            else
                ds = MSSQL_GetDataSet(query, null, null);

            PatientList list = new PatientList();
            list.Facility_name = clinic;

            list.Patients = ds.Tables[0].AsEnumerable().Select(x => new Patient
            {
                Ptd_No = (x.Table.Columns.Contains("Ptd_No") ? x.Field<string>("Ptd_No") : null),
                File_No = (x.Table.Columns.Contains("File_No") ? x.Field<string>("File_No") : null),
                Cellphone_number = (x.Table.Columns.Contains("Cellphone_number") ? x.Field<string>("Cellphone_number") : null)
            });

            return list;
        }
    }
}
