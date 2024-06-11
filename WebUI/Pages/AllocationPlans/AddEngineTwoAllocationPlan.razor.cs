using Microsoft.AspNetCore.Components.Forms;
using WebUI.Services;
using WebUI.Extensions;
using WebUI.Shared;
using Microsoft.JSInterop;

using ConfirmDialog = WebUI.Components.ConfirmDialog;
using ComponentBase = Microsoft.AspNetCore.Components.ComponentBase;
using WebUI.Components;
using M2P.Web.Shell.Services;

namespace WebUI.Pages.AllocationPlans
{
    public partial class AddEngineTwoAllocationPlan : ComponentBase
    {
        #region INJECTS
        [Inject] private IAllocationPlansService AllocationPlansService { get; init; } = default!;
        [Inject] private IODataService ODataService { get; init; } = default!;
        [Inject] private IDialogService DialogService { get; init; } = default!;
        [Inject] private NavigationManager NavManager { get; init; } = default!;
        [Inject] private ISnackbar SnackbarService { get; init; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; init; } = default!;
        [Inject] private IStringLocalizer<Resource> ResourceExt { get; init; } = default!;
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
        private IEnumerable<ProductionPlan> ProductionPlans { get; set; } = Enumerable.Empty<ProductionPlan>();
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

                var executor = new ProductionPlansQueryExecutor(ODataService);
                ProductionPlans = await executor.ForEngineTwo(Guid.Parse(AllocationPlanId), true);
            }

