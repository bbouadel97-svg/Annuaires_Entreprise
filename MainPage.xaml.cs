using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
using AnnuaireEntreprise.Services;
#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
#endif

namespace AnnuaireEntreprise;

public partial class MainPage : ContentPage
{
	private readonly SalarieService _salarieService = new();
	private readonly ServiceService _serviceService = new();
	private readonly SiteService _siteService = new();
	private readonly RandomUserService _randomUserService = new();
	private readonly AuthService _authService = new();
	private string _databasePath = string.Empty;
	private bool _isAdminMode;
	private bool _shortcutRegistered;
	private int? _editingSalarieId;

	private List<Service> _services = new();
	private List<Site> _sites = new();
	private List<Salarie> _allSalaries = new();
	private readonly List<SiteFilterItem> _siteFilters = new();
	private readonly List<ServiceFilterItem> _serviceFilters = new();

	public ObservableCollection<SalarieItemViewModel> Salaries { get; } = new();

	public MainPage()
	{
		InitializeComponent();
		BindingContext = this;

		InitializeDatabase();
		LoadReferenceData();
		UpdateAdminModeUi();
		ApplyFilters();
		RegisterAdminShortcut();
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
		ServiceAdminPicker.ItemsSource = _services;
		SiteAdminPicker.ItemsSource = _sites;

		BuildFilterSources();
		SiteFilterPicker.ItemsSource = _siteFilters;
		ServiceFilterPicker.ItemsSource = _serviceFilters;

		if (_services.Count > 0)
		{
			ServicePicker.SelectedIndex = 0;
			if (ServiceAdminPicker.SelectedIndex < 0)
			{
				ServiceAdminPicker.SelectedIndex = 0;
			}
		}

		if (_sites.Count > 0)
		{
			SitePicker.SelectedIndex = 0;
			if (SiteAdminPicker.SelectedIndex < 0)
			{
				SiteAdminPicker.SelectedIndex = 0;
			}
		}

		if (SiteFilterPicker.SelectedIndex < 0 && _siteFilters.Count > 0)
		{
			SiteFilterPicker.SelectedIndex = 0;
		}

		if (ServiceFilterPicker.SelectedIndex < 0 && _serviceFilters.Count > 0)
		{
			ServiceFilterPicker.SelectedIndex = 0;
		}
	}

	private void BuildFilterSources()
	{
		_siteFilters.Clear();
		_siteFilters.Add(new SiteFilterItem { Id = null, Ville = "Tous les sites" });
		foreach (var site in _sites)
		{
			_siteFilters.Add(new SiteFilterItem { Id = site.Id, Ville = site.Ville });
		}

		_serviceFilters.Clear();
		_serviceFilters.Add(new ServiceFilterItem { Id = null, Nom = "Tous les services" });
		foreach (var service in _services)
		{
			_serviceFilters.Add(new ServiceFilterItem { Id = service.Id, Nom = service.Nom });
		}
	}

	private void ApplyFilters()
	{
		_allSalaries = _salarieService.Lister();

		var searchText = SearchEntry.Text?.Trim() ?? string.Empty;
		var selectedSiteId = (SiteFilterPicker.SelectedItem as SiteFilterItem)?.Id;
		var selectedServiceId = (ServiceFilterPicker.SelectedItem as ServiceFilterItem)?.Id;

		var filteredList = _allSalaries.Where(s =>
			(string.IsNullOrWhiteSpace(searchText)
				|| s.Nom.Contains(searchText, StringComparison.OrdinalIgnoreCase)
				|| s.Prenom.Contains(searchText, StringComparison.OrdinalIgnoreCase))
			&& (!selectedSiteId.HasValue || s.SiteId == selectedSiteId.Value)
			&& (!selectedServiceId.HasValue || s.ServiceId == selectedServiceId.Value))
			.ToList();

		Salaries.Clear();

		foreach (var salarie in filteredList)
		{
			Salaries.Add(ToViewModel(salarie));
		}
	}

