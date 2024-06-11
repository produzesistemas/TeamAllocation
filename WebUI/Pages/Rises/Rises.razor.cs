using M2P.Web.Shell.Services;
using System.Linq.Expressions;
using System.Runtime.Intrinsics.Arm;
using WebUI.Components;
using WebUI.Extensions;
using WebUI.Services;
using WebUI.Shared;
using WebUI.Theming;
using ComponentBase = Microsoft.AspNetCore.Components.ComponentBase;
using ConfirmDialog = WebUI.Components.ConfirmDialog;

namespace WebUI.Pages.Rises;

public partial class Rises : ComponentBase
{
    #region INJECTS

    [Inject] private IRisesService RisesService { get; init; } = default!;
    [Inject] private IODataService ODataService { get; init; } = default!;
    [Inject] private IDialogService DialogService { get; init; } = default!;
    [Inject] private ISnackbar SnackbarService { get; init; } = default!;
    [Inject] private NavigationManager NavManager { get; init; } = default!;
    [Inject] private IStringLocalizer<Resource> ResourceExt { get; init; } = default!;
    [Inject] private ISessionManager Session { get; init; } = default!;

    #endregion INJECTS

    #region PROPS

    private bool loading = true;

    private bool IsLoading
    {
        get => loading;
        set
        {
            loading = value;
            StateHasChanged();
        }
    }

    private IEnumerable<Rise> RiseList = Enumerable.Empty<Rise>();
    private List<int> RiseDays = new List<int>();

    private MudTable<Rise> _table = default!;

    private Paging Paging = new Paging();

    #endregion PROPS

    #region FILTERS

    private DateTime? FilterStartCreationDate { get; set; }
    private DateTime? FilterEndCreationDate { get; set; }
    private bool DisableCategoryList = true;
    private IEnumerable<Product> Products = Enumerable.Empty<Product>();
    public IEnumerable<string> Categories { get; set; } = Enumerable.Empty<string>();
    private Product? FilterProduct { get; set; } = default;
    private string FilterCategory { get; set; } = string.Empty;
    private int? TrainingMin { get; set; }
    private int? TrainingMax { get; set; }

    private DateTime? FilteredStartCreationDate { get; set; }
    private DateTime? FilteredEndCreationDate { get; set; }
    private int? FilteredTrainingMin { get; set; }
    private int? FilteredTrainingMax { get; set; }
    private Product? FilteredProduct { get; set; } = default;
    private string FilteredCategory { get; set; } = string.Empty;

