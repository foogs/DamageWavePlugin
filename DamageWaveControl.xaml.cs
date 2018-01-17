#region

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using System.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Collections;
using Torch.Managers;
using Torch.Session;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Definitions;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRageMath;
using VRage.Utils;
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

        private void Reveal_OnClick(object sender, RoutedEventArgs e)
        {
           var p = Plugin;
            Plugin.Torch.Invoke(delegate { p.ProcessDegradate(); });
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
    }
}