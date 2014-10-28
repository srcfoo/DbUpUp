using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Text;

namespace DbUp.Engine
{
    public class DatabaseVersion
    {
        private string version;
        private string dbConn;

        public DatabaseVersion(string dbConn) {
            this.dbConn = dbConn;
            this.getVersion();
        }

        public string Version {
            get
            {
                return version;
            }

            set
            {
                version = value;
                this.setVersion(version);
            }
        }

        private string getVersion() {
            using (SqlConnection conn = new SqlConnection(this.dbConn))
            {
                SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 hash FROM SyncHistory.dbo.databaseVersion ORDER BY deployed DESC", conn
                        );
                try
                {
                    conn.Open();
                    this.version = (string)cmd.ExecuteScalar();
                } catch (Exception ex) {
                    Console.WriteLine("Could not retrieve the version from the database. Msg:");
                    Console.WriteLine(ex.Message);
                }
            }
            return this.version;
        }

        private bool setVersion(string hash)
        {
            using(SqlConnection conn = new SqlConnection(this.dbConn))
            {
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO SyncHistory.dbo.databaseVersion (hash,deployed) VALUES (@hash,GETDATE())", conn
                    );
                cmd.Parameters.AddWithValue("@hash", hash);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                } catch (Exception ex) {
                    Console.WriteLine("Could not update the database version. Msg:");
                    Console.WriteLine(ex.Message);
                    return false;
                }

                return true;
            }
        }
    }
}
