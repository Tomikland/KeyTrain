﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.IO;
using KeyTrain;
using static Pythonic.ListHelpers;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;

namespace KeyTrain
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : INotifyPropertyChanged
    {
        public static ChainMap<string, dynamic> settings_copy { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public SettingsPage()
        {
            DataContext = this;
            settings_copy = ConfigManager.Settings.Clone();
            InitializeComponent();
        }

        public int LessonLength
        {
            get => settings_copy["lessonLength"]; 
            set { settings_copy["lessonLength"] = value; OnPropertyChanged(); }
        }
        public string PresetText
        {
            get => settings_copy["presetText"];
            set { settings_copy["presetText"] = value; OnPropertyChanged(); }
        }
        public bool RandomGenerator
        {
            get => settings_copy["generator"] == "random";
            set { settings_copy["generator"] = value ? "random" : "custom"; OnPropertyChanged(); }
        }
        public bool PresetGenerator
        {
            get => settings_copy["generator"] != "random";
            set { settings_copy["generator"] = value ? "custom" : "random"; OnPropertyChanged(); }
        }

        public int CapitalsLevel
        {
            get => settings_copy["capitalsLevel"];
            set { settings_copy["capitalsLevel"] = value; OnPropertyChanged(); OnPropertyChanged("capitalDescription"); }
        }
        public readonly string[] capitalChoices = new string[] { "Force lowercase", "Keep existing","50% chance first letter", "First letter every word", "ALL CAPS" };
        public string capitalDescription => capitalChoices[CapitalsLevel];


        MainWindow window;

        private void dictFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Choose dictionary file(s)",
                Multiselect = true,
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Resources"),
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"
            };


            if (dialog.ShowDialog() == true)
            {
                settings_copy["dictionaryPath"] = dialog.FileNames.Select(p => new string[]
                    {p, Path.GetRelativePath(Directory.GetCurrentDirectory(), p)}   //Compare absolute and relative paths
                    .OrderBy(p => p.Count(c => c == '\\')).First()).ToList();       //Keep the simpler one

            }

        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            window.LoadMainPage();
        }
        

        //TODO: hooks for each config value change: functions to call whenever a setting gets updated
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            //settings_copy["lessonLength"] = (int)lengthslider.Value;
            ConfigManager.Settings.dicts = settings_copy.dicts;

            MainPage.Generator = RandomGenerator ? 
                RandomizedLesson.FromDictionaryFiles(ConfigManager.dictionaryPaths) : 
                new PresetTextLesson(ConfigManager.Settings["presetText"]);
            MainPage.Text = MainPage.Generator.CurrentText;

            //Trace.WriteLine(ConfigManager.lessonLength);
            ConfigManager.WriteConfigFile();
            window.LoadMainPage(reset: true, reEmphasize: true);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(ApplyButton);
            window = (MainWindow)Window.GetWindow(this);
            settings_copy = ConfigManager.Settings.Clone();
            foreach (string key in settings_copy.Keys.Append("capitalDescription"))
            {
                OnPropertyChanged(key);
            }
        }

        private void Page_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                window.LoadMainPage();
            }
            if(e.Key == Key.Enter)
            {
                Apply_Click(sender, e);
            }
        }
    }

    public class BoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is bool)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
