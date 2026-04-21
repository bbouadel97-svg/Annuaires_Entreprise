using AnnuaireEntreprise.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QColors = QuestPDF.Helpers.Colors;

namespace AnnuaireEntreprise.Services;

public class PdfExportService
{
	public byte[] BuildEmployeeDirectoryPdf(
		IReadOnlyCollection<Salarie> salaries,
		Func<int, string> resolveServiceName,
		Func<int, string> resolveSiteName,
		string databasePath)
	{
		return Document.Create(container =>
		{
			foreach (var salarie in salaries)
			{
				var serviceName = resolveServiceName(salarie.ServiceId);
				var siteName = resolveSiteName(salarie.SiteId);

				container.Page(page =>
				{
					page.Margin(32);
					page.Size(PageSizes.A4);
					page.DefaultTextStyle(style => style.FontSize(11));
					page.Header().Column(column =>
					{
						column.Item().Text("Annuaire Entreprise").Bold().FontSize(22).FontColor(QColors.Blue.Darken2);
						column.Item().Text("Fiche salarie").FontSize(14).FontColor(QColors.Grey.Darken1);
					});

					page.Content().PaddingVertical(18).Column(column =>
					{
						column.Spacing(14);
						column.Item().Border(1).BorderColor(QColors.Grey.Lighten2).Padding(16).Column(card =>
						{
							card.Spacing(8);
							card.Item().Text($"{salarie.Nom} {salarie.Prenom}").Bold().FontSize(18);
							card.Item().Text($"Email: {FallbackValue(salarie.Email)}");
							card.Item().Text($"Telephone fixe: {FallbackValue(salarie.TelephoneFixe)}");
							card.Item().Text($"Telephone portable: {FallbackValue(salarie.TelephonePortable)}");
							card.Item().Text($"Service: {serviceName}");
							card.Item().Text($"Site: {siteName}");
						});

						column.Item().Text($"Fichier de base: {databasePath}").FontSize(9).FontColor(QColors.Grey.Darken1);
					});

					page.Footer().AlignCenter().Text(text =>
					{
						text.Span("Genere le ");
						text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
						text.Span(" - Page ");
						text.CurrentPageNumber();
					});
				});
			}

			if (salaries.Count == 0)
			{
				container.Page(page =>
				{
					page.Margin(32);
					page.Size(PageSizes.A4);
					page.Content().AlignMiddle().AlignCenter().Text("Aucun salarie dans la base.").FontSize(18).Bold();
				});
			}
		}).GeneratePdf();
	}

	private static string FallbackValue(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? "Non renseigne" : value.Trim();
	}
}