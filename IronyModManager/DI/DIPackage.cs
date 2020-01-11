﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 01-11-2020
//
// Last Modified By : Mario
// Last Modified On : 01-11-2020
// ***********************************************************************
// <copyright file="DIPackage.cs" company="Mario">
//     Copyright (c) Mario. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Collections.Generic;
using System;
using ReactiveUI;
using SimpleInjector;
using SimpleInjector.Packaging;
using Splat;
using Splat.SimpleInjector;

namespace IronyModManager.DI
{
    /// <summary>
    /// Class DIPackage.
    /// </summary>
    public partial class DIPackage : IPackage
    {
        #region Methods

        /// <summary>
        /// Registers the set of services in the specified <paramref name="container" />.
        /// </summary>
        /// <param name="container">The container the set of services is registered into.</param>
        public void RegisterServices(Container container)
        {
            var resolver = new SimpleInjectorDependencyResolver(container);
            resolver.InitializeSplat();
            resolver.InitializeReactiveUI();

            RxApp.MainThreadScheduler = Avalonia.Threading.AvaloniaScheduler.Instance;

            RegisterAvaloniaServices(container);
        }

        #endregion Methods
    }
}
