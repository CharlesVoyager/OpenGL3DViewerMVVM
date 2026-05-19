using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OpenGL3DViewerMVVM.View
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Func<object?, bool>? canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        public bool CanExecute(object? parameter) => canExecute == null || canExecute(parameter);
        public void Execute(object? parameter) => execute(parameter);
    }
}
