using Npgsql;

namespace TelegramBot
{
	class DbMethods
	{
		static async Task<NpgsqlConnection> OpenConnection()
		{
			string cs = "Host=127.0.0.1;Database=PW;User ID=postgres;Password=postgres;";
			NpgsqlConnection conn = new NpgsqlConnection(cs);

			try
			{
				await conn.OpenAsync();
			}
			catch (NpgsqlException ex)
			{
				Console.WriteLine(ex.Message);
			}

			return conn;
		}

		public static async Task<int> GetUserId(long UserId)
		{
			NpgsqlConnection conn = await OpenConnection();

			string cmdTxt = "SELECT user_id FROM users WHERE ext_id = " + UserId;
			NpgsqlCommand cmd = new NpgsqlCommand(cmdTxt, conn);
			NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

			int result = 0;
			if (reader.HasRows)
			{
				await reader.ReadAsync();
				result = (int)reader["user_id"];
			}

			return result;
		}

		public static async Task<string> SearchPetByName(string PetName)
		{
			NpgsqlConnection conn = await OpenConnection();

			string cmdTxt = "SELECT pet_id FROM pets WHERE pet_name = '" + PetName + "'";
			NpgsqlCommand cmd = new NpgsqlCommand(cmdTxt, conn);
			NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

			string result = "";
			if (reader.HasRows)
			{
				await reader.ReadAsync();
				result = (string)reader["pet_name"];
			}

			return result;
		}

		public static async Task<int> ChangeUserRegState(long UserId)
		{
			NpgsqlConnection conn = await OpenConnection();

			string cmdTxt = "UPDATE users SET reg_pending = TRUE WHERE ext_id = " + UserId;
			NpgsqlCommand cmd = new NpgsqlCommand(cmdTxt, conn);
			NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

			int result = reader.RecordsAffected;
			return result;
		}

		public static async Task<int> RegisterUser(long UserId)
		{
			NpgsqlConnection conn = await OpenConnection();

			string cmdTxt = "INSERT INTO users (ext_id, reg_pending, created_at) VALUES (" + UserId + ", TRUE, NOW()) ON CONFLICT (ext_id) DO NOTHING";
			NpgsqlCommand cmd = new NpgsqlCommand(cmdTxt, conn);
			NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

			int result = reader.RecordsAffected;
			return result;
		}

		public static async Task<int> SetUserPet(long UserId, int PetId)
		{
			NpgsqlConnection conn = await OpenConnection();

			string cmdTxt = "UPDATE users SET pet_id = " + PetId + ", reg_pending = FALSE WHERE ext_id = " + UserId + " AND reg_pending = TRUE";
			NpgsqlCommand cmd = new NpgsqlCommand(cmdTxt, conn);
			NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

			int result = reader.RecordsAffected;
			return result;
		}

		public static async Task<string> GetUserPet(long UserId)
		{
			NpgsqlConnection conn = await OpenConnection();

			string cmdTxt = "SELECT pets.pet_name AS pet_name FROM users LEFT JOIN pets ON users.pet_id = pets.pet_id WHERE users.ext_id = " + UserId;
			NpgsqlCommand cmd = new NpgsqlCommand(cmdTxt, conn);
			NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

			string result = "";
			if (reader.HasRows)
			{
				await reader.ReadAsync();
				result = (string)reader["pet_name"];
			}

			return result;
		}
	}
}