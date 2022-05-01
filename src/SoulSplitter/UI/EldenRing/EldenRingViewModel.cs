﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using SoulMemory.EldenRing;
using SoulSplitter.Splits.EldenRing;

namespace SoulSplitter.UI.EldenRing
{
    public class EldenRingViewModel : INotifyPropertyChanged
    {
        public EldenRingViewModel()
        {
            //AddSplit(TimingType.OnLoading, EldenRingSplitType.Boss, Boss.BolsCarianKnight);
            //AddSplit(TimingType.OnLoading, EldenRingSplitType.Boss, Boss.AncestralSpirit);
            //AddSplit(TimingType.Immediate, EldenRingSplitType.Boss, Boss.GlintstoneDragonSmarag);
            //AddSplit(TimingType.Immediate, EldenRingSplitType.Boss, Boss.BlackBladeKindredBestialSanctum);
            //AddSplit(TimingType.Immediate, EldenRingSplitType.Boss, Boss.CommanderNiall);
        }



        private bool _startAutomatically = true;
        public bool StartAutomatically
        {
            get => _startAutomatically;
            set => SetField(ref _startAutomatically, value);
        }


        private bool _noLogo = false;
        public bool NoLogo
        {
            get => _noLogo;
            set => SetField(ref _noLogo, value);
        }


        #region Adding new splits ================================================================================================================

        public TimingType? NewSplitTimingType
        {
            get => _newSplitTimingType;
            set
            {
                SetField(ref _newSplitTimingType, value);
                EnabledSplitType = NewSplitTimingType.HasValue;
            }
        }

        private TimingType? _newSplitTimingType;
        
        public bool EnabledSplitType
        {
            get => _enabledSplitType;
            set => SetField(ref _enabledSplitType, value);
        }
        private bool _enabledSplitType = false;

        public EldenRingSplitType? NewSplitType
        {
            get => _newSplitType;
            set
            {
                SetField(ref _newSplitType, value);

                VisibleBossSplit = false;
                VisibleGraceSplit = false;
                VisibleFlagSplit = false;

                switch (NewSplitType)
                {
                    default:
                        throw new Exception($"split type not supported: {NewSplitType}");

                    case null:
                        break;

                    case EldenRingSplitType.Boss:
                        VisibleBossSplit = true;
                        break;

                    case EldenRingSplitType.Grace:
                        VisibleGraceSplit = true;
                        break;

                    case EldenRingSplitType.Flag:
                        VisibleFlagSplit = true;
                        break;
                }
            }
        }
        private EldenRingSplitType? _newSplitType;

        public Boss? NewSplitBoss
        {
            get => _newSplitBoss;
            set
            {
                SetField(ref _newSplitBoss, value);
                EnabledAddSplit = NewSplitBoss.HasValue;
            }
        }
        private Boss? _newSplitBoss;

        public bool VisibleBossSplit
        {
            get => _visibleBossSplit;
            set => SetField(ref _visibleBossSplit, value);
        }
        private bool _visibleBossSplit;

        public Grace? NewSplitGrace
        {
            get => _newSplitGrace;
            set
            {
                SetField(ref _newSplitGrace, value);
                EnabledAddSplit = NewSplitGrace.HasValue;
            }
        }
        private Grace? _newSplitGrace;

        public bool VisibleGraceSplit
        {
            get => _visibleGraceSplit;
            set => SetField(ref _visibleGraceSplit, value);
        }
        private bool _visibleGraceSplit;

        public uint? NewSplitFlag
        {
            get => _newSplitFlag;
            set
            {
                SetField(ref _newSplitFlag, value);
                EnabledAddSplit = NewSplitFlag.HasValue;
            }
        }
        private uint? _newSplitFlag;

        public bool VisibleFlagSplit
        {
            get => _visibleFlagSplit;
            set => SetField(ref _visibleFlagSplit, value);
        }
        private bool _visibleFlagSplit;

        public bool EnabledAddSplit
        {
            get => _enabledAddSplit;
            set => SetField(ref _enabledAddSplit, value);
        }
        private bool _enabledAddSplit;
        
