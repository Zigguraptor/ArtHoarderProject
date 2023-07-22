using System;
using ArtHoarderClient.Infrastructure.Commands.Base;

namespace ArtHoarderClient.Infrastructure.Commands
{
    internal class ActionCommand : Command
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public ActionCommand(Action<object> execute, Func<object, bool> canExecute = null!)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public override bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public override void Execute(object? parameter) => _execute(parameter);
    }

    internal class ActionCommand<T> : Command where T : struct, Enum
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public ActionCommand(Action<T> execute, Func<T, bool> canExecute = null!)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public override bool CanExecute(object? parameter)
        {
            if (Enum.TryParse<T>(parameter as string, out var enumValue))
                return _canExecute?.Invoke(enumValue) ?? true;

            return false;
        }

        public override void Execute(object? parameter)
        {
            if (Enum.TryParse<T>(parameter as string, out var enumValue))
                _execute(enumValue);
        }
    }
}