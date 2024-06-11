using M2P.Web.Shell.Services;
using M2P.Web.Shell.Tenancy;
using WebUI.Components;
using WebUI.Extensions;
using WebUI.Services;
using WebUI.Shared;
using ComponentBase = Microsoft.AspNetCore.Components.ComponentBase;

namespace WebUI.Pages.Teams
{
    public enum AddTeamState
    {
        ADDING_TEAM,
        ADDING_PRODUCT_TEAM,
    }

    public partial class AddTeam : ComponentBase
    {
        #region INJECTS
        [Inject] private NavigationManager NavManager { get; init; } = default!;
        [Inject] private ITenancyManager Tenancy { get; init; } = default!;
        [Inject] private ITeamService TeamService { get; init; } = default!;
        [Inject] private ISnackbar SnackbarService { get; init; } = default!;
        [Inject] private IStringLocalizer<Resource> ResourceExt { get; init; } = default!;
        [Inject] private IDialogService DialogService { get; init; } = default!;
        [Inject] private IODataService ODataService { get; init; } = default!;
		[Inject] private ISessionManager Session { get; init; } = default!;
		#endregion INJECTS

		#region PARAMETERS

		[Parameter] public string TeamId { get; set; } = default!;

        #endregion PARAMETERS

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
        private bool IsEditingProduct { get; set; } = false;
        private MudForm AddingTeamForm { get; set; } = default!;
        private MudAutocomplete<LeaderModel> MudAutocompleteLeader { get; set; } = default!;
        private MudAutocomplete<ProductModel> MudAutocompleteProduct { get; set; } = default!;
        private bool AddingTeamValidationSuccess { get; set; }
        private MudForm AddingProductForm { get; set; } = default!;
        private bool AddingProductValidationSuccess { get; set; }
        private bool ValidateAddingProductFormOnAfterRender { get; set; }

        private TeamModel Team { get; set; } = new TeamModel();
        private Stack<AddTeamState> stateHistoric = new Stack<AddTeamState>();
        private AddTeamState _State { get; set; } = AddTeamState.ADDING_TEAM;
        private IEnumerable<string> Categories { get; set; } = Enumerable.Empty<string>();

        private TeamProductModel productModel = new TeamProductModel() { };
        private IEnumerable<Shift> Shifts { get; set; } = new List<Shift>();
        private IEnumerable<LeaderModel> Leaders { get; set; } = new List<LeaderModel>();
        private IEnumerable<ProductModel> Products { get; set; } = Enumerable.Empty<ProductModel>();
        private IEnumerable<HCModel> ShiftHCs { get; set; } = Enumerable.Empty<HCModel>();
        private IEnumerable<LayoutModel> Layouts { get; set; } = Enumerable.Empty<LayoutModel>();
        private IEnumerable<LayoutModel> FilteredLayouts { get; set; } = Enumerable.Empty<LayoutModel>();

        private MudTable<HCModel> TeamHCTable = default!;
        private List<HCModel> TeamHCs { get; set; } = new List<HCModel>();

        private AddTeamState State
        {
            get => _State;
            set
            {
                if (value == AddTeamState.ADDING_TEAM)
                {
                    IsEditingProduct = false;
                    ValidateAddingProductFormOnAfterRender = false;
                    productModel = new TeamProductModel();
				}

                _State = value;
                StateHasChanged();
            }
        }

        private string Category
        {
            get => productModel.Category;
            set => productModel.Category = value;
        }

        private IEnumerable<TeamProductWorkstationModel> Workstations
        {
            get
            {
                return productModel.Workstations;
            }
        }

        private ProductModel Product
        {
            get
            {
                if (Products.Any())
                {
                    return Products.Where(p => p.Id == productModel.ProductId).FirstOrDefault()!;
                }
                return default!;
            }
            set
            {
                if (productModel.ProductId != value.Id)
                {
                    productModel.RemoveProduct();
                    productModel.SetProduct(value);
                    Category = string.Empty;

                    _ = LoadCategoriesAndLayoutsAsync(productModel.ProductId);
                }
            }
        }