        public void AddSplit()
        {
            if (!NewSplitTimingType.HasValue || !NewSplitType.HasValue)
            {
                return;
            }

            var hierarchicalTimingType = Splits.FirstOrDefault(i => i.TimingType == NewSplitTimingType);
            if (hierarchicalTimingType == null)
            {
                hierarchicalTimingType = new HierarchicalTimingTypeViewModel() { TimingType = NewSplitTimingType.Value };
                Splits.Add(hierarchicalTimingType);
            }

            var hierarchicalSplitType = hierarchicalTimingType.Children.FirstOrDefault(i => i.EldenRingSplitType == NewSplitType);
            if (hierarchicalSplitType == null)
            {
                hierarchicalSplitType = new HierarchicalSplitTypeViewModel() { EldenRingSplitType = NewSplitType.Value, Parent = hierarchicalTimingType };
                hierarchicalTimingType.Children.Add(hierarchicalSplitType);
            }

            switch (NewSplitType)
            {
                default:
                    throw new Exception($"split type not supported: {NewSplitType}");

                case EldenRingSplitType.Boss:
                    if (hierarchicalSplitType.Children.All(i => (Boss)i.Split != NewSplitBoss))
                    {
                        hierarchicalSplitType.Children.Add(new HierarchicalSplitViewModel() { Split = NewSplitBoss.Value, Parent = hierarchicalSplitType });
                    }
                    break;

                case EldenRingSplitType.Grace:
                    if (hierarchicalSplitType.Children.All(i => (Grace)i.Split != NewSplitGrace))
                    {
                        hierarchicalSplitType.Children.Add(new HierarchicalSplitViewModel() { Split = NewSplitGrace.Value, Parent = hierarchicalSplitType });
                    }
                    break;

                case EldenRingSplitType.Flag:
                    if (hierarchicalSplitType.Children.All(i => (uint)i.Split != NewSplitFlag))
                    {
                        hierarchicalSplitType.Children.Add(new HierarchicalSplitViewModel() { Split = NewSplitFlag.Value, Parent = hierarchicalSplitType });
                    }
                    break;
            }

            NewSplitTimingType = null;
            NewSplitType = null;
            NewSplitBoss = null;
            NewSplitGrace = null;
            NewSplitFlag = null;
        }



        #endregion

        #region Removing splits
        public bool EnabledRemoveSplit
        {
            get => _enabledRemoveSplit;
            set => SetField(ref _enabledRemoveSplit, value);
        }
        private bool _enabledRemoveSplit;

        public HierarchicalSplitViewModel SelectedSplit
        {
            get => _selectedSplit;
            set
            {
                SetField(ref _selectedSplit, value);
                EnabledRemoveSplit = SelectedSplit != null;
            }
        }
        private HierarchicalSplitViewModel _selectedSplit = null;
        
        public void RemoveSplit()
        {
            if (SelectedSplit != null)
            {
                var parent = SelectedSplit.Parent;
                parent.Children.Remove(SelectedSplit);
                if (parent.Children.Count <= 0)
                {
                    var nextParent = parent.Parent;
                    nextParent.Children.Remove(parent);
                    if (nextParent.Children.Count <= 0)
                    {
                        Splits.Remove(nextParent);
                    }
                }

                SelectedSplit = null;
            }
        }
        #endregion

        #region Splits hierarchy

        public void RestoreHierarchy()
        {
            //When serializing the model, we can't serialize the parent relation, because that would be a circular reference. Instead, parent's are not serialized.
            //After deserializing, the parent relations must be restored.

            foreach (var timingType in Splits)
            {
                foreach (var splitType in timingType.Children)
                {
                    splitType.Parent = timingType;
                    foreach (var boss in splitType.Children)
                    {
                        boss.Parent = splitType;
                    }
                }
            }
        }

        #endregion


        public ObservableCollection<HierarchicalTimingTypeViewModel> Splits { get; set; }= new ObservableCollection<HierarchicalTimingTypeViewModel>();





        //source lists
        public static ObservableCollection<Boss> Bosses { get; set; } = new ObservableCollection<Boss>(Enum.GetValues(typeof(Boss)).Cast<Boss>());
        public static ObservableCollection<Grace> Graces { get; set; } = new ObservableCollection<Grace>(Enum.GetValues(typeof(Grace)).Cast<Grace>());
        
        #region INotifyPropertyChanged

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName ?? "");
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
        }

        #endregion
    }

    public class TimingSplitsCollection : INotifyPropertyChanged
    {
        public TimingType? TimingType
        {
            get => _timingType;
            set => SetField(ref _timingType, value);
        }
        private TimingType? _timingType;



        //public TimingType TimingType;
        #region INotifyPropertyChanged

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName ?? "");
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
        }

        #endregion
    }

    
}
