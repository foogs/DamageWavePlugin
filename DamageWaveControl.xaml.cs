#region

using System.IO;
using System.Windows;
using System.Windows.Controls;
#endregion

namespace DamageWave
{
    /// <summary>
    ///     Interaction logic for BlockDegradationControl.xaml
    /// </summary>
    public partial class DamageWaveControl : UserControl
    {
        public DamageWaveControl()
        {
            InitializeComponent();
        }

        public DamageWavePlugin Plugin => (DamageWavePlugin)DataContext;

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
         /*   var groups = Concealed.SelectedItems.Cast<ConcealGroup>().ToList();
            Concealed.SelectedItems.Clear();
            if (!groups.Any())
                return;

            var p = Plugin;
            Plugin.Torch.InvokeBlocking(delegate
            {
                foreach (var current in groups)
                    p.RevealGroup(current);
            });*/
        }

        private void CheckNow_OnClick(object sender, RoutedEventArgs e)
        {
           var p = Plugin;
            Plugin.Torch.Invoke(delegate { p.DamageProcess(); });
        }

        private void Conceal_OnClick(object sender, RoutedEventArgs e)
        {
          var p = Plugin;
            Plugin.Torch.Invoke(delegate { p.Settings.Save(Path.Combine(p.StoragePath, "DamageWave.cfg")); });
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void EditRules_Click(object sender, RoutedEventArgs e)
        {
            var editor = new EditRulesForm
            {
                Owner = Window.GetWindow(this),
                DataContext = DataContext,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            editor.ShowDialog();
        }
    }
}