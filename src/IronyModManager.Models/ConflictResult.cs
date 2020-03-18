﻿// ***********************************************************************
// Assembly         : IronyModManager.Models
// Author           : Mario
// Created          : 03-18-2020
//
// Last Modified By : Mario
// Last Modified On : 03-18-2020
// ***********************************************************************
// <copyright file="ConflictResult.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System;
using IronyModManager.Models.Common;
using IronyModManager.Parser.Common.Definitions;

namespace IronyModManager.Models
{
    /// <summary>
    /// Class ConflictResult.
    /// Implements the <see cref="IronyModManager.Models.Common.BaseModel" />
    /// Implements the <see cref="IronyModManager.Models.Common.IConflictResult" />
    /// </summary>
    /// <seealso cref="IronyModManager.Models.Common.BaseModel" />
    /// <seealso cref="IronyModManager.Models.Common.IConflictResult" />
    public class ConflictResult : BaseModel, IConflictResult
    {
        #region Properties

        /// <summary>
        /// Gets or sets the conflicts.
        /// </summary>
        /// <value>The conflicts.</value>
        public IIndexedDefinitions Conflicts { get; set; }

        /// <summary>
        /// Gets or sets the orphan conflicts.
        /// </summary>
        /// <value>The orphan conflicts.</value>
        public IIndexedDefinitions OrphanConflicts { get; set; }

        #endregion Properties
    }
}
