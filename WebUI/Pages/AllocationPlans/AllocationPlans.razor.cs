using M2P.Web.Shell.Services;
using WebUI.Components;
using WebUI.Extensions;
using WebUI.Services;
using WebUI.Shared;
using WebUI.Theming;
using ComponentBase = Microsoft.AspNetCore.Components.ComponentBase;
using ConfirmDialog = WebUI.Components.ConfirmDialog;

namespace WebUI.Pages.AllocationPlans;

public partial class AllocationPlans : ComponentBase
{
    #region INJECTS

    [Inject]
    private IAllocationPlansService AllocationPlansService { get; init; } = default!;

    [Inject]
    private IODataService ODataService { get; init; } = default!;

    [Inject]
    private IDialogService DialogService { get; init; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; init; } = default!;

    [Inject]
    private IStringLocalizer<Resource> ResourceExt { get; init; } = default!;

    [Inject]
    private NavigationManager NavManager { get; init; } = default!;

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

    private IEnumerable<AllocationPlanStatus> AllocationPlanStatusList = AllocationPlanStatus.All();
    private IEnumerable<AllocationPlanType> AllocationPlanTypeList = AllocationPlanType.All();
    private IEnumerable<AllocationPlanEngine> AllocationPlanEngineList = AllocationPlanEngine.All();
    private IEnumerable<AllocationPlan> AllocationPlanList = Enumerable.Empty<AllocationPlan>();

    #region FILTERS

    private int? FilterNumber { get; set; }
    private AllocationPlanEngine? FilterEngine { get; set; }
    private DateTime? FilterStartDate { get; set; }
    private AllocationPlanStatus? FilterStatus { get; set; }
    private DateTime? FilterStartCreationDate { get; set; }
    private DateTime? FilterEndCreationDate { get; set; }
    private DateTime? FilteredStartCreationDate { get; set; }
    private DateTime? FilteredEndCreationDate { get; set; }
    private int? FilteredNumber { get; set; }
    private AllocationPlanEngine? FilteredEngine { get; set; }
    private DateTime? FilteredStartDate { get; set; }
    private AllocationPlanStatus? FilteredStatus { get; set; }

    #endregion FILTERS

    private MudTable<AllocationPlan> _table = default!;