            IsLoading = false;
        }

        private async void OnSelectTeam(Guid shift, DateTime date, ProductionPlan productionPlan)
        {
            var team = await SelectTeamDialog.ShowAsync(
                dialogService: DialogService, 
                line: productionPlan.ProductionLine,
                product: productionPlan.Product, 
                layout: productionPlan.Layout,
                shiftId: shift,
                allocatedTeams: GetAllocatedTeam(shift, date)
                );

            if (team != null)
            {
                var teamProduct = team.GetTeamProduct(productionPlan.ProductId);
                    var getRiseResult = await AllocationPlansService.GetCandidateRise(teamProduct.Id);
                    //Applies in production day
                    productionPlan.SetTeamOnDay(shift, date, team, getRiseResult.Data);
                    this.StateHasChanged();
            }
        }

        private async void OnRemoveTeam(Guid shift, DateTime date, ProductionPlan productionPlan)
        {
            var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
                   Resource.DeleteTeamMessage, Resource.Delete);
            if (!result)
            {
                return;
            }
            productionPlan.RemoveTeamOnDay(shift, date);
            this.StateHasChanged();
        }

        private async void OnRemoveTeams()
        {
            var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
                       Resource.DeleteAllocatedTeamsMessage, Resource.Delete);
            if (!result)
            {
                return;
            }

            foreach (var productionPlan in ProductionPlans)
            {
                productionPlan.RemoveAllTeams();
            }
            this.StateHasChanged();
        }

        private async void OnSaveRequested()
        {
            var hasPriorityNotTrained = ProductionPlans.Where(x => x.Priority == 1)
                .Any(x => !x.IsFullTrained(AllocationPlanEngine.Two()));
         
            if (hasPriorityNotTrained)
            {
                var confimation = await ConfirmDialog.ShowAsync(DialogService, Resource.Priority,
                   Resource.ProductionPlanWithPriorityNotTrained, Resource.Confirm);
                if (!confimation)
                {
                    return;
                }
            }

            IsLoading = true;

            if (IsEdit())
            {
                var reason =  await ReasonDialog.ShowAsync(this.DialogService);
                if (reason is null)
                {
                    IsLoading = false;
                    return;
                }

                var editResult = await AllocationPlansService.UpdateAllocationPlanAsync(
                    Guid.Parse(AllocationPlanId),
                    ProductionPlans.GetStartDate(), ProductionPlans.GetEndDate(), 
                    AllocationPlanType.Box(), AllocationPlanEngine.Two(), ProductionPlans.ToList(), reason, SessionManager.Account.SamAccount);

                if (editResult.Valid)
                {
                    NavManager.NavigateTo(Route.AllocationPlans);
                    SnackbarService.Add(Resource.AllocationPlanUpdateSuccess, Severity.Success);
                    return;
                }

                SnackbarService.Add(Resource.Error, editResult.Errors.Select(e => e.Name), Severity.Error);

                IsLoading = false;
                return;
            }

            var createResult = await AllocationPlansService.CreateAllocationPlanAsync(
                ProductionPlans.GetStartDate(), ProductionPlans.GetEndDate(), 
                AllocationPlanType.Box(), AllocationPlanEngine.Two(), ProductionPlans.ToList());

            if (createResult.Valid)
            {
                NavManager.NavigateTo(Route.AllocationPlans);
                SnackbarService.Add(Resource.AllocationPlanCreateSuccess, Severity.Success);
                return;
            }
            SnackbarService.Add(Resource.Error, createResult.Errors.Select(e => e.Name), Severity.Error);

            IsLoading = false;
        }

        private async void OnSpreadsheetUpload(IBrowserFile file)
        {
            var canContinue = await ConfirmDataDisposal(Resource.SpreadsheetUpload);
            if (!canContinue)
            {
                return;
            }

            IsLoading = true;

            var result = await AllocationPlansService.AllocateOnSecondaryEngine(file);
            if (result.Valid && result.Data != null)
            {
                ProductionPlans = result.Data.ProductionPlans ?? Enumerable.Empty<ProductionPlan>();
                IsLoading = false;
                if (result.Data.Errors.Any())
                {
                    SnackbarService.Add(Resource.Error, result.Data.Errors.GetLocalizedStrings(), Severity.Error);
                }
                
            }
            else
            {
                SnackbarService.Add(Resource.Error, result.Errors.GetLocalizedStrings(), Severity.Error);
            }

            IsLoading = false;
        }

        private IEnumerable<Team> GetAllocatedTeam(Guid ShiftId, DateTime date)
        {
            var teams = ProductionPlans
                .SelectMany(p => p.ProductionDays
                    .Where(x =>
                        x.ShiftId == ShiftId
                        && x.Date.ToShortDateString() == date.ToShortDateString()
                        && x.Team is not null)
                    .Select(x => x.Team)
                )
                .DistinctBy(x => x.Id);
            return teams;
        }

        private bool HasAllocatedTeams()
        {
            return ProductionPlans
                .SelectMany(p => p.ProductionDays.Where(x => x.Team is not null))
                .Any();
        }

        private async void OnTemplateRequested()
        {
            IsLoading = true;

            var result = await AllocationPlansService.DownloadSecondaryEngineTemplate();
            if (result != null && result.Any())
            {
                var fileName = "SecondaryEngineTemplate.xlsx";
                var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var base64String = Convert.ToBase64String(result.ToArray());
                var dataUrl = $"data:{mimeType};base64,{base64String}";

                await JSRuntime.InvokeVoidAsync("downloadFile", dataUrl, fileName);
            }
            else
            {
                SnackbarService.Add(Resource.DownloadTemplateFail, Severity.Error);
            }

            IsLoading = false;
        }

        private bool IsEdit()
        {
            return !string.IsNullOrEmpty(AllocationPlanId);
        }

        private void OnBack()
        {
            _ = GoBack(Resource.Back);
        }

        private void OnCancel()
        {
            _ = GoBack(Resource.Cancel);
        }

        private async Task GoBack(string title)
        {
            var canContinue = await ConfirmDataDisposal(title);
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

        private async Task<bool> ConfirmDataDisposal(string title)
        {
            if (!ProductionPlans.Any())
            {
                return true;
            }

            return await ConfirmDialog.ShowAsync(DialogService, title,
                    Resource.CancelAllocationPlanMessage, Resource.Confirm);
        }

        private bool IsValid()
        {
            if (!ProductionPlans.Any())
            {
                return false;
            }

            // There are ProductionDays with no Teams allocated.
            if (ProductionPlans.Any(x => x.ProductionDays.Any(x => x.Quantity != 0 && x.TeamId is null)))
            {
                return false;
            }

            return true;
        }
    }
}
