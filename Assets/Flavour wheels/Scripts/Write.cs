using UnityEngine;
using UnityEngine.UI;
using MySql.Data.MySqlClient;

public class Write : MonoBehaviour
{
    public Text name;
    public Text surnames;
    public Text age;
    private string connectionString;
    private MySqlConnection MS_Connection;
    private MySqlCommand MS_Command;
    string query;

    void Start()
    {
        connectionString = "Server=sql8.freesqldatabase.com; Database=sql8721580; User=sql8721580; Password=6wdc5VDnaQ; Charset=utf8;";
        EnsureTableExists();
    }

    void EnsureTableExists()
    {
        using (MS_Connection = new MySqlConnection(connectionString))
        {
            try
            {
                MS_Connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Douglas (
                        Nom VARCHAR(50),
                        Cognom VARCHAR(50),
                        Edat INT
                    );";
                using (MS_Command = new MySqlCommand(createTableQuery, MS_Connection))
                {
                    MS_Command.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                Debug.LogError("MySQL Error: " + ex.Message);
            }
            finally
            {
                if (MS_Connection.State == System.Data.ConnectionState.Open)
                {
                    MS_Connection.Close();
                }
            }
        }
    }

    public void SendInfo()
    {
        using (MS_Connection = new MySqlConnection(connectionString))
        {
            try
            {
                MS_Connection.Open();
                query = "INSERT INTO Douglas(Nom, Cognom, Edat) VALUES(@name, @surnames, @age);";

                using (MS_Command = new MySqlCommand(query, MS_Connection))
                {
                    MS_Command.Parameters.AddWithValue("@name", name.text);
                    MS_Command.Parameters.AddWithValue("@surnames", surnames.text);
                    MS_Command.Parameters.AddWithValue("@age", age.text);

                    MS_Command.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                Debug.LogError("MySQL Error: " + ex.Message);
            }
            finally
            {
                if (MS_Connection.State == System.Data.ConnectionState.Open)
                {
                    MS_Connection.Close();
                }
            }
        }
    }
}
