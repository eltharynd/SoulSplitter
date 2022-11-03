﻿// This file is part of the SoulSplitter distribution (https://github.com/FrankvdStam/SoulSplitter).
// Copyright (c) 2022 Frank van der Stam.
// https://github.com/FrankvdStam/SoulSplitter/blob/main/LICENSE
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LiveSplit.Model;
using SoulMemory.DarkSouls1;
using SoulSplitter.Splits.DarkSouls1;
using SoulSplitter.UI.DarkSouls1;
using SoulSplitter.UI.Generic;

namespace SoulSplitter.Splitters
{
    internal class DarkSouls1Splitter : ISplitter
    {
        private LiveSplitState _liveSplitState;
        private DarkSouls1 _darkSouls1;
        private DarkSouls1ViewModel _darkSouls1ViewModel;
        public Exception Exception { get; set; }

        public DarkSouls1Splitter(LiveSplitState state)
        {
            _darkSouls1 = new DarkSouls1();
            _liveSplitState = state;
            _liveSplitState.OnStart += OnStart;
            _liveSplitState.OnReset += OnReset;
            _liveSplitState.IsGameTimePaused = true;

            _timerModel = new TimerModel();
            _timerModel.CurrentState = state;
        }

        public void Update(object settings)
        {
            Logger.TryOrLogError(() =>
            {
                _darkSouls1ViewModel = (DarkSouls1ViewModel)settings;
            });

            Exception = null;
            if(!_darkSouls1.TryRefresh(out Exception e))
            {
                if(e.Message != "DarkSouls not running")
                {
                    Logger.Log(e);
                }
                Exception = e;
            }

            Logger.TryOrLogError(() =>
            {
                _darkSouls1ViewModel.CurrentPosition = _darkSouls1.GetPosition();
            });

            Logger.TryOrLogError(() =>
            {
                UpdateTimer();
            });

            Logger.TryOrLogError(() =>
            {
                UpdateAutoSplitter();
            });
        }

        public void Dispose()
        {
            _liveSplitState.OnStart -= OnStart;
            _liveSplitState.OnReset -= OnReset;
        }

        private void OnStart(object sender, EventArgs e)
        {
            StartTimer();
            StartAutoSplitting();
        }

        private void OnReset(object sender, TimerPhase timerPhase)
        {
            ResetTimer();
        }

        #region Timer

        private readonly TimerModel _timerModel;
        private int _inGameTime;
        private TimerState _timerState = TimerState.WaitForStart;
        private string _savefilePath = null;
        private int _saveSlot = -1;
        private bool _isPtde = false;
        private int _previousIgt = 0;
        private bool _previousCredits = false;

        private void StartTimer()
        {
            if (_darkSouls1ViewModel.ResetInventoryIndices)
            {
                _darkSouls1.ResetInventoryIndices();
            }

            _liveSplitState.IsGameTimePaused = true;
            _timerState = TimerState.Running;
            _inGameTime = _darkSouls1.GetInGameTimeMilliseconds();
            _timerModel.Start();
        }

        private void ResetTimer()
        {
            _savefilePath = null;
            _saveSlot = -1;
            _timerState = TimerState.WaitForStart;
            _inGameTime = 0;
            _previousIgt = 0;
            _previousCredits = false;
            _timerModel.Reset();
        }

        private void UpdateTimer()
        {
            switch (_timerState)
            {
                case TimerState.WaitForStart:
                    if (_darkSouls1ViewModel.StartAutomatically)
                    {
                        var igt = _darkSouls1.GetInGameTimeMilliseconds();
                        if (igt > 0 && igt < 150)
                        {
                            StartTimer();
                            StartAutoSplitting();
                        }
                    }
                    break;

                case TimerState.Running:
                    var currentIgt = _darkSouls1.GetInGameTimeMilliseconds();
                    if (currentIgt != 0)
                    {
                        //Only latch in these values if IGT is not 0, which means where actually on a save.
                        //You can otherwise start the timer on the main menu, which would latch in any kind of saveslot,
                        //but not the real saveslot used later on
                        if (_savefilePath == null)
                        {
                            _savefilePath = _darkSouls1.GetSaveFileLocation();
                            _saveSlot = _darkSouls1.GetCurrentSaveSlot();
                            _isPtde = _darkSouls1.IsPtde();
                        }

                        _inGameTime = currentIgt;
                    }


                    var credits = _darkSouls1.AreCreditsRolling();

                   
                    if (
                        //Detect going from a savefile to the main menu
                        //Only do this once to prevent save file reading race conditions
                        (currentIgt == 0 && _previousIgt != 0) ||

                        //When the credits are active, show savefile time as well
                        (credits && credits != _previousCredits)
                        
                        )
                    {
                        Debug.WriteLine("Read savefile time");
                        var saveFileTime = _darkSouls1.GetSaveFileGameTimeMilliseconds(_savefilePath, _saveSlot, _isPtde);
                        if (saveFileTime != 0)
                        {
                            _inGameTime = saveFileTime;
                        }
                    }

                    _previousCredits = credits;
                    _previousIgt = currentIgt;
                    _timerModel.CurrentState.SetGameTime(TimeSpan.FromMilliseconds(_inGameTime));
                    break;
            }
        }

