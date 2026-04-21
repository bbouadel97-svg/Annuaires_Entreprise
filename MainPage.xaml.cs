using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
using AnnuaireEntreprise.Services;
#if WINDOWS
using Microsoft.UI.Xaml;
using Windows.System;
using WinKeyboardAccelerator = Microsoft.UI.Xaml.Input.KeyboardAccelerator;
using WinKeyboardAcceleratorInvokedEventArgs = Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs;
#endif

namespace AnnuaireEntreprise;

public partial class MainPage : ContentPage
{
	private readonly SalarieService _salarieService = new();
	private readonly ServiceService _serviceService = new();
	private readonly SiteService _siteService = new();
	private readonly RandomUserService _randomUserService = new();
	private readonly AuthService _authService = new();
	private readonly AppLogService _appLogService = new();
	private readonly PdfExportService _pdfExportService = new();
	private string _databasePath = string.Empty;
	private bool _isAdminMode;
	private bool _shortcutRegistered;
	private int? _editingSalarieId;
	private string _currentAdminUsername = string.Empty;
#if WINDOWS
	private FrameworkElement? _shortcutOwner;
	private WinKeyboardAccelerator? _adminKeyboardAccelerator;
#endif

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
		DbPathLabel.Text = $"Base locale: {_databasePath}\nLogs: {_appLogService.GetLogFilePath()}";
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
		LogoutButton.IsVisible = _isAdminMode;

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

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		_appLogService.LogAdminLogout(_currentAdminUsername);
		_isAdminMode = false;
		_currentAdminUsername = string.Empty;
		UpdateAdminModeUi();
		ApplyFilters();
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
			_appLogService.LogAdminAccess(username, false);
			await DisplayAlertAsync("Acces refuse", "Identifiant ou mot de passe incorrect.", "OK");
			return;
		}

		_isAdminMode = true;
		_currentAdminUsername = username.Trim();
		_appLogService.LogAdminAccess(_currentAdminUsername, true);
		UpdateAdminModeUi();
		ApplyFilters();
	}

	private async void OnAddClicked(object? sender, EventArgs e)
	{
		try
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
		catch (Exception ex)
		{
			_appLogService.LogError("Ajout salarie", ex);
			await DisplayAlertAsync("Erreur", $"Impossible d'ajouter le salarie: {ex.Message}", "OK");
		}
	}

	private async void OnUpdateSalarieClicked(object? sender, EventArgs e)
	{
		try
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
		catch (Exception ex)
		{
			_appLogService.LogError("Modification salarie", ex);
			await DisplayAlertAsync("Erreur", $"Impossible de modifier le salarie: {ex.Message}", "OK");
		}
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
			_appLogService.LogError("Import API", ex);
			await DisplayAlertAsync("Erreur API", $"Impossible d'importer depuis l'API: {ex.Message}", "OK");
		}
		finally
		{
			ImportApiButton.IsEnabled = true;
		}
	}

	private async void OnDeleteClicked(object? sender, EventArgs e)
	{
		try
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
		catch (Exception ex)
		{
			_appLogService.LogError("Suppression salarie", ex);
			await DisplayAlertAsync("Erreur", $"Impossible de supprimer le salarie: {ex.Message}", "OK");
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
		try
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
		catch (Exception ex)
		{
			_appLogService.LogError("Ajout site", ex);
			await DisplayAlertAsync("Erreur", $"Impossible d'ajouter le site: {ex.Message}", "OK");
		}
	}

	private async void OnUpdateSiteClicked(object? sender, EventArgs e)
	{
		try
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
		catch (Exception ex)
		{
			_appLogService.LogError("Modification site", ex);
			await DisplayAlertAsync("Erreur", $"Impossible de modifier le site: {ex.Message}", "OK");
		}
	}

	private async void OnDeleteSiteClicked(object? sender, EventArgs e)
	{
		try
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
		catch (Exception ex)
		{
			_appLogService.LogError("Suppression site", ex);
			await DisplayAlertAsync("Erreur", $"Impossible de supprimer le site: {ex.Message}", "OK");
		}
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
		try
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
		catch (Exception ex)
		{
			_appLogService.LogError("Ajout service", ex);
			await DisplayAlertAsync("Erreur", $"Impossible d'ajouter le service: {ex.Message}", "OK");
		}
	}

	private async void OnUpdateServiceClicked(object? sender, EventArgs e)
	{
		try
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
		catch (Exception ex)
		{
			_appLogService.LogError("Modification service", ex);
			await DisplayAlertAsync("Erreur", $"Impossible de modifier le service: {ex.Message}", "OK");
		}
	}

	private async void OnDeleteServiceClicked(object? sender, EventArgs e)
	{
		try
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
		catch (Exception ex)
		{
			_appLogService.LogError("Suppression service", ex);
			await DisplayAlertAsync("Erreur", $"Impossible de supprimer le service: {ex.Message}", "OK");
		}
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
			var salariesToExport = GetSalariesToExport();
			var pdfBytes = _pdfExportService.BuildEmployeeDirectoryPdf(
				salariesToExport,
				serviceId => _services.Find(s => s.Id == serviceId)?.Nom ?? "Service inconnu",
				siteId => _sites.Find(s => s.Id == siteId)?.Ville ?? "Site inconnu",
				_databasePath);

			File.WriteAllBytes(exportPath, pdfBytes);
			var exportScope = salariesToExport.Count == 1 ? "fiche salarie" : "fiches salaries";
			await DisplayAlertAsync("Export termine", $"PDF genere ({exportScope}):\n{exportPath}", "OK");
		}
		catch (Exception ex)
		{
			_appLogService.LogError("Export PDF", ex);
			await DisplayAlertAsync("Erreur", $"Impossible d'exporter en PDF: {ex.Message}", "OK");
		}
	}

	private List<Salarie> GetSalariesToExport()
	{
		if (_editingSalarieId.HasValue)
		{
			var selectedSalarie = _allSalaries.Find(s => s.Id == _editingSalarieId.Value);
			if (selectedSalarie is not null)
			{
				return [selectedSalarie];
			}
		}

		return _allSalaries.ToList();
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
		Unloaded += OnMainPageUnloaded;
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

		_shortcutOwner = root;
		_adminKeyboardAccelerator = new WinKeyboardAccelerator
		{
			Key = VirtualKey.A,
			Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift
		};
		_adminKeyboardAccelerator.Invoked += OnAdminShortcutInvoked;
		_shortcutOwner.KeyboardAccelerators.Add(_adminKeyboardAccelerator);

		_shortcutRegistered = true;
	}

	private void OnMainPageUnloaded(object? sender, EventArgs e)
	{
		if (_shortcutOwner is not null && _adminKeyboardAccelerator is not null)
		{
			_adminKeyboardAccelerator.Invoked -= OnAdminShortcutInvoked;
			_ = _shortcutOwner.KeyboardAccelerators.Remove(_adminKeyboardAccelerator);
		}

		_adminKeyboardAccelerator = null;
		_shortcutOwner = null;
		_shortcutRegistered = false;
	}

	private async void OnAdminShortcutInvoked(WinKeyboardAccelerator sender, WinKeyboardAcceleratorInvokedEventArgs args)
	{
		args.Handled = true;
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
