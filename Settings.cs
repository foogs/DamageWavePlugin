using System.IO;
using System.Xml.Serialization;
using Torch;
using System;

namespace DamageWave
{
    public class Settings : ViewModel
    {
        private bool _enabled = true;
        private string _typeid = "Battary";
        private string _subtypeid = "na";

        [System.Xml.Serialization.XmlIgnoreAttribute]
        private ulong _checkInterval = 1739;

       
        public DateTime CommandRunTime;

        public DateTime LastExecCommandTime;
        private string _commandTime = "23:45";
        private ulong _damageamount = 1;

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

        public ulong DamageAmount
        {
            get => _damageamount;
            set { _damageamount = value; OnPropertyChanged(); }
        }

        public ulong CheckInterval //1 min?
        {
            get => _checkInterval;
            set { _checkInterval = value; OnPropertyChanged(); }
        }

        public string TypeID
        {
            get => _typeid;
            set { _typeid = value; OnPropertyChanged(); }
        }

        public string SubTypeID
        {
            get => _subtypeid;
            set { _subtypeid = value; OnPropertyChanged(); }
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
    }
}