using Microsoft.Maui.Storage;

namespace AnnuaireEntreprise.Services;

public class AppLogService
{
	private readonly string _logFilePath;

	public AppLogService()
	{
		var logDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "logs");
		Directory.CreateDirectory(logDirectory);
		_logFilePath = Path.Combine(logDirectory, "annuaire.log");
	}

	public string GetLogFilePath()
	{
		return _logFilePath;
	}

	public void LogAdminAccess(string username, bool isSuccess)
	{
		var normalizedUsername = string.IsNullOrWhiteSpace(username) ? "inconnu" : username.Trim();
		var outcome = isSuccess ? "SUCCES" : "ECHEC";
		WriteEntry("ADMIN", $"Connexion {outcome} pour '{normalizedUsername}'");
	}

	public void LogAdminLogout(string username)
	{
		var normalizedUsername = string.IsNullOrWhiteSpace(username) ? "inconnu" : username.Trim();
		WriteEntry("ADMIN", $"Deconnexion pour '{normalizedUsername}'");
	}

	public void LogError(string context, Exception exception)
	{
		WriteEntry("ERROR", $"{context}: {exception.Message}\n{exception}");
	}

	private void WriteEntry(string category, string message)
	{
		var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{category}] {message}{Environment.NewLine}";
		File.AppendAllText(_logFilePath, entry);
	}
}