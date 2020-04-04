﻿// ***********************************************************************
// Assembly         : IronyModManager.IO
// Author           : Mario
// Created          : 02-23-2020
//
// Last Modified By : Mario
// Last Modified On : 04-04-2020
// ***********************************************************************
// <copyright file="DIPackage.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System;
using IronyModManager.IO.Common.Mods;
using IronyModManager.IO.Common.Readers;
using IronyModManager.IO.Mods;
using IronyModManager.IO.Readers;
using IronyModManager.Shared;
using SimpleInjector;
using SimpleInjector.Packaging;

namespace IronyModManager.IO
{
    /// <summary>
    /// Class DIPackage.
    /// Implements the <see cref="SimpleInjector.Packaging.IPackage" />
    /// </summary>
    /// <seealso cref="SimpleInjector.Packaging.IPackage" />
    [ExcludeFromCoverage("Should not test external DI.")]
    public class DIPackage : IPackage
    {
        #region Methods

        /// <summary>
        /// Registers the set of services in the specified <paramref name="container" />.
        /// </summary>
        /// <param name="container">The container the set of services is registered into.</param>
        public void RegisterServices(Container container)
        {
            container.Collection.Register(typeof(IFileReader), typeof(DIPackage).Assembly);
            container.Register<IFileInfo, FileInfo>();
            container.Register<IReader, Reader>();
            container.Register<IModCollectionExporter, ModCollectionExporter>();
            container.Register<IModWriter, ModWriter>();
            container.Register<IModPatchExporter, ModPatchExporter>();
            container.Collection.Register(typeof(IDefinitionMerger), typeof(DIPackage).Assembly);
        }

        #endregion Methods
    }
}