        private LayoutModel Layout
        {
            get
            {
                if (Layouts.Any())
                {
                    return Layouts.Where(p => p.Id == productModel.LayoutId).FirstOrDefault()!;
                }
                return default!;
            }
            set
            {
                if (productModel.LayoutId != value.Id)
                {
                    //WARNING: change this order may result in glitches
                    //on Layout Select.
                    IsLoading = true;

                    productModel.RemoveLayout();
                    productModel.SetLayout(value);

                    StateHasChanged();

                    new Task(async () =>
                    {
                        await QueryWorkstationsAsync();
                        await IsAddingProductFormValid();

                        IsLoading = false;
                    }).Start();
                }
            }
        }

        #endregion PROPS

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await QueryDataAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
			if (!Session.HasAccountPermissionTo(Permissions.MFG_CREATE_TEAMS))
			{
				return;
			}

            await base.OnAfterRenderAsync(firstRender);

            if (IsEditingProduct && ValidateAddingProductFormOnAfterRender)
            {
                _ = IsAddingProductFormValid();
            }
        }

        #region QUERIES AND LOADERS
        private async Task LoadCategoriesAndLayoutsAsync(Guid ProductId)
        {
            IsLoading = true;
            var categoryTask = QueryCategoriesAsync(ProductId);
            var layoutTask = QueryLayoutsAsync(ProductId);
            await Task.WhenAll(categoryTask, layoutTask);
            IsLoading = false;
        }

        private async Task QueryCategoriesAsync(Guid ProductId)
        {
            var categoriesResponse = await this.ODataService.QueryCategoriesAsync(q =>
            {
                q.OrderBy(p => p.Category);
                q.Filter(p => p.Product.Id == ProductId && p.Category != string.Empty);
            },
            expanded: true);

            if (categoriesResponse.Valid)
            {
                Categories = categoriesResponse.Data;
            }
        }

        private async Task QueryLayoutsAsync(Guid ProductId)
        {
            if (AddingProductForm != null)
            {
                AddingProductForm!.ResetValidation();
            }

            var layoutResponse = await ODataService.GetLayoutsAsync(q =>
            {
                q.Filter((p,f,o) => p.ProductId == ProductId);
                q.OrderBy(p => p.Name);
            });

            if (layoutResponse.Valid)
            {
                if (!layoutResponse.Data.Any())
                {
                    SnackbarService.Add(Resource.NoLayoutAvailable, Severity.Error);
                }

                Layouts = layoutResponse.Data.Select(p => new LayoutModel(p.Id, p.Name, p.ProductId, p.ProductionLineId));
                
                if (IsEditingProduct)
                {
                    FilteredLayouts = Layouts;
                    return;
                }
                var addedLayoutIds = Team.TeamProducts
                   .Where(p => p.ProductId == ProductId)
                   .Select(p => p.LayoutId);
                FilteredLayouts = Layouts.Where(x => !addedLayoutIds.Contains(x.Id));
            }
        }

        private async Task QueryWorkstationsAsync()
        {
            var workstationResult = await ODataService.GetWorkstationsAsync(q =>
            {
                q.Filter(p => p.ProductId == Product.Id && p.LayoutId == Layout.Id);
                q.OrderBy(q => q.Name);
            });

            if (workstationResult.Valid && workstationResult.Data.Any())
            {
                var list = workstationResult.Data;
                foreach (var workstation in list)
                {
                    productModel.AddWorkstation(workstation);
                }
            }
        }

        private async Task QueryProductsAsync()
        {
            var productResponse = await ODataService.GetProductsAsync(q =>
            {
                q.OrderBy(p => p.Name);
            });
            if (productResponse.Valid)
            {
                Products = productResponse.Data.Select(p => new ProductModel(p.Id, p.Name));
                MudAutocompleteProduct?.Clear();
            }
        }

        private async Task QueryShiftsAsync()
        {
            var shiftResponse = await ODataService.QueryShiftsAsync(q =>
            {
                q.OrderBy(q => q.Name);
            });
            if (shiftResponse.Valid)
            {
                Shifts = shiftResponse.Data;
            }
        }

