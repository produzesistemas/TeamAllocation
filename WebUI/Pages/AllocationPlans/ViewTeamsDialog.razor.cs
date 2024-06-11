using M2P.WebUI.Dialogs;
using WebApi.Shared;
using WebUI.Services;
using WebUI.Shared;


namespace WebUI.Pages.AllocationPlans;

public partial class ViewTeamsDialog : Dialog
{
    record PresentedTeam(Team Team, ProductionPlan ProductionPlan);

    #region PARAMETERS

    [Parameter] public Guid AllocationPlanId { get; set; } = default!;
    [Parameter] public AllocationPlanEngine AllocationPlanEngine { get; set; } = default!;

    #endregion PARAMETERS

    #region INJECTS

    [Inject] private IDialogService DialogService { get; init; } = default!;
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

    //private IEnumerable<ProductionPlan> ProductionPlans { get; set; } = default!;
    private List<PresentedTeam> PresentedTeams { get; set; } = new List<PresentedTeam>();
    private List<TeamProduct> TeamProducts { get; set; } = new List<TeamProduct>();
    private List<HC> HCs { get; set; } = new List<HC>();
    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await QueryDataAsync();
    }

    private async Task QueryDataAsync()
    {
        IsLoading = true;

        IEnumerable<ProductionPlan> ProductionPlans;

        var executor = new ProductionPlansQueryExecutor(ODataService);
        if (AllocationPlanEngine.IsOne())
        {
            ProductionPlans = (await executor.ForEngineOne(AllocationPlanId)).ToList();
            ProductionPlans = ProductionPlans.OrderByDescending(x => x.Quantity);
            foreach (var productionPlan in ProductionPlans)
            {
                TeamProducts = new List<TeamProduct>();
                TeamProducts.AddRange(productionPlan.Team!.TeamProducts.ToList().Where(x => x.LayoutId == productionPlan.LayoutId));
                var newTeam = new Team(
                    productionPlan.Team!.Id,
                    productionPlan.Team!.Description, productionPlan.Team!.IsActive, productionPlan.Team!.CreationDate, productionPlan.Team!.Status, productionPlan.Team!.ShiftId, productionPlan.Team!.ResponsibleId, productionPlan.Team!.SiteId, productionPlan.Team!.ResponsibleName, TeamProducts);
                PresentedTeams.Add(new PresentedTeam(
                    newTeam,
                    productionPlan));
            }
        }
        else
        {
            ProductionPlans = (await executor.ForEngineTwo(AllocationPlanId)).ToList();
            foreach (var productionPlan in ProductionPlans)
            {
                var teams = productionPlan.ProductionDays.Select(x => x.Team)
                    .Where(x => x is not null)
                    .Distinct()
                    .ToList();

                foreach (var team in teams)
                {
                    TeamProducts = new List<TeamProduct>();
                    TeamProducts.AddRange(team.TeamProducts.ToList().Where(x => x.LayoutId == productionPlan.LayoutId));
                    var newTeam = new Team(
                    team.Id,
                    team.Description,
                    team.IsActive,
                    team.CreationDate,
                    team.Status,
                    team.ShiftId,
                    team.ResponsibleId,
                    team.SiteId,
                    team.ResponsibleName,
                    TeamProducts);
                    PresentedTeams.Add(new PresentedTeam(newTeam, productionPlan));
                }
            }
        }
        
        await QueryHCTrainnings();

        IsLoading = false;
    }

    private async Task QueryHCTrainnings()
    {
        var hcs = PresentedTeams.Select(pp => pp.Team.TeamProducts.Select(tp => tp.TeamWorkstations.Select(tw => tw.TeamWorkstationHCs.Select(tow => tow.HC))))
           .SelectMany(s => s)
           .SelectMany(s => s)
           .SelectMany(s => s)
           .ToList();

        var hcIds = hcs.Select(x => x.Id).Distinct();
        var workstationIds = PresentedTeams.Select(pp => pp.Team.TeamProducts.Select(tp => tp.TeamWorkstations.Select(tw => tw.WorkstationId)))
          .SelectMany(s => s)
          .SelectMany(s => s)
          .Distinct();

        foreach(var hc in hcs)
        {
            hc.HCTrainnings = Enumerable.Empty<HCTrainning>();
        }

        await QueueExecutor.ExecuteListInParallelAsync(
            list: workstationIds.ToList(),
            queueFunc: async (list, progress, total) => {
                var hcTrainningQuery = await ODataService.GetHCTrainningsAsync(query =>
                {
                    query.Filter((t, f, o) => o.In(t.HCId, hcIds) && o.In(t.WorkstationId, list));
                });
                if (hcTrainningQuery.Valid)
                {
                    foreach (HC hc in hcs)
                    {
                        var hcTrainnings = hc.HCTrainnings.ToList();
                        hcTrainnings.AddRange(hcTrainningQuery.Data.Where(x => x.HCId == hc.Id));
                        hc.HCTrainnings = hcTrainnings;
                    }
                }

                if (progress + list.Count() == total)
                {
                    StateHasChanged();
                }
            }
        );
    }

    protected async Task OnViewTeam(Team team, Product product, Guid layoutId)
    {
        HCs.Clear();

        var teamProduct = team.TeamProducts
            .Where(x => x.ProductId == product.Id && x.LayoutId == layoutId)
            .First();

        teamProduct.TeamWorkstations.ToList().ForEach(tpWorkstation =>
        {
            tpWorkstation.TeamWorkstationHCs.ToList().ForEach(twh =>
            {
                HCs.Add(twh.HC);
            });
        });

        await ViewTeamDialog.ShowAsync(DialogService, team, product, HCs.ToList(), layoutId);
    }

    public static async Task ShowAsync(IDialogService DialogService, Guid AllocationPlanId, AllocationPlanEngine AllocationPlanEngine)
    {
        var parameters = new DialogParameters<ViewTeamsDialog>
        {
            { x => x.AllocationPlanId, AllocationPlanId },
            { x => x.AllocationPlanEngine, AllocationPlanEngine }
        };
        var options = new DialogOptions() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialog = await DialogService.ShowAsync<ViewTeamsDialog>(Resource.AllocationPlanTeams, parameters, options);
        var result = await dialog.Result;
    }
}