﻿// ***********************************************************************
// Assembly         : IronyModManager.Services
// Author           : Mario
// Created          : 06-19-2020
//
// Last Modified By : Mario
// Last Modified On : 11-23-2020
// ***********************************************************************
// <copyright file="ModMergeService.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using IronyModManager.DI;
using IronyModManager.IO.Common.Mods;
using IronyModManager.IO.Common.Readers;
using IronyModManager.Models.Common;
using IronyModManager.Parser.Common;
using IronyModManager.Parser.Common.Definitions;
using IronyModManager.Parser.Common.Mod;
using IronyModManager.Services.Common;
using IronyModManager.Services.Common.MessageBus;
using IronyModManager.Shared;
using IronyModManager.Shared.Cache;
using IronyModManager.Shared.MessageBus;
using IronyModManager.Storage.Common;

namespace IronyModManager.Services
{
    /// <summary>
    /// Class ModMergeService.
    /// Implements the <see cref="IronyModManager.Services.ModBaseService" />
    /// Implements the <see cref="IronyModManager.Services.Common.IModMergeService" />
    /// </summary>
    /// <seealso cref="IronyModManager.Services.ModBaseService" />
    /// <seealso cref="IronyModManager.Services.Common.IModMergeService" />
    public class ModMergeService : ModBaseService, IModMergeService
    {
        #region Fields

        /// <summary>
        /// The message bus
        /// </summary>
        private readonly IMessageBus messageBus;

        /// <summary>
        /// The mod merge exporter
        /// </summary>
        private readonly IModMergeExporter modMergeExporter;

        /// <summary>
        /// The mod patch exporter
        /// </summary>
        private readonly IModPatchExporter modPatchExporter;

