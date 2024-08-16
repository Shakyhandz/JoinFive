using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JoinFive
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value ?? "";
                 OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
