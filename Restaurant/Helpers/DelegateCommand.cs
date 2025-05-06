using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Restaurant.Helpers
{
    public class DelegateCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool> _canExec;
        public event EventHandler? CanExecuteChanged;

        public DelegateCommand(Action<object?> exec, Func<object?, bool> canExec)
        {
            _execute = exec;
            _canExec = canExec;
        }
        public bool CanExecute(object? p) => _canExec(p);
        public void Execute(object? p) => _execute(p);
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
