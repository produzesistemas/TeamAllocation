using Microsoft.AspNetCore.Components.Forms;
using WebUI.Extensions;
using WebUI.Services;
using WebUI.Shared;

using ConfirmDialog = WebUI.Components.ConfirmDialog;
using ComponentBase = Microsoft.AspNetCore.Components.ComponentBase;
using Microsoft.JSInterop;
using WebUI.Components;
using M2P.Web.Shell.Services;

namespace WebUI.Pages.AllocationPlans;

public partial class AddEngineOneAllocationPlan : ComponentBase
{
    public enum AddAllocationPlanState
    {
        Begin,
        ProductionPlan,
        TeamsDefinition,
        Finish
    }

    #region INJECTS

    [Inject] private IAllocationPlansService AllocationPlansService { get; init; } = default!;
    [Inject] private IODataService ODataService { get; init; } = default!;
    [Inject] private IDialogService DialogService { get; init; } = default!;
    [Inject] private NavigationManager NavManager { get; init; } = default!;
    [Inject] private ISnackbar SnackbarService { get; init; } = default!;
    [Inject] private IStringLocalizer<Resource> ResourceExt { get; init; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; init; } = default!;
    [Inject] private ISessionManager SessionManager { get; init; } = default!;

    #endregion INJECTS

    #region PARAMETERS
    [Parameter] public string AllocationPlanId { get; set; } = default!;
    #endregion PARAMETERS

    #region PROPS
    private bool loading = false;
    private bool IsLoading
    {
        get => loading;
        set
        {
            loading = value;
            StateHasChanged();
        }
    }
    private AddAllocationPlanState State { get; set; } = AddAllocationPlanState.Begin;
    private DateTime? _StartDate { get; set; }
    private DateTime? StartDate
    {
        get => _StartDate;
        set
        {
            if (value != _StartDate)
            {
                _ = HandleStartDateChange(value);
            }
        }
    }
    private DateTime? EndDate { get; set; }

    //TODO: read it from the select field
    private AllocationPlanType? AllocationPlanType { get; set; } = AllocationPlanType.Box();
    private AllocationPlanEngine? AllocationPlanEngine { get; set; }
    private List<ProductionPlan> ProductionPlans = new List<ProductionPlan>();

