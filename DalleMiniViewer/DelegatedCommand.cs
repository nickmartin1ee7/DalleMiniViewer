using System;
using System.Windows.Input;

namespace DalleMiniViewer
{
    public class DelegatedCommand : ICommand
    {
        private readonly Action<object?> _action;
        private readonly Func<object?, bool>? _canExecuteAction;

        public event EventHandler? CanExecuteChanged;

        public DelegatedCommand(Action<object?> action,
            Func<object?, bool>? canExecuteAction = null)
        {
            _action = action;
            _canExecuteAction = canExecuteAction;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecuteAction is null) return true;

            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return _canExecuteAction(parameter);
        }

        public void Execute(object? parameter)
        {
            _action(parameter);
        }
    }
}