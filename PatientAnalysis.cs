using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using VMS.CA.Scripting;
using VMS.DV.PD.Scripting.Views;
using VMS.DV.PD.Scripting.ViewModels;

namespace VMS.DV.PD.Scripting
{

    public class Script
    {

        public Script()
        {
        }

        public void Execute(ScriptContext context, System.Windows.Window window)
        {
            // TODO : Add here your code that is called when the script is launched from Portal Dosimetry
            window.Height = 900;
            window.Width = 1200;
            var mainView = new MainView();
            var mainViewModel = new MainViewModel(context.PDPlanSetup);
            mainView.DataContext = mainViewModel;
            window.Content = mainView;
        }

    }

}