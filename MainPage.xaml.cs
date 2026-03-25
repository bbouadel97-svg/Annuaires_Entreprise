using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
using AnnuaireEntreprise.Services;

namespace AnnuaireEntreprise;

public partial class MainPage : ContentPage
{
	private readonly SalarieService _salarieService = new();
	private readonly ServiceService _serviceService = new();
	private readonly SiteService _siteService = new();
	private string _databasePath = string.Empty;
	private const string AdminPin = "1997";
	private bool _isAdminMode;

	private List<Service> _services = new();
	private List<Site> _sites = new();
	private List<Salarie> _allSalaries = new();

	public ObservableCollection<SalarieItemViewModel> Salaries { get; } = new();

	public MainPage()
	{
		InitializeComponent();
		BindingContext = this;

		InitializeDatabase();
		LoadReferenceData();
		UpdateAdminModeUi();
		LoadSalaries();
	}

	private void InitializeDatabase()
	{
		var db = new Database();
		db.CreateTables();
		db.SeedData();
		_databasePath = db.GetDatabasePath();
		DbPathLabel.Text = $"Base locale: {_databasePath}";
	}

	private void LoadReferenceData()
	{
		_services = _serviceService.Lister();
		_sites = _siteService.Lister();

		ServicePicker.ItemsSource = _services;
		SitePicker.ItemsSource = _sites;

		if (_services.Count > 0)
		{
			ServicePicker.SelectedIndex = 0;
		}

		if (_sites.Count > 0)
		{
			SitePicker.SelectedIndex = 0;
		}
	}

	private void LoadSalaries(string? filter = null)
	{
		_allSalaries = _salarieService.Lister();

		var filteredList = string.IsNullOrWhiteSpace(filter)
			? _allSalaries
			: _allSalaries.FindAll(s => s.Nom.Contains(filter, StringComparison.OrdinalIgnoreCase));

		Salaries.Clear();

		foreach (var salarie in filteredList)
		{
			Salaries.Add(ToViewModel(salarie));
		}
	}

	private void UpdateAdminModeUi()
	{
		AdminModeButton.Text = _isAdminMode ? "Desactiver mode admin" : "Activer mode admin";
		AdminModeLabel.Text = _isAdminMode ? "Mode admin active" : "Mode admin desactive";
		AdminModeLabel.TextColor = _isAdminMode ? Color.FromArgb("#1B5E20") : Color.FromArgb("#B00020");

		AddButton.IsVisible = _isAdminMode;
		ExportPdfButton.IsVisible = _isAdminMode;
		AddFormTopGrid.IsVisible = _isAdminMode;
		AddFormBottomGrid.IsVisible = _isAdminMode;

		if (!_isAdminMode)
		{
			ClearForm();
		}
	}

	private SalarieItemViewModel ToViewModel(Salarie salarie)
	{
		var serviceName = _services.Find(s => s.Id == salarie.ServiceId)?.Nom ?? "Service inconnu";
		var siteName = _sites.Find(s => s.Id == salarie.SiteId)?.Ville ?? "Site inconnu";

		return new SalarieItemViewModel
		{
			Id = salarie.Id,
			NomComplet = $"{salarie.Nom} {salarie.Prenom}",
			Email = salarie.Email,
			Details = $"Service: {serviceName} | Site: {siteName} | Fixe: {salarie.TelephoneFixe} | Portable: {salarie.TelephonePortable}",
			IsAdminActionsVisible = _isAdminMode
		};
	}

	private async void OnToggleAdminModeClicked(object? sender, EventArgs e)
	{
		if (_isAdminMode)
		{
			_isAdminMode = false;
			UpdateAdminModeUi();
			LoadSalaries(SearchEntry.Text);
			return;
		}

		var pin = await DisplayPromptAsync("Mode admin", "Saisis le code administrateur:", "Valider", "Annuler", "Code", maxLength: 32, keyboard: Keyboard.Numeric);
		if (string.IsNullOrWhiteSpace(pin))
		{
			return;
		}

		if (!string.Equals(pin.Trim(), AdminPin, StringComparison.Ordinal))
		{
			await DisplayAlertAsync("Acces refuse", "Code administrateur incorrect.", "OK");
			return;
		}

		_isAdminMode = true;
		UpdateAdminModeUi();
		LoadSalaries(SearchEntry.Text);
	}

