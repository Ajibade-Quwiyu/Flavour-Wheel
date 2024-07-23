using UnityEngine;
using UnityEngine.UI;
using MySql.Data.MySqlClient;

public class Read : MonoBehaviour
{
    private string connectionString;
    string query;
    private MySqlConnection MS_Connection;
    private MySqlCommand MS_Command;
    private MySqlDataReader MS_Reader;
    public Text textCanvas;

    void Start()
    {
        connectionString = "Server=sql8.freesqldatabase.com; Database=sql8721580; User=sql8721580; Password=6wdc5VDnaQ; Charset=utf8;";
    }

    public void ViewInfo()
    {
        query = "SELECT * FROM Douglas";

        using (MS_Connection = new MySqlConnection(connectionString))
        {
            try
            {
                MS_Connection.Open();
                using (MS_Command = new MySqlCommand(query, MS_Connection))
                {
                    MS_Reader = MS_Command.ExecuteReader();
                    textCanvas.text = "";  // Clear previous text
                    while (MS_Reader.Read())
                    {
                        textCanvas.text += "\n" + MS_Reader["Nom"] + " " + MS_Reader["Cognom"] + " " + MS_Reader["Edat"];
                    }
                    MS_Reader.Close();
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
