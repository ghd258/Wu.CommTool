﻿namespace Wu.CommTool.ViewModels.DesignViewModels
{
    public class MainWindowDesignViewModel : MainWindowViewModel
    {
        private static MainWindowDesignViewModel _Instance;
        public static MainWindowDesignViewModel Instance => _Instance ??= new();
        public MainWindowDesignViewModel()
        {

        }
    }
}
