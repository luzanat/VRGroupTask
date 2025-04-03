using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using VRGroupTask.Models;

namespace VRGroupTask
{
    public static class DataAccess
    {
        // SQL Server connection string
        private const string ConnectionString = "";
        
        /// <summary>
        /// Try to establish a connection to the database
        /// </summary>
        /// <param name="errorMsg">Message if connection wasn't established</param>
        public static bool IsDbOnline(out string errorMsg)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                connection.Open();
                errorMsg = string.Empty;
                connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Insert boxes and its content into the database in a single transaction. Allows to be sure that all the
        /// content of the box is in the database. Using <c>SqlBulkCopy</c> for bulk insert. 
        /// </summary>
        public static void BoxBulkInsert(List<Box> boxes, List<BoxContent> boxContent)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            BoxesInsert(boxes, transaction);
            BoxContentInsert(boxContent, transaction);

            transaction.Commit();
            connection.Close();
        }

        #region private

        private static void BoxesInsert(List<Box> boxes, SqlTransaction transaction)
        {
            const string tableName = "Boxes";
            const string tempTable = "#BoxesTemp";

            using var bulkCopy = new SqlBulkCopy(transaction.Connection, SqlBulkCopyOptions.TableLock, transaction);
            bulkCopy.ColumnMappings.Add("SupplierIdentifier", "SupplierIdentifier");
            bulkCopy.ColumnMappings.Add("Identifier", "Identifier");

            using var dt = new DataTable();
            dt.Columns.Add("SupplierIdentifier", typeof(string));
            dt.Columns.Add("Identifier", typeof(string));

            foreach (var box in boxes)
            {
                dt.Rows.Add(box.SupplierIdentifier, box.Identifier);
            }

            CreateTempTable(transaction, tableName, tempTable);
            bulkCopy.DestinationTableName = tempTable;
            bulkCopy.WriteToServer(dt);
            MergeInsertOnly(transaction, tableName, tempTable, dt, ["Identifier"]);
            DropTempTable(transaction, tempTable);

            bulkCopy.Close();
        }

        private static void BoxContentInsert(List<BoxContent> content, SqlTransaction transaction)
        {
            const string tableName = "BoxContent";
            const string tempTable = "#BoxContentTemp";
            
            DataTable dt = new();
            dt.Columns.Add("BoxIdentifier", typeof(string));
            dt.Columns.Add("PoNumber", typeof(string));
            dt.Columns.Add("Isbn", typeof(string));
            dt.Columns.Add("Quantity", typeof(int));

            foreach (var contentItem in content)
            {
                dt.Rows.Add(contentItem.BoxIdentifier, contentItem.PoNumber, contentItem.Isbn, contentItem.Quantity);
            }

            using var bulkCopy = new SqlBulkCopy(transaction.Connection, SqlBulkCopyOptions.TableLock, transaction);
            bulkCopy.ColumnMappings.Add("BoxIdentifier", "BoxIdentifier");
            bulkCopy.ColumnMappings.Add("PoNumber", "PoNumber");
            bulkCopy.ColumnMappings.Add("Isbn", "Isbn");
            bulkCopy.ColumnMappings.Add("Quantity", "Quantity");

            CreateTempTable(transaction, tableName, tempTable);
            bulkCopy.DestinationTableName = tempTable;
            bulkCopy.WriteToServer(dt);
            MergeInsertOnly(transaction, tableName, tempTable, dt, ["BoxIdentifier", "Isbn"]);
            DropTempTable(transaction, tempTable);

            bulkCopy.Close();
        }

        private static void CreateTempTable(SqlTransaction transaction, string sourceTableName, string tempTableName)
        {
            var createTempTableSql = $"SELECT TOP 0 * INTO {tempTableName} FROM {sourceTableName}";
            using var sqlCmd = new SqlCommand(createTempTableSql, transaction.Connection, transaction);
            sqlCmd.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Generates and executes merge script. Currently only inserts new records to the target table.
        /// </summary>
        private static void MergeInsertOnly(SqlTransaction transaction, string targetTableName, string tempTableName, DataTable dt, List<string> sqlMatchColumns)
        {
            var columListSb = new StringBuilder();
            var valuesListSb = new StringBuilder();

            for (var i = 0; i < dt.Columns.Count; i++)
            {
                var col = dt.Columns[i];
                columListSb.Append(col.ColumnName);
                valuesListSb.Append($"source.{col.ColumnName}");

                if (i != dt.Columns.Count - 1)
                {
                    columListSb.Append(", ");
                    valuesListSb.Append(", ");
                }
            }

            var matchColSb = new StringBuilder();
            for (var i = 0; i < sqlMatchColumns.Count; i++)
            {
                var matchCol = sqlMatchColumns[i];
                matchColSb.Append($"target.{matchCol} = source.{matchCol}");

                if (i != sqlMatchColumns.Count - 1)
                    matchColSb.Append(" AND ");
            }

            var sql = $"""
                       MERGE INTO {targetTableName} AS target USING {tempTableName} AS source
                       ON {matchColSb}
                       WHEN NOT MATCHED BY TARGET THEN
                            INSERT ({columListSb})
                            VALUES ({valuesListSb});
                       """;

            var sqlCmd = new SqlCommand(sql, transaction.Connection, transaction);
            sqlCmd.ExecuteNonQuery();
        }

        private static void DropTempTable(SqlTransaction transaction, string tableName)
        {
            if (!tableName.StartsWith('#')) return;
            var sql = $"DROP TABLE {tableName}";
            var sqlCmd = new SqlCommand(sql, transaction.Connection, transaction);
            sqlCmd.ExecuteNonQuery();
        }

        #endregion
    }
}