    private Paging Paging = new Paging();

    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await QueryAllocationPlansAsync();
    }

    public async void OnFilterRequested()
    {
        await QueryAllocationPlansAsync();
    }

    private async Task QueryAllocationPlansAsync()
    {
        if (!Session.HasAccountPermissionTo(Permissions.MFG_VIEW_ALLOCATION_PLAN))
        {
            return;
        }

        IsLoading = true;

        if (FilterNumber != FilteredNumber
            || FilterEngine != FilteredEngine
            || FilterStartDate != FilteredStartDate
            || FilterStatus != FilteredStatus
            || FilterStartCreationDate != FilteredStartCreationDate
            || FilterEndCreationDate != FilteredEndCreationDate)
        {
            Paging.CurrentPage = 0;
        }
        FilteredNumber = FilterNumber;
        FilteredEngine = FilterEngine;
        FilteredStartDate = FilterStartDate;
        FilteredStatus = FilterStatus;
        FilteredStartCreationDate = FilterStartCreationDate;
        FilteredEndCreationDate = FilterEndCreationDate;

        AllocationPlanList = Enumerable.Empty<AllocationPlan>();
        Paging.TotalItems = 0;

        var query = await ODataService.QueryAllocationPlanAsync(q =>
        {
            q.OrderByDescending(x => x.CreationDate)
             .Top(Paging.PageSize)
             .Skip(Paging.SkipItems());

            if (FilterNumber != null)
            {
                q.Filter(p => p.Number == FilterNumber);
            }
            if (FilterEngine != null)
            {
                q.Filter(p => p.Engine.Name == FilterEngine.Name);
            }
            if (FilterStatus != null)
            {
                q.Filter(p => p.Status.Name == FilterStatus.Name);
            }
            if (FilterStartDate != null)
            {
                q.Filter(p => p.StartDate >= FilterStartDate);
            }
            if (FilterStartCreationDate.HasValue)
            {
                q.Filter(x => x.CreationDate >= FilterStartCreationDate.Value);
            }

            if (FilterEndCreationDate.HasValue)
            {
                q.Filter((x , i) => x.CreationDate <= i.ConvertDateTimeToString(FilterEndCreationDate.Value, "yyyy-MM-dd"));
            }
        });

        if (query.Valid)
        {
            query.Data.ToList().ForEach(a =>
            {
                a.Type = AllocationPlanTypeList.FirstOrDefault(x => x.Name == a.Type.Name)!;
                a.Engine = AllocationPlanEngineList.FirstOrDefault(x => x.Name == a.Engine.Name)!;
            });

            AllocationPlanList = query.Data;
            Paging.TotalItems = query.Total;
        }

        IsLoading = false;
    }

    private async Task PageChanged(int i)
    {
        Paging.CurrentPage = i - 1;
        await QueryAllocationPlansAsync();
    }

    private void OnAddAllocationPlan()
    {
        NavManager.NavigateTo(Route.AllocationPlansEngines);
    }

    private async Task OnHistoricChanges(AllocationPlan allocationPlan)
    {
        await HistoricDialog.ShowAsync(DialogService, allocationPlan.Id.ToString(), allocationPlan.GetType());
    }

    private async Task OnViewPlan(AllocationPlan allocationPlan)
    {
        if (allocationPlan.Engine.IsOne())
        {
            await ViewEngineOnePlansDialog.ShowAsync(DialogService,
                allocationPlan.StartDate,
                allocationPlan.EndDate,
                allocationPlan.Id);
            return;
        }

        await ViewEngineTwoPlansDialog.ShowAsync(DialogService, allocationPlan.Id);
    }

    private async Task OnViewTeam(AllocationPlan allocationPlan)
    {
        if (!Session.HasAccountPermissionTo(Permissions.MFG_VIEW_TEAMS))
        {
            SnackbarService.Add(Resource.WithoutPermission, Severity.Error);
            return;
        }
        await ViewTeamsDialog.ShowAsync(DialogService, allocationPlan.Id, allocationPlan.Engine);
    }

    private async Task OnValidate(AllocationPlan allocationPlan)
    {
        IsLoading = true;
        var result = await AllocationPlansService.ValidateAllocationPlanAsync(allocationPlan.Id);
        if (result.Valid)
        {
            await QueryAllocationPlansAsync();
            SnackbarService.Add(Resource.AllocationPlanValidateSuccess, Severity.Success);
            return;
        }

        IsLoading = false;
        SnackbarService.Add(
               ResourceExt.ResultFailOrDefault(result, Resource.AllocationPlanValidateFail),
               Severity.Error);
    }

    private async Task OnClone(AllocationPlan allocationPlan)
    {
        if (!Session.HasAccountPermissionTo(Permissions.MFG_CLONE_ALLOCATION_PLAN))
        {
            SnackbarService.Add(Resource.WithoutPermission, Severity.Error);
            return;
        }

        IsLoading = true;
        var result = await AllocationPlansService.CloneAllocationPlanAsync(allocationPlan.Id);
        if (result.Valid)
        {
            await QueryAllocationPlansAsync();
            SnackbarService.Add(Resource.AllocationPlanCloneSuccess, Severity.Success);
            return;
        }

        IsLoading = false;
        SnackbarService.Add(
               ResourceExt.ResultFailOrDefault(result, Resource.AllocationPlanCloneFail),
               Severity.Error);
    }

    private void OnEdit(AllocationPlan allocationPlan)
    {
        if (!Session.HasAccountPermissionTo(Permissions.MFG_EDIT_ALLOCATION_PLAN))
        {
            SnackbarService.Add(Resource.WithoutPermission, Severity.Error);
            return;
        }

        if (allocationPlan.Engine.IsOne())
        {
            NavManager.NavigateTo(Route.AllocationPlansEnginesEditPrimary + allocationPlan.Id);
            return;
        }

        NavManager.NavigateTo(Route.AllocationPlansEnginesEditSecondary + allocationPlan.Id);
    }

    private async Task OnDelete(AllocationPlan allocationPlan)
    {
        if (!Session.HasAccountPermissionTo(Permissions.MFG_DELETE_ALLOCATION_PLAN))
        {
            SnackbarService.Add(Resource.WithoutPermission, Severity.Error);
            return;
        }
        var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
           Resource.DeleteAllocationPlanMessage, Resource.Delete);
        if (result)
        {
            await DeleteAllocationPlan(allocationPlan);
        }
    }

    private async Task DeleteAllocationPlan(AllocationPlan allocationPlan)
    {
        IsLoading = true;
        var result = await AllocationPlansService.DeleteAllocationPlanAsync(allocationPlan.Id);
        if (result.Valid)
        {
            SnackbarService.Add(Resource.AllocationPlanDeleteSuccess, Severity.Success);

            Paging.UpdateCurrentPageAfterDeleteItem();
            await QueryAllocationPlansAsync();
            return;
        }

        IsLoading = false;
        SnackbarService.Add(
               ResourceExt.ResultFailOrDefault(result, Resource.AllocationPlanDeleteFail),
               Severity.Error);
    }
}