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

                if (result == null)
                    return default!;

                if (typeof(T) == typeof(int))
                {
                    if (result is long l) return (T)(object)(int)l;
                    if (result is int i) return (T)(object)i;
                    if (int.TryParse(result.ToString(), out var parsed)) return (T)(object)parsed;
                }

                return (T)Convert.ChangeType(result, typeof(T));
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
