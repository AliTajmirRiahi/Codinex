using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Codify.ViewModels
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Text { get; set; }
        public bool IsUser { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => this.canExecute == null || this.canExecute(parameter);
        public void Execute(object parameter) => this.execute(parameter);
        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class ChatViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

        private string inputText;
        public string InputText
        {
            get => inputText;
            set { inputText = value; OnPropertyChanged(); SendCommand?.RaiseCanExecuteChanged(); }
        }

        public RelayCommand SendCommand { get; }
        public RelayCommand AddFileCommand { get; }

        public ChatViewModel()
        {
            SendCommand = new RelayCommand(async _ => await Send(), _ => !string.IsNullOrWhiteSpace(InputText));
            AddFileCommand = new RelayCommand(_ => AddFile());

            // sample welcome message
            Messages.Add(new ChatMessage { Sender = "Codify", Text = "Hello — ask me to generate code, explain errors, or open related files.", IsUser = false });
        }

        private Task Send()
        {
            var text = InputText?.Trim();
            if (string.IsNullOrEmpty(text)) return Task.CompletedTask;

            Messages.Add(new ChatMessage { Sender = "You", Text = text, IsUser = true });
            InputText = string.Empty;

            // Simulate assistant reply
            Messages.Add(new ChatMessage { Sender = "Codify", Text = "(Simulated) Here's a quick reply for: " + text, IsUser = false });

            return Task.CompletedTask;
        }

        private void AddFile()
        {
            // Placeholder behavior: in a real extension we'd show an OpenFileDialog and attach file contents
            Messages.Add(new ChatMessage { Sender = "Codify", Text = "(File attached) [placeholder]", IsUser = false });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