    #endregion FILTERS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await QueryRisesAsync();
        _ = QueryCategoriesAsync();
        _ = QueryProductsAsync();
    }

    public async void OnFilterRequested()
    {
        await QueryRisesAsync();
    }

    private async Task QueryRisesAsync()
    {
        if (!Session.HasAccountPermissionTo(Permissions.MFG_VIEW_RAMP_CONFIGURATION))
        {
            return;
        }

        IsLoading = true;

        if (FilterCategory != FilteredCategory
    || TrainingMin != FilteredTrainingMin
    || TrainingMax != FilteredTrainingMax
    || FilterProduct  != FilteredProduct
    || FilterStartCreationDate != FilteredStartCreationDate
    || FilterEndCreationDate != FilteredEndCreationDate)
        {
            Paging.CurrentPage = 0;
        }
        FilteredCategory = FilterCategory;
        FilteredTrainingMin = TrainingMin;
        FilteredTrainingMax = TrainingMax;
        FilteredProduct = FilterProduct;
        FilteredStartCreationDate = FilterStartCreationDate;
        FilteredEndCreationDate = FilterEndCreationDate;

        RiseList = Enumerable.Empty<Rise>();
        RiseDays = new List<int>();
        Paging.TotalItems = 0;

        var query = await ODataService.QueryRiseAsync(q =>
        {
            q.OrderByDescending(x => x.CreationDate);
            q.Top(Paging.PageSize);
            q.Skip(Paging.SkipItems());

            if (FilterStartCreationDate.HasValue)
            {
                q.Filter(x => x.CreationDate >= FilterStartCreationDate.Value);
            }

            if (FilterEndCreationDate.HasValue)
            {
                q.Filter((x, i) => x.CreationDate <= i.ConvertDateTimeToString(FilterEndCreationDate.Value, "yyyy-MM-dd"));
            }

            if (FilterProduct != null)
            {
                q.Filter(x => x.ProductId == FilterProduct.Id);
            }

            if (!string.IsNullOrWhiteSpace(FilterCategory))
            {
                q.Filter(x => x.Category == FilterCategory);
            }

            if (TrainingMin != null)
            {
                q.Filter(x => x.TrainingMin == TrainingMin);
            }

            if (TrainingMax != null)
            {
                q.Filter(x => x.TrainingMax == TrainingMax);
            }
        },
        expanded: true);

        if (query.Valid)
        {
            RiseList = query.Data;
            Paging.TotalItems = query.Total;
        }

        var maxRiseDays = 0;
        foreach (var item in RiseList)
        {
            if (maxRiseDays < item.RiseParams.Count())
            {
                maxRiseDays = item.RiseParams.Count();
            }
        }
        for (int i = 1; i <= maxRiseDays; i++)
        {
            RiseDays.Add(i);
        }

        IsLoading = false;
    }

    private async Task QueryCategoriesAsync()
    {
        if (!Categories.Any())
        {
            var odataCategories = await ODataService.QueryCategoriesAsync(query =>
            {
                query.OrderBy(x => x.Category);
            });
            if (odataCategories.Data.Any())
            {
                Categories = odataCategories.Data;
                DisableCategoryList = false;
                StateHasChanged();
            }
        }
    }

    private async Task QueryProductsAsync()
    {
        if (!Products.Any())
        {
            var resultProducts = await ODataService.GetProductsAsync(
                query => query.OrderBy(x => x.Name), 
                includeWithoutWorkstations: true
            );
            if (resultProducts != null)
            {
                Products = resultProducts.Data;
            }
        }
    }

    private Task<IEnumerable<Product>> SearchProductAsync(string filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return Task.FromResult(Products);
        }

        return Task.FromResult(Products.Where(x => x.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)));
    }

    private async Task PageChanged(int i)
    {
        Paging.CurrentPage = i - 1;
        await QueryRisesAsync();
    }

    private void OnAddSetup()
    {
        NavManager.NavigateTo(Route.RisesAdd);
    }

    private async Task OnClone(Rise rise)
    {
        if (!Session.HasAccountPermissionTo(Permissions.MFG_CLONE_RAMP_CONFIGURATION))
        {
            SnackbarService.Add(Resource.WithoutPermission, Severity.Error);
            return;
        }
        IsLoading = true;
        var result = await RisesService.CloneRiseAsync(rise.Id);
        if (result.Valid)
        {
            await QueryRisesAsync();
            SnackbarService.Add(Resource.RiseCloneSuccess, Severity.Success);
            return;
        }

        SnackbarService.Add(
              ResourceExt.ResultFailOrDefault(result, Resource.RiseCloneFail),
              Severity.Error);

        IsLoading = false;
    }

    private async Task OnShowHistory(Rise rise)
    {
        await HistoricDialog.ShowAsync(DialogService, rise.Id.ToString(), rise.GetType());
    }

    private void OnEdit(Rise rise)
    {
        if (!Session.HasAccountPermissionTo(Permissions.MFG_EDIT_RAMP_CONFIGURATION))
        {
            SnackbarService.Add(Resource.WithoutPermission, Severity.Error);
            return;
        }
        NavManager.NavigateTo(Route.RisesEdit + rise.Id);
    }

    private async Task OnDelete(Rise rise)
    {
        if (!Session.HasAccountPermissionTo(Permissions.MFG_DELETE_RAMP_CONFIGURATION))
        {
            SnackbarService.Add(Resource.WithoutPermission, Severity.Error);
            return;
        }
        var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
            Resource.DeleteRiseMessage, Resource.Delete);
        if (result)
        {
            await DeleteRise(rise);
        }
    }

    private async Task DeleteRise(Rise rise)
    {
        IsLoading = true;
        var result = await RisesService.DeleteRiseAsync(rise.Id);
        if (result.Valid)
        {
            SnackbarService.Add(Resource.RiseDeleteSuccess, Severity.Success);

            Paging.UpdateCurrentPageAfterDeleteItem();
            await QueryRisesAsync();
            return;
        }

        SnackbarService.Add(
              ResourceExt.ResultFailOrDefault(result, Resource.RiseDeleteFail),
              Severity.Error);

        IsLoading = false;
    }
}