	private async void OnAddClicked(object? sender, EventArgs e)
	{
		if (!_isAdminMode)
		{
			await DisplayAlertAsync("Mode admin requis", "Active le mode admin pour ajouter un salarie.", "OK");
			return;
		}

		if (ServicePicker.SelectedItem is not Service selectedService || SitePicker.SelectedItem is not Site selectedSite)
		{
			await DisplayAlertAsync("Selection requise", "Choisis un service et un site.", "OK");
			return;
		}

		if (string.IsNullOrWhiteSpace(NomEntry.Text) || string.IsNullOrWhiteSpace(PrenomEntry.Text))
		{
			await DisplayAlertAsync("Champs requis", "Nom et prenom sont obligatoires.", "OK");
			return;
		}

		_salarieService.Ajouter(new Salarie
		{
			Nom = NomEntry.Text.Trim(),
			Prenom = PrenomEntry.Text.Trim(),
			Email = EmailEntry.Text?.Trim() ?? string.Empty,
			TelephoneFixe = FixeEntry.Text?.Trim() ?? string.Empty,
			TelephonePortable = PortableEntry.Text?.Trim() ?? string.Empty,
			ServiceId = selectedService.Id,
			SiteId = selectedSite.Id
		});

		ClearForm();
		LoadSalaries(SearchEntry.Text);
	}

	private async void OnDeleteClicked(object? sender, EventArgs e)
	{
		if (!_isAdminMode)
		{
			await DisplayAlertAsync("Mode admin requis", "Active le mode admin pour supprimer un salarie.", "OK");
			return;
		}

		if (sender is Button { CommandParameter: int id })
		{
			_salarieService.Supprimer(id);
			LoadSalaries(SearchEntry.Text);
		}
	}

	private void OnRefreshClicked(object? sender, EventArgs e)
	{
		LoadReferenceData();
		LoadSalaries(SearchEntry.Text);
	}

	private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
	{
		LoadSalaries(e.NewTextValue);
	}

	private void OnClearSearchClicked(object? sender, EventArgs e)
	{
		SearchEntry.Text = string.Empty;
		LoadSalaries();
	}