	private void UpdateAdminModeUi()
	{
		AdminModeButton.Text = _isAdminMode ? "Desactiver mode admin" : "Activer mode admin";
		AdminModeLabel.Text = _isAdminMode
			? "Mode admin active"
			: "Mode visiteur actif (Ctrl+Shift+A)";
		AdminModeLabel.TextColor = _isAdminMode ? Color.FromArgb("#1B5E20") : Color.FromArgb("#B00020");

		AddButton.IsVisible = _isAdminMode;
		ImportApiButton.IsVisible = _isAdminMode;
		ExportPdfButton.IsVisible = _isAdminMode;
		UpdateButton.IsVisible = _isAdminMode;
		AddFormTopGrid.IsVisible = _isAdminMode;
		AddFormBottomGrid.IsVisible = _isAdminMode;
		AdminPanel.IsVisible = _isAdminMode;

		if (!_isAdminMode)
		{
			_editingSalarieId = null;
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
			Nom = salarie.Nom,
			Prenom = salarie.Prenom,
			TelephoneFixe = salarie.TelephoneFixe,
			TelephonePortable = salarie.TelephonePortable,
			ServiceId = salarie.ServiceId,
			SiteId = salarie.SiteId,
			ServiceNom = serviceName,
			SiteVille = siteName,
			NomComplet = $"{salarie.Nom} {salarie.Prenom}",
			Email = salarie.Email,
			Details = $"Service: {serviceName} | Site: {siteName} | Fixe: {salarie.TelephoneFixe} | Portable: {salarie.TelephonePortable}",
			IsAdminActionsVisible = _isAdminMode
		};
	}

	private async void OnToggleAdminModeClicked(object? sender, EventArgs e)
	{
		await ToggleAdminModeAsync();
	}

	private async Task ToggleAdminModeAsync()
	{
		if (_isAdminMode)
		{
			_isAdminMode = false;
			UpdateAdminModeUi();
			ApplyFilters();
			return;
		}

		var username = await DisplayPromptAsync("Mode admin", "Identifiant administrateur:", "Valider", "Annuler", "Identifiant", maxLength: 64);
		if (string.IsNullOrWhiteSpace(username))
		{
			return;
		}

		var password = await DisplayPromptAsync("Mode admin", "Mot de passe:", "Valider", "Annuler", "Mot de passe", maxLength: 128);
		if (string.IsNullOrWhiteSpace(password))
		{
			return;
		}

		if (!_authService.AuthenticateAdmin(username, password))
		{
			await DisplayAlertAsync("Acces refuse", "Identifiant ou mot de passe incorrect.", "OK");
			return;
		}

		_isAdminMode = true;
		UpdateAdminModeUi();
		ApplyFilters();
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
		ApplyFilters();
	}

	private async void OnUpdateSalarieClicked(object? sender, EventArgs e)
	{
		if (!_isAdminMode)
		{
			await DisplayAlertAsync("Mode admin requis", "Active le mode admin pour modifier un salarie.", "OK");
			return;
		}

		if (!_editingSalarieId.HasValue)
		{
			await DisplayAlertAsync("Selection requise", "Selectionne d'abord un salarie dans la liste.", "OK");
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

		_salarieService.Modifier(new Salarie
		{
			Id = _editingSalarieId.Value,
			Nom = NomEntry.Text.Trim(),
			Prenom = PrenomEntry.Text.Trim(),
			Email = EmailEntry.Text?.Trim() ?? string.Empty,
			TelephoneFixe = FixeEntry.Text?.Trim() ?? string.Empty,
			TelephonePortable = PortableEntry.Text?.Trim() ?? string.Empty,
			ServiceId = selectedService.Id,
			SiteId = selectedSite.Id
		});

		_editingSalarieId = null;
		ClearForm();
		ApplyFilters();
	}

	private async void OnImportApiClicked(object? sender, EventArgs e)
	{
		if (!_isAdminMode)
		{
			await DisplayAlertAsync("Mode admin requis", "Active le mode admin pour importer des utilisateurs depuis l'API.", "OK");
			return;
		}

		if (_services.Count == 0 || _sites.Count == 0)
		{
			await DisplayAlertAsync("Donnees manquantes", "Ajoute au moins un service et un site avant l'import.", "OK");
			return;
		}

		try
		{
			ImportApiButton.IsEnabled = false;

			var apiResponse = await _randomUserService.GetRandomUsersAsync(10);
			if (apiResponse?.Results is null || apiResponse.Results.Length == 0)
			{
				await DisplayAlertAsync("Import API", "Aucun utilisateur recu depuis l'API RandomUser.", "OK");
				return;
			}

			var insertedCount = 0;
			foreach (var user in apiResponse.Results)
			{
				var selectedService = _services[Random.Shared.Next(_services.Count)];
				var selectedSite = _sites[Random.Shared.Next(_sites.Count)];

				_salarieService.Ajouter(new Salarie
				{
					Nom = NormalizeName(user.Name.Last),
					Prenom = NormalizeName(user.Name.First),
					Email = user.Email.Trim(),
					TelephoneFixe = user.Phone.Trim(),
					TelephonePortable = user.Cell.Trim(),
					ServiceId = selectedService.Id,
					SiteId = selectedSite.Id
				});

				insertedCount++;
			}

			ApplyFilters();
			await DisplayAlertAsync("Import termine", $"{insertedCount} utilisateurs importes depuis l'API.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Erreur API", $"Impossible d'importer depuis l'API: {ex.Message}", "OK");
		}
		finally
		{
			ImportApiButton.IsEnabled = true;
		}
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
			ApplyFilters();
		}
	}

