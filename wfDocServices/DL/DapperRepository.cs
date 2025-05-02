using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using Dapper;
using Newtonsoft.Json;
using NLog;
using wfDocServices.BL;
using wfDocServices.Constants;
using wfDocServices.DL;
using wfDocServices.Models;

namespace wfDocServices.DL
{
    public class DapperRepository<T> : IDisposable where T : class
    {
        private readonly string _tableName;
        private IDbConnection _connection;
        private IDbTransaction _transaction;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public DapperRepository()
        {
            var type = typeof(T);
            var tableAttr = type.GetCustomAttribute<TableAttribute>();

            _tableName = tableAttr != null ? tableAttr.Name : type.Name;
            _connection = DatabaseHelper.OpenConnection();

        }

        public bool TestConnection()
        {
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    // Execute a simple query to check if the connection is successful
                    var result = db.QueryFirstOrDefault<int>("SELECT 1");
                    return result == 1; // If the query returns 1, connection is successful
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error testing database connection: {ex.Message}");
                return false;
            }
        }

        public void BeginTransaction()
        {
            try
            {
                _transaction = _connection.BeginTransaction();
            }
            catch (Exception ex)
            {
                logger.Error("BeginTransaction : " + ex.Message);
            }
        }

        public void CommitTransaction()
        {
            try
            {
                _transaction?.Commit();
                _transaction = null;
            }
            catch (Exception ex)
            {
                logger.Error("CommitTransaction : " + ex.Message);
            }
        }

        public void RollbackTransaction()
        {
            try
            {
                _transaction?.Rollback();
                _transaction = null;
            }
            catch (Exception ex)
            {
                logger.Error("RollbackTransaction : " + ex.Message);
            }
        }

        public IEnumerable<T> GetAll(IEnumerable<FilterOption> whereParams = null)
        {
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    string query = $"SELECT * FROM {_tableName}";
                    var queryParams = new DynamicParameters();
                    if (whereParams != null && whereParams.Any())
                    {
                        string whereClause = string.Join(" AND ", whereParams.Where(c => c.Value != null && c.Operator != null).GroupBy(c => c.Key)
                                            .SelectMany(group => group.Select((c, index) => GetWhereClause(c, index > 0 ? index.ToString() : ""))));
                        if (whereClause != "")
                        {
                            query += $" WHERE {whereClause}";
                        }

                        // Add parameters with dynamic indexing to prevent duplicates
                        var paramCounter = new Dictionary<string, int>();

                        foreach (var filter in whereParams.Where(c => c.Value != null && c.Operator != null))
                        {
                            string paramName = filter.Key;

                            // Increment index if key already exists
                            if (paramCounter.ContainsKey(paramName))
                            {
                                paramCounter[paramName]++;
                                paramName += paramCounter[paramName].ToString();
                            }
                            else
                            {
                                paramCounter[paramName] = 0;
                            }

                            // Add the indexed parameter to DynamicParameters
                            if (filter.Operator.ToLower() == "like")
                            {
                                queryParams.Add($"@{paramName}", $"{filter.Value}%");
                            }
                            else if (filter.Operator.ToLower() == "match")
                            {
                                queryParams.Add($"@{paramName}", $"%{filter.Value}%");
                            }
                            else
                            {
                                queryParams.Add($"@{paramName}", filter.Value);
                            }
                        }
                    }
                    if (whereParams != null && whereParams.Any())
                    {
                        return db.Query<T>(query, queryParams);
                    }
                    else
                    {
                        return db.Query<T>(query);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("GetAll : " + ex.Message);
                return null;
            }
        }
        public IEnumerable<T> GetAllWithPaging(IEnumerable<FilterOption> whereParams = null, int pageNumber = 0, int pageSize = 0, string orderby = "(SELECT NULL)")
        {
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    string query = $"SELECT * FROM {_tableName}";
                    var queryParams = new DynamicParameters();

                    if (whereParams != null && whereParams.Any())
                    {
                        string whereClause = string.Join(" AND ", whereParams.Where(c => c.Value != null && c.Operator != null && !c.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase))
                                            .GroupBy(c => c.Key)
                                            .SelectMany(group => group.Select((c, index) => GetWhereClause(c, index > 0 ? index.ToString() : ""))));

                        if (!string.IsNullOrEmpty(whereClause))
                        {
                            query += $" WHERE {whereClause}";
                        }

                        // Add parameters with dynamic indexing to prevent duplicates
                        var paramCounter = new Dictionary<string, int>();

                        foreach (var filter in whereParams.Where(c => c.Value != null && c.Operator != null))
                        {
                            string paramName = filter.Key;

                            if (paramCounter.ContainsKey(paramName))
                            {
                                paramCounter[paramName]++;
                                paramName += paramCounter[paramName].ToString();
                            }
                            else
                            {
                                paramCounter[paramName] = 0;
                            }

                            if (filter.Operator.ToLower() == "like")
                            {
                                queryParams.Add($"@{paramName}", $"{filter.Value}%");
                            }
                            else if (filter.Operator.ToLower() == "match")
                            {
                                queryParams.Add($"@{paramName}", $"%{filter.Value}%");
                            }
                            else if (filter.Operator.ToLower() == "in")
                            {
                                List<string> values = filter.Value.Split(',')
                                  .Select(v => v.Trim()) // Remove extra spaces
                                  .Where(v => !string.IsNullOrEmpty(v)) // Remove empty values
                                  .ToList();


                                if (values.Any())
                                {
                                    // Create unique parameter names
                                    var paramNames = values.Select((v, i) => $"@{paramName}{i}").ToList();

                                    // Append correct IN clause to the query
                                    query += $" AND {filter.Key} IN ({string.Join(",", paramNames)})";

                                    // Add each parameter to the query
                                    for (int i = 0; i < values.Count; i++)
                                    {
                                        queryParams.Add(paramNames[i], values[i]);
                                    }
                                }
                              
                            }
                            else
                            {
                                queryParams.Add($"@{paramName}", filter.Value);
                            }
                        }
                    }