        private async Task QueryTeamAsync()
        {
            var teamId = Guid.Parse(TeamId);
            var queryTeam = await ODataService.TeamByIdAsync(teamId);

            if (queryTeam.Valid && queryTeam.Data != null)
            {
                var team = queryTeam.Data;
                Team.Id = team.Id;
                Team.Name = team.Description;
                Team.Shift = Shifts.Where(x => x.Id == team.ShiftId).First();

                var queryHCs = QueryHCAsync(Team.Shift.Id);
                var queryLayouts = QueryLayoutsAsync(team.TeamProducts.First().ProductId);
                await Task.WhenAll(queryHCs, queryLayouts);

                var firstLayout = team.TeamProducts.First().Layout;
                var firstLayoutModel = new LayoutModel(firstLayout.Id, firstLayout.Name, firstLayout.ProductId, firstLayout.ProductionLineId);
                Layouts = new List<LayoutModel> { firstLayoutModel };
                Team.Leader = Leaders.Where(x => x.Id == team.ResponsibleId).First();

                foreach (var tp in team.TeamProducts)
                {
                    var tpModel = ConvertTeamProduct(tp);
                    Team.TeamProducts.Add(tpModel);
                }
            }
            else
            {
                SnackbarService.Add(Resource.TeamNotFound, Severity.Error);
                NavManager.NavigateTo(Route.Teams);
                return;
            }
        }

        private async Task QueryDataAsync()
        {
            IsLoading = true;

            var queryProducts = QueryProductsAsync();
            var queryShifts = QueryShiftsAsync();
            await Task.WhenAll(queryProducts, queryShifts);

            if (IsEditing())
            {
                await QueryTeamAsync();
            }

            IsLoading = false;
        }

        private Task<IEnumerable<LeaderModel>> SearchLeaderAsync(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return Task.FromResult(Leaders);
            }