        /// <summary>
        /// The parser manager
        /// </summary>
        private readonly IParserManager parserManager;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModMergeService" /> class.
        /// </summary>
        /// <param name="parserManager">The parser manager.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="messageBus">The message bus.</param>
        /// <param name="modPatchExporter">The mod patch exporter.</param>
        /// <param name="modMergeExporter">The mod merge exporter.</param>
        /// <param name="definitionInfoProviders">The definition information providers.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="modWriter">The mod writer.</param>
        /// <param name="modParser">The mod parser.</param>
        /// <param name="gameService">The game service.</param>
        /// <param name="storageProvider">The storage provider.</param>
        /// <param name="mapper">The mapper.</param>
        public ModMergeService(IParserManager parserManager, ICache cache, IMessageBus messageBus, IModPatchExporter modPatchExporter,
            IModMergeExporter modMergeExporter, IEnumerable<IDefinitionInfoProvider> definitionInfoProviders,
            IReader reader, IModWriter modWriter,
            IModParser modParser, IGameService gameService,
            IStorageProvider storageProvider, IMapper mapper) : base(cache, definitionInfoProviders, reader, modWriter, modParser, gameService, storageProvider, mapper)
        {
            this.parserManager = parserManager;
            this.messageBus = messageBus;
            this.modMergeExporter = modMergeExporter;
            this.modPatchExporter = modPatchExporter;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// merge collection as an asynchronous operation.
        /// </summary>
        /// <param name="conflictResult">The conflict result.</param>
        /// <param name="modOrder">The mod order.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns>Task&lt;IMod&gt;.</returns>
        public virtual async Task<IMod> MergeCollectionByDefinitionsAsync(IConflictResult conflictResult, IList<string> modOrder, string collectionName)
        {
            static string cleanString(string text)
            {
                text ??= string.Empty;
                text = text.Replace(" ", string.Empty).Replace("\t", string.Empty).Trim();
                return text;
            }
            static string getNextVariableName(List<IDefinition> exportDefinitons, IDefinition definition)
            {
                var count = exportDefinitons.Where(p => p.Id.Equals(definition.Id, StringComparison.OrdinalIgnoreCase)).Count() + 1;
                var name = $"{definition.Id}_{count}";
                while (exportDefinitons.Any(p => p.Id.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    count++;
                    name = $"{definition.Id}_{count}";
                }
                return name;
            }
            void parseNameSpaces(List<IDefinition> exportDefinitions, IDefinition def)
            {
                var namespaces = def.Variables?.Where(p => p.ValueType == Parser.Common.ValueType.Namespace);
                if (namespaces?.Count() > 0)
                {
                    foreach (var name in namespaces)
                    {
                        if (!exportDefinitions.Any(p => p.ValueType == Parser.Common.ValueType.Namespace && cleanString(p.Code).Equals(cleanString(name.Code))))
                        {
                            var copy = CopyDefinition(name);
                            copy.CodeTag = def.CodeTag;
                            copy.CodeSeparator = def.CodeSeparator;
                            exportDefinitions.Add(copy);
                        }
                    }
                }
            }
            void parseVariables(List<IDefinition> exportDefinitions, IDefinition def)
            {
                var variables = def.Variables?.Where(p => p.ValueType == Parser.Common.ValueType.Variable);
                if (variables?.Count() > 0)
                {
                    foreach (var variable in variables)
                    {
                        var copy = CopyDefinition(variable);
                        var oldId = copy.Id;
                        copy.Id = getNextVariableName(exportDefinitions, variable);
                        copy.Code = string.Join(" ", copy.Code.Split(" ", StringSplitOptions.None).Select(p => p.Contains(oldId) && p.ReplaceNewLine().Trim() == oldId ? p.Replace(oldId, copy.Id) : p));
                        copy.OriginalCode = string.Join(" ", copy.OriginalCode.Split(" ", StringSplitOptions.None).Select(p => p.Contains(oldId) && p.ReplaceNewLine().Trim() == oldId ? p.Replace(oldId, copy.Id) : p));
                        copy.CodeTag = def.CodeTag;
                        copy.CodeSeparator = def.CodeSeparator;
                        exportDefinitions.Add(copy);
                        def.Code = string.Join(" ", def.Code.Split(" ", StringSplitOptions.None).Select(p => p.Contains(oldId) && p.ReplaceNewLine().Trim() == oldId ? p.Replace(oldId, copy.Id) : p));
                        def.OriginalCode = string.Join(" ", def.OriginalCode.Split(" ", StringSplitOptions.None).Select(p => p.Contains(oldId) && p.ReplaceNewLine().Trim() == oldId ? p.Replace(oldId, copy.Id) : p));
                    }
                }
            }

            var game = GameService.GetSelected();
            if (game == null)
            {
                return null;
            }
            var total = conflictResult.AllConflicts.GetAll().Count(p => p.ValueType != Parser.Common.ValueType.Variable && p.ValueType != Parser.Common.ValueType.Namespace);
            if (conflictResult.AllConflicts.GetAll().Count() > 0)
            {
                var allMods = GetInstalledModsInternal(game, false).ToList();
                var mergeCollectionPath = collectionName.GenerateValidFileName();
                var collectionMods = GetCollectionMods(allMods);

                await ModWriter.PurgeModDirectoryAsync(new ModWriterParameters()
                {
                    RootDirectory = game.UserDirectory,
                    Path = Path.Combine(Shared.Constants.ModDirectory, mergeCollectionPath)
                }, true);
                await ModWriter.CreateModDirectoryAsync(new ModWriterParameters()
                {
                    RootDirectory = game.UserDirectory,
                    Path = Shared.Constants.ModDirectory
                });
                await ModWriter.CreateModDirectoryAsync(new ModWriterParameters()
                {
                    RootDirectory = game.UserDirectory,
                    Path = Path.Combine(Shared.Constants.ModDirectory, mergeCollectionPath)
                });

                var mod = DIResolver.Get<IMod>();
                mod.DescriptorFile = $"{Shared.Constants.ModDirectory}/{mergeCollectionPath}{Shared.Constants.ModExtension}";
                mod.FileName = GetModDirectory(game, mergeCollectionPath).Replace("\\", "/");
                mod.Name = collectionName;
                mod.Source = ModSource.Local;
                mod.Version = allMods.OrderByDescending(p => p.Version).FirstOrDefault() != null ? allMods.OrderByDescending(p => p.Version).FirstOrDefault().Version : string.Empty;
                await ModWriter.WriteDescriptorAsync(new ModWriterParameters()
                {
                    Mod = mod,
                    RootDirectory = game.UserDirectory,
                    Path = mod.DescriptorFile
                }, true);
                Cache.Invalidate(ModsCachePrefix, ConstructModsCacheKey(game, true), ConstructModsCacheKey(game, false));

                var exportPath = Path.Combine(game.UserDirectory, Shared.Constants.ModDirectory, mergeCollectionPath);

                var collection = GetAllModCollectionsInternal().FirstOrDefault(p => p.IsSelected);
                var patchName = GenerateCollectionPatchName(collection.Name);
                var state = await modPatchExporter.GetPatchStateAsync(new ModPatchExporterParameters()
                {
                    RootPath = Path.Combine(game.UserDirectory, Shared.Constants.ModDirectory),
                    PatchName = patchName
                }, true);

                var resolvedConflicts = state?.ResolvedConflicts ?? new List<IDefinition>();
                var ignoredConflicts = state?.IgnoredConflicts ?? new List<IDefinition>();
                var conflictHistory = state?.ConflictHistory ?? new List<IDefinition>();
                var customConflicts = state?.CustomConflicts ?? new List<IDefinition>();

                var resolvedIndex = DIResolver.Get<IIndexedDefinitions>();
                resolvedIndex.InitMap(resolvedConflicts, true);
                conflictResult.ResolvedConflicts = resolvedIndex;
                var ignoredIndex = DIResolver.Get<IIndexedDefinitions>();
                ignoredIndex.InitMap(ignoredConflicts, true);
                conflictResult.IgnoredConflicts = ignoredIndex;
                var customConflictsIndex = DIResolver.Get<IIndexedDefinitions>();
                customConflictsIndex.InitMap(customConflicts);
                conflictResult.CustomConflicts = customConflictsIndex;
                var conflictHistoryIndex = DIResolver.Get<IIndexedDefinitions>();
                conflictHistoryIndex.InitMap(conflictHistory);
                if (customConflicts.Count() > 0)
                {
                    total += customConflicts.Count();
                }

                double lastPercentage = 0;
                int processed = 0;

                foreach (var file in conflictResult.CustomConflicts.GetAllFileKeys())
                {
                    var definition = CopyDefinition(conflictResult.CustomConflicts.GetByFile(file).FirstOrDefault());
                    definition.Code = conflictHistoryIndex.GetByTypeAndId(definition.TypeAndId).FirstOrDefault().Code;
                    await modMergeExporter.ExportDefinitionsAsync(new ModMergeDefinitionExporterParameters()
                    {
                        ExportPath = exportPath,
                        Definitions = definition != null ? PopulateModPath(new List<IDefinition>() { definition }, collectionMods) : null,
                        Game = game.Type
                    });
                    processed++;
                    var percentage = GetProgressPercentage(total, processed, 99.9);
                    if (lastPercentage != percentage)
                    {
                        await messageBus.PublishAsync(new ModDefinitionMergeProgressEvent(percentage));
                    }
                    lastPercentage = percentage;
                }

                var dumpedIds = new HashSet<string>();
                var fileCount = conflictResult.AllConflicts.GetAllFileKeys().Count();
                var counter = 0;
                foreach (var file in conflictResult.AllConflicts.GetAllFileKeys())
                {
                    counter++;
                    var definitions = conflictResult.AllConflicts.GetByFile(file).Where(p => p.ValueType != Parser.Common.ValueType.EmptyFile);
                    if (definitions.Count() > 0)
                    {
                        var exportDefinitions = new List<IDefinition>();
                        foreach (var definitionGroup in definitions.GroupBy(p => p.TypeAndId).Where(p => p.FirstOrDefault() != null && p.FirstOrDefault().ValueType != Parser.Common.ValueType.Namespace && p.FirstOrDefault().ValueType != Parser.Common.ValueType.Variable))
                        {
                            // Orphans are placed under resolved items during analysis so no need to check them
                            var resolved = conflictResult.ResolvedConflicts.GetByTypeAndId(definitionGroup.FirstOrDefault().TypeAndId);
                            var overwritten = conflictResult.OverwrittenConflicts.GetByTypeAndId(definitionGroup.FirstOrDefault().TypeAndId);
                            if (resolved.Count() > 0 || overwritten.Count() > 0)
                            {
                                // Resolved takes priority, since if an item was resolved no need to use the overwritten code
                                // Also fetch the code from the patch state.json object to get the latest version of the code to dump
                                if (resolved.Count() > 0)
                                {
                                    foreach (var item in resolved)
                                    {
                                        if (!dumpedIds.Contains(item.TypeAndId))
                                        {
                                            var copy = CopyDefinition(item);
                                            if (copy.ValueType != Parser.Common.ValueType.Binary)
                                            {
                                                copy.Code = conflictHistoryIndex.GetByTypeAndId(item.TypeAndId).FirstOrDefault().Code;
                                                var parsed = parserManager.Parse(new Parser.Common.Args.ParserManagerArgs()
                                                {
                                                    ContentSHA = copy.ContentSHA,
                                                    File = copy.File,
                                                    GameType = GameService.GetSelected().Type,
                                                    Lines = copy.Code.SplitOnNewLine(false),
                                                    ModDependencies = copy.Dependencies,
                                                    ModName = copy.ModName
                                                });
                                                var others = parsed.Where(p => p.ValueType != Parser.Common.ValueType.Variable && p.ValueType != Parser.Common.ValueType.Namespace);
                                                foreach (var other in others)
                                                {
                                                    var variables = parsed.Where(p => p.ValueType == Parser.Common.ValueType.Variable || p.ValueType == Parser.Common.ValueType.Namespace);
                                                    other.Variables = variables;
                                                    parseNameSpaces(exportDefinitions, other);
                                                    parseVariables(exportDefinitions, other);
                                                    dumpedIds.Add(other.TypeAndId);
                                                    exportDefinitions.Add(CopyDefinition(other));
                                                }
                                            }
                                            else
                                            {
                                                dumpedIds.Add(copy.TypeAndId);
                                                exportDefinitions.Add(copy);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var item in overwritten)
                                    {
                                        if (!dumpedIds.Contains(item.TypeAndId))
                                        {
                                            var copy = CopyDefinition(item);
                                            if (copy.Variables?.Count() > 0)
                                            {
                                                parseNameSpaces(exportDefinitions, copy);
                                                parseVariables(exportDefinitions, copy);
                                            }
                                            dumpedIds.Add(copy.TypeAndId);
                                            exportDefinitions.Add(copy);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Check if this is a conflict so we can then perform evaluation of which definition would win based on current order
                                var files = new List<string>();
                                var conflicted = conflictResult.Conflicts.GetByTypeAndId(definitionGroup.FirstOrDefault().TypeAndId);
                                IDefinition priorityDef;
                                bool allowDuplicate = false;
                                if (conflicted.Count() > 0)
                                {
                                    files.AddRange(conflicted.Select(p => p.File));
                                    // More then 1 per file in a mod?
                                    var modGroups = conflicted.GroupBy(p => p.ModName);
                                    if (modGroups.Any(p => p.GroupBy(p => p.FileCI).Count() > 1))
                                    {
                                        allowDuplicate = true;
                                        var validDefinitions = new List<IDefinition>();
                                        foreach (var modGroup in modGroups)
                                        {
                                            if (modGroup.GroupBy(p => p.FileCI).Count() > 1)
                                            {
                                                foreach (var item in modGroup)
                                                {
                                                    if (item.FileCI.Equals(file.ToLowerInvariant()))
                                                    {
                                                        validDefinitions.Add(item);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                validDefinitions.AddRange(modGroup);
                                            }
                                        }
                                        priorityDef = EvalDefinitionPriorityInternal(validDefinitions.OrderBy(p => modOrder.IndexOf(p.ModName))).Definition;
                                    }
                                    else
                                    {
                                        priorityDef = EvalDefinitionPriorityInternal(conflicted.OrderBy(p => modOrder.IndexOf(p.ModName))).Definition;
                                    }
                                }
                                else
                                {
                                    priorityDef = definitionGroup.FirstOrDefault();
                                    files.Add(priorityDef.File);
                                }
                                if (priorityDef != null)
                                {
                                    IDefinition priorityDefCopy = null;
                                    if (allowDuplicate)
                                    {
                                        priorityDefCopy = CopyDefinition(priorityDef);
                                        exportDefinitions.Add(priorityDefCopy);
                                        dumpedIds.Add(priorityDef.TypeAndId);
                                    }
                                    else if (!dumpedIds.Contains(priorityDef.TypeAndId))
                                    {
                                        priorityDefCopy = CopyDefinition(priorityDef);
                                        exportDefinitions.Add(priorityDefCopy);
                                        dumpedIds.Add(priorityDef.TypeAndId);
                                    }
                                    if (priorityDefCopy?.Variables?.Count() > 0)
                                    {
                                        parseNameSpaces(exportDefinitions, priorityDefCopy);
                                        parseVariables(exportDefinitions, priorityDefCopy);
                                    }
                                }
                            }
                            processed += definitionGroup.Count();
                            var percentage = GetProgressPercentage(total, processed, 99.9);
                            if (lastPercentage != percentage)
                            {
                                await messageBus.PublishAsync(new ModDefinitionMergeProgressEvent(percentage));
                            }
                            lastPercentage = percentage;
                        }

                        // Prevent exporting only namespaces or variables?
                        if (exportDefinitions.All(p => p.ValueType == Parser.Common.ValueType.Namespace || p.ValueType == Parser.Common.ValueType.Variable))
                        {
                            exportDefinitions.Clear();
                        }
                        // Something to export?
                        if (exportDefinitions.Count > 0)
                        {
                            IDefinition merged = null;
                            var groupedMods = exportDefinitions.GroupBy(p => p.ModName);
                            if (groupedMods.Count() > 1)
                            {
                                var topGroup = groupedMods.OrderByDescending(p => p.Count()).FirstOrDefault();
                                foreach (var groupedMod in groupedMods.Where(p => p.Key != topGroup.Key))
                                {
                                    foreach (var item in groupedMod)
                                    {
                                        var allConflicts = conflictResult.AllConflicts.GetByTypeAndId(item.TypeAndId).Where(p => p.ModName.Equals(topGroup.Key));
                                        if (allConflicts.Count() > 1)
                                        {
                                            var match = allConflicts.FirstOrDefault(p => p.FileCI.Equals(item.FileCI));
                                            if (match != null)
                                            {
                                                item.Order = match.Order;
                                            }
                                            else
                                            {
                                                var infoProvider = DefinitionInfoProviders.FirstOrDefault(p => p.CanProcess(game.Type));
                                                if (infoProvider.DefinitionUsesFIOSRules(item))
                                                {
                                                    item.Order = allConflicts.OrderBy(p => p.File, StringComparer.Ordinal).FirstOrDefault().Order;
                                                }
                                                else
                                                {
                                                    item.Order = allConflicts.OrderByDescending(p => p.File, StringComparer.Ordinal).FirstOrDefault().Order;
                                                }
                                            }
                                        }
                                        else if (allConflicts.Count() == 1)
                                        {
                                            item.Order = allConflicts.FirstOrDefault().Order;
                                        }
                                    }
                                }
                                var variables = exportDefinitions.Where(p => p.ValueType == Parser.Common.ValueType.Variable || p.ValueType == Parser.Common.ValueType.Namespace).OrderBy(p => p.Id);
                                var other = exportDefinitions.Where(p => p.ValueType != Parser.Common.ValueType.Variable && p.ValueType != Parser.Common.ValueType.Namespace).OrderBy(p => p.Order);
                                merged = MergeDefinitions(variables.Concat(other));
                                // Preserve proper file casing
                                var conflicts = conflictResult.AllConflicts.GetByFile(file);
                                merged.File = conflicts.FirstOrDefault().File;
                                merged.DiskFile = conflicts.FirstOrDefault().DiskFile;
                            }
                            await modMergeExporter.ExportDefinitionsAsync(new ModMergeDefinitionExporterParameters()
                            {
                                ExportPath = exportPath,
                                Definitions = merged != null ? PopulateModPath(new List<IDefinition>() { merged }, collectionMods) : null,
                                Game = game.Type
                            });
                            if (counter >= fileCount)
                            {
                                await messageBus.PublishAsync(new ModDefinitionMergeProgressEvent(100));
                            }
                        }
                    }
                }

                return mod;
            }
            return null;
        }

        /// <summary>
        /// Merges the collection by files asynchronous.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns>Task&lt;IMod&gt;.</returns>
        public virtual async Task<IMod> MergeCollectionByFilesAsync(string collectionName)
        {
            var game = GameService.GetSelected();
            if (game == null || string.IsNullOrWhiteSpace(collectionName))
            {
                return null;
            }

            var allMods = GetInstalledModsInternal(game, false).ToList();
            var collectionMods = GetCollectionMods(allMods).ToList();
            if (collectionMods.Count == 0)
            {
                return null;
            }

            var mergeCollectionPath = collectionName.GenerateValidFileName();
            await ModWriter.PurgeModDirectoryAsync(new ModWriterParameters()
            {
                RootDirectory = game.UserDirectory,
                Path = Path.Combine(Shared.Constants.ModDirectory, mergeCollectionPath)
            }, true);
            await ModWriter.CreateModDirectoryAsync(new ModWriterParameters()
            {
                RootDirectory = game.UserDirectory,
                Path = Shared.Constants.ModDirectory
            });
            await ModWriter.CreateModDirectoryAsync(new ModWriterParameters()
            {
                RootDirectory = game.UserDirectory,
                Path = Path.Combine(Shared.Constants.ModDirectory, mergeCollectionPath)
            });

            var mod = DIResolver.Get<IMod>();
            mod.DescriptorFile = $"{Shared.Constants.ModDirectory}/{mergeCollectionPath}{Shared.Constants.ModExtension}";
            mod.FileName = GetModDirectory(game, mergeCollectionPath).Replace("\\", "/");
            mod.Name = collectionName;
            mod.Source = ModSource.Local;
            mod.Version = allMods.OrderByDescending(p => p.Version).FirstOrDefault() != null ? allMods.OrderByDescending(p => p.Version).FirstOrDefault().Version : string.Empty;
            mod.FullPath = GetModDirectory(game, mergeCollectionPath);
            await ModWriter.WriteDescriptorAsync(new ModWriterParameters()
            {
                Mod = mod,
                RootDirectory = game.UserDirectory,
                Path = mod.DescriptorFile
            }, true);
            Cache.Invalidate(ModsCachePrefix, ConstructModsCacheKey(game, true), ConstructModsCacheKey(game, false));

            var exportPath = Path.Combine(game.UserDirectory, Shared.Constants.ModDirectory, mergeCollectionPath);
            var collection = GetAllModCollectionsInternal().FirstOrDefault(p => p.IsSelected);
            var patchName = GenerateCollectionPatchName(collection.Name);
            var patchMod = allMods.FirstOrDefault(p => p.Name.Equals(patchName));
            if (patchMod != null)
            {
                collectionMods.Add(patchMod);
            }

            await messageBus.PublishAsync(new ModFileMergeProgressEvent(1, 0));
            await PopulateModFilesInternalAsync(collectionMods);
            await messageBus.PublishAsync(new ModFileMergeProgressEvent(1, 100));

            var totalFiles = collectionMods.Where(p => p.Files != null).SelectMany(p => p.Files.Where(f => game.GameFolders.Any(s => f.StartsWith(s, StringComparison.OrdinalIgnoreCase)))).Count();
            double lastPercentage = 0;
            var processed = 0;
            foreach (var collectionMod in collectionMods.Where(p => p.Files != null))
            {
                foreach (var file in collectionMod.Files.Where(p => game.GameFolders.Any(s => p.StartsWith(s, StringComparison.OrdinalIgnoreCase))))
                {
                    processed++;
                    await modMergeExporter.ExportFilesAsync(new ModMergeFileExporterParameters()
                    {
                        RootModPath = collectionMod.FullPath,
                        ExportFile = file,
                        ExportPath = mod.FullPath
                    });
                    var percentage = GetProgressPercentage(totalFiles, processed, 100);
                    if (lastPercentage != percentage)
                    {
                        await messageBus.PublishAsync(new ModFileMergeProgressEvent(2, percentage));
                    }
                    lastPercentage = percentage;
                }
            }
            return mod;
        }

        /// <summary>
        /// Gets the progress percentage.
        /// </summary>
        /// <param name="total">The total.</param>
        /// <param name="processed">The processed.</param>
        /// <param name="maxPerc">The maximum perc.</param>
        /// <returns>System.Int32.</returns>
        protected virtual double GetProgressPercentage(double total, double processed, double maxPerc = 100)
        {
            var perc = Math.Round(processed / total * 100, 2);
            if (perc < 0)
            {
                perc = 0;
            }
            else if (perc > maxPerc)
            {
                perc = maxPerc;
            }
            return perc;
        }

        /// <summary>
        /// Merges the definitions.
        /// </summary>
        /// <param name="definitions">The definitions.</param>
        /// <returns>IDefinition.</returns>
        protected virtual IDefinition MergeDefinitions(IEnumerable<IDefinition> definitions)
        {
            static void appendLine(StringBuilder sb, IEnumerable<string> lines, int indent = 0)
            {
                foreach (var item in lines)
                {
                    if (indent == 0)
                    {
                        sb.AppendLine(item);
                    }
                    else
                    {
                        sb.AppendLine($"{new string(' ', indent)}{item}");
                    }
                }
            }
            static void mergeCode(StringBuilder sb, string codeTag, string separator, IEnumerable<string> variables, IEnumerable<string> lines)
            {
                if (Shared.Constants.CodeSeparators.ClosingSeparators.Map.ContainsKey(separator))
                {
                    var closingTag = Shared.Constants.CodeSeparators.ClosingSeparators.Map[separator];
                    sb.AppendLine($"{codeTag} = {separator}");
                    if (lines.Count() == 0)
                    {
                        foreach (var item in variables)
                        {
                            var splitLines = item.SplitOnNewLine();
                            appendLine(sb, splitLines, 4);
                        }
                    }
                    else
                    {
                        bool varsInserted = false;
                        foreach (var item in lines)
                        {
                            var splitLines = item.SplitOnNewLine();
                            foreach (var split in splitLines)
                            {
                                sb.AppendLine($"{new string(' ', 4)}{split}");
                                if (!varsInserted && split.Contains(Shared.Constants.CodeSeparators.ClosingSeparators.CurlyBracket))
                                {
                                    varsInserted = true;
                                    foreach (var var in variables)
                                    {
                                        var splitVars = var.SplitOnNewLine();
                                        appendLine(sb, splitVars, 8);
                                    }
                                }
                            }
                        }
                    }
                    sb.AppendLine(closingTag);
                }
                else
                {
                    sb.AppendLine($"{codeTag}{separator}");
                    foreach (var item in variables)
                    {
                        var splitLines = item.SplitOnNewLine();
                        appendLine(sb, splitLines, 4);
                    }
                    foreach (var item in lines)
                    {
                        var splitLines = item.SplitOnNewLine();
                        appendLine(sb, splitLines, 4);
                    }
                }
            }

            var sb = new StringBuilder();
            var copy = CopyDefinition(definitions.FirstOrDefault());
            if (copy.ValueType == Parser.Common.ValueType.Namespace || copy.ValueType == Parser.Common.ValueType.Variable)
            {
                copy.ValueType = Parser.Common.ValueType.Object;
            }
            var groups = definitions.GroupBy(p => p.CodeTag, StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups.OrderBy(p => p.FirstOrDefault().CodeTag, StringComparer.OrdinalIgnoreCase))
            {
                bool hasCodeTag = !string.IsNullOrWhiteSpace(group.FirstOrDefault().CodeTag);
                if (!hasCodeTag)
                {
                    var namespaces = group.Where(p => p.ValueType == Parser.Common.ValueType.Namespace);
                    var variables = group.Where(p => p.ValueType == Parser.Common.ValueType.Variable);
                    var other = group.Where(p => p.ValueType != Parser.Common.ValueType.Variable && p.ValueType != Parser.Common.ValueType.Namespace);
                    var code = namespaces.Select(p => p.OriginalCode).Concat(variables.Select(p => p.OriginalCode)).Concat(other.Select(p => p.OriginalCode));
                    appendLine(sb, code);
                }
                else
                {
                    var namespaces = group.Where(p => p.ValueType == Parser.Common.ValueType.Namespace);
                    var variables = definitions.Where(p => p.ValueType == Parser.Common.ValueType.Variable && !string.IsNullOrWhiteSpace(p.CodeTag));
                    var other = group.Where(p => p.ValueType != Parser.Common.ValueType.Variable && p.ValueType != Parser.Common.ValueType.Namespace);
                    var vars = namespaces.Select(p => p.OriginalCode).Concat(variables.Select(p => p.OriginalCode));
                    var code = other.Select(p => p.OriginalCode);
                    mergeCode(sb, group.FirstOrDefault().CodeTag, group.FirstOrDefault().CodeSeparator, vars, code);
                }
            }

            copy.Code = sb.ToString();
            return copy;
        }

        #endregion Methods
    }
}