	private async void OnExportDbPdfClicked(object? sender, EventArgs e)
	{
		if (!_isAdminMode)
		{
			await DisplayAlertAsync("Mode admin requis", "Active le mode admin pour exporter la base en PDF.", "OK");
			return;
		}

		try
		{
			LoadReferenceData();
			_allSalaries = _salarieService.Lister();

			var exportDirectory = Path.GetDirectoryName(_databasePath);
			if (string.IsNullOrWhiteSpace(exportDirectory) || !Directory.Exists(exportDirectory))
			{
				exportDirectory = FileSystem.Current.AppDataDirectory;
			}

			var fileName = $"annuaire-export-{DateTime.Now:yyyyMMdd-HHmmss}.pdf";
			var exportPath = Path.Combine(exportDirectory, fileName);
			var pdfBytes = BuildPdfDocument();

			File.WriteAllBytes(exportPath, pdfBytes);
			await DisplayAlertAsync("Export termine", $"PDF genere:\n{exportPath}", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Erreur", $"Impossible d'exporter en PDF: {ex.Message}", "OK");
		}
	}

	private byte[] BuildPdfDocument()
	{
		var lines = new List<string>
		{
			"Annuaire Entreprise - Export base de donnees",
			$"Date: {DateTime.Now:dd/MM/yyyy HH:mm}",
			$"Fichier DB: {_databasePath}",
			"",
			$"Nombre de salaries: {_allSalaries.Count}",
			""
		};

		foreach (var salarie in _allSalaries)
		{
			var serviceName = _services.Find(s => s.Id == salarie.ServiceId)?.Nom ?? "Service inconnu";
			var siteName = _sites.Find(s => s.Id == salarie.SiteId)?.Ville ?? "Site inconnu";
			lines.Add($"- {salarie.Nom} {salarie.Prenom} | {salarie.Email} | Fixe: {salarie.TelephoneFixe} | Portable: {salarie.TelephonePortable} | Service: {serviceName} | Site: {siteName}");
		}

		if (_allSalaries.Count == 0)
		{
			lines.Add("Aucun salarie dans la base.");
		}

		const int linesPerPage = 42;
		var pageChunks = lines
			.Select((line, index) => new { line, index })
			.GroupBy(item => item.index / linesPerPage)
			.Select(group => group.Select(item => item.line).ToList())
			.ToList();

		if (pageChunks.Count == 0)
		{
			pageChunks.Add(new List<string> { "Document vide." });
		}

		var pageCount = pageChunks.Count;
		var fontObjectId = 3 + (2 * pageCount);
		var objectCount = fontObjectId;
		var objects = new string[objectCount + 1];

		objects[1] = "<< /Type /Catalog /Pages 2 0 R >>";

		var kids = string.Join(" ", Enumerable.Range(0, pageCount).Select(i => $"{3 + i} 0 R"));
		objects[2] = $"<< /Type /Pages /Count {pageCount} /Kids [ {kids} ] >>";

		for (var i = 0; i < pageCount; i++)
		{
			var pageObjectId = 3 + i;
			var contentObjectId = 3 + pageCount + i;
			objects[pageObjectId] = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontObjectId} 0 R >> >> /Contents {contentObjectId} 0 R >>";

			var streamBuilder = new StringBuilder();
			streamBuilder.AppendLine("BT");
			streamBuilder.AppendLine("/F1 11 Tf");

			var y = 800;
			foreach (var rawLine in pageChunks[i])
			{
				var safeLine = EscapePdfLiteral(ToAscii(rawLine));
				streamBuilder.AppendLine($"1 0 0 1 40 {y} Tm");
				streamBuilder.AppendLine($"({safeLine}) Tj");
				y -= 18;
			}

			streamBuilder.AppendLine("ET");

			var streamContent = streamBuilder.ToString();
			var streamLength = Encoding.ASCII.GetByteCount(streamContent);
			objects[contentObjectId] = $"<< /Length {streamLength} >>\nstream\n{streamContent}endstream";
		}

		objects[fontObjectId] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

		var pdfBuilder = new StringBuilder();
		pdfBuilder.AppendLine("%PDF-1.4");

		var offsets = new int[objectCount + 1];
		for (var i = 1; i <= objectCount; i++)
		{
			offsets[i] = Encoding.ASCII.GetByteCount(pdfBuilder.ToString());
			pdfBuilder.AppendLine($"{i} 0 obj");
			pdfBuilder.AppendLine(objects[i]);
			pdfBuilder.AppendLine("endobj");
		}

		var xrefOffset = Encoding.ASCII.GetByteCount(pdfBuilder.ToString());
		pdfBuilder.AppendLine("xref");
		pdfBuilder.AppendLine($"0 {objectCount + 1}");
		pdfBuilder.AppendLine("0000000000 65535 f ");

		for (var i = 1; i <= objectCount; i++)
		{
			pdfBuilder.AppendLine($"{offsets[i]:D10} 00000 n ");
		}

		pdfBuilder.AppendLine("trailer");
		pdfBuilder.AppendLine($"<< /Size {objectCount + 1} /Root 1 0 R >>");
		pdfBuilder.AppendLine("startxref");
		pdfBuilder.AppendLine(xrefOffset.ToString());
		pdfBuilder.Append("%%EOF");

		return Encoding.ASCII.GetBytes(pdfBuilder.ToString());
	}

	private static string EscapePdfLiteral(string input)
	{
		return input.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
	}

	private static string ToAscii(string input)
	{
		var normalized = input.Normalize(NormalizationForm.FormD);
		var builder = new StringBuilder();

		foreach (var c in normalized)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
			{
				continue;
			}

			builder.Append(c is >= ' ' and <= '~' ? c : ' ');
		}

		return builder.ToString();
	}

	private void ClearForm()
	{
		NomEntry.Text = string.Empty;
		PrenomEntry.Text = string.Empty;
		EmailEntry.Text = string.Empty;
		FixeEntry.Text = string.Empty;
		PortableEntry.Text = string.Empty;
	}

	public class SalarieItemViewModel
	{
		public int Id { get; set; }
		public string NomComplet { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Details { get; set; } = string.Empty;
		public bool IsAdminActionsVisible { get; set; }
	}
}
