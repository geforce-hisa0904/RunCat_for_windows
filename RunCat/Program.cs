// Copyright 2020 Takuto Nakamura
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using RunCat.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Resources;
using System.ComponentModel;
using System.Linq;

namespace RunCat
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RunCatApplicationContext());
        }
    }

    public class RunCatApplicationContext: ApplicationContext
    {
        private PerformanceCounter cpuUsage;
        private NotifyIcon notifyIcon;
        private int current = 0;
        private string theme = "";
        private Icon[] icons;
        private Timer animateTimer = new Timer();
        private Timer cpuTimer = new Timer();
        private readonly Container _container;
        private readonly ToolStripMenuItem _lightMenuItem;
        private readonly ToolStripMenuItem _darkMenuItem;
        private readonly IDictionary<string, ToolStripMenuItem> _colorMenuItems;

        public RunCatApplicationContext()
        {
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuUsage.NextValue(); // discards first return value

            _container = new Container();
            notifyIcon = new NotifyIcon()
            {
                Icon = Resources.light_cat0,
                ContextMenuStrip = new ContextMenuStrip(_container),
                Text = "0.0%",
                Visible = true
            };
            var colorItems = new ToolStripMenuItem("Color");
            {
                _lightMenuItem = new ToolStripMenuItem("Light", null, ColorMenuItem_Click, "light");
                _darkMenuItem = new ToolStripMenuItem("Dark", null, ColorMenuItem_Click, "dark");
                _colorMenuItems = new Dictionary<string, ToolStripMenuItem>
                {
                    ["light"] = _lightMenuItem,
                    ["dark"] = _darkMenuItem
                };
            }
            colorItems.DropDownItems.AddRange(_colorMenuItems.Values.ToArray());

            notifyIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[]{
                colorItems,
                new ToolStripSeparator(),
                new ToolStripMenuItem("Exit", null, Exit)
            });

            SetIcons();
            SetAnimation();
            CPUTick();
            StartObserveCPU();
            current = 1;
        }

        private void ColorMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem changeItem = (ToolStripMenuItem)sender;
            ChangeCheckStateColorMenuItem(changeItem);
            ChangeIcons(changeItem.Name);
        }
        private void ChangeCheckStateColorMenuItem(ToolStripMenuItem changeItem)
        {
            foreach (ToolStripMenuItem item in _colorMenuItems.Values)
            {
                if (ReferenceEquals(item, changeItem))
                {
                    item.CheckState = CheckState.Indeterminate;
                }
                else
                {
                    item.CheckState = CheckState.Unchecked;
                }
            }
        }

        private string GetAppsUseTheme()
        {
            string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                object value;
                if (rKey == null || (value = rKey.GetValue("SystemUsesLightTheme")) == null)
                {
                    Console.WriteLine("Oh No! Couldn't get theme light/dark");
                    return "light";
                }
                int theme = (int)value;
                return theme == 0 ? "dark" : "light";
            }
        }

        private void ChangeIcons(string newTheme)
        {
            if (theme.Equals(newTheme)) return;
            theme = newTheme;
            ResourceManager rm = Resources.ResourceManager;
            icons = new List<Icon>
            {
                (Icon)rm.GetObject(theme + "_cat0"),
                (Icon)rm.GetObject(theme + "_cat1"),
                (Icon)rm.GetObject(theme + "_cat2"),
                (Icon)rm.GetObject(theme + "_cat3"),
                (Icon)rm.GetObject(theme + "_cat4")
            }
            .ToArray();
        }
        private void SetIcons()
        {
            string newTheme = GetAppsUseTheme();
            ChangeIcons(newTheme);
            ChangeCheckStateColorMenuItem(_colorMenuItems[theme]);
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General) SetIcons();
        }

        private void Exit(object sender, EventArgs e)
        {
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
        }

        private void SetAnimation()
        {
            animateTimer.Interval = 200;
            animateTimer.Tick += new EventHandler(AnimationTick);
        }

        private void CPUTick()
        {
            float s = cpuUsage.NextValue();
            notifyIcon.Text = $"{s:f1}%";
            s = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, s / 5.0f));
            animateTimer.Stop();
            animateTimer.Interval = (int)s;
            animateTimer.Start();
        }

        private void ObserveCPUTick(object sender, EventArgs e)
        {
            CPUTick();
        }

        private void StartObserveCPU()
        {
            cpuTimer.Interval = 3000;
            cpuTimer.Tick += new EventHandler(ObserveCPUTick);
            cpuTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _container.Dispose();
            base.Dispose(disposing);
        }
    }
}
