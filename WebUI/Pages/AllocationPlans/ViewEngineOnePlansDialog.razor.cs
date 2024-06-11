using WebUI.Services;
using WebUI.Shared;
using Dialog = M2P.WebUI.Dialogs.Dialog;

namespace WebUI.Pages.AllocationPlans;

public partial class ViewEngineOnePlansDialog : Dialog
{
    #region PARAMETERS
    [Parameter] public DateTime StartDate { get; set; } = default!;
    [Parameter] public DateTime EndDate { get; set; } = default!;
    [Parameter] public IEnumerable<ProductionPlan> ProductionPlans { get; set; } = default!;
    [Parameter] public Guid AllocationPlanId { get; set; } = default!;
    #endregion PARAMETERS

    #region INJECTS
    [Inject] private IODataService ODataService { get; init; } = default!;
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

    private List<DateTime> Dates { get; set; } = new List<DateTime>();
    private IEnumerable<Shift> Shifts { get; set; } = Enumerable.Empty<Shift>();
    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await QueryDataAsync();
    }

    private async Task QueryDataAsync()
    {
        IsLoading = true;

        if (AllocationPlanId != Guid.Empty)
        {
            var executor = new ProductionPlansQueryExecutor(ODataService);
            ProductionPlans = (await executor.ForEngineOne(AllocationPlanId)).ToList();
        }

        Shifts = Enumerable.Empty<Shift>();
        var query = await ODataService.QueryShiftAsync(q =>
        {
            q.OrderBy(x => x.Name);
        });
        if (query.Valid)
        {
            Shifts = query.Data;
        }

        var date = StartDate;
        while (date <= EndDate)
        {
            Dates.Add(date);
            date = date.AddDays(1);
        }

        IsLoading = false;
    }

    private string GetQuantity(ProductionPlan productionPlan, Guid shiftId, DateTime dateTime)
    {
        if (productionPlan.ShiftId == shiftId)
        {
            var productionDay = productionPlan.ProductionDays
                .Where(x => x.Date == dateTime)
                .FirstOrDefault();
            if (productionDay != null && productionDay.Quantity != 0)
            {
                return productionDay.Quantity.ToString();
            }
        }
        return "-";
    }

    public async static Task ShowAsync(IDialogService DialogService,
        DateTime StartDate, DateTime EndDate,
        IEnumerable<ProductionPlan> ProductionPlans)
    {
        await ShowAsync(DialogService, StartDate, EndDate, ProductionPlans, Guid.Empty);
    }

    public async static Task ShowAsync(IDialogService DialogService,
        DateTime StartDate, DateTime EndDate,
        Guid AllocationPlanId)
    {
        await ShowAsync(DialogService, StartDate, EndDate, Enumerable.Empty<ProductionPlan>(), AllocationPlanId);
    }

    private async static Task ShowAsync(IDialogService DialogService,
        DateTime StartDate, DateTime EndDate,
        IEnumerable<ProductionPlan> ProductionPlans,
        Guid AllocationPlanId)
    {
        var parameters = new DialogParameters<ViewEngineOnePlansDialog>
        {
            { x => x.StartDate, StartDate },
            { x => x.EndDate, EndDate },
            { x => x.ProductionPlans, ProductionPlans },
            { x => x.AllocationPlanId, AllocationPlanId }
        };
        var options = new DialogOptions() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialog = await DialogService.ShowAsync<ViewEngineOnePlansDialog>(Resource.AllocationPlan, parameters, options);
        await dialog.Result;
    }
}