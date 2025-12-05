using CustomerOpinionETL.Dashboard.Models;
using CustomerOpinionETL.Dashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CustomerOpinionETL.Dashboard.Pages;

public class IndexModel : PageModel
{
    private readonly IDashboardDataService _dataService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IDashboardDataService dataService,
        ILogger<IndexModel> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public DashboardViewModel DashboardData { get; set; } = new();
    public List<CategoryOption> Categories { get; set; } = new();
    public List<SourceOption> Sources { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Categoria { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Fuente { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Clasificacion { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var filters = new DashboardFilters
            {
                StartDate = StartDate,
                EndDate = EndDate,
                Categoria = Categoria,
                Fuente = Fuente,
                Clasificacion = Clasificacion
            };

            DashboardData = await _dataService.GetDashboardDataAsync(filters);
            Categories = await _dataService.GetCategoriesAsync();
            Sources = await _dataService.GetSourcesAsync();

            _logger.LogInformation("Dashboard loaded with filters: {Filters}",
                System.Text.Json.JsonSerializer.Serialize(filters));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            // Continuar con datos vacíos
        }
    }

    public IActionResult OnPostClearFilters()
    {
        return RedirectToPage();
    }
}