    private MudForm Form { get; set; } = default!;
    private bool ValidationSuccess { get; set; }
    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await QueryData();
    }

    private async Task QueryData()
    {
        IsLoading = true;

        if (IsEdit())
        {
            var allocationPlanQuery = await ODataService.AllocationPlanByIdAsync(
                Guid.Parse(AllocationPlanId));

            if (!allocationPlanQuery.Valid || allocationPlanQuery.Data == null)
            {
                SnackbarService.Add(Resource.AllocationPlanNotFound, Severity.Error);
                NavManager.NavigateTo(Route.AllocationPlans);
                return;
            }

            AllocationPlan allocationPlan = allocationPlanQuery.Data;
            StartDate = allocationPlan.StartDate;
            EndDate = allocationPlan.EndDate;
            AllocationPlanType = allocationPlan.Type;
            AllocationPlanEngine = allocationPlan.Engine;

            if (AllocationPlanEngine.IsOne())
            {
                var executor = new ProductionPlansQueryExecutor(ODataService);
                ProductionPlans = (await executor.ForEngineOne(allocationPlan.Id)).ToList();
            }
        }

        IsLoading = false;
    }

    private async Task HandleStartDateChange(DateTime? newValue)
    {
        if (!ProductionPlans.Any())
        {
            _StartDate = newValue;
            EndDate = null;
            return;
        }
        var result = await ConfirmDialog.ShowAsync(DialogService, Resource.EditStartDate,
            Resource.AllocatedTeamsWillBeRemoved, Resource.Confirm);
        if (result)
        {
            _StartDate = newValue;
            EndDate = null;
            OnRemoveTeams(ignoreConfirmation: true);
        }
    }

    private void OnAddProductionPlan()
    {
        if (State == AddAllocationPlanState.Begin)
        {
            State = AddAllocationPlanState.ProductionPlan;
            return;
        }
    }

    private void OnEditProductionPlans()
    {
        State = AddAllocationPlanState.ProductionPlan;
    }

    private void OnDeleteProductionPlan(ProductionPlan item)
    {
        ProductionPlans.Remove(item);

        UpdateEndDate();
    }

    private async Task OnAutoAddTeams()
    {
        try
        {
        IsLoading = true;      
            var startDate = (DateTime)StartDate!;
            var result = await AllocationPlansService.AllocateTeamSync(startDate, ProductionPlans);
            if (result != null 
                && result.Valid
                && result.Data != null
                )
            {
                foreach (var plan in result.Data)
                {
                    var productionPlan = ProductionPlans.Find(p => p.Id == plan.Id);
                    if (productionPlan!= null)
                    {
                        ProccessAllocationResponse(productionPlan, plan);
                    }
                }
            }

            UpdateEndDate();
            IsLoading = false;
        } 
        catch (Exception ex)
        {
            SnackbarService.Add( ex.Message, Severity.Error);
        }
    }

    private void ProccessAllocationResponse(ProductionPlan productionPlan, ProductionPlan plan)
    {
        productionPlan.Team = plan.Team;
        productionPlan.TeamId = plan.TeamId;
        productionPlan.LayoutId = plan.LayoutId;
        productionPlan.Rise = plan.Rise;
        productionPlan.RiseId = plan.RiseId;
        productionPlan.ProductionDays = plan.ProductionDays;

        if ((plan.TeamId == Guid.Empty) || (plan.TeamId == null))
        {
            SnackbarService.Add(String.Format(Resource.TeamNotAllocatedToProductionPlan, productionPlan.Product.Name, productionPlan.Shift?.Name), Severity.Error);
        }
    }

    private async Task OnSelectTeam(ProductionPlan productionPlan)
    {
        var team = await SelectTeamDialog.ShowAsync(
            dialogService: DialogService,
            line: productionPlan.ProductionLine,
            product: productionPlan.Product,
            layout: productionPlan.Layout,
            shiftId: productionPlan.ShiftId!.Value,
            allocatedTeams: GetAllocatedTeamIds(productionPlan.ShiftId!.Value));

        if (team != null)
        {
            productionPlan.TeamId = team.Id;
            productionPlan.Team = team;

            await AllocateProductionPlan(productionPlan);
        }
    }

    private async void OnRemoveTeam(ProductionPlan productionPlan)
    {
        var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
                   Resource.DeleteTeamMessage, Resource.Delete);
        if (!result)
        {
            return;
        }

        productionPlan.TeamId = Guid.Empty;
        productionPlan.Team = default!;
 
        UpdateEndDate();
        this.StateHasChanged();
    }

    private async void OnRemoveTeams(bool ignoreConfirmation = false)
    {
        if (!ignoreConfirmation)
        {
            var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
                  Resource.DeleteAllocatedTeamsMessage, Resource.Delete);
            if (!result)
            {
                return;
            }
        }
       
        foreach (var item in ProductionPlans)
        {
            item.TeamId = Guid.Empty;
            item.Team = default!;
        }

        UpdateEndDate();
        this.StateHasChanged();
    }

    private async Task AllocateProductionPlan(ProductionPlan productionPlan)
    {
        IsLoading = true;

        productionPlan.ProductionDays = Enumerable.Empty<ProductionDay>();

        var startDate = (DateTime)StartDate!;
        var resultAllocate = await AllocationPlansService.AllocateProductionPlan(
            startDate, productionPlan);

        if (resultAllocate.Valid && resultAllocate.Data != null)
        {
            productionPlan.RiseId = resultAllocate.Data.RiseId;
            productionPlan.Rise = resultAllocate.Data.Rise;
            productionPlan.ProductionDays = resultAllocate.Data.ProductionDays;

            SnackbarService.Add(Resource.TeamAllocationSuccess, Severity.Success);
        }
        else
        {
            productionPlan.TeamId = Guid.Empty;
            productionPlan.Team = default!;
            SnackbarService.Add(Resource.TeamAllocationFail, Severity.Error);
        }
        
        UpdateEndDate();
        IsLoading = false;
    }

    private async Task OnViewTeam(ProductionPlan productionPlan)
    {
        await ViewTeamDialog.ShowAsync(DialogService, productionPlan.Team!, productionPlan.Product, productionPlan.LayoutId);
    }

    private async void OnSpreadsheetUpload(IBrowserFile file)
    {
        IsLoading = true;
        ProductionPlans = new List<ProductionPlan>();

        var resultImport = await AllocationPlansService.ImportProductionPlan(file);
        if (resultImport.Valid)
        {
            if (resultImport.Data != null && resultImport.Data.Any())
            {
                ProductionPlans = resultImport.Data.ToList();
                SnackbarService.Add(Resource.ImportSuccess, Severity.Success);
            }
            else
            {
                SnackbarService.Add(Resource.ImportFailed, 
                    new List<string>{ Resource.CheckSpreadsheetAndTryAgain },
                    Severity.Error);
            }
        }
        else if (resultImport.Errors.Any())
        {
            //var messages = resultImport.Errors.Select(x => ResourceExt.ResourceFormat(x.Name, x.Message)).ToList();
            SnackbarService.Add(Resource.ImportFailed, resultImport.Errors.GetLocalizedStrings(), Severity.Error);
        }

        IsLoading = false;
    }

    private void UpdateEndDate()
    {
        EndDate = null;
        foreach (ProductionPlan productionPlan in ProductionPlans)
        {
            if (productionPlan.ProductionDays != null && productionPlan.ProductionDays.Any())
            {
                var lastDatetime = productionPlan.ProductionDays.Last().Date;
                if (EndDate == null || lastDatetime > EndDate)
                {
                    EndDate = lastDatetime;
                }
            }
        }
    }

    private async Task OnPreview()
    {
        await ViewEngineOnePlansDialog.ShowAsync(DialogService, 
            (DateTime)StartDate!, (DateTime)EndDate!, ProductionPlans);
    }

    private IEnumerable<Team> GetAllocatedTeamIds(Guid shiftId)
    {
        return ProductionPlans
            .Where(x => x.ShiftId == shiftId && x.TeamId is not null && x.TeamId != Guid.Empty)
            .Select(x => x.Team)
            .Cast<Team>();
    }

    private bool HasAllocatedTeams()
    {
        return ProductionPlans
            .Where(x => x.TeamId is not null && x.TeamId != Guid.Empty)
            .Any();
    }

    #region GENERAL
    private bool IsEdit()
    {
        return !string.IsNullOrEmpty(AllocationPlanId);
    }

    private bool IsBeginOrFinishState()
    {
        return State == AddAllocationPlanState.Begin || State == AddAllocationPlanState.Finish;
    }

    private async void OnNext()
    {
        if (State == AddAllocationPlanState.ProductionPlan)
        {
            if (IsProductionPlanValid())
            {
                State = AddAllocationPlanState.TeamsDefinition;
            }
            return;
        }
        if (State == AddAllocationPlanState.TeamsDefinition)
        {
            State = AddAllocationPlanState.Finish;
            return;
        }
  
        if (!await IsAllocationPlanValid())
        {
            return;
        }

        IsLoading = true;

        if (IsEdit())
        {
            var reason = await ReasonDialog.ShowAsync(this.DialogService);
            if (reason is null)
            {
                IsLoading = false;
                return;
            }

            var editResult = await AllocationPlansService.UpdateAllocationPlanAsync(
                Guid.Parse(AllocationPlanId),
                (DateTime)StartDate!, (DateTime)EndDate!,
                AllocationPlanType!, AllocationPlanEngine!, ProductionPlans, reason, SessionManager.Account.SamAccount);

            if (editResult.Valid)
            {
                NavManager.NavigateTo(Route.AllocationPlans);
                SnackbarService.Add(Resource.AllocationPlanUpdateSuccess, Severity.Success);
                return;
            }

            SnackbarService.Add(
                ResourceExt.ResultFailOrDefault(editResult, Resource.AllocationPlanUpdateFail),
                Severity.Error);

            IsLoading = false;
            return;
        }

        var createResult = await AllocationPlansService.CreateAllocationPlanAsync(
            (DateTime)StartDate!, (DateTime)EndDate!, 
            AllocationPlanType!, AllocationPlanEngine.One(), ProductionPlans);

        if (createResult.Valid)
        {
            NavManager.NavigateTo(Route.AllocationPlans);
            SnackbarService.Add(Resource.AllocationPlanCreateSuccess, Severity.Success);
            return;
        }

        SnackbarService.Add(
            ResourceExt.ResultFailOrDefault(createResult, Resource.AllocationPlanCreateFail),
            Severity.Error);

        IsLoading = false;
    }

    private async Task OnBack()
    {
        if (State == AddAllocationPlanState.Begin)
        {
            await GoBack(Resource.Back);
            return;
        }
        if (State == AddAllocationPlanState.ProductionPlan)
        {
            State = AddAllocationPlanState.Begin;
            return;
        }
        if (State == AddAllocationPlanState.TeamsDefinition)
        {
            State = AddAllocationPlanState.ProductionPlan;
            return;
        }
        if (State == AddAllocationPlanState.Finish)
        {
            State = AddAllocationPlanState.TeamsDefinition;
            return;
        }
    }

    private void OnCancel()
    {
        _ = GoBack(Resource.Cancel);
    }

    private async Task GoBack(string title)
    {
        var canContinue = !ProductionPlans.Any();

        if (!canContinue)
        {
            canContinue = await ConfirmDialog.ShowAsync(DialogService, title,
                Resource.CancelAllocationPlanMessage, Resource.Confirm);
        }

        if (!canContinue)
        {
            return;
        }

        if (IsEdit())
        {
            NavManager.NavigateTo(Route.AllocationPlans);
            return;
        }
        NavManager.NavigateTo(Route.AllocationPlansEngines);
    }

    private bool IsProductionPlanValid()
    {
        //TODO: validate if there are duplicated produdtion plans
        return true;
    }
    private bool IsTeamDefinitionValid()
    {
        return ProductionPlans.Find(x => x.Team == null) == null;
    }
    private async Task<bool> IsAllocationPlanValid()
    {
        await Form.Validate();
        return ValidationSuccess;
    }

    private bool CanGoNext()
    {
        if (State == AddAllocationPlanState.Begin)
        {
            if (!ProductionPlans.Any() || EndDate == null)
            {
                return false;
            }
            return IsTeamDefinitionValid();
        }
        if (State == AddAllocationPlanState.TeamsDefinition)
        {
            return IsTeamDefinitionValid();
        }
        if (State == AddAllocationPlanState.Finish)
        {
            _ = IsAllocationPlanValid();
            return ValidationSuccess && IsTeamDefinitionValid() && EndDate != null;
        }
        return ProductionPlans.Any();
    }

    private string? QuantityValidation(int value)
    {
        if (value <= 0)
        {
            return ResourceExt.ValueMustBeGreaterThanZero(Resource.Quantity);
        }
        return null;
    }

    private string? SpeedValidation(int value)
    {
        if (value <= 0)
        {
            return ResourceExt.ValueMustBeGreaterThanZero(Resource.ProductionSpeed);
        }
        return null;
    }

    #endregion GENERAL

    #region DOWNLOAD_TEMPLATE
    private async void OnDownloadTemplate()
    {
        IsLoading = true;

        var fileBytes = await AllocationPlansService.DownloadPrimaryEngineTemplate();
        if (fileBytes != null)
        {
            if (fileBytes != null)
            {
                var fileName = "PrimaryEngineTemplate.xlsx";
                var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                var base64String = Convert.ToBase64String(fileBytes);
                var dataUrl = $"data:{mimeType};base64,{base64String}";
                await JSRuntime.InvokeVoidAsync("downloadFile", dataUrl, fileName);
            }
        }

        IsLoading = false;
    }
    #endregion DOWNLOAD_TEMPLATE
}
