﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.Library.Utilities;

namespace ColorPicker.Settings
{
    [Export(typeof(IUserSettings))]
    public class UserSettings : IUserSettings
    {
        private readonly ISettingsUtils _settingsUtils;
        private const string ColorPickerModuleName = "ColorPicker";
        private const string DefaultActivationShortcut = "Ctrl + Break";
        private const int MaxNumberOfRetry = 5;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Actually, call back is LoadSettingsFromJson")]
        private readonly IFileSystemWatcher _watcher;

        private readonly object _loadingSettingsLock = new object();

        private bool _loadingColorsHistory;
        private bool _loadingVisibleColorRepresentations;

        [ImportingConstructor]
        public UserSettings()
        {
            _settingsUtils = new SettingsUtils();
            ChangeCursor = new SettingItem<bool>(true);
            ActivationShortcut = new SettingItem<string>(DefaultActivationShortcut);
            CopiedColorRepresentation = new SettingItem<ColorRepresentationType>(ColorRepresentationType.HEX);
            UseEditor = new SettingItem<bool>(true);
            ColorHistoryLimit = new SettingItem<int>(20);
            ColorHistory.CollectionChanged += ColorHistory_CollectionChanged;
            VisibleColorFormats.CollectionChanged += VisibleColorFormats_CollectionChanged;

            LoadSettingsFromJson();
            _watcher = Helper.GetFileWatcher(ColorPickerModuleName, "settings.json", LoadSettingsFromJson);
        }

        private void VisibleColorFormats_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!_loadingVisibleColorRepresentations)
            {
                var settings = _settingsUtils.GetSettings<ColorPickerSettings>(ColorPickerModuleName);
                settings.Properties.VisibleColorFormats = VisibleColorFormats.ToList();
                settings.Save(_settingsUtils);
            }
        }

        private void ColorHistory_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!_loadingColorsHistory)
            {
                var settings = _settingsUtils.GetSettings<ColorPickerSettings>(ColorPickerModuleName);
                settings.Properties.ColorHistory = ColorHistory.ToList();
                settings.Save(_settingsUtils);
            }
        }

        public SettingItem<string> ActivationShortcut { get; private set; }

        public SettingItem<bool> ChangeCursor { get; private set; }

        public SettingItem<ColorRepresentationType> CopiedColorRepresentation { get; set; }

        public SettingItem<bool> UseEditor { get; private set; }

        public ObservableCollection<string> ColorHistory { get; private set; } = new ObservableCollection<string>();

        public SettingItem<int> ColorHistoryLimit { get; }

        public ObservableCollection<string> VisibleColorFormats { get; private set; } = new ObservableCollection<string>();

        private void LoadSettingsFromJson()
        {
            // TODO this IO call should by Async, update GetFileWatcher helper to support async
            lock (_loadingSettingsLock)
            {
                {
                    var retry = true;
                    var retryCount = 0;

                    while (retry)
                    {
                        try
                        {
                            retryCount++;

                            if (!_settingsUtils.SettingsExists(ColorPickerModuleName))
                            {
                                Logger.LogInfo("ColorPicker settings.json was missing, creating a new one");
                                var defaultColorPickerSettings = new ColorPickerSettings();
                                defaultColorPickerSettings.Save(_settingsUtils);
                            }

                            var settings = _settingsUtils.GetSettings<ColorPickerSettings>(ColorPickerModuleName);
                            if (settings != null)
                            {
                                ChangeCursor.Value = settings.Properties.ChangeCursor;
                                ActivationShortcut.Value = settings.Properties.ActivationShortcut.ToString();
                                CopiedColorRepresentation.Value = settings.Properties.CopiedColorRepresentation;
                                UseEditor.Value = settings.Properties.UseEditor;
                                ColorHistoryLimit.Value = settings.Properties.ColorHistoryLimit;
                                if (settings.Properties.ColorHistory == null)
                                {
                                    settings.Properties.ColorHistory = new System.Collections.Generic.List<string>();
                                }

                                if (settings.Properties.VisibleColorFormats == null)
                                {
                                    // todo remove this default values, they should be in settings only
                                    settings.Properties.VisibleColorFormats = new System.Collections.Generic.List<string>() { "HEX", "RGB", "HSL" };
                                }

                                _loadingColorsHistory = true;
                                ColorHistory.Clear();
                                foreach (var item in settings.Properties.ColorHistory)
                                {
                                    ColorHistory.Add(item);
                                }

                                _loadingColorsHistory = false;

                                _loadingVisibleColorRepresentations = true;
                                VisibleColorFormats.Clear();
                                foreach (var item in settings.Properties.VisibleColorFormats)
                                {
                                    VisibleColorFormats.Add(item);
                                }

                                _loadingVisibleColorRepresentations = false;
                            }

                            retry = false;
                        }
                        catch (IOException ex)
                        {
                            if (retryCount > MaxNumberOfRetry)
                            {
                                retry = false;
                            }

                            Logger.LogError("Failed to read changed settings", ex);
                            Thread.Sleep(500);
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                        {
                            if (retryCount > MaxNumberOfRetry)
                            {
                                retry = false;
                            }

                            Logger.LogError("Failed to read changed settings", ex);
                            Thread.Sleep(500);
                        }
                    }
                }
            }
        }
    }
}
