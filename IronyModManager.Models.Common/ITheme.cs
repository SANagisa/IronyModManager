﻿// ***********************************************************************
// Assembly         : IronyModManager.Models.Common
// Author           : Mario
// Created          : 01-13-2020
//
// Last Modified By : Mario
// Last Modified On : 01-20-2020
// ***********************************************************************
// <copyright file="ITheme.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace IronyModManager.Models.Common
{
    /// <summary>
    /// Interface ITheme
    /// Implements the <see cref="IronyModManager.Models.Common.IModel" />
    /// </summary>
    /// <seealso cref="IronyModManager.Models.Common.IModel" />
    public interface ITheme : IModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        /// <value><c>true</c> if this instance is selected; otherwise, <c>false</c>.</value>
        bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        Enums.Theme Type { get; set; }

        #endregion Properties
    }
}
