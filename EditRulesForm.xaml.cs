using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DamageWave
{
    /// <summary>
    /// Логика взаимодействия для EditRulesForm.xaml
    /// </summary>
    public partial class EditRulesForm : Window
    {
        public EditRulesForm()
        {
            InitializeComponent();
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var plugin = DataContext as DamageWavePlugin;
            if (plugin == null) return;
            // ReSharper disable PossibleUnintendedReferenceComparison
            if (sender == OkayButton)
            {
                Close();
            }
            else if (sender == AddButton)
            {
                plugin.Settings.Data.DynamicConcealment.Add(new Settings.BlocksToDamageSettings());
            }
            else if (sender == RemoveButton)
            {
                var entry = List.SelectedItem as Settings.BlocksToDamageSettings;
                if (entry != null)
                    plugin.Settings.Data.DynamicConcealment.Remove(entry);
            }
            // ReSharper restore PossibleUnintendedReferenceComparison
        }
    }
}
