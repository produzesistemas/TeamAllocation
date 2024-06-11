using M2P.WebUI.Dialogs;
using WebUI.Services;

namespace WebUI.Pages.Teams;

public partial class ViewTeamDialog : Dialog
{
    #region PARAMETERS
    [Parameter] public Guid TeamId { get; set; } = default!;
    #endregion PARAMETERS

    #region INJECTS
    [Inject] private IODataService ODataService { get; init; } = default!;
    #endregion INJECTS

    #region PROPS
    private Team? Team { get; set; }
    private TeamProduct SelectedTeamProduct { get; set; } = default!;
    private IEnumerable<HC> HCs { get; set; } = Enumerable.Empty<HC>();

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
    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await QueryDataAsync();
    }

    private async Task QueryDataAsync()
    {
        IsLoading = true;

        var queryTeam = await ODataService.TeamByIdAsync(TeamId);
        if (queryTeam.Valid && queryTeam.Data != null)
        {
            Team = queryTeam.Data;
            var teamProduct = Team.TeamProducts.FirstOrDefault();
            if (teamProduct != null)
            {
                SelectedTeamProduct = teamProduct;
            }
            await QueryWorkstation();
            await QueryHCTrainnings();
        }

        IsLoading = false;
    }

    private async Task QueryWorkstation()
    {
        var workstationQuery = await ODataService.GetWorkstationsAsync(query =>
        {
            query.Filter(x => x.LayoutId == SelectedTeamProduct.LayoutId
                && x.ProductId == SelectedTeamProduct.ProductId);
            query.OrderBy(x => x.Name);
        });

        if (workstationQuery.Valid)
        {
            SelectedTeamProduct.Product.Workstations = workstationQuery.Data;
        }
    }

    private async Task QueryHCTrainnings()
    {
        HCs = Enumerable.Empty<HC>();
        var hcs = SelectedTeamProduct.TeamWorkstations
            .Select(tw => tw.TeamWorkstationHCs.Select(tow => tow.HC))
            .SelectMany(s => s)
            .ToList();
        var hcIds = hcs.Select(x => x.Id).Distinct();
        var workstationIds = SelectedTeamProduct.TeamWorkstations
            .Select(tw => tw.WorkstationId)
            .Distinct();
        var hcTrainningQuery = await ODataService.GetHCTrainningsAsync(query =>
        {
            query.Filter((t, f, o) => o.In(t.HCId, hcIds) && o.In(t.WorkstationId, workstationIds));
        });

        if (hcTrainningQuery.Valid)
        {
            var queredHCs = hcTrainningQuery.Data;
            foreach (HC hc in hcs)
            {
                hc.HCTrainnings = queredHCs.Where(x => x.HCId == hc.Id);
            }
        }
        HCs = hcs;
    }

    private async Task OnProductSelected(TeamProduct selected)
    {
        IsLoading = true;

        SelectedTeamProduct = selected;
        await QueryWorkstation();
        await QueryHCTrainnings();

        IsLoading = false;
    }

    public bool IsHCAllocated(Workstation workstation, HC hc)
    {
        return SelectedTeamProduct
            .TeamWorkstations.First(x => x.WorkstationId == workstation.Id)
            .TeamWorkstationHCs.Any(x => x.HC.Id == hc.Id);
    }

    public async static Task ShowAsync(IDialogService DialogService, Team Team)
    {
        var parameters = new DialogParameters<ViewTeamDialog>
        {
            { x => x.TeamId, Team.Id },
        };
        var options = new DialogOptions() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialog = await DialogService.ShowAsync<ViewTeamDialog>(Team.Description, parameters, options);
        await dialog.Result;
    }
}