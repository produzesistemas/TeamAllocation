using M2P.WebUI.Dialogs;
using WebUI.Services;

namespace WebUI.Pages.AllocationPlans;

public partial class ViewTeamDialog : Dialog
{ 
    #region INJECTS
    [Inject] private IODataService ODataService { get; init; } = default!;
    #endregion INJECTS

    #region PARAMETERS
    [Parameter] public Team Team { get; set; } = default!;
    [Parameter] public Product Product { get; set; } = default!;
    [Parameter] public IEnumerable<HC> HCs { get; set; } = Enumerable.Empty<HC>();
    [Parameter] public Guid LayoutId { get; set; } = Guid.Empty;
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

    public TeamProduct TeamProduct { get; set; } = default!;
    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await QueryData();
    }

    private async Task QueryData()
    {
        IsLoading = true;

        if (!HCs.Any())
        {
            var teamQuery = await ODataService.QueryTeamsAsync(query =>
            {
                query.Filter(t => t.Id == Team.Id);
            }, expanded: true);

            if (teamQuery.Valid)
            {
                var workstations = new List<Workstation>();
                var team = teamQuery.Data.First();
                TeamProduct = team.TeamProducts.Where(
                    x => x.ProductId == Product.Id
                    && x.LayoutId == LayoutId)
                .First();
                await QueryHCTrainnings();
            }
        } 
        else
        {
            await QueryWorkstations();
        }
        IsLoading = false;
    }

    private async Task QueryHCTrainnings()
    {
        var hcs = TeamProduct.TeamWorkstations
                    .Select(tw => tw.TeamWorkstationHCs.Select(tow => tow.HC))
                    .SelectMany(s => s)
                    .ToList();
        var hcIds = hcs.Select(x => x.Id).Distinct();
        var workstationIds = TeamProduct.TeamWorkstations.Select(tw => tw.WorkstationId).Distinct();
        var hcTrainningQuery = await ODataService.GetHCTrainningsAsync(query =>
        {
            query.Filter((t, f, o) => 
                o.In(t.HCId, hcIds) 
                && o.In(t.WorkstationId, workstationIds)
                && t.LayoutId == LayoutId
            );
        });

        if (hcTrainningQuery.Valid)
        {
            hcs.ForEach(hc =>
            {
                hc.HCTrainnings = hcTrainningQuery.Data.Where(x => x.HCId == hc.Id).ToList();
            });
        }

        await QueryWorkstations();

        IsLoading = false;
        HCs = hcs;
    }

    private async Task QueryWorkstations()
    {
        var workstationQuery = await ODataService.GetWorkstationsAsync(query =>
        {
            query.Filter(x => x.LayoutId == LayoutId && x.ProductId == Product.Id);
            query.OrderBy(x => x.Name);
        });
        if (workstationQuery.Valid)
        {
            Product.Workstations = workstationQuery.Data;
        }
    }

    public TeamWorkstation GetTeamWorkstation(Guid workstationId)
    {
        return Team
            .TeamProducts.First(x => x.LayoutId == LayoutId && x.ProductId == Product.Id)
            .TeamWorkstations.First(x => x.WorkstationId == workstationId);
    }

    public bool IsHCAllocated(Workstation workstation, HC hc)
    {
        var teamWorkstation = GetTeamWorkstation(workstation.Id);
        return teamWorkstation.TeamWorkstationHCs.Any(x => x.HCId == hc.Id);
    }

    public static async Task ShowAsync(IDialogService DialogService, Team Team, Product Product, Guid LayoutId)
    {
        await ShowAsync(DialogService, Team, Product, new List<HC>(), LayoutId);
    }

    public static async Task ShowAsync(IDialogService DialogService, Team Team, Product Product, List<HC> HCs, Guid LayoutId)
    {
        var parameters = new DialogParameters<ViewTeamDialog>
        {
            { x => x.Team, Team },
            { x => x.Product, Product },
            { x => x.HCs, HCs },
            { x => x.LayoutId, LayoutId }
        };

        var options = new DialogOptions() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialog = await DialogService.ShowAsync<ViewTeamDialog>(Team.Description, parameters, options);
        await dialog.Result;
    }
}