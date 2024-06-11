using M2P.WebUI.Dialogs;
using WebUI.Extensions;
using WebUI.Services;
using WebUI.Shared;

namespace WebUI.Pages.AllocationPlans;

public partial class SelectTeamDialog : Dialog
{
    private record PresentedTeamProduct : TeamProduct
    {
        public bool IsAllocated { get; set; }
        public bool IsBlocked { get; set; }

        public PresentedTeamProduct(TeamProduct original, Team team, List<Team> AllocatedTeam) : base(original)
        {
            if (AllocatedTeam.Any())
            {
                IsAllocated = AllocatedTeam.Any(t=>t.Id == original.TeamId);
                if (IsAllocated)
                {
                    return;
                }

                var hcs = team.GetHCs().ToArray();
                var allocatedHcs = AllocatedTeam.GetHCs().ToArray();
                foreach (var hC in hcs)
                {
                    if (allocatedHcs.Any(h=>h.Id == hC.Id))
                    {
                        IsBlocked = true;
                        break;
                    }
                }
            }
        }
        public string GetTextClass()
        {
            if (IsAllocated || IsBlocked)
            {
                return "text-disabled";
            }
            return string.Empty;
        }
    }

    #region PARAMETERS
    [CascadingParameter] MudDialogInstance? MudDialog { get; set; }

    [Parameter] public ProductionLine Line { get; set; } = default!;
    [Parameter] public Product Product { get; set; } = default!;
    [Parameter] public Layout Layout { get; set; } = default!;
    [Parameter] public Guid ShiftId { get; set; } = default!;
    [Parameter] public IEnumerable<Team> AllocatedTeams { get; set; } = Enumerable.Empty<Team>();

    #endregion PARAMETERS

    #region INJECTS
    [Inject] private IODataService ODataService { get; init; } = default!;
    [Inject] private IDialogService DialogService { get; init; } = default!;
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

