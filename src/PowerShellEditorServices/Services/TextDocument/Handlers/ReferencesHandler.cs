﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerShell.EditorServices.Services;
using Microsoft.PowerShell.EditorServices.Services.Symbols;
using Microsoft.PowerShell.EditorServices.Services.TextDocument;
using Microsoft.PowerShell.EditorServices.Utility;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.PowerShell.EditorServices.Handlers
{
    class ReferencesHandler : IReferencesHandler
    {
        private readonly ILogger _logger;
        private readonly SymbolsService _symbolsService;
        private readonly WorkspaceService _workspaceService;
        private ReferenceCapability _capability;

        public ReferencesHandler(ILoggerFactory factory, SymbolsService symbolsService, WorkspaceService workspaceService)
        {
            _logger = factory.CreateLogger<ReferencesHandler>();
            _symbolsService = symbolsService;
            _workspaceService = workspaceService;
        }

        public ReferenceRegistrationOptions GetRegistrationOptions()
        {
            return new ReferenceRegistrationOptions
            {
                DocumentSelector = LspUtils.PowerShellDocumentSelector
            };
        }

        public Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
        {
            ScriptFile scriptFile = _workspaceService.GetFile(request.TextDocument.Uri);

            SymbolReference foundSymbol =
                _symbolsService.FindSymbolAtLocation(
                    scriptFile,
                    (int)request.Position.Line + 1,
                    (int)request.Position.Character + 1);

            List<SymbolReference> referencesResult =
                _symbolsService.FindReferencesOfSymbol(
                    foundSymbol,
                    _workspaceService.ExpandScriptReferences(scriptFile),
                    _workspaceService);

            var locations = new List<Location>();

            if (referencesResult != null)
            {
                foreach (SymbolReference foundReference in referencesResult)
                {
                    locations.Add(new Location
                    {
                        Uri = PathUtils.ToUri(foundReference.FilePath),
                        Range = GetRangeFromScriptRegion(foundReference.ScriptRegion)
                    });
                }
            }

            return Task.FromResult(new LocationContainer(locations));
        }

        public void SetCapability(ReferenceCapability capability)
        {
            _capability = capability;
        }

        private static Range GetRangeFromScriptRegion(ScriptRegion scriptRegion)
        {
            return new Range
            {
                Start = new Position
                {
                    Line = scriptRegion.StartLineNumber - 1,
                    Character = scriptRegion.StartColumnNumber - 1
                },
                End = new Position
                {
                    Line = scriptRegion.EndLineNumber - 1,
                    Character = scriptRegion.EndColumnNumber - 1
                }
            };
        }
    }
}
