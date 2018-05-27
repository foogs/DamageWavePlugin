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

namespace DamageWave
{
    //[Plugin("BlockDegradation", "1.0", "5658de3e-60c0-438b-a0a0-b235bc16a4ca")]
    public class DamageWavePlugin : TorchPluginBase, IWpfPlugin
    {
        public Persistent<Settings> Settings { get; private set; }
        private static readonly Logger Log = LogManager.GetLogger("DamageWave");
        private UserControl _control;


        MethodInfo ReflectMethodRevealAll = null;
        ITorchPlugin concealment = null;

        public ulong _mycounter;
        public ulong _mycounter2;
        private bool _init;
        public HashSet<IMyEntity> entities = new HashSet<IMyEntity>();  //create and get list of entities

        public DamageWavePlugin()
        {
            //  Settings = Settings.LoadOrCreate("BlockDegradation.cfg");
        }

        UserControl IWpfPlugin.GetControl()
        {
            return _control ?? (_control = new DamageWaveControl { DataContext = this });
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            try
            {
                Settings = Persistent<Settings>.Load(Path.Combine(StoragePath, "DamageWave.cfg"));
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (Settings?.Data == null)
                Settings = new Persistent<Settings>(Path.Combine(StoragePath, "DamageWave.cfg"), new Settings());


            foreach (var plugin in torch.Managers.GetManager<PluginManager>())
            {
                if (plugin.Id == Guid.Parse("17f44521-b77a-4e85-810f-ee73311cf75d"))
                {
                    concealment = plugin;
                    ReflectMethodRevealAll = plugin.GetType().GetMethod("RevealAll", BindingFlags.Public | BindingFlags.Instance);
                }
            }
            LogTo("Init block degra");
        }

        public override void Update()
        {
            if (MyAPIGateway.Session == null || !Settings.Data.Enabled)
                return;

            if (_mycounter % Settings.Data.CheckInterval == 0)
            {

                if (_mycounter2 > 0) _mycounter2 = _mycounter2 - 1;

                if (((DateTime.Now.Hour == Settings.Data.CommandRunTime.Hour) && (DateTime.Now.Minute == Settings.Data.CommandRunTime.Minute) && _mycounter2 == 0) || isSkippedDoubleTimeCheck())
                {
                    _mycounter2 = 60;


                    LogTo("before ProcessDegradate " + _mycounter2);
                    DamageProcess();
                }
            }
            _mycounter += 1;

            if (_init) return;
            _init = true;

            bool isSkippedDoubleTimeCheck()
            {
                TimeSpan difference = DateTime.Now - Settings.Data.LastExecCommandTime;
                if (difference.TotalDays > 1)
                    return true;
                return false;
            }
        }

        public override void Dispose()
        {
            Settings.Save(Path.Combine(StoragePath, "DamageWave.cfg"));
        }

        public void LogTo(string text)
        {
            if (Settings.Data.Enabled_Debug)
                Log.Info(text);
        }

        public void DamageProcess()
        {

            Settings.Data.LastExecCommandTime = DateTime.Now;
            LogTo("DEGRADATE STARTED - start");
            ReflectRevealAll();
            // LogTo("DEGRADATE    Torch.Invoke(() => ");
            //    Torch.Invoke(() =>
            MyAPIGateway.Entities.GetEntities(entities);
            LogTo("DEGRADATE2:" + entities.Count().ToString());

            foreach (IMyEntity entity in entities)  //cycles through all entities
            {
                //add parallel task
                var grid = entity as IMyCubeGrid;   //assumes entity is a grid
                if (grid?.Physics == null || grid.Closed)   //if it is not a grid, or no longer exists, skip to next entity
                    continue;
                LogTo("DEGRADATE   in foreach");
                long owner = grid.BigOwners.FirstOrDefault();
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);
                var toRemove = new HashSet<IMySlimBlock>();
                if (blocks.Count != 0)
                {
                    LogTo("DEGRADATE   blocks Count " + blocks.Count);
                    if (Settings.Data.TypeID != null && Settings.Data.TypeID.Length > 4)
                        blocks = blocks.FindAll(block => block.BlockDefinition.Id.TypeId.ToString() == "MyObjectBuilder_" + Settings.Data.TypeID);

                    if (Settings.Data.SubTypeID != null && Settings.Data.SubTypeID.Length > 4)
                        blocks = blocks.FindAll(block => block.BlockDefinition.Id.SubtypeId.ToString() == Settings.Data.SubTypeID);

                        foreach (IMySlimBlock target in blocks)  //cycles through list of targeted blocks
                        {
                        LogTo("found block " + target.BlockDefinition.DisplayNameText + "Integrity " + target.Integrity + "/" + target.MaxIntegrity + " " + target.CurrentDamage + " BuildPercent: " + target.BuildPercent());
                        if (target?.CubeGrid == null || target.BuildPercent() <= (float)0.05 || target.Closed())  //if the block doesnt exist or is below min build percentage
                        {
                            LogTo("add block for delete: " + target.FatBlock.DisplayName);
                            toRemove.Add(target);  //slates block for removal, skips damage step
                            continue;
                        }
                        //MyAPIGateway.Utilities.InvokeOnGameThread(() => 
                        LogTo("block DoDamage: " + target.BlockDefinition.DisplayNameText);
                        //add list for parralel dmg after

                        target.DoDamage((target.MaxIntegrity / 100) * Settings.Data.DamageAmount, MyStringHash.GetOrCompute("Degradation"), true);  //applies damage
                        }

                    foreach (IMySlimBlock block in toRemove)  //removes blocks slated for removal
                        block.CubeGrid.RemoveBlock(block);
                }
                blocks.Clear();  //clears block list
                                 //only process fatblocks here
            }
            Settings.Save(Path.Combine(StoragePath, "DamageWave.cfg"));
        }


        private void ReflectRevealAll()
        {
            //then to call
            if (concealment != null && ReflectMethodRevealAll != null)
                ReflectMethodRevealAll.Invoke(concealment, null);
        }
    }
    public static class Extensions
    {
        public static float BuildPercent(this IMySlimBlock block)  //converts health to build percent
        {
            return block.Integrity / block.MaxIntegrity;
        }

        public static bool Closed(this IMySlimBlock block)  //if block has no position
        {
            return block?.CubeGrid?.GetCubeBlock(block.Position) == null;
        }
    }
}