    private bool _filterFreeTeamsOnly { get; set; } = true;
    private bool FilterFreeTeamsOnly
    {
        get => _filterFreeTeamsOnly;
        set
        {
            if (value != _filterFreeTeamsOnly)
            {
                _filterFreeTeamsOnly = value;
                UpdateFilteredTeamProductList();
            }
        }
    }
    private IEnumerable<Team> Teams = Enumerable.Empty<Team>();
    private List<PresentedTeamProduct> TeamProductList = new List<PresentedTeamProduct>();
    private List<PresentedTeamProduct> FilteredTeamProductList = new List<PresentedTeamProduct>();
    private PresentedTeamProduct? _selectedTeamProduct = null;
    private PresentedTeamProduct? SelectedTeamProduct
    {
        get => _selectedTeamProduct;
        set
        {
            if (_selectedTeamProduct != value && value is not null && !value.IsAllocated && !value.IsBlocked)
            {
                _selectedTeamProduct = value;
            }
        }
    }
    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await QueryData();
    }

    private async Task QueryData()
    {
        IsLoading = true;

        TeamProductList = new List<PresentedTeamProduct>();
        var teamQuery = await ODataService.QueryTeamsAsync(query =>
        {
            query.OrderBy(x => x.Description);
            query.Filter((t, f, o) => o.Any(t.TeamProducts, v => v.ProductId == Product.Id && v.LayoutId == Layout.Id));
            query.Filter(t => t.ShiftId == ShiftId);
            query.Expand(x => x.Shift);
            query.Expand(f => f.For<TeamProduct>(x => x.TeamProducts));
        }, 
        expanded: true);

        if (teamQuery.Valid && teamQuery.Data.Any())
        {
            var teamProducts = new List<TeamProduct>();
            Teams = teamQuery.Data;
            foreach (Team team in Teams)
            {
                teamProducts.AddRange(team.TeamProducts.Where(x => x.ProductId == Product.Id && x.LayoutId == Layout.Id));
            }

            foreach (var teamProduct in teamProducts)
            {
                TeamProductList.Add(new PresentedTeamProduct(teamProduct, GetTeam(teamProduct.TeamId), AllocatedTeams.ToList()));
            }

            await QueryHCTrainnings();
        }

        UpdateFilteredTeamProductList();

        IsLoading = false;
    }

    private async Task QueryHCTrainnings()
    {
        var hcs = TeamProductList
            .Select(tp => tp.TeamWorkstations.Select(tw => tw.TeamWorkstationHCs.Select(tow => tow.HC)))
            .SelectMany(s => s)
            .SelectMany(s => s)
            .ToList();

        var hcIds = hcs.Select(x => x.Id).Distinct();

        var workstationIds = TeamProductList
            .Select(tp => tp.TeamWorkstations.Select(tw => tw.WorkstationId))
            .SelectMany(s => s)
            .Distinct();

        var hcTrainningQuery = await ODataService.GetHCTrainningsAsync(query =>
        {
            query.Filter((t, f, o) => o.In(t.HCId, hcIds) && o.In(t.WorkstationId, workstationIds));
        });

        if (hcTrainningQuery.Valid)
        {
            hcs.ForEach(hc =>
            {
                hc.HCTrainnings = hcTrainningQuery.Data.Where(x => x.HCId == hc.Id).ToList();
            });
        }
    }

    private void UpdateFilteredTeamProductList()
    {
        if (!FilterFreeTeamsOnly)
        {
            FilteredTeamProductList = TeamProductList;
        }
        else
        {
            FilteredTeamProductList = TeamProductList.Where(x => !x.IsAllocated && !x.IsBlocked).ToList();
        }

        StateHasChanged();
    }

    private Team GetTeam(Guid TeamId)
    {
        return Teams.Where(x => x.Id == TeamId).FirstOrDefault()!;
    }

    private Shift? GetShift()
    {
        return Teams
            .Where(x => x.ShiftId == ShiftId)
            .Select(x => x.Shift)
            .FirstOrDefault();
    }

    private void OnSelectTeam(PresentedTeamProduct team)
    {
        SelectedTeamProduct = team;
    }

    private async Task OnViewTeam(TeamProduct teamProduct)
    {
        var teamSelected = GetTeam(teamProduct.TeamId);

        var hcs = teamProduct.TeamWorkstations
            .Select(tw => tw.TeamWorkstationHCs.Select(tow => tow.HC))
            .SelectMany(s => s)
            .ToList();

        await ViewTeamDialog.ShowAsync(DialogService, teamSelected, teamProduct.Product, hcs, teamProduct.LayoutId);
    }

    protected void OnConfirm()
    {
        if (SelectedTeamProduct != null)
        {
            var team = GetTeam(SelectedTeamProduct.TeamId);
            var result = DialogResult.Ok(team);
            MudDialog?.Close(result);
        }
        else
        {
            OnCancel();
        }
    }

    protected void OnCancel()
    {
        MudDialog?.Cancel();
    }

    public async static Task<Team?> ShowAsync(IDialogService dialogService, ProductionLine line, Product product, Layout layout, Guid shiftId, IEnumerable<Team> allocatedTeams)
    {
        var parameters = new DialogParameters<SelectTeamDialog>
        {
            { x => x.Line, line },
            { x => x.Product, product },
            { x => x.Layout, layout },
            { x => x.ShiftId, shiftId },
            { x => x.AllocatedTeams, allocatedTeams },
        };
        var options = new DialogOptions() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialog = await dialogService.ShowAsync<SelectTeamDialog>(Resource.AvailableTeams, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            return (Team)result.Data;
        }
        return null;
    }

    //public async static Task<Team?> ShowAsync(IDialogService dialogService, ProductionLine line, Product product, Layout layout, Guid shiftId, IEnumerable<Guid> allocatedTeamIds)
    //{
    //    var parameters = new DialogParameters<SelectTeamDialog>
    //    {
    //        { x => x.Line, line },
    //        { x => x.Product, product },
    //        { x => x.Layout, layout },
    //        { x => x.ShiftId, shiftId },
    //        { x => x.AllocatedTeam, allocatedTeamIds },
    //    };
    //    var options = new DialogOptions() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Large };
    //    var dialog = await dialogService.ShowAsync<SelectTeamDialog>(Resource.AvailableTeams, parameters, options);
    //    var result = await dialog.Result;

    //    if (!result.Canceled)
    //    {
    //        return (Team)result.Data;
    //    }
    //    return null;
    //}

    public string GetSkill(TeamProduct teamProduct)
    {
        return Skill.Get(teamProduct);
    }
}