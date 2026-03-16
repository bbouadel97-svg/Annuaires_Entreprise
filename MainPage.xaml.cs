using System.Collections.ObjectModel;
using AnnuaireEntreprise.Data;
using AnnuaireEntreprise.Models;
using AnnuaireEntreprise.Services;

namespace AnnuaireEntreprise;

public partial class MainPage : ContentPage
{
	private readonly SalarieService _salarieService = new();
	private readonly ServiceService _serviceService = new();
	private readonly SiteService _siteService = new();

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
		LoadSalaries();
	}

	private void InitializeDatabase()
	{
		var db = new Database();
		db.CreateTables();
		db.SeedData();
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

	private SalarieItemViewModel ToViewModel(Salarie salarie)
	{
		var serviceName = _services.Find(s => s.Id == salarie.ServiceId)?.Nom ?? "Service inconnu";
		var siteName = _sites.Find(s => s.Id == salarie.SiteId)?.Ville ?? "Site inconnu";

		return new SalarieItemViewModel
		{
			Id = salarie.Id,
			NomComplet = $"{salarie.Nom} {salarie.Prenom}",
			Email = salarie.Email,
			Details = $"Service: {serviceName} | Site: {siteName} | Fixe: {salarie.TelephoneFixe} | Portable: {salarie.TelephonePortable}"
		};
	}

	private async void OnAddClicked(object? sender, EventArgs e)
	{
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

	private void OnDeleteClicked(object? sender, EventArgs e)
	{
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
	}
}
