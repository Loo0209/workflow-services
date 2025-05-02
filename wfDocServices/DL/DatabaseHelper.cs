using wfDocServices.BL;
using MySql.Data.MySqlClient;
using System.Data;
using Microsoft.Data.SqlClient;
using wfDocServices.Constants;

namespace wfDocServices.DL
{
    public static class DatabaseHelper
    {
        public static IDbConnection OpenConnection()
        {
            UtilityBL utilitylogic = new UtilityBL();

            string dbtype = utilitylogic.ReadConfigFile(ConfigConstants.dbtype);
            string dbhost = utilitylogic.ReadConfigFile(ConfigConstants.dbhost);
            string dbname = utilitylogic.ReadConfigFile(ConfigConstants.dbname);
            string dbuser = utilitylogic.ReadConfigFile(ConfigConstants.dbuser);
            string dbpass = utilitylogic.ReadConfigFile(ConfigConstants.dbpass); // DecryptPassword(utilitylogic.ReadConfigFile("eidb:dbpass"));
            string connectionString = utilitylogic.ReadConfigFile($"ConnectionStrings:{dbtype}");

            connectionString = connectionString.Replace("{Host}", dbhost);
            connectionString = connectionString.Replace("{Name}", dbname);
            connectionString = connectionString.Replace("{User}", dbuser);
            connectionString = connectionString.Replace("{Pass}", dbpass);

            utilitylogic.Dispose();

            switch (dbtype)
            {
                case "MYSQL":
                    return new MySqlConnection(connectionString);

                case "MSSQL":
                    return new SqlConnection(connectionString);

                default:
                    throw new ArgumentException("Invalid database type");
            }
        }
    }
}
