﻿// ***********************************************************************
// Assembly         : IronyModManager.Parser
// Author           : Mario
// Created          : 02-22-2020
//
// Last Modified By : Mario
// Last Modified On : 04-25-2020
// ***********************************************************************
// <copyright file="CodeParser.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CWTools.CSharp;
using CWTools.Parser;
using CWTools.Process;
using IronyModManager.DI;
using IronyModManager.Parser.Common.Parsers;
using IronyModManager.Parser.Common.Parsers.Models;
using IronyModManager.Shared;

namespace IronyModManager.Parser
{
    /// <summary>
    /// Class TextParser.
    /// Implements the <see cref="IronyModManager.Parser.Common.Parsers.ICodeParser" />
    /// </summary>
    /// <seealso cref="IronyModManager.Parser.Common.Parsers.ICodeParser" />
    [ExcludeFromCoverage("Code parser is tested in parser implementations.")]
    public class CodeParser : ICodeParser
    {
        #region Fields

        /// <summary>
        /// The cleaner conversion map
        /// </summary>
        protected static readonly Dictionary<string, string> cleanerConversionMap = new Dictionary<string, string>()
        {
            { $" {Common.Constants.Scripts.VariableSeparatorId}", Common.Constants.Scripts.VariableSeparatorId.ToString() },
            { $"{Common.Constants.Scripts.VariableSeparatorId} ", Common.Constants.Scripts.VariableSeparatorId.ToString() },
            { $" {Common.Constants.Scripts.OpeningBracket}", Common.Constants.Scripts.OpeningBracket.ToString() },
            { $"{Common.Constants.Scripts.OpeningBracket} ", Common.Constants.Scripts.OpeningBracket.ToString() },
            { $" {Common.Constants.Scripts.ClosingBracket}", Common.Constants.Scripts.ClosingBracket.ToString() },
            { $"{Common.Constants.Scripts.ClosingBracket} ", Common.Constants.Scripts.ClosingBracket.ToString() },
        };

