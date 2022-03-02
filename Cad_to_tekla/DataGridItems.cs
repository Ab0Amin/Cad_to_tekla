using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Cad_to_tekla
{
    internal class DataGridItems : INotifyPropertyChanged
    {
       public string Symbol { set; get; }
        public string TeklaProfiles { set; get; }
        public string Material { set; get; }
        public string CurrentTeklaProfiles { set; get; }
        public string CurrentMaterial { set; get; }
        public List<string> BeamAtt { set; get; }
        private bool _IsDefault;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsDefault
        {
            get
            {
                return _IsDefault;
            }
            set
            {
                _IsDefault = value;
            }
        }
        private int _WorkState;


        private int pro_counter
        {
            get { return _WorkState; }
            set
            {
                _WorkState = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("WorkState"));

                }
            }
        }

    }
   

    }


      