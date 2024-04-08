using System;
using System.ComponentModel;

namespace LaborantBarcodingStaion
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public class GridRow : INotifyPropertyChanged
    {
        private int _rowNum;
        private string _operatorName;
        private string _message;
        private string _status;
        private string _time;
        private string _entityName;
        public string Time
        {
            get { return _time; }
        }

        public int RowNum
        {
            get { return _rowNum; }
            set
            {
                _rowNum = value;
                _time = DateTime.Now.ToString("HH:mm:ss.ff");
                OnPropertyChanged("RowNum");
            }
        }

        public string OperatorName
        {
            get { return _operatorName; }
            set
            {
                _operatorName = value;
                OnPropertyChanged("OperatorName");
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged("Message");
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        public string EntityName
        {
            get { return _entityName; }
            set { _entityName = value;
                OnPropertyChanged("EntityName"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}