using System;
using System.Data.SQLite;
using System.Text.RegularExpressions;


class dataBase {
    SQLiteConnection connection;
    private static dataBase? singleTone = null;
    private static string insertData = "INSERT INTO JOBS (Company, Position, Skills, State, City) VALUES (@Company, @Position, @Skills, @State, @City)";

    public static dataBase getInstance() {
        if(singleTone == null) {
            singleTone = new dataBase();
        }
        return singleTone;
    }

    public void reset() {

        string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS JOBS (
                    JobID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Company TEXT NOT NULL,
                    Position TEXT NOT NULL,
                    Skills TEXT NOT NULL,
                    State TEXT DEFAULT 'Not Available',
                    City TEXT DEFAULT 'Not Available'
                );
            ";
        executeCommand(createTableQuery);
        executeCommand("DELETE FROM JOBS");
        executeCommand("DELETE FROM SQLITE_SEQUENCE WHERE NAME='JOBS'");
    }

    private dataBase() {
        string connectionString = "Data Source=jobs.db;Version=3;";
        this.connection = new SQLiteConnection(connectionString);
        this.connection.Open();
    }

    public int executeSelect(string command) {
        SQLiteCommand sql_select = new SQLiteCommand(command,this.connection);
        int trackNumber = 0;
        using (SQLiteDataReader reader = sql_select.ExecuteReader())
        {
            while (reader.Read())
            {
                trackNumber++;
                Console.WriteLine($"Company: {reader["Company"]}, Position: {reader["Position"]}, Skills: {reader["Skills"]}, State: {reader["State"]}, City: {reader["City"]}");
            }
        }
        sql_select.Dispose();
        return trackNumber;
    }

    public void insert(string title, string jobPosition, string skills, string state, string city) {
        var command = new SQLiteCommand(insertData, this.connection);
        command.Parameters.AddWithValue("@Company", title);
        command.Parameters.AddWithValue("@Position",jobPosition);
        command.Parameters.AddWithValue("@Skills", skills);
        if (String.Compare(state, 0, "Plătit", 0, 6, StringComparison.OrdinalIgnoreCase) == 0) {
            state = "Paid";
        } else if (String.Compare(state, 0, "Neplătit", 0, 8, StringComparison.OrdinalIgnoreCase) == 0) {
            state = "Unpaid";
        }
    
        command.Parameters.AddWithValue("@State", state);
        command.Parameters.AddWithValue("@City",city);
        command.ExecuteNonQuery();
        command.Dispose();
    }

    public void executeCommand(string command) {
        SQLiteCommand comm = new SQLiteCommand(command, this.connection);
        comm.ExecuteNonQuery();
        comm.Dispose();
    }

    public int countEntries() {
        string command = "SELECT COUNT(*) FROM JOBS";
        SQLiteCommand sql_select = new SQLiteCommand(command,this.connection);
        int count = 0;
        using (SQLiteDataReader reader = sql_select.ExecuteReader()) {
            while (reader.Read()) {
                count = reader.GetInt32(0);
            }
        }
        sql_select.Dispose();
        return count;
    }
    
    public int selectCommand(Dictionary<string, object> parameters, string comm) {
        int nOfFinds = 0;
        SQLiteCommand sql_select = new SQLiteCommand(comm, this.connection);
        foreach(var param in parameters) {
            sql_select.Parameters.AddWithValue(param.Key,param.Value);
        }
        using (SQLiteDataReader reader = sql_select.ExecuteReader()) {
            while (reader.Read()) {
                nOfFinds++;
                Console.WriteLine($"Company: {reader["Company"]}, Position: {reader["Position"]},Skills: {reader["Skills"]}, City: {reader["City"]}, State: {reader["State"]}");
            }
        }
        sql_select.Dispose();
        return nOfFinds;
    }

    public int processInputParams(string[] skills, string[] orase, string[] statusPaid) {

        int countFinds = 0;
        string selectComm = "SELECT * FROM JOBS";
        string searchParams;
        char[] delims = {' '};
        string[] skill = new String[100];
        int i = 0;
        string? paid = null;
        string? cityLoc = null;
        List<string> conditions = new List<string>();
        List<string> skillConditions = new List<string>();
        Dictionary<string, object> parameters = new Dictionary<string, object>();

        searchParams = Console.ReadLine() ?? "";
        string [] strtokRullz = searchParams.Split(' ');
        int findStatus = 0;
        int findCity = 0;
        foreach(string str in strtokRullz) {
            if(skills.Contains(str, StringComparer.OrdinalIgnoreCase)) {
                skill[i] = str;
                i++;
            }
            if(statusPaid.Contains(str, StringComparer.OrdinalIgnoreCase)) {
                paid = str;
                findStatus++;
            }
            if(orase.Contains(str,StringComparer.OrdinalIgnoreCase)) {
                cityLoc = str;
                findCity++;
            }
        }
        if(findCity > 1 || findStatus > 1) {
            Console.WriteLine("INVALID QUERY!");
            return 0;
        }
        if(cityLoc != null) {
            if(cityLoc.Equals("Bucuresti",StringComparison.OrdinalIgnoreCase)) {
                cityLoc = "București";
            }
            if(cityLoc.Equals("Iasi",StringComparison.OrdinalIgnoreCase)) {
                cityLoc = "Iași";
            }
        }
    
        if(cityLoc != null) {
            conditions.Add("City COLLATE NOCASE = @city");
            parameters["@city"] = cityLoc;
        }
        if(paid != null) {
            conditions.Add("State COLLATE NOCASE = @paid");
            parameters["@paid"] = paid;
        }
        for(int j = 0; j < i; j++) {
           skillConditions.Add(
                    "(Skills LIKE " + $"@skillStart_{j}" +" COLLATE NOCASE OR " +
                    "Skills LIKE " + $"@skillMiddle_{j}" + " COLLATE NOCASE OR " +
                    "Skills LIKE " + $"@skillEnd_{j}" +" COLLATE NOCASE OR " +
                    "Skills = " + $"@exactSkill_{j}" + " COLLATE NOCASE)"
            );
            parameters[$"@skillStart_{j}"] = skill[j] + ", %"; 
            parameters[$"@skillMiddle_{j}"] = "%, " + skill[j] + ", %";
            parameters[$"@skillEnd_{j}"] = "%, " + skill[j];
            parameters[$"@exactSkill_{j}"] = skill[j];
        }
        bool check_entry = false;
        if(skillConditions.Count > 0) {
            selectComm += " WHERE " + string.Join(" OR ", skillConditions);
            check_entry = true;
        }

        if(conditions.Count > 0) {
            if(check_entry) {
                selectComm += " AND " + string.Join(" AND ", conditions);
            } else {
                selectComm += " WHERE " + string.Join(" AND ", conditions);
            }
        }

        countFinds = selectCommand(parameters,selectComm);
        return countFinds;
    }

    public void helloMessage() {
        Console.WriteLine("Available commands: ");
        Console.WriteLine("");
        Console.WriteLine("SELECT + ENTER + skills, location, paid/unpaid");
        Console.WriteLine("");
        Console.WriteLine("SELECT ALL");
        Console.WriteLine("");
        Console.WriteLine("EXIT");
        Console.WriteLine("");
        Console.WriteLine("INIT_DB");
        Console.WriteLine("");
        Console.WriteLine("Input command: ");
        Console.WriteLine("");
    }
}