using M2P.Web.Shell.Services;
using WebUI.Components;
using WebUI.Extensions;
using WebUI.Services;
using WebUI.Shared;
using WebUI.Theming;

namespace WebUI.Pages.Teams
{
    public partial class Teams : ComponentBase
    {
        #region INJECTS

        [Inject] private IODataService ODataService { get; init; } = default!;
        [Inject] private ITeamService TeamService { get; init; } = default!;
        [Inject] private NavigationManager NavManager { get; init; } = default!;
        [Inject] private IDialogService DialogService { get; init; } = default!;
        [Inject] private ISnackbar SnackbarService { get; init; } = default!;
        [Inject] private IStringLocalizer<Resource> ResourceExt { get; init; } = default!;
        [Inject] private ISessionManager Session { get; init; } = default!;

        #endregion INJECTS

        #region PROPS

        private MudTable<Team> _table = default!;

        public IEnumerable<string> Categories { get; set; } = Enumerable.Empty<string>();
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

        private bool DisableCategoryList => IsLoading || !Categories.Any();

        private IEnumerable<Team> teams { get; set; } = Enumerable.Empty<Team>();
        private IEnumerable<Product> Products = Enumerable.Empty<Product>();
        private IEnumerable<Shift> Shifts = Enumerable.Empty<Shift>();

        private Paging Paging = new Paging();

        #region FILTERS

        private string FilterName { get; set; } = string.Empty;
        private Shift? FilterShift { get; set; } = default;
        private Product? FilterProduct { get; set; } = default;
        private string FilterCategory { get; set; } = string.Empty;
        private DateTime? FilterStartCreationDate { get; set; }
        private DateTime? FilterEndCreationDate { get; set; }
        private DateTime? FilteredStartCreationDate { get; set; }
        private DateTime? FilteredEndCreationDate { get; set; }
        private string FilteredName { get; set; } = string.Empty;
        private Shift? FilteredShift { get; set; } = default;
        private Product? FilteredProduct { get; set; } = default;
        private string FilteredCategory { get; set; } = string.Empty;

        #endregion FILTERS

        #endregion PROPS

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (!Session.HasAccountPermissionTo(Permissions.MFG_VIEW_TEAM_MEMBERS))
            {
                return;
            }

            IsLoading = true;

            var queryCategories = QueryCategoriesAsync();
            var queryProducts = QueryProductsAsync();
            var queryShifts = QueryShiftsAsync();
            var queryTeams = QueryTeamsAsync();

            await Task.WhenAll(queryCategories, queryProducts, queryShifts, queryTeams);

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
                if (odataCategories != null)
                {
                    Categories = odataCategories.Data;
                }
            }
        }

        private async Task QueryProductsAsync()
        {
            if (!Products.Any())
            {
                var resultProducts = await ODataService.GetProductsAsync(
                    query => query.OrderBy(x => x.Name)
                );
                if (resultProducts != null)
                {
                    Products = resultProducts.Data;
                }
            }
        }

        private async Task QueryShiftsAsync()
        {
            if (!Shifts.Any())
            {
                var resultShifts = await ODataService.QueryShiftAsync(
                    query => query.OrderBy(x => x.Name)
                );
                if (resultShifts != null)
                {
                    Shifts = resultShifts.Data;
                }
            }
        }

        private async Task QueryTeamsAsync()
        {
            if (FilterName != FilteredName
                || FilterCategory != FilteredCategory
                || FilterProduct != FilteredProduct
                || FilterShift != FilteredShift
                || FilterStartCreationDate != FilteredStartCreationDate
                || FilterEndCreationDate != FilteredEndCreationDate)
            {
                Paging.CurrentPage = 0;
            }
            FilteredName = FilterName;
            FilteredCategory = FilterCategory;
            FilteredProduct = FilterProduct;
            FilteredShift = FilterShift;
            FilteredStartCreationDate = FilterStartCreationDate;
            FilteredEndCreationDate = FilterEndCreationDate;

            teams = Enumerable.Empty<Team>();
            Paging.TotalItems = 0;

            var queryResult = await ODataService.QueryTeamsAsync(q =>
            {
                q
                    .OrderByDescending(x => x.CreationDate)
                    .Top(Paging.PageSize)
                    .Skip(Paging.SkipItems());

                q
                    .Expand(f => f.Shift)
                    .Expand(f => f.For<TeamProduct>(t => t.TeamProducts)
                        .Expand(f2 => f2.Product)
                    )
                    .Expand(f => f.For<TeamProduct>(t => t.TeamProducts)
                        .Expand(f2 => f2.Layout)
                    );

                if (!string.IsNullOrWhiteSpace(FilterName))
                {
                    var valueUpper = FilterName.ToUpper();
                    q.Filter((t, f) => f.Contains(f.ToUpper(t.Description), valueUpper));
                }

                if (!string.IsNullOrWhiteSpace(FilterCategory))
                {
                    q.Filter((t, f, o) => o.Any(t.TeamProducts, v => v.Category == FilterCategory));
                }

                if (FilterProduct != null)
                {
                    q.Filter((t, f, o) => o.Any(t.TeamProducts, v => v.ProductId == FilterProduct.Id));
                }

                if (FilterShift != null)
                {
                    q.Filter((t, f, o) => t.ShiftId == FilterShift.Id);
                }

                if (FilterStartCreationDate.HasValue)
                {
                    q.Filter(x => x.CreationDate >= FilterStartCreationDate.Value);
                }

                if (FilterEndCreationDate.HasValue)
                {
                    q.Filter((x, i) => x.CreationDate <= i.ConvertDateTimeToString(FilterEndCreationDate.Value, "yyyy-MM-dd"));
                }
            },
            expanded: false);

            if (queryResult.Valid)
            {
                teams = queryResult.Data;
                Paging.TotalItems = queryResult.Total;
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

        private void OnAddTeam()
        {
            NavManager.NavigateTo(Route.TeamsAdd);
        }

        private async Task OnView(Team team)
        {
            await ViewTeamDialog.ShowAsync(DialogService, team);
        }

        private async Task OnShowHistory(Team team)
        {
            await HistoricDialog.ShowAsync(DialogService, team.Id.ToString(), team.GetType());
        }

        private void OnEdit(Team team)
        {
            if (!Session.HasAccountPermissionTo(Permissions.MFG_EDIT_TEAMS))
            {
                SnackbarService.Add(Resource.WithoutPermission, Severity.Error);
                return;
            }
            NavManager.NavigateTo(Route.TeamsEdit + team.Id);
        }

        private async Task OnDelete(Team team)
        {
            if (!Session.HasAccountPermissionTo(Permissions.MFG_DELETE_TEAMS))
            {
                SnackbarService.Add(Resource.WithoutPermission, Severity.Error);
                return;
            }

            var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
            Resource.DeleteTeamMessage, Resource.Delete);
            if (result)
            {
                await DeleteTeam(team);
            }
        }

        private async Task DeleteTeam(Team team)
        {
            IsLoading = true;
            var result = await TeamService.DeleteTeamAsync(team.Id);
            if (result.Valid)
            {
                SnackbarService.Add(Resource.TeamDeleteSuccess, Severity.Success);

                Paging.UpdateCurrentPageAfterDeleteItem();
                await QueryTeamsAsync();
                return;
            }

            SnackbarService.Add(
               ResourceExt.ResultFailOrDefault(result, Resource.TeamDeleteFail),
               Severity.Error);

            IsLoading = false;
        }

        private async Task PageChanged(int i)
        {
            Paging.CurrentPage = i - 1;
            await QueryTeamsAsync();
        }
    }
}