        /// <summary>
        /// The quotes regex
        /// </summary>
        protected static readonly Regex quotesRegex = new Regex("\".*?\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// The reverse cleaner conversion map
        /// </summary>
        protected static readonly Dictionary<string, string> reverseCleanerConversionMap = new Dictionary<string, string>()
        {
            { Common.Constants.Scripts.VariableSeparatorId.ToString(), $" {Common.Constants.Scripts.VariableSeparatorId} " },
            { Common.Constants.Scripts.OpeningBracket.ToString(), $" {Common.Constants.Scripts.OpeningBracket} " },
            { Common.Constants.Scripts.ClosingBracket.ToString(), $" {Common.Constants.Scripts.ClosingBracket} " },
        };

        #endregion Fields

        #region Methods

        /// <summary>
        /// Cleans the parsed text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        public string CleanParsedText(string text)
        {
            var sb = new StringBuilder();
            foreach (var item in text)
            {
                if (!char.IsWhiteSpace(item) &&
                    !item.Equals(Common.Constants.Scripts.OpeningBracket) &&
                    !item.Equals(Common.Constants.Scripts.ClosingBracket))
                {
                    sb.Append(item);
                }
                else
                {
                    break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Cleans the whitespace.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>System.String.</returns>
        public string CleanWhitespace(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return string.Empty;
            }
            var cleaned = string.Join(' ', line.Trim().Replace("\t", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries));
            foreach (var item in cleanerConversionMap)
            {
                cleaned = cleaned.Replace(item.Key, item.Value);
            }
            return cleaned;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        public string GetKey(string line, char key)
        {
            return GetKey(line, key.ToString());
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        public string GetKey(string line, string key)
        {
            var cleaned = CleanWhitespace(line);
            if (cleaned.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                var prev = cleaned.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (prev == 0 || !char.IsWhiteSpace(cleaned[prev - 1]))
                {
                    var parsed = cleaned.Split(key, StringSplitOptions.RemoveEmptyEntries);
                    if (parsed.Count() > 0)
                    {
                        if (parsed.First().StartsWith("\""))
                        {
                            return quotesRegex.Match(parsed.First().Trim()).Value.Replace("\"", string.Empty);
                        }
                        else
                        {
                            return CleanParsedText(parsed.First().Trim().Replace("\"", string.Empty));
                        }
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        public string GetValue(string line, char key)
        {
            return GetValue(line, key.ToString());
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        public string GetValue(string line, string key)
        {
            var cleaned = CleanWhitespace(line);
            if (cleaned.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                var prev = cleaned.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (prev == 0 || (char.IsWhiteSpace(cleaned[prev - 1]) || cleaned[prev - 1] == Common.Constants.Scripts.OpeningBracket || cleaned[prev - 1] == Common.Constants.Scripts.ClosingBracket))
                {
                    var part = cleaned.Substring(cleaned.IndexOf(key, StringComparison.OrdinalIgnoreCase));
                    var parsed = part.Split(key, StringSplitOptions.RemoveEmptyEntries);
                    if (parsed.Count() > 0)
                    {
                        if (parsed.First().StartsWith("\""))
                        {
                            return quotesRegex.Match(parsed.First().Trim()).Value.Replace("\"", string.Empty);
                        }
                        else
                        {
                            return CleanParsedText(parsed.First().Trim().Replace("\"", string.Empty));
                        }
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Parses the script.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="file">The file.</param>
        /// <returns>IParseResponse.</returns>
        public IParseResponse ParseScript(IEnumerable<string> lines, string file)
        {
            var line = string.Join(Environment.NewLine, lines);
            var response = Parsers.ParseScriptFile(file, line);
            var result = DIResolver.Get<IParseResponse>();
            if (response.IsSuccess)
            {
                var successResponse = Parsers.ProcessStatements(Path.GetFileName(file), file, response.GetResult());
                result.Value = MapNode(successResponse);
            }
            else
            {
                var errorResponse = response.GetError();
                var error = DIResolver.Get<IScriptError>();
                error.Column = errorResponse.Column;
                error.Line = errorResponse.Line;
                error.Message = errorResponse.ErrorMessage;
                result.Error = error;
            }
            return result;
        }

        /// <summary>
        /// Prettifies the line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>System.String.</returns>
        public string PrettifyLine(string line)
        {
            var cleaned = CleanWhitespace(line);
            foreach (var item in reverseCleanerConversionMap)
            {
                cleaned = cleaned.Replace(item.Key, item.Value);
            }
            cleaned = string.Join(' ', cleaned.Trim().Replace("\t", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return cleaned;
        }

        /// <summary>
        /// Formats the code.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>System.String.</returns>
        protected string FormatCode(Types.Statement source)
        {
            return source.PrettyPrint().Replace("\r", string.Empty).Replace("\n", Environment.NewLine).Trim(Environment.NewLine.ToCharArray()).ReplaceTabs();
        }

        /// <summary>
        /// Maps the node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="level">The level.</param>
        /// <returns>IScriptNode.</returns>
        protected IScriptNode MapNode(Node node, int level = 1)
        {
            string code = null;
            if (level >= 2 && level <= 3)
            {
                code = FormatCode(node.ToRaw);
            }
            var nodes = node.AllChildren.Where(x => x.IsNodeC).Select(x => MapNode(x.node, level + 1)).ToList();
            var leaves = node.AllChildren.Where(x => x.IsLeafC).Select(x => MapScriptKeyValue(x.leaf, level + 1)).ToList();
            var values = node.AllChildren.Where(x => x.IsLeafValueC).Select(x => MapScriptValue(x.leafvalue, level + 1)).ToList();
            var result = DIResolver.Get<IScriptNode>();
            result.Key = node.Key.Trim();
            result.Nodes = nodes;
            result.Values = values;
            result.KeyValues = leaves;
            result.Code = code;
            return result;
        }

        /// <summary>
        /// Maps the script key value.
        /// </summary>
        /// <param name="leaf">The leaf.</param>
        /// <param name="level">The level.</param>
        /// <returns>IScriptKeyValue.</returns>
        protected IScriptKeyValue MapScriptKeyValue(Leaf leaf, int level = 1)
        {
            string code = null;
            if (level >= 2 && level <= 3)
            {
                code = FormatCode(leaf.ToRaw);
            }
            var result = DIResolver.Get<IScriptKeyValue>();
            result.Key = leaf.Key.Trim();
            result.Value = leaf.Value.ToRawString().Trim();
            result.Code = code;
            return result;
        }

        /// <summary>
        /// Maps the script value.
        /// </summary>
        /// <param name="leafValue">The leaf value.</param>
        /// <param name="level">The level.</param>
        /// <returns>IScriptValue.</returns>
        protected IScriptValue MapScriptValue(LeafValue leafValue, int level = 1)
        {
            string code = null;
            if (level >= 2 && level <= 3)
            {
                code = FormatCode(leafValue.ToRaw);
            }
            var result = DIResolver.Get<IScriptValue>();
            result.Code = code;
            result.Value = leafValue.Key.Trim();
            return result;
        }

        #endregion Methods
    }
}
