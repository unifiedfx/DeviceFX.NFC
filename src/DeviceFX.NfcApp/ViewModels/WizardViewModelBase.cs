using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceFX.NfcApp.Views.Shared;
using DeviceFX.NfcApp.Helpers;

namespace DeviceFX.NfcApp.ViewModels;

public abstract partial class WizardViewModelBase : ObservableObject, IQueryAttributable
{
    public List<StepContentPage> Steps { get; }
    public StepContentPage CurrentStep { get; set; }

    public WizardViewModelBase(IEnumerable<StepContentPage> steps)
    {
        Steps = steps.ToList();
        if(!Steps.Any()) throw new ArgumentException("Steps collection cannot be empty.", nameof(steps));
        Steps.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        CurrentStep = Steps.First();
    }
    [RelayCommand(CanExecute = nameof(CanExecuteNext))]
    protected virtual async Task NextAsync(string? page = null)
    {
        CurrentStep = Shell.Current.CurrentPage as StepContentPage ?? CurrentStep;
        if (page == null)
        {
            var next = Steps.OrderBy(s => s.Priority)
                .FirstOrDefault(s => s.Priority > CurrentStep.Priority && s.Group == CurrentStep.Group) ?? Steps.First();
            page = next.Name;
        }
        await Shell.Current.GoToAsync($@"//{page.ToLowerInvariant()}", false);
    }
    protected virtual bool CanExecuteNext() => true;

    [RelayCommand(CanExecute = nameof(CanExecuteBack))]
    protected virtual async Task BackAsync(string? page = null)
    {
        CurrentStep = Shell.Current.CurrentPage as StepContentPage ?? CurrentStep;
        if (page == null)
        {
            var previous = Steps.OrderByDescending(s => s.Priority)
                .FirstOrDefault(s => s.Priority < CurrentStep.Priority && s.Group == CurrentStep.Group) ?? Steps.First();
            page = previous.Name;
        }
        await Shell.Current.GoToAsync($@"//{page.ToLowerInvariant()}", false);
    }
    protected virtual bool CanExecuteBack() => true;

    [RelayCommand]
    protected async Task OpenUrlAsync(string url) => await Launcher.OpenAsync(url);

    public void ApplyQueryAttributes(IDictionary<string, object> query)=> this.ApplyQuery(query);

}