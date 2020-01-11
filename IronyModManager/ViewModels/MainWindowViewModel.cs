﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 01-10-2020
//
// Last Modified By : Mario
// Last Modified On : 01-11-2020
// ***********************************************************************
// <copyright file="MainWindowViewModel.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;

namespace IronyModManager.ViewModels
{
    /// <summary>
    /// Class MainWindowViewModel.
    /// Implements the <see cref="IronyModManager.ViewModels.ViewModelBase" />
    /// </summary>
    /// <seealso cref="IronyModManager.ViewModels.ViewModelBase" />
    public class MainWindowViewModel : ViewModelBase
    {
        #region Properties

        /// <summary>
        /// Gets the greeting.
        /// </summary>
        /// <value>The greeting.</value>
        public virtual string Greeting => "Welcome to Avalonia!";

        #endregion Properties
    }
}