        #endregion

        #region Autosplitting

        private List<Split> _splits = new List<Split>();
       

        public void ResetAutoSplitting()
        {
            _splits.Clear();
        }

        private void StartAutoSplitting()
        {
            _splits = (
                from timingType in _darkSouls1ViewModel.SplitsViewModel.Splits
                from splitType in timingType.Children
                from split in splitType.Children
                select new Split(timingType.TimingType, splitType.SplitType, split.Split)
            ).ToList();
        }

        public void UpdateAutoSplitter()
        {
            TrackWarps();

            if (_timerState != TimerState.Running)
            {
                return;
            }

            List<Item> inventory = null;
            foreach (var s in _splits)
            {
                if (!s.SplitTriggered)
                {
                    if (!s.SplitConditionMet)
                    {
                        switch (s.SplitType)
                        {
                            default:
                                throw new Exception($"Unsupported split type {s.SplitType}");

                            case SplitType.Boss:
                            case SplitType.Flag:
                                s.SplitConditionMet = _darkSouls1.ReadEventFlag(s.Flag);
                                break;

                            case SplitType.Attribute:
                                s.SplitConditionMet = _darkSouls1.GetAttribute(s.Attribute.AttributeType) >= s.Attribute.Level;
                                break;

                            case SplitType.Position:
                                if (s.Position.Position.X + s.Position.Size > _darkSouls1ViewModel.CurrentPosition.X &&
                                    s.Position.Position.X - s.Position.Size < _darkSouls1ViewModel.CurrentPosition.X &&

                                    s.Position.Position.Y + s.Position.Size > _darkSouls1ViewModel.CurrentPosition.Y &&
                                    s.Position.Position.Y - s.Position.Size < _darkSouls1ViewModel.CurrentPosition.Y &&

                                    s.Position.Position.Z + s.Position.Size > _darkSouls1ViewModel.CurrentPosition.Z &&
                                    s.Position.Position.Z - s.Position.Size < _darkSouls1ViewModel.CurrentPosition.Z)
                                {
                                    s.SplitConditionMet = true;
                                }
                                break;

                            case SplitType.Bonfire:
                                s.SplitConditionMet = _darkSouls1.GetBonfireState(s.BonfireState.Bonfire.Value) >= s.BonfireState.State;
                                break;

                            case SplitType.Item:
                                if (inventory == null)
                                {
                                    inventory = _darkSouls1.GetInventory();
                                }
                                s.SplitConditionMet = inventory.Any(i => i.ItemType == s.ItemState.ItemType);
                                break;

                            case SplitType.Credits:
                                s.SplitConditionMet = _darkSouls1.AreCreditsRolling();
                                break;
                        }
                    }
                    
                    if (s.SplitConditionMet)
                    {
                        ResolveSplitTiming(s);
                    }
                }
            }
        }

        private void ResolveSplitTiming(Split s)
        {
            switch (s.TimingType)
            {
                default:
                    throw new Exception($"Unsupported timing type {s.TimingType}");

                case TimingType.Immediate:
                    _timerModel.Split();
                    s.SplitTriggered = true;
                    break;

                case TimingType.OnLoading:
                    if (!s.Quitout && !_darkSouls1.IsPlayerLoaded())
                    {
                        s.Quitout = true;
                    }

                    if (s.Quitout && _darkSouls1.IsPlayerLoaded())
                    {
                        _timerModel.Split();
                        s.SplitTriggered = true;
                    }
                    break;

                case TimingType.OnWarp:
                    if (!_darkSouls1.IsPlayerLoaded() && _isWarping)
                    {
                        _timerModel.Split();
                        s.SplitTriggered = true;
                    }
                    break;
            }
        }



        private bool _isWarpRequested = false;
        private bool _isWarping = false;
        private bool _warpHasPlayerBeenUnloaded = false;

        private void TrackWarps()
        {
            //Track warps - the game handles warps before the loading screen starts.
            //That's why they have to be tracked while playing, and then resolved on the next loading screen

            if (!_isWarpRequested)
            {
                _isWarpRequested = _darkSouls1.IsWarpRequested();
                return;
            }

            var isPlayerLoaded = _darkSouls1.IsPlayerLoaded();


            //Warp is requested - wait for loading screen
            if (_isWarpRequested)
            {
                if (!_warpHasPlayerBeenUnloaded)
                {
                    if (!isPlayerLoaded)
                    {
                        _warpHasPlayerBeenUnloaded = true;
                    }
                }
                else
                {
                    _isWarping = true;
                }

                if (_isWarping && isPlayerLoaded)
                {
                    _isWarping = false;
                    _warpHasPlayerBeenUnloaded = false;
                    _isWarpRequested = false;
                }
            }
        }
        #endregion
    }
}