                    if (pageNumber == 0)
                    {

                    }
                    else
                    {
                        // Add pagination using OFFSET and FETCH NEXT
                        UtilityBL utility = new UtilityBL();
                        string dbtype = utility.ReadConfigFile(ConfigConstants.dbtype);
                        utility.Dispose();

                        query += (dbtype == "MYSQL" ?
                            $" ORDER BY {orderby} LIMIT @PageSize OFFSET @Offset;"
                            :
                            $" ORDER BY {orderby} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");

                        queryParams.Add("@Offset", (pageNumber - 1) * pageSize);
                        queryParams.Add("@PageSize", pageSize);
                    }
                    return db.Query<T>(query, queryParams);
                }
            }
            catch (Exception ex)
            {
                logger.Error("GetAll : " + ex.Message);
                return null;
            }
        }
        public int CountAll(IEnumerable<FilterOption> whereParams = null)
        {
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    string query = $"SELECT COUNT(*) FROM {_tableName}";
                    var queryParams = new DynamicParameters();

                    if (whereParams != null && whereParams.Any())
                    {
                        string whereClause = string.Join(" AND ", whereParams
                            .Where(c => c.Value != null && c.Operator != null)
                            .GroupBy(c => c.Key)
                            .SelectMany(group => group.Select((c, index) => GetWhereClause(c, index > 0 ? index.ToString() : ""))));

                        if (!string.IsNullOrEmpty(whereClause))
                        {
                            query += $" WHERE {whereClause}";
                        }

                        // Add parameters with dynamic indexing
                        var paramCounter = new Dictionary<string, int>();

                        foreach (var filter in whereParams.Where(c => c.Value != null && c.Operator != null))
                        {
                            string paramName = filter.Key;

                            if (paramCounter.ContainsKey(paramName))
                            {
                                paramCounter[paramName]++;
                                paramName += paramCounter[paramName].ToString();
                            }
                            else
                            {
                                paramCounter[paramName] = 0;
                            }

                            if (filter.Operator.ToLower() == "like")
                                queryParams.Add($"@{paramName}", $"{filter.Value}%");
                            else if (filter.Operator.ToLower() == "match")
                                queryParams.Add($"@{paramName}", $"%{filter.Value}%");
                            else
                                queryParams.Add($"@{paramName}", filter.Value);
                        }
                    }

                    // Execute the query to get the count
                    return db.ExecuteScalar<int>(query, queryParams);
                }
            }
            catch (Exception ex)
            {
                logger.Error("CountAll : " + ex.Message);
                return 0; // Return 0 if an error occurs
            }
        }
        public T GetById(int id)
        {
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    return db.QueryFirstOrDefault<T>($"SELECT * FROM {_tableName} WHERE Id = @Id", new { Id = id });
                }
            }
            catch (Exception ex)
            {
                logger.Error("GetById : " + ex.Message);
                return null;
            }
        }

        public T GetByKey(string columnName, object columnValue)
        {
            using (IDbConnection db = DatabaseHelper.OpenConnection())
            {
                string query = $"SELECT * FROM {_tableName} WHERE {columnName} = @ColumnValue";
                return db.QueryFirstOrDefault<T>(query, new { ColumnValue = columnValue });
            }
        }

        public void Insert(T entity)
        {
            string query = "";
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    query = GenerateInsertQuery();
                    logger.Info("query2 " + query);
                    db.Execute(query, entity);
                }
            }
            catch (Exception ex)
            {
                logger.Error(query);
                logger.Error(entity);
                logger.Error(ex);
                //return null;
            }
        }

        public int InsertReturnIdentity(T entity)
        {
            string query = "";

            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    query = GenerateInsertQuery();

                    UtilityBL utility = new UtilityBL();
                    string dbtype = utility.ReadConfigFile(ConfigConstants.dbtype);
                    utility.Dispose();

                    query += (dbtype == "MYSQL" ? " SELECT LAST_INSERT_ID();" : " SELECT CAST(SCOPE_IDENTITY() AS INT);");

                    logger.Info(query);
                    return db.Query<int>(query, entity).Single();

                }
            }
            catch (Exception ex)
            {
                logger.Error("Error executing query:");
                logger.Error(query);
                logger.Error("Entity data:");
                logger.Error(JsonConvert.SerializeObject(entity, Formatting.Indented));
                logger.Error("Exception:", ex);
                return 0;
            }
        }

        public void BulkInsert(IEnumerable<T> entities)
        {
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    string query = GenerateInsertQuery();
                    db.Execute(query, entities);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                //return null;
            }
        }

        public bool Update(T entity)
        {
            string query = "";
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    query = GenerateUpdateQuery();
                    db.Execute(query, entity);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(query);
                logger.Error(entity);
                logger.Error(ex);
                return false;
            }
        }

        public void BulkUpdate(IEnumerable<T> entities)
        {
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    string query = GenerateUpdateQuery();
                    db.Execute(query, entities);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                //return null;
            }
        }

        public bool Delete(int id)
        {
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    string query = $"DELETE FROM {_tableName} WHERE Id = @Id";
                    db.Execute(query, new { Id = id });
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return false;
            }
        }

        public int Upsert(T entity, string[] uniqueColumns)
        {
            var properties = GetPropertiesExceptId();
            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => "@" + p.Name));
            string query;

            try
            {
                UtilityBL utility = new UtilityBL();
                string dbtype = utility.ReadConfigFile(ConfigConstants.dbtype);

                utility.Dispose();

                switch (dbtype)
                {
                    case "MYSQL":
                        var updateColumns = string.Join(", ", properties.Select(p => $"{p.Name} = VALUES({p.Name})"));
                        query = $"INSERT INTO {_tableName} ({columns}) VALUES ({values}) " +
                                $"ON DUPLICATE KEY UPDATE {updateColumns}; SELECT LAST_INSERT_ID();";
                        break;

                    case "MSSQL":
                        var updateStatements = string.Join(", ", properties.Select(p => $"{p.Name} = source.{p.Name}"));
                        var insertColumns = string.Join(", ", properties.Select(p => $"source.{p.Name}"));
                        query = $"MERGE INTO {_tableName} AS target " +
                                $"USING (VALUES ({values})) AS source ({columns}) " +
                                $"ON {string.Join(" AND ", uniqueColumns.Select(col => $"target.{col} = source.{col}"))} " +
                                $"WHEN MATCHED THEN UPDATE SET {updateStatements} " +
                                $"WHEN NOT MATCHED THEN INSERT ({columns}) VALUES ({insertColumns}) " +
                                $"OUTPUT INSERTED.Id;";
                        return _connection.QueryFirstOrDefault<int>(query, entity);

                        break;

                    default:
                        throw new ArgumentException("Invalid database type");
                }

                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    return _connection.QueryFirstOrDefault<int>(query, entity);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return 0;
            }
        }

        public IEnumerable<T> ExecuteQuery(string sql, object param = null)
        {
            try
            {
                using (IDbConnection db = DatabaseHelper.OpenConnection())
                {
                    return db.Query<T>(sql, param);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return null;
            }
        }

        private string GenerateInsertQuery()
        {
            var properties = GetPropertiesExceptId();
            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => "@" + p.Name));

            UtilityBL utility = new UtilityBL();
            string dbtype = utility.ReadConfigFile(ConfigConstants.dbtype);
            utility.Dispose();
           
            string tablename = (dbtype == "MYSQL" ? $"`{_tableName}`" : $"[{_tableName}]");

            return $"INSERT INTO `{_tableName}` ({columns}) VALUES ({values});";
        }

        private string GenerateUpdateQuery()
        {
            var properties = GetPropertiesExceptId();
            var setStatements = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
            return $"UPDATE {_tableName} SET {setStatements} WHERE Id = @Id";
        }

        private IEnumerable<PropertyInfo> GetPropertiesExceptId()
        {
            return typeof(T).GetProperties().Where(p => p.Name != "id");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

                if (_connection != null)
                {
                    _connection.Dispose();
                    _connection = null;
                }
            }
        }

        private string GetWhereClause(FilterOption filter, string sameparameter = "")
        {
            if (filter.Operator != null)
            {
                switch (filter.Operator.ToLower())
                {

                    case "Like":
                        return $"{filter.Key} LIKE @{filter.Key} ";
                    case "Match":
                        return $"{filter.Key} Match @{filter.Key}  ";

                    default:
                        return $"{filter.Key} {filter.Operator} @{filter.Key}";
                }
            }
            else
            {
                return "";
            }
        }
     

    }
}