	private void OnRefreshClicked(object? sender, EventArgs e)
	{
		LoadReferenceData();
		ApplyFilters();
	}

	private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
	{
		ApplyFilters();
	}

	private void OnSiteFilterChanged(object? sender, EventArgs e)
	{
		ApplyFilters();
	}

	private void OnServiceFilterChanged(object? sender, EventArgs e)
	{
		ApplyFilters();
	}

	private void OnClearSearchClicked(object? sender, EventArgs e)
	{
		SearchEntry.Text = string.Empty;
		if (SiteFilterPicker.ItemsSource is not null)
		{
			SiteFilterPicker.SelectedIndex = 0;
		}

		if (ServiceFilterPicker.ItemsSource is not null)
		{
			ServiceFilterPicker.SelectedIndex = 0;
		}

		ApplyFilters();
	}

	private async void OnSalarieSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not SalarieItemViewModel selected)
		{
			return;
		}

		_editingSalarieId = selected.Id;
		NomEntry.Text = selected.Nom;
		PrenomEntry.Text = selected.Prenom;
		EmailEntry.Text = selected.Email;
		FixeEntry.Text = selected.TelephoneFixe;
		PortableEntry.Text = selected.TelephonePortable;

		var serviceIndex = _services.FindIndex(s => s.Id == selected.ServiceId);
		if (serviceIndex >= 0)
		{
			ServicePicker.SelectedIndex = serviceIndex;
		}

		var siteIndex = _sites.FindIndex(s => s.Id == selected.SiteId);
		if (siteIndex >= 0)
		{
			SitePicker.SelectedIndex = siteIndex;
		}

		await DisplayAlertAsync(
			"Fiche salarie",
			$"Nom: {selected.Nom}\nPrenom: {selected.Prenom}\nTelephone fixe: {selected.TelephoneFixe}\nTelephone portable: {selected.TelephonePortable}\nEmail: {selected.Email}\nService: {selected.ServiceNom}\nSite: {selected.SiteVille}",
			"OK");

		if (sender is CollectionView collectionView)
		{
			collectionView.SelectedItem = null;
		}
	}

	private async void OnAddSiteClicked(object? sender, EventArgs e)
	{
		if (!_isAdminMode)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(SiteVilleEntry.Text))
		{
			await DisplayAlertAsync("Champ requis", "Saisis la ville du site.", "OK");
			return;
		}

		_siteService.Ajouter(new Site { Ville = SiteVilleEntry.Text.Trim() });
		SiteVilleEntry.Text = string.Empty;
		LoadReferenceData();
		ApplyFilters();
	}

	private async void OnUpdateSiteClicked(object? sender, EventArgs e)
	{
		if (SiteAdminPicker.SelectedItem is not Site selectedSite)
		{
			await DisplayAlertAsync("Selection requise", "Selectionne un site a modifier.", "OK");
			return;
		}

		if (string.IsNullOrWhiteSpace(SiteVilleEntry.Text))
		{
			await DisplayAlertAsync("Champ requis", "Saisis la nouvelle ville.", "OK");
			return;
		}

		selectedSite.Ville = SiteVilleEntry.Text.Trim();
		_siteService.Modifier(selectedSite);
		LoadReferenceData();
		ApplyFilters();
	}

	private async void OnDeleteSiteClicked(object? sender, EventArgs e)
	{
		if (SiteAdminPicker.SelectedItem is not Site selectedSite)
		{
			await DisplayAlertAsync("Selection requise", "Selectionne un site a supprimer.", "OK");
			return;
		}

		var salaries = _salarieService.Lister();
		if (salaries.Any(s => s.SiteId == selectedSite.Id))
		{
			await DisplayAlertAsync("Suppression impossible", "Ce site est utilise par des salaries.", "OK");
			return;
		}

		_siteService.Supprimer(selectedSite.Id);
		SiteVilleEntry.Text = string.Empty;
		LoadReferenceData();
		ApplyFilters();
	}

	private void OnSiteAdminSelectionChanged(object? sender, EventArgs e)
	{
		if (SiteAdminPicker.SelectedItem is Site selectedSite)
		{
			SiteVilleEntry.Text = selectedSite.Ville;
		}
	}

	private async void OnAddServiceClicked(object? sender, EventArgs e)
	{
		if (!_isAdminMode)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(ServiceNomEntry.Text))
		{
			await DisplayAlertAsync("Champ requis", "Saisis le nom du service.", "OK");
			return;
		}

		_serviceService.Ajouter(new Service { Nom = ServiceNomEntry.Text.Trim() });
		ServiceNomEntry.Text = string.Empty;
		LoadReferenceData();
		ApplyFilters();
	}

	private async void OnUpdateServiceClicked(object? sender, EventArgs e)
	{
		if (ServiceAdminPicker.SelectedItem is not Service selectedService)
		{
			await DisplayAlertAsync("Selection requise", "Selectionne un service a modifier.", "OK");
			return;
		}

		if (string.IsNullOrWhiteSpace(ServiceNomEntry.Text))
		{
			await DisplayAlertAsync("Champ requis", "Saisis le nouveau nom du service.", "OK");
			return;
		}

		selectedService.Nom = ServiceNomEntry.Text.Trim();
		_serviceService.Modifier(selectedService);
		LoadReferenceData();
		ApplyFilters();
	}

	private async void OnDeleteServiceClicked(object? sender, EventArgs e)
	{
		if (ServiceAdminPicker.SelectedItem is not Service selectedService)
		{
			await DisplayAlertAsync("Selection requise", "Selectionne un service a supprimer.", "OK");
			return;
		}

		var salaries = _salarieService.Lister();
		if (salaries.Any(s => s.ServiceId == selectedService.Id))
		{
			await DisplayAlertAsync("Suppression impossible", "Ce service est utilise par des salaries.", "OK");
			return;
		}

		_serviceService.Supprimer(selectedService.Id);
		ServiceNomEntry.Text = string.Empty;
		LoadReferenceData();
		ApplyFilters();
	}

	private void OnServiceAdminSelectionChanged(object? sender, EventArgs e)
	{
		if (ServiceAdminPicker.SelectedItem is Service selectedService)
		{
			ServiceNomEntry.Text = selectedService.Nom;
		}
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

	private static string NormalizeName(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "Inconnu";
		}

		var trimmed = value.Trim();
		return char.ToUpperInvariant(trimmed[0]) + trimmed[1..].ToLowerInvariant();
	}

	private void ClearForm()
	{
		NomEntry.Text = string.Empty;
		PrenomEntry.Text = string.Empty;
		EmailEntry.Text = string.Empty;
		FixeEntry.Text = string.Empty;
		PortableEntry.Text = string.Empty;
	}

	private void RegisterAdminShortcut()
	{
#if WINDOWS
		Loaded += OnMainPageLoaded;
#endif
	}

