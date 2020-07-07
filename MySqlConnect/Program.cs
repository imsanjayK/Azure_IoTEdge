using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace NetworkBridge
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("server: ");
            var server = Console.ReadLine();
            Console.WriteLine("User: ");
            var user = Console.ReadLine();
            Console.WriteLine("Password: ");
            var pwd = Console.ReadLine();
            //Console.WriteLine("database: ");
            //var database = Console.ReadLine();
            MySql(server, "",user, pwd);
            //using (var connection = new MySqlConnection("Server=localhost;Database=cdactraining;Uid=root;Pwd=root;"))
            //{
            //    connection.Open();
            //    Console.WriteLine(connection.Database + " " + connection.DataSource);

            //    using (var command = connection.CreateCommand())
            //    {

            //        command.CommandText = "SELECT * FROM cdactraining.employee";
            //        MySqlDataAdapter adapter = new MySqlDataAdapter(command.CommandText, connection);
            //        DataTable data = new DataTable();
            //        adapter.Fill(data);
            //        foreach (DataRow row in data.Rows)
            //        {
            //            Console.WriteLine(row["ID"]);
            //        }

            //        using (var reader = command.ExecuteReader())
            //        {

            //            if (reader.HasRows)
            //            {

            //                var count = reader.FieldCount;

            //                for (var i = 0; i < count; i++)
            //                {
            //                    Console.Write(reader.GetName(i) + "|");
            //                }
            //                do
            //                {
            //                    reader.NextResult();
            //                    for (var i = 0; i < count; i++)
            //                    {
            //                        Debug.WriteLine(reader.GetValue(i));
            //                        //only returns one row. How to view all data?
            //                    }
            //                }
            //                while (reader.Read());
            //            }
            //        }


            //    }
            //    connection.Close();
            //}
        }

        static void MySql(string server, string database, string user, string pwd)
        {
            try
            {
                // Build connection string
                MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
                if (string.IsNullOrEmpty(server.Trim()))
                {
                    //server = "10.0.1.4";
                    server = "host.docker.internal";
                }
                if (string.IsNullOrEmpty(database))
                {
                    database = "cpm";
                }
                if (string.IsNullOrEmpty(user.Trim()))
                {
                    user = "sa";
                }
                if (string.IsNullOrEmpty(pwd.Trim()))
                {
                    pwd = "abc@123";
                }

                //builder.UserID = "root";
                //builder.Password = "root";
                //builder.UserID = "sa";
                //builder.Password = "abc@123";

                builder.Server = server;
                builder.Database = database;
                builder.UserID = user;
                builder.Password = pwd;

                builder.Port = 3306;
                // Connect to SQL
                Console.WriteLine("Connecting to SQL Server ... ");
                using (var connection = new MySqlConnection(builder.GetConnectionString(true)))
                {
                    connection.Open();
                    Console.WriteLine(connection.Database + " " + connection.DataSource);
                    Console.WriteLine("Done.");
                    Debug.WriteLine("Done.");

                    var query = "SELECT * FROM cpm.modelidentity";
                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                    DataTable data = new DataTable();
                    adapter.Fill(data);
                    foreach (DataRow row in data.Rows)
                    {
                        Console.WriteLine(row["Id"]);
                    }

                }
            }
            catch (MySqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("All done. Press any key to finish...");
            Console.ReadKey(true);
        }
        void Sql()
        {
            try
            {
                // Build connection string
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "localhost";
                builder.UserID = "root";
                builder.Password = "root";
                builder.InitialCatalog = "database";

                // Connect to SQL
                Console.Write("Connecting to SQL Server ... ");
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("Done.");
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("All done. Press any key to finish...");
            Console.ReadKey(true);
        }
    }
     
}
