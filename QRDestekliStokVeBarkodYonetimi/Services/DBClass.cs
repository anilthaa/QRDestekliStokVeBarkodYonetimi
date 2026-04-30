using Dapper;
using Npgsql;
using System.Data.Common;
using System.Runtime.Intrinsics.Arm;
namespace QRDestekliStokVeBarkodYonetimi.Services
{
    public class DBClass
    {
        private static bool _factoryRegistered = false;
        private readonly DbProviderFactory dbProviderFactory;
        private readonly string _connectionString;

        static DBClass()
        {
            if (!_factoryRegistered)
            {
                DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);
                _factoryRegistered = true;
            }
        }

        public DBClass(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Bağlantı cümlesi boş olamaz.", nameof(connectionString));
            }
            dbProviderFactory = DbProviderFactories.GetFactory("Npgsql");
            _connectionString = connectionString;
        }

        private DbConnection CreDB()
        {
            var db = dbProviderFactory.CreateConnection();
            db.ConnectionString = _connectionString;
            return db;
        }

        public async Task<int> SQLExecute(string SQLSorgu, object? Params = null)
        {
            using var db = CreDB();
            try
            {
                await db.OpenAsync();
                return await db.ExecuteAsync(SQLSorgu, Params);
            }
            catch (Exception ex)
            {
                return 0;
            }
            finally
            {
                if (db.State != System.Data.ConnectionState.Closed)
                    db.Close();
                db.Dispose();
            }
        }

        public async Task<T> SQLExecuteScalar<T>(string SQLSorgu, object? Params = null)
        {
            using var db = CreDB();

            try
            {
                await db.OpenAsync();
                var result = await db.ExecuteScalarAsync(SQLSorgu, Params);

                if (result == null || result == DBNull.Value)
                    return default!;

                // Nullable tip desteği: int? gibi nullable türler için underlying type al
                var targetType = typeof(T);
                var underlyingType = Nullable.GetUnderlyingType(targetType);
                var actualType = underlyingType ?? targetType;

                // int / int? özel dönüşüm (PostgreSQL long döner)
                if (actualType == typeof(int))
                {
                    int intVal = result switch
                    {
                        long l  => (int)l,
                        int  i  => i,
                        _       => int.Parse(result.ToString()!)
                    };
                    return (T)(object)intVal;
                }

                return (T)Convert.ChangeType(result, actualType);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {

                }
                throw;
            }
            finally
            {
                if (db.State != System.Data.ConnectionState.Closed)
                    db.Close();
                db.Dispose();
            }
        }

        public async Task<T> SQLQueryFirstOrDefaultAsync<T>(string SQLSorgu, object? Params = null)
        {
            using var Db = CreDB();

            try
            {
                await Db.OpenAsync();
                return await Db.QueryFirstOrDefaultAsync<T>(SQLSorgu, Params);
            }
            catch (Exception ex)
            {
                var den = ex.Message;
                return default;
            }
            finally
            {
                if (Db.State != System.Data.ConnectionState.Closed)
                    Db.Close();
                Db.Dispose();
            }
        }

        public async Task<IEnumerable<T>> SQLQueryAsync<T>(string SQLSorgu, object? Params = null)
        {
            using var Db = CreDB();

            try
            {
                await Db.OpenAsync();
                return await Db.QueryAsync<T>(SQLSorgu, Params);
            }
            catch (Exception ex)
            {
                var den = ex.Message;
                return Enumerable.Empty<T>();
            }
            finally
            {
                if (Db.State != System.Data.ConnectionState.Closed)
                    Db.Close();
                Db.Dispose();
            }
        }
    }
}
