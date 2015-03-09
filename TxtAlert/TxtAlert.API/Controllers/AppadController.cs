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

                return ExecuteQuery(cmd);
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

        [HttpGet]
        public IEnumerable<Appad> PatientList()
        {
            MySqlConnection connection = new MySqlConnection(connString);
            MySqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT 
                                        *
                                    FROM 
                                        txtalertdb.p_appad 
                                    GROUP BY 
                                        Ptd_No";

                return ExecuteQuery(cmd);
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

        [HttpGet]
        public IEnumerable<Appad> ComingVisits(DateTime dateFrom, DateTime dateTo)
        {
            MySqlConnection connection = new MySqlConnection(connString);
            MySqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT 
                                        * 
                                    FROM 
                                        txtalertdb.p_appad 
                                    WHERE 
                                        Next_tcb > NOW() 
                                    AND 
                                        Next_tcb
                                            BETWEEN 
                                                @dateFrom 
                                            AND 
                                                @dateTo";

                cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                cmd.Parameters.AddWithValue("@dateTo", dateTo);

                return ExecuteQuery(cmd);
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

        [HttpGet]
        public IEnumerable<Appad> MissedVisits(DateTime dateFrom, DateTime dateTo)
        {
            string status = "Status = 'M'";
            string dateField = "Next_tcb";

            return GetByStatus(dateFrom, dateTo, status, dateField);
        }

        [HttpGet]
        public IEnumerable<Appad> DoneVisits(DateTime dateFrom, DateTime dateTo)
        {
            string status = "Status = 'A' or Status = 'AE'";
            string dateField = "Return_date";

            return GetByStatus(dateFrom, dateTo, status, dateField);
        }

        // RescheduledVisits returns appointments that were rescheduled:
        // -- We assume that there will be a status equal to 'R' to signify this change
        [HttpGet]
        public IEnumerable<Appad> RescheduledVisits(DateTime dateFrom, DateTime dateTo)
        {
            string status = "Status = 'R'";
            string dateField = "Next_tcb";

            return GetByStatus(dateFrom, dateTo, status, dateField);
        }

        // DeletedVisits returns appointments that were deleted:
        // -- We assume that there will be a status equal to 'D' to signify this change
        [HttpGet]
        public IEnumerable<Appad> DeletedVisits(DateTime dateFrom, DateTime dateTo)
        {
            string status = "Status = 'D'";
            string dateField = "Next_tcb";

            return GetByStatus(dateFrom, dateTo, status, dateField);
        }

        private IEnumerable<Appad> GetByStatus(DateTime dateFrom, DateTime dateTo, string status, string dateField)
        {

            MySqlConnection connection = new MySqlConnection(connString);
            MySqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT 
                                        * 
                                    FROM 
                                        txtalertdb.p_appad 
                                    WHERE 
                                        (" + status + @")
                                    AND 
                                        NOT ISNULL(" + dateField + @")
                                    AND
                                        " + dateField + @"
                                            BETWEEN 
                                                @dateFrom 
                                            AND 
                                                @dateTo
                                    ORDER BY
                                        " + dateField + " DESC";

                cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                cmd.Parameters.AddWithValue("@dateTo", dateTo);

                return ExecuteQuery(cmd);
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

        private IEnumerable<Appad> ExecuteQuery(MySqlCommand cmd)
        {
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
    }
}
