using M2P.WebUI.Dialogs;
using M2P.WebUI.UIParts;
using WebUI.Services;
using WebUI.Shared;

namespace WebUI.Pages.Teams;

public partial class AddHCDialog : Dialog
{
    public record AddHCDialogParams(Guid ProductId, string ProductName, 
        string LayoutName, string TeamName, 
        IEnumerable<TeamProductWorkstationModel> TPWorkstations, 
        IEnumerable<HCModel> HCs);

    private class PresentedHC
    {
        internal Guid Id { get; set; }
        internal string Name { get; set; } = string.Empty;
        internal string Workday { get; set; } = string.Empty;
        internal Statuses Statuses { get; set; } = default!;

        internal PresentedHC(Guid id, string name, string workday, Statuses statuses)
        {
            Id = id;
            Name = name;
            Workday = workday;
            Statuses = statuses;
        }

        internal bool IsActive() => Statuses.Name == Statuses.Active;
    }

    #region PARAMETERS
    [CascadingParameter] MudDialogInstance? MudDialog { get; set; }

    [Parameter] public AddHCDialogParams Params { get; set; } = default!;
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

    private M2PTable<PresentedHC> tableAvailables = default!;
    private M2PTable<PresentedHC> tableSelecteds = default!;
    private string FilterName { get; set; } = string.Empty;
    private bool _filterActiveOnly { get; set; } = true;
    private bool FilterActiveOnly
    {
        get => _filterActiveOnly;
        set
        {
            if (value != _filterActiveOnly)
            {
                _filterActiveOnly = value;
                UpdatePresentedHCs();
            }
        }
    }
    private bool _orderTrainedFirst { get; set; } = true;
    private bool OrderTrainedFirst
    {
        get => _orderTrainedFirst;
        set
        {
            if (value != _orderTrainedFirst)
            {
                _orderTrainedFirst = value;
                UpdatePresentedHCs();
            }
        }
    }

    private HashSet<PresentedHC> SelectedHCs { get; set; } = new HashSet<PresentedHC>();
    private HashSet<PresentedHC> HCs { get; set; } = new HashSet<PresentedHC>();
    private List<Guid> TrainedIds { get; set; } = new List<Guid>();
    private HashSet<PresentedHC> PresentedHCs { get; set; } = new HashSet<PresentedHC>();
    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        foreach (var hc in Params.HCs)
        {
            HCs.Add(new PresentedHC(hc.Id, hc.Name, hc.Workday, hc.Statuses));
        }

        var workstationIds = Params.TPWorkstations.Select(x => x.Workstation.Id).ToList();
        TrainedIds = Params.HCs
            .Where(x => x.IsTrained(workstationIds))
            .Distinct()
            .Select(x => x.Id)
            .ToList();
        
        UpdatePresentedHCs();
    }

    private void UpdatePresentedHCs()
    {
        IsLoading = true;

        var result = HCs;

        if (FilterActiveOnly)
        {
            result = result.Where(x => x.IsActive()).ToHashSet();
        }

        if (OrderTrainedFirst)
        {
            result = result.OrderByDescending(x => TrainedIds.Contains(x.Id)).ToHashSet();
        }

        PresentedHCs = result;

        IsLoading = false;
    }

    private HCModel GetHCModel(Guid id)
    {
        return Params.HCs.Where(x => x.Id == id).FirstOrDefault()!;
    }

    private bool FilterFunc(PresentedHC element) => FilterByName(element, FilterName);

    private bool FilterByName(PresentedHC element, string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        if (element.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            return true;
        if (element.Workday.Contains(searchString))
            return true;
        return false;
    }

    private void OnAddSelected(PresentedHC element)
    {
        PresentedHCs.Remove(element);
        SelectedHCs.Add(element);
    }

    private void OnRemoveSelected(PresentedHC element)
    {
        PresentedHCs.Add(element);
        SelectedHCs.Remove(element);
    }

    private void AvailablesPageChanged(int i)
    {
        tableAvailables.NavigateTo(i - 1);
    }

    private void SelectedsPageChanged(int i)
    {
        tableSelecteds.NavigateTo(i - 1);
    }

    private Func<PresentedHC, int, string> RowClass => (hc, row) =>
    {
        if (!hc.IsActive())
        {
            return "row-disabled";
        }

        return string.Empty;
    };

    protected void OnConfirm()
    {
        var hcs = new HashSet<HCModel>();
        foreach (var presentedHc in SelectedHCs)
        {
            hcs.Add(GetHCModel(presentedHc.Id));
        }
        MudDialog?.Close(DialogResult.Ok(hcs));
    }

    protected void OnCancel()
    {
        MudDialog?.Cancel();
    }

    public async static Task<HashSet<HCModel>?> ShowAsync(IDialogService DialogService, AddHCDialogParams Params)
    {
        var parameters = new DialogParameters<AddHCDialog>
        {
            { x => x.Params, Params }
        };
        var options = new DialogOptions() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialog = await DialogService.ShowAsync<AddHCDialog>(Resource.AvailableHCs, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            return (HashSet<HCModel>?)result.Data;
        }
        return null;
    }
}