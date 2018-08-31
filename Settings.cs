using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Torch;
using Torch.Collections;
using VRage.ObjectBuilders;

namespace DamageWave
{
    public class Settings : ViewModel
    {
        private bool _enabled;
        private bool _debug_enabled;

        private uint _checkInterval = 3000;

        [XmlIgnore]
        public MtObservableList<BlocksToDamageSettings> BigRuleList { get; } =
            new MtObservableList<BlocksToDamageSettings>();

        public DateTime CommandRunTime;
        public DateTime LastExecCommandTime;
        private string _commandTime = "23:45";

        [XmlElement(nameof(BigRuleList))]
        public BlocksToDamageSettings[] DynamicConcealmentSerial
        {
            get => BigRuleList.ToArray();
            set
            {
                BigRuleList.Clear();
                if (value != null)
                    foreach (var k in value)
                        BigRuleList.Add(k);
            }
        }
        
        public string CommandTime
        {
            get => _commandTime;
            set
            {   _commandTime = value;
                CommandRunTime = DateTime.Parse(_commandTime);
                OnPropertyChanged();
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(); }
        }

        public bool Debug
        {
            get => _debug_enabled;
            set { _debug_enabled = value; OnPropertyChanged(); }
        }
                
        public uint CheckInterval //1 min?
        {
            get => _checkInterval;
            set { _checkInterval = value; OnPropertyChanged(); }
        }
               
        public void Save(string path)
        {
            var xmlSerializer = new XmlSerializer(typeof(Settings));
            using (var fileStream = File.Open(path, FileMode.OpenOrCreate))
            {
                xmlSerializer.Serialize(fileStream, this);
            }
        }

        public static Settings LoadOrCreate(string path)
        {
            if (!File.Exists(path))
                return new Settings();

            var xmlSerializer = new XmlSerializer(typeof(Settings));
            Settings result;
            using (var fileStream = File.OpenRead(path))
            {
                result = (Settings)xmlSerializer.Deserialize(fileStream);
            }
            return result;
        }


        public class BlocksToDamageSettings : ViewModel
        {
            private string _typeId;
            private string _subtypeId;
            private float _distance;

            /// <summary>
            /// Target type ID
            /// </summary>
            [XmlIgnore]
            public MyObjectBuilderType? TargetTypeId
            {
                get
                {
                    if (MyObjectBuilderType.TryParse(_typeId, out var type))
                        return type;
                    if (MyObjectBuilderType.TryParse("MyObjectBuilder_" + _typeId, out type))
                        return type;
                    return null;
                }
            }

            /// <summary>
            /// Subtype ID to target, or null to target all subtypes
            /// </summary>
            [XmlAttribute("TargetSubtype")]
            [DefaultValue(null)]
            public string TargetSubtypeId
            {
                get => _subtypeId;
                set
                {
                    _subtypeId = value;
                    OnPropertyChanged();
                }
            }

            /// <summary>
            /// The assembly qualified name of the <see cref="Target"/>
            /// </summary>
            [XmlAttribute("TargetType")]
            public string TargetTypeIdString
            {
                get => _typeId;
                set
                {
                    _typeId = value.Trim();
                    OnPropertyChanged();
                    // ReSharper disable once ExplicitCallerInfoArgument
                    OnPropertyChanged(nameof(TargetSubtypeIdOptions));
                }
            }
            
            /// <summary>
            /// Distance to conceal at
            /// </summary>
            [XmlAttribute("Damage")]
            public float Damage
            {
                get => _distance;
                set => SetValue(ref _distance, value);
            }

            [XmlIgnore]
            public ICollection<string> TargetTypeIdOptions =>
                MyDefinitionManager.Static?.GetAllDefinitions()
                    .OfType<MyCubeBlockDefinition>()
                    .Select(x => x.Id.TypeId).Distinct()
                    .Select(x => x.ToString().Replace("MyObjectBuilder_", "")).ToList() ??
                new List<string>();

            [XmlIgnore]
            public ICollection<string> TargetSubtypeIdOptions =>
                MyDefinitionManager.Static?.GetAllDefinitions()
                    .OfType<MyCubeBlockDefinition>()
                    .Where(x => TargetTypeId.HasValue && x.Id.TypeId == TargetTypeId.Value)
                    .Select(x => x.Id.SubtypeName ?? "")
                    .ToList() ?? new List<string>();
        }

    }
}