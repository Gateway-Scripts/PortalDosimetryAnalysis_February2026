using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.DV.PD.Scripting.Models;

namespace VMS.DV.PD.Scripting.ViewModels
{
    public class MainViewModel
    {
        public FieldModel SelectedField{ get; set; }
        public List<FieldModel> Fields { get; private set; }
        public MainViewModel(PDPlanSetup plan)
        {
            Fields = new List<FieldModel>();
            foreach (var beam in plan.Beams)
            {
                Fields.Add(new FieldModel(beam));
            }
        }
    }
}
