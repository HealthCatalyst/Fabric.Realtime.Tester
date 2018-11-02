// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestingHelper.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the TestingHelper type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Realtime.Tester
{
    using System.Data.SqlClient;

    /// <summary>
    /// The testing helper.
    /// </summary>
    public static class TestingHelper
    {
        /// <summary>
        /// The set object attribute base.
        /// </summary>
        /// <param name="sqlServer">sql server</param>
        /// <param name="server">
        ///     The server.
        /// </param>
        public static void SetObjectAttributeBase(string sqlServer, string server)
        {
            using (var connection =
                new SqlConnection($"server={sqlServer};initial catalog=EDWAdmin;Trusted_Connection=True;"))
            {
                connection.Open();

                var sqlCommandText = $"UPDATE [CatalystAdmin].[ObjectAttributeBASE]  SET AttributeValueTXT = '{server}' WHERE AttributeNM = 'FabricRealtimeBrokerHostName'";
                using (var command = new SqlCommand(sqlCommandText, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