            return Task.FromResult(Leaders.Where(x => x.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)));
        }

        private Task<IEnumerable<ProductModel>> SearchProductAsync(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return Task.FromResult(Products);
            }

            return Task.FromResult(Products.Where(x => x.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)));
        }
        #endregion QUERIES AND LOADERS

        private TeamProductModel ConvertTeamProduct(TeamProduct tp)
        {
            var tpModel = new TeamProductModel();
            tpModel.Id = tp.Id;
            tpModel.ProductId = tp.ProductId;
            tpModel.ProductName = tp.Product.Name;
            tpModel.Category = tp.Category;
            tpModel.LayoutId = tp.LayoutId;
            tpModel.LayoutName = tp.Layout.Name;
            
            foreach (var tpw in tp.TeamWorkstations)
            {
                var tpwModel = new TeamProductWorkstationModel();
                tpwModel.Id = tpw.Id;
                tpwModel.Workstation = tpw.Workstation;

                tpModel.AddWorkstation(tpwModel);
                foreach (var hc in tpw.TeamWorkstationHCs)
                {
                    var hcModel = ShiftHCs.Where(x => x.Id == hc.HCId).FirstOrDefault();
                    if (hcModel != null)
                    {
                        tpModel.AddHCToWorkstation(hcModel, tpwModel.Workstation.Name);
                    }
                }
            }

            return tpModel;
        }

        private void OnAddProductRequested()
        {
			TeamHCs = new List<HCModel>();
			GoNextState();
        }

		private void OnEditProductRequested(TeamProductModel product)
		{
			new Task(async () =>
			{
				IsLoading = true;

				IsEditingProduct = true;
				ValidateAddingProductFormOnAfterRender = true;
				productModel.Copy(product);
				TeamHCs = productModel.HCs().ToList();
				await LoadCategoriesAndLayoutsAsync(productModel.ProductId);
				GoNextState();

				IsLoading = false;
			}).Start();
		}

		private async void OnAddTeamRequested()
        {
            if (!await IsAddingTeamFormValid())
            {
                if (!Team.TeamProducts.Any())
                {
                    SnackbarService.Add(ResourceExt.ValueIsRequired(Resource.Product), Severity.Error);
                }
                return;
            }

            IsLoading = true;

            var site = Tenancy.GetTenant()?.Site;
            var shift = Team.Shift;
            var leaderId = Team.Leader.Id;
            var leaderName = Team.Leader.Name;
            HashSet<TeamProduct> products = new HashSet<TeamProduct>();
            foreach (var product in Team.TeamProducts)
            {
                var id = Guid.NewGuid();
                var workstations = new HashSet<TeamWorkstation>();
                foreach (var workstation in product.Workstations)
                {
                    var HCs = new List<TeamWorkstationHC>();
                    var HCIds = workstation.HCs.Select(hc => hc.Id);
                    foreach (Guid HCId in HCIds)
                    {
                        HCs.Add(new TeamWorkstationHC(Guid.NewGuid(), Guid.Empty, HCId));
                    }
                    workstations.Add(new TeamWorkstation(Guid.NewGuid(),
                        HCs,
                        id,
                        workstation.Workstation, workstation.Workstation.Id
                        ));
                }
                if (product.Category == null)
                {
                    product.Category = string.Empty;
                }
                products.Add(new TeamProduct(id, Team.Id, product.ProductId, product.LayoutId, product.Category, workstations));
            }
            if (IsEditing())
            {
                var reason = await ReasonDialog.ShowAsync(this.DialogService);
                if (reason is null)
                {
                    IsLoading = false;
                    return;
                }

                var result = await TeamService.CreateTeamAsync(Team.Id, Team.Name, leaderId, leaderName, shift.Id, products, reason, Session.Account.SamAccount);
                if (result.Valid)
                {
                    SnackbarService.Add(
                        IsEditing() ? Resource.TeamEditSuccess : Resource.TeamCreateSuccess,
                        Severity.Success);
                    NavManager.NavigateTo(Route.Teams);
                    return;
                }
                SnackbarService.Add(
                ResourceExt.ResultFailOrDefault(result, IsEditing() ? Resource.TeamEditFail : Resource.TeamCreateFail),
                Severity.Error);
            }
            else
            {
                var result = await TeamService.CreateTeamAsync(Team.Id, Team.Name, leaderId, leaderName, shift.Id, products);
                if (result.Valid)
                {
                    SnackbarService.Add(
                        IsEditing() ? Resource.TeamEditSuccess : Resource.TeamCreateSuccess,
                        Severity.Success);
                    NavManager.NavigateTo(Route.Teams);
                    return;
                }

                SnackbarService.Add(
                    ResourceExt.ResultFailOrDefault(result, IsEditing() ? Resource.TeamEditFail : Resource.TeamCreateFail),
                    Severity.Error);
            }


            IsLoading = false;
        }

        private async Task OnAddPersonOnWorkstationRequested(TeamProductWorkstationModel TPWorkstation, HCModel HC)
        {
            ValidateAddingProductFormOnAfterRender = false;

            productModel.RemoveHCFromAnyWorkstation(HC);
            productModel.AddHCToWorkstation(HC, TPWorkstation.Workstation.Name);

            await IsAddingProductFormValid();
            StateHasChanged();
        }

        private async Task OnAddPersonRequested()
        {
            ValidateAddingProductFormOnAfterRender = false;

            var teamHCsIds = TeamHCs.Select(x => x.Id);
            var hcs = ShiftHCs.Where(hc => hc.ShiftId == Team.Shift.Id && !teamHCsIds.Contains(hc.Id));
            var dialogParams = new AddHCDialog.AddHCDialogParams(Product.Id, Product.Name, Layout.Name, 
                Team.Name, Workstations, hcs);
            var result = await AddHCDialog.ShowAsync(DialogService, dialogParams);
            if (result != null)
            {
                TeamHCs.AddRange(result);
                TeamHCs = TeamHCs.OrderBy(x => x.Name).ToList();
            }

            await IsAddingProductFormValid();
            StateHasChanged();
        }

        private async Task OnDeletePersonRequested(HCModel HC)
        {
            ValidateAddingProductFormOnAfterRender = false;

            var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
            Resource.DeleteHCMessage, Resource.Delete);
            if (result)
            {
                productModel.RemoveHCFromAnyWorkstation(HC);
                TeamHCs.Remove(HC);
            }
            StateHasChanged();
        }

        private async Task OnProductSelected()
        {
            if (await IsAddingProductFormValid())
            {
                var tp = Team.TeamProducts.FirstOrDefault(x =>
                    x.ProductId == productModel.ProductId
                    && x.LayoutId == productModel.LayoutId);
                
                if (tp != null)
                {
                    // if a teamProduct already exists for this Product and Layout, replace it
                    tp.Copy(productModel);
                }
                else
                {
                    // otherwise add the new one
                    Team.TeamProducts.Add(this.productModel);
                }
                
                GoPreviousState();

                await IsAddingTeamFormValid();
            }
        }

        private async Task OnDeleteProduct(TeamProductModel product)
        {
            var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
            Resource.DeleteProductMessage, Resource.Delete);
            if (result)
            {
                Team.TeamProducts.Remove(product);
                StateHasChanged();
            }
        }

        private void GoNextState()
        {
			stateHistoric.Push(State);
            if (State == AddTeamState.ADDING_TEAM)
            {
                State = AddTeamState.ADDING_PRODUCT_TEAM;
                return;
            }
        }

        private bool GoPreviousState()
        {
            if (stateHistoric.Any())
            {
                stateHistoric.Pop();
                State = AddTeamState.ADDING_TEAM;
                return true;
            }
            return false;
        }

        private async Task QueryHCAsync(Guid shiftId)
        {
            var hcQuery = await ODataService.GetHCsAsync(q =>
            {
                q.Filter(x => x.ShiftId == shiftId);
                q.OrderBy(q => q.Name);
            },
            expanded: true);

            if (hcQuery.Valid)
            {
                ShiftHCs = hcQuery.Data.Select(s => HCModel.FromHC(s));
                MudAutocompleteLeader?.Clear();
                Leaders = new List<LeaderModel>();
                Leaders = hcQuery.Data.Select(m => new LeaderModel(m.Id, m.Name));
            }
        }

        private async void OnSelectShift(Shift shift)
        {
            IsLoading = true;

            Team.Shift = shift;

            await QueryHCAsync(shift.Id);

            if (Team.TeamProducts.Any())
            {
                Team.TeamProducts.Clear();
            }

            IsLoading = false;
        }

        private async void OnBack()
        {
            if (State == AddTeamState.ADDING_PRODUCT_TEAM && productModel.ProductId != Guid.Empty)
            {
                if (!await ConfirmDialog.ShowAsync(DialogService, Resource.Back,
                    Resource.CancelTeamProductMessage, Resource.Confirm))
                {
                    return; 
                }
            }

            if (GoPreviousState())
            {
                return;
            }

            var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Back,
            Resource.CancelTeamMessage, Resource.Confirm);

            if (result)
            {
                NavManager.NavigateTo(Route.Teams);
            }
        }

        private async Task OnCancel()
        {
            var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Cancel,
            Resource.CancelTeamMessage, Resource.Confirm);

            if (result)
            {
                NavManager.NavigateTo(Route.Teams);
            }
        }

        private void TeamHCPageChanged(int i)
        {
            TeamHCTable.NavigateTo(i - 1);
        }

        #region VALIDATIONS

        private bool IsEditing()
        {
            return !string.IsNullOrEmpty(TeamId);
        }

        private async Task<bool> IsAddingTeamFormValid()
        {
            await AddingTeamForm.Validate();
            if (AddingTeamValidationSuccess)
            {
                AddingTeamValidationSuccess = Team.TeamProducts.Any();
            }
            return AddingTeamValidationSuccess;
        }

        private async Task<bool> IsAddingProductFormValid()
        {
            await AddingProductForm.Validate();
            if (AddingProductValidationSuccess)
            {
                AddingProductValidationSuccess = IsWorkstationListValid();
            }
            if (AddingProductValidationSuccess)
            {
                AddingProductValidationSuccess = IsTeamHCsAllocated();
            }
            return AddingProductValidationSuccess;
        }

        private bool IsWorkstationListValid()
        {
            if (!Workstations.Any())
            {
                return false;
            }
            return !Workstations.Where(
                x => !x.HCs.Any() ||
                     x.HCs.Where(h => productModel.IsHCDuplicated(h)).Any()
                ).Any();
        }

        private bool IsWorkstationAllocated(Workstation workstation)
        {
            return Workstations
                .First(x => x.Workstation.Id == workstation.Id)
                .HCs.Any();
        }

        private bool IsHCAllocated(HCModel hc)
        {
            return Workstations
                .Select(x => x.HCs.Select(x => x.Id))
                .SelectMany(x => x)
                .Any(x => x == hc.Id);
        }

        private bool IsTeamHCsAllocated()
        {
            if (!TeamHCs.Any())
            {
                return false;
            }

            var hcsAllocatedIds = Workstations.Select(x => x.HCs.Select(x => x.Id)).SelectMany(x => x);
            var IsAllAllocated = TeamHCs.All(x => hcsAllocatedIds.Contains(x.Id));
            return IsAllAllocated;
        }

        private bool CanAddProduct()
        {
            return Team.Leader != null && !string.IsNullOrEmpty(Team.Name.Trim());
        }

        #endregion VALIDATIONS
    }
}