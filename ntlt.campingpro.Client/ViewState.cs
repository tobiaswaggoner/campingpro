// *********************************************************
// (c) 2020 - 2020 Netzalist GmbH & Co.KG
// *********************************************************

using System;

namespace ntlt.campingpro.Client
{
    public class ViewState
    {
        private string _headline;
        private bool _connected;
        public event EventHandler OnViewStateChanged;
        
        public string Headline
        {
            get => _headline;
            set
            {
                _headline = value;
                OnViewStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public bool Connected
        {
            get => _connected;
            set
            {
                _connected = value;
                OnViewStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}