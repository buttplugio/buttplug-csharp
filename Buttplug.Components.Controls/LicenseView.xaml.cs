// <copyright file="LicenseView.xaml.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Server;

namespace Buttplug.Components.Controls
{
    /// <summary>
    /// Interaction logic for LicenseView.xaml
    /// </summary>
    public partial class LicenseView
    {
        public LicenseView()
        {
            InitializeComponent();
            LicenseText.Text = ButtplugServer.GetLicense();
        }
    }
}
