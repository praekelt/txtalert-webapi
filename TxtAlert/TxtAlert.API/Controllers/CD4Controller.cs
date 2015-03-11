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
    public class CD4Controller : ApiController
    {
        readonly string connString = Properties.Settings.Default.ConnectionString;

        [HttpGet, ActionName("DefaultCall")]
        public IEnumerable<CD4> Get()
        {
            MySqlConnection connection = new MySqlConnection(connString);
            MySqlCommand cmd;
            connection.Open();

            try
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT * FROM txtalertdb.p_cd4_recruit";

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

        private IEnumerable<CD4> ExecuteQuery(MySqlCommand cmd)
        {
            MySqlDataAdapter adap = new MySqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            adap.Fill(ds);

            IEnumerable<CD4> results = ds.Tables[0].AsEnumerable().Select(x => new CD4
            {
                LAB_ID = x.Field<string>("LAB_ID"),
                FACILITY = x.Field<string>("FACILITY"),
                CELL_NUM = x.Field<string>("CELL_NUM"),
                CD4_VALUE = x.Field<int?>("CD4_VALUE"),
                ENROLMENT_DATE = x.Field<DateTime?>("ENROLMENT_DATE")
            });

            return results;
        }
    }
}
