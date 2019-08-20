using NLog;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Managers;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace DamageWave
{
    //ca5feba5-355e-413b-8c1b-794144d67444
    public class DamageWavePlugin : TorchPluginBase, IWpfPlugin
    {
        public Persistent<Settings> Settings { get; private set; }
        private static readonly Logger Log = LogManager.GetLogger("DamageWave");
        public static DamageWavePlugin Instance { get; private set; }
        private UserControl _control;


        MethodInfo ReflectMethodRevealAll = null;
        ITorchPlugin concealment = null;

        public uint _mycounter;
        public uint _mycounter2;
        private bool _init; 

        public DamageWavePlugin()
        {
            //  Settings = Settings.LoadOrCreate("BlockDegradation.cfg");
        }

        UserControl IWpfPlugin.GetControl()
        {
            return _control ?? (_control = new DamageWaveControl { DataContext = this });
        }
        private void Initialize()
        {            
            _init = true;
            Instance = this;


            foreach (var plugin in DamageWavePlugin.Instance.Torch.Managers.GetManager<PluginManager>())
            {
                if (plugin.Id == Guid.Parse("17f44521-b77a-4e85-810f-ee73311cf75d")) //find concealment
                {
                    concealment = plugin;
                    ReflectMethodRevealAll = plugin.GetType().GetMethod("RevealAll", BindingFlags.Public | BindingFlags.Instance);
                }
            }
        }
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Settings = Persistent<Settings>.Load(Path.Combine(StoragePath, "DamageWave.cfg"));          
            Log.Info("Init DamageWavePlugin");          

            if (Settings?.Data == null)
                Settings = new Persistent<Settings>(Path.Combine(StoragePath, "DamageWave.cfg"), new Settings());

        }

        public override void Update()
        {
            if (MyAPIGateway.Session == null || !Settings.Data.Enabled)
                return;


            if (!_init)
                Initialize();

            if (_mycounter == Settings.Data.CheckInterval)
            {
                _mycounter = Settings.Data.CheckInterval;

                if (_mycounter2 > 0) _mycounter2 = _mycounter2 - 1;

                if (((DateTime.Now.Hour == Settings.Data.CommandRunTime.Hour) && (DateTime.Now.Minute == Settings.Data.CommandRunTime.Minute) && _mycounter2 == 0) || isSkippedDoubleTimeCheck())
                {
                    _mycounter2 = 60;

                    LogTo("before start DamageWave " + _mycounter2);
                    DamageProcess();
                }
            }
            _mycounter += 1;

           

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
            if (Settings.Data.Debug)
                Log.Info(text);
        }

        public void DamageProcess()
        {

            Settings.Data.LastExecCommandTime = DateTime.Now;
            LogTo("Damage wave incoming");
            ReflectRevealAll();           

            List<MyCubeGrid> AllGrids =MyEntities.GetEntities().OfType<MyCubeGrid>().ToList<MyCubeGrid>();

            LogTo("All grids count:" + AllGrids.Count());

            var tblocks = new ConcurrentDictionary<Sandbox.Game.Entities.Cube.MySlimBlock, float>();            
        ParallelTasks.Parallel.ForEach(AllGrids, (cubegrid) =>  //cycles through all entities
            {
                
                try
                {
                    if (cubegrid.MarkedForClose || cubegrid.Closed) return;   //if it is not a Physics grid, or no longer exists, skip to next 

                    var blocks = new HashSet<Sandbox.Game.Entities.Cube.MySlimBlock>();
                    blocks = cubegrid.GetBlocks();

                    if (blocks.Count == 0) return;
                    foreach (var rule in Settings.Data.BigRuleList)
                    {
                        // LogTo("TargetSubtypeId " + rule.TargetSubtypeId + " TargetTypeIdString " + rule.TargetTypeId.ToString());
                        if (rule.TargetTypeIdString != null && rule.TargetSubtypeId != null)
                        {
                            foreach (var blockk in blocks)
                            {
                                //LogTo("block = SubtypeId: " + blockk.BlockDefinition.Id.SubtypeId.String + " TypeId:" + blockk.BlockDefinition.Id.TypeId);
                                if ((blockk.BlockDefinition.Id.TypeId.ToString() == rule.TargetTypeId.ToString()) && (blockk.BlockDefinition.Id.SubtypeId.String == rule.TargetSubtypeId))
                                {
                                    tblocks.AddOrUpdate(blockk, rule.Damage, (k, v) => v);
                                }
                            }
                        }
                    }
                }
                catch(Exception ex) { LogTo("ex= " + ex.ToString() );}
            },ParallelTasks.WorkPriority.High,null,true);
             
            var toRemove = new HashSet<Sandbox.Game.Entities.Cube.MySlimBlock>();

           
            foreach (var target in tblocks)  //cycles through list of targeted blocks
            {
                LogTo("Found block for damage " + target.Key.BlockDefinition.DisplayNameText + "Integrity " + target.Key.Integrity + "/" + target.Key.MaxIntegrity + " " + target.Key.CurrentDamage + " BuildPercent: " + target.Key.BuildPercent() + "damage percent: " + target.Value);
                if (target.Key?.CubeGrid == null || target.Key.BuildPercent() <= (float)0.05 || target.Key.Closed())  //if the block doesnt exist or is below min build percentage
                {
                   // LogTo("add block for delete: " + target.Key.FatBlock.DisplayName);
                    toRemove.Add(target.Key);  //mark block for removal, skips damage step
                    continue;
                }
                target.Key.DoDamage((target.Key.MaxIntegrity / 100) * target.Value, MyStringHash.GetOrCompute("Degradation"), true, null, 0);  //applies damage
            }

            foreach (Sandbox.Game.Entities.Cube.MySlimBlock block in toRemove)  //removes blocks marked for removal
            {
               LogTo("Remove critical damaged block " + block.BlockDefinition.Id.SubtypeId.String);  
               block.CubeGrid.RemoveBlock(block);
            }

            Settings.Save(Path.Combine(StoragePath, "DamageWave.cfg"));
        }


        private void ReflectRevealAll()
        {
            //hack things
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