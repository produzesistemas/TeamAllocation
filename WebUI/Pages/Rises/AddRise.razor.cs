using M2P.Web.Shell.Services;
using M2P.WebUI.UIParts;
using WebUI.Components;
using WebUI.Extensions;
using WebUI.Services;
using WebUI.Shared;
using ComponentBase = Microsoft.AspNetCore.Components.ComponentBase;
using ConfirmDialog = WebUI.Components.ConfirmDialog;

namespace WebUI.Pages.Rises;

public partial class AddRise : ComponentBase
{
    #region INJECTS
    [Inject] private ISessionManager Session { get; init; } = default!;
    [Inject] private IRisesService RisesService { get; init; } = default!;
    [Inject] private IODataService ODataService { get; init; } = default!;
    [Inject] private IDialogService DialogService { get; init; } = default!;
    [Inject] private ISnackbar SnackbarService { get; init; } = default!;
    [Inject] private NavigationManager NavManager { get; init; } = default!;
    [Inject] private IStringLocalizer<Resource> ResourceExt { get; init; } = default!;

    #endregion INJECTS

    #region PARAMETERS
    [Parameter] public string RiseId { get; set; } = default!;
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

    private IEnumerable<Product> ProductList = Enumerable.Empty<Product>();

    private MudForm Form { get; set; } = default!;
    private bool ValidationSuccess { get; set; }
    private bool ValidateFormAfterRender { get; set; }
    private M2PTextField<string> CategoryTextField { get; set; } = default!;
    private MudNumericField<int?> TrainingMinNumericField { get; set; } = default!;
    private MudNumericField<int?> TrainingMaxNumericField { get; set; } = default!;

    private Product Product { get; set; } = default!;
    private string category { get; set; } = string.Empty;
    private string Category
    {
        get => category;
        set
        {
            if (value != category)
            {
                category = value;
                _ = TrainingMinNumericField.Validate();
                _ = TrainingMaxNumericField.Validate();
            }
        }
    }
    private int? PlanImpact { get; set; }
    private int? trainingMin { get; set; }
    private int? TrainingMin
    {
        get => trainingMin;
        set
        {
            if (value != trainingMin)
            {
                trainingMin = value;
                _ = CategoryTextField.Validate();
                _ = TrainingMaxNumericField.Validate();
            }
        }
    }
    private int? trainingMax { get; set; }
    private int? TrainingMax
    {
        get => trainingMax;
        set
        {
            if (value != trainingMax)
            {
                trainingMax = value;
                _ = CategoryTextField.Validate();
                _ = TrainingMinNumericField.Validate();
            }
        }
    }
    private List<RiseParam> RiseParams = new List<RiseParam>();
    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        IsLoading = true;

        await QueryDataAsync();

        if (!IsEditing())
        {
            OnAddRiseParam();
        } 

        IsLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (ValidateFormAfterRender)
        {
            await IsFormValid();
            ValidateFormAfterRender = false;
        }
    }

    private async Task QueryDataAsync()
    {
        IsLoading = true;

        ProductList = Enumerable.Empty<Product>();
        var queryProduct = await ODataService.GetProductsAsync(
            q => q.OrderBy(x => x.Name),
            includeWithoutWorkstations: true
        );
        if (queryProduct.Valid)
        {
            ProductList = queryProduct.Data;
        }

        if (IsEditing())
        {            
            var riseId = Guid.Parse(RiseId);
            var queryRise = await ODataService.RiseByIdAsync(riseId);
            if (queryRise.Valid && queryRise.Data != null)
            {
                var rise = queryRise.Data;
                Product = ProductList.First(x => x.Id == rise.ProductId);
                Category = rise.Category;
                PlanImpact = rise.PlanImpact;
                TrainingMin = rise.TrainingMin;
                TrainingMax = rise.TrainingMax;
                RiseParams = rise.RiseParams.ToList();
            }
            else
            {
                SnackbarService.Add(Resource.RiseNotFound, Severity.Error);
                NavManager.NavigateTo(Route.Rises);
                return;
            }
        }

        Form.ResetValidation();
        IsLoading = false;
    }

    private void OnAddRiseParam()
    {
        RiseParams.Add(new RiseParam(RiseParams.Count() + 1));
    }

    private async Task OnDeleteRiseParam(RiseParam item)
    {
        var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Delete,
            Resource.DeleteDayMessage, Resource.Delete);
        if (result)
        {
            RiseParams.Remove(item);
            int count = RiseParams.Count();
            for (int i = 1; i <= count; i++)
            {
                RiseParams[i - 1].Day = i;
            }
        }
        await IsFormValid();
    }

    private async Task OnSave()
    {
        if (!await IsFormValid())
        {
            return;
        }

        IsLoading = true;

        if (IsEditing())
        {
            var reason = await ReasonDialog.ShowAsync(this.DialogService);
            if (reason is null)
            {
                IsLoading = false;
                return;
            }

            var editResult = await RisesService.UpdateRiseAsync(Guid.Parse(RiseId), 
                Product.Id, Category, TrainingMin, TrainingMax, 
                PlanImpact, RiseParams, reason, Session.Account.SamAccount);

            if (editResult.Valid)
            {
                NavManager.NavigateTo(Route.Rises);
                SnackbarService.Add(Resource.RiseUpdateSuccess, Severity.Success);
                return;
            }

            SnackbarService.Add(
              ResourceExt.ResultFailOrDefault(editResult, Resource.RiseUpdateFail),
              Severity.Error);

            IsLoading = false;
            return;
        }

        var result = await RisesService.CreateRiseAsync(Product.Id, Category, 
            TrainingMin, TrainingMax, PlanImpact, RiseParams);
        if (result.Valid)
        {
            SnackbarService.Add(Resource.RiseCreateSuccess, Severity.Success);
            NavManager.NavigateTo(Route.Rises);
            return;
        }

        SnackbarService.Add(
             ResourceExt.ResultFailOrDefault(result, Resource.RiseCreateFail),
             Severity.Error);

        IsLoading = false;
    }

    private Task<IEnumerable<Product>> SearchProductAsync(string filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return Task.FromResult(ProductList);
        }

        return Task.FromResult(ProductList.Where(x => x.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)));
    }

    private async Task OnBack()
    {
        var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Back,
        Resource.CancelRiseMessage, Resource.Confirm);
        if (result)
        {
            NavManager.NavigateTo(Route.Rises);
            return;
        }
    }

    private async Task OnCancel()
    {
        var result = await ConfirmDialog.ShowAsync(DialogService, Resource.Cancel,
            Resource.CancelRiseMessage, Resource.Confirm);
        if (result)
        {
            NavManager.NavigateTo(Route.Rises);
        }
    }

    #region VALIDATIONS
    private bool IsEditing()
    {
        return !string.IsNullOrEmpty(RiseId);
    }

    private async Task<bool> IsFormValid()
    {
        await Form.Validate();
        return ValidationSuccess;
    }

    private static bool BetweenValueAndValue(int value, int v1, int v2)
    {
        return value >= v1 && value <= v2;
    }

    private string? ValidatePlanImpact(int? value)
    {
        return (value != null && !BetweenValueAndValue((int)value, 0, 100)) 
            ? ResourceExt.ValueMustBeBetweenValueAndValue(Resource.ImpactOnThePlan, "0", "100") 
            : null;
    }

    private string? ValidateCategory(string? value)
    {
        if (string.IsNullOrEmpty(Category) && TrainingMin == null && TrainingMax == null)
        {
            return Resource.CategoryOrTrainingRequired;
        }
        return null;
    }

    private string? ValidateTrainingMin(int? value)
    {
        if (value != null)
        {
            return !BetweenValueAndValue((int)value, 0, 100)
            ? ResourceExt.ValueMustBeBetweenValueAndValue(Resource.MinimumTraining, "0", "100")
            : null;
        }
        else
        {
            if (TrainingMax != null)
            {
                return ResourceExt.ValueIsRequired(Resource.MinimumTraining);
            }
            else if (string.IsNullOrEmpty(Category))
            {
                return Resource.CategoryOrTrainingRequired;
            }
        }
        return null;
    }

    private string? ValidateTrainingMax(int? value)
    {
        if (value != null)
        {
            if (!BetweenValueAndValue((int)value, 0, 100))
            {
                return ResourceExt.ValueMustBeBetweenValueAndValue(Resource.MaximumTraining, "0", "100");
            }
            if (value < TrainingMin)
            {
                return ResourceExt.ValueMustBeGreaterThanOrEqualToValue(Resource.MaximumTraining, Resource.MinimumTraining);
            }
        }
        else
        {
            if (TrainingMin != null)
            {
                return ResourceExt.ValueIsRequired(Resource.MaximumTraining);
            }
            else if (string.IsNullOrEmpty(Category))
            {
                return Resource.CategoryOrTrainingRequired;
            }
        }
        return null;
    }

    private string? ValidatePercentage(int day, int? value)
    {
        if (value == null)
        {
            return ResourceExt.ValueIsRequired(Resource.Percentage);
        }
        if (!BetweenValueAndValue((int)value, 1, 100))
        {
            return ResourceExt.ValueMustBeBetweenValueAndValue(Resource.Percentage, "1", "100");
        }

        var errors = string.Empty;
        var index = day - 1;
        if (index > 0 && value <= RiseParams.ElementAt(index - 1).Percentage)
        {
            errors += Resource.PercentageMustBeGreaterThanPreviousPercentage;
        }
        if (RiseParams.Count - 1 >= index + 1)
        {
            var nextValue = RiseParams.ElementAt(index + 1).Percentage;
            if (value >= nextValue)
            {
                if (!string.IsNullOrEmpty(errors))
                {
                    errors += "\n";
                }
                errors += Resource.PercentageMustBeSmallerThanNextPercentage;
            }
        }
        if (day == RiseParams.Count && value != 100)
        {
            if (!string.IsNullOrEmpty(errors))
            {
                errors += "\n";
            }
            errors += Resource.LastPercentageMustBeEqualTo100;
        }

        ValidateFormAfterRender = true;
        return errors;
    }

    #endregion VALIDATIONS
}