#if WINDOWS
	private void OnMainPageLoaded(object? sender, EventArgs e)
	{
		if (_shortcutRegistered)
		{
			return;
		}

		var appWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
		if (appWindow?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
		{
			return;
		}

		if (nativeWindow.Content is not FrameworkElement root)
		{
			return;
		}

		root.KeyDown += OnRootKeyDown;
		_shortcutRegistered = true;
	}

	private async void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key != VirtualKey.A)
		{
			return;
		}

		var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
		var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
		var ctrlDown = (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
		var shiftDown = (shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

		if (!ctrlDown || !shiftDown)
		{
			return;
		}

		e.Handled = true;
		await ToggleAdminModeAsync();
	}
#endif

	public class SalarieItemViewModel
	{
		public int Id { get; set; }
		public string Nom { get; set; } = string.Empty;
		public string Prenom { get; set; } = string.Empty;
		public string NomComplet { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string TelephoneFixe { get; set; } = string.Empty;
		public string TelephonePortable { get; set; } = string.Empty;
		public int ServiceId { get; set; }
		public int SiteId { get; set; }
		public string ServiceNom { get; set; } = string.Empty;
		public string SiteVille { get; set; } = string.Empty;
		public string Details { get; set; } = string.Empty;
		public bool IsAdminActionsVisible { get; set; }
	}

	private class SiteFilterItem
	{
		public int? Id { get; set; }
		public string Ville { get; set; } = string.Empty;
	}

	private class ServiceFilterItem
	{
		public int? Id { get; set; }
		public string Nom { get; set; } = string.Empty;
	}
}
