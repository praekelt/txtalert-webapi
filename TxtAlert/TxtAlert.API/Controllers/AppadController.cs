﻿using System;
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
            string query = @"SELECT * FROM txtalertdb.p_appad";
            return ExecuteQuery(query, null, null);
        }

        [HttpGet]
        public IEnumerable<Appad> PatientList()
        {
            string query = @"SELECT 
                                *
                            FROM 
                                txtalertdb.p_appad 
                            GROUP BY 
                                Ptd_No";

            return ExecuteQuery(query, null, null);
        }

        [HttpGet]
        public IEnumerable<Appad> ComingVisits(DateTime? dateFrom, DateTime? dateTo)
        {
            string query = @"SELECT 
                                * 
                            FROM 
                                p_appad
                            WHERE 
                                NOT ISNULL(Next_tcb)
                            AND
                                Next_tcb
                                    BETWEEN 
                                        @dateFrom
                                    AND 
                                        @dateTo
                            GROUP BY 
                                Ptd_No
                            ORDER BY 
                                Next_tcb DESC";

            return ExecuteQuery(query, dateFrom, dateTo);
        }

        [HttpGet]
        public IEnumerable<Appad> MissedVisits(DateTime? dateFrom, DateTime? dateTo)
        {
            string query = @"SELECT 
                                * 
                            FROM 
                                txtalertdb.p_appad 
                            WHERE 
                                Status = 'M'
                            AND 
                                NOT ISNULL(Return_date)
                            AND
                                Return_date
                                    BETWEEN 
                                        @dateFrom
                                    AND 
                                        @dateTo
                            ORDER BY
                                Return_date DESC";

            return ExecuteQuery(query, dateFrom, dateTo);
        }

        [HttpGet]
        public IEnumerable<Appad> DoneVisits(DateTime? dateFrom, DateTime? dateTo)
        {
            string query = @"SELECT 
                                * 
                            FROM 
                                txtalertdb.p_appad 
                            WHERE 
                                (Status = 'AE' OR Status = 'A')
                            AND 
                                NOT ISNULL(Return_date)
                            AND
                                Return_date
                                    BETWEEN 
                                        @dateFrom
                                    AND 
                                        @dateTo
                            ORDER BY
                                Return_date DESC";

            return ExecuteQuery(query, dateFrom, dateTo);
        }

        [HttpGet]
        public IEnumerable<Appad> RescheduledVisits(DateTime? dateFrom, DateTime? dateTo)
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
        public IEnumerable<Appad> DeletedVisits(DateTime? dateFrom, DateTime? dateTo)
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

        private IEnumerable<Appad> ExecuteQuery(string query, DateTime? dateFrom, DateTime? dateTo)
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
