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

    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T, Task> _execute;
        private readonly Func<T, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T, Task> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) =>
            !_isExecuting && (_canExecute?.Invoke((T)parameter!) ?? true);

        public async void Execute(object? parameter)
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();        // → disables the button

            try { await _execute((T)parameter!); }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();    // → re-enables the button
            }
        }

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
