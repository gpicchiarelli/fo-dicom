﻿// Copyright (c) 2012-2019 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using FellowOakDicom.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace FellowOakDicom.Tests.Network
{
    [Collection("Network")]
    public class AsyncDicomCGetProviderTests : IClassFixture<GlobalFixture>
    {
        private readonly XUnitDicomLogger _logger;
        private readonly IDicomServerFactory _serverFactory;
        private readonly IDicomClientFactory _clientFactory;

        public AsyncDicomCGetProviderTests(ITestOutputHelper testOutputHelper, GlobalFixture globalFixture)
        {
            _logger = new XUnitDicomLogger(testOutputHelper)
                .IncludeTimestamps()
                .IncludeThreadId()
                .WithMinimumLevel(LogLevel.Debug);
            _serverFactory = globalFixture.GetRequiredService<IDicomServerFactory>();
            _clientFactory = globalFixture.GetRequiredService<IDicomClientFactory>();
        }

        [Fact]
        public async Task OnCGetRequestAsync_ImmediateSuccess_ShouldRespond()
        {
            var port = Ports.GetNext();

            using (_serverFactory.Create<ImmediateSuccessAsyncDicomCGetProvider>(port, logger: _logger.IncludePrefix("DicomServer")))
            {
                var client = _clientFactory.Create("127.0.0.1", port, false, "SCU", "ANY-SCP");
                client.Logger = _logger.IncludePrefix(typeof(DicomClient).Name);

                DicomCGetResponse response = null;
                DicomRequest.OnTimeoutEventArgs timeout = null;
                var studyInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID().ToString();
                var request = new DicomCGetRequest(studyInstanceUID)
                {
                    OnResponseReceived = (req, res) => response = res,
                    OnTimeout = (sender, args) => timeout = args
                };

                await client.AddRequestAsync(request).ConfigureAwait(false);
                await client.SendAsync().ConfigureAwait(false);

                Assert.NotNull(response);
                Assert.Equal(DicomStatus.Success, response.Status);
                Assert.Null(timeout);
            }
        }

        [Fact]
        public async Task OnCGetRequestAsync_Pending_ShouldRespond()
        {
            var port = Ports.GetNext();

            using (_serverFactory.Create<PendingAsyncDicomCGetProvider>(port, logger: _logger.IncludePrefix("DicomServer")))
            {
                var client = _clientFactory.Create("127.0.0.1", port, false, "SCU", "ANY-SCP");
                client.Logger = _logger.IncludePrefix(typeof(DicomClient).Name);

                var responses = new ConcurrentQueue<DicomCGetResponse>();
                DicomRequest.OnTimeoutEventArgs timeout = null;
                var studyInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID().ToString();
                var request = new DicomCGetRequest(studyInstanceUID)
                {
                    OnResponseReceived = (req, res) => responses.Enqueue(res),
                    OnTimeout = (sender, args) => timeout = args
                };

                await client.AddRequestAsync(request).ConfigureAwait(false);
                await client.SendAsync().ConfigureAwait(false);

                Assert.Collection(
                    responses,
                    response1 => Assert.Equal(DicomStatus.Pending, response1.Status),
                    response2 => Assert.Equal(DicomStatus.Pending, response2.Status),
                    response3 => Assert.Equal(DicomStatus.Success, response3.Status)
                );
                Assert.Null(timeout);
            }
        }
    }

    #region helper classes

    public class ImmediateSuccessAsyncDicomCGetProvider : DicomService, IDicomServiceProvider, IDicomCGetProvider
    {
        public ImmediateSuccessAsyncDicomCGetProvider(INetworkStream stream, Encoding fallbackEncoding, Logger log,
            ILogManager logManager, INetworkManager networkManager, ITranscoderManager transcoderManager)
            : base(stream, fallbackEncoding, log, logManager, networkManager, transcoderManager)
        {
        }

        /// <inheritdoc />
        public async Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            foreach (var pc in association.PresentationContexts)
            {
                pc.SetResult(DicomPresentationContextResult.Accept);
            }

            await SendAssociationAcceptAsync(association);
        }

        /// <inheritdoc />
        public async Task OnReceiveAssociationReleaseRequestAsync()
            => await SendAssociationReleaseResponseAsync().ConfigureAwait(false);

        /// <inheritdoc />
        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            // do nothing here
        }

        /// <inheritdoc />
        public void OnConnectionClosed(Exception exception)
        {
            // do nothing here
        }

        public async Task<IEnumerable<Task<DicomCGetResponse>>> OnCGetRequestAsync(DicomCGetRequest request)
        {
            return InnerOnCGetRequestAsync();

            IEnumerable<Task<DicomCGetResponse>> InnerOnCGetRequestAsync()
            {
                yield return Task.FromResult(new DicomCGetResponse(request, DicomStatus.Success));
            }
        }
    }

    public class PendingAsyncDicomCGetProvider : DicomService, IDicomServiceProvider, IDicomCGetProvider
    {
        public PendingAsyncDicomCGetProvider(INetworkStream stream, Encoding fallbackEncoding, Logger log,
            ILogManager logManager, INetworkManager networkManager,
            ITranscoderManager transcoderManager)
            : base(stream, fallbackEncoding, log, logManager, networkManager, transcoderManager)
        {
        }

        /// <inheritdoc />
        public async Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            foreach (var pc in association.PresentationContexts)
            {
                pc.SetResult(DicomPresentationContextResult.Accept);
            }

            await SendAssociationAcceptAsync(association);
        }

        /// <inheritdoc />
        public async Task OnReceiveAssociationReleaseRequestAsync()
            => await SendAssociationReleaseResponseAsync().ConfigureAwait(false);

        /// <inheritdoc />
        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            // do nothing here
        }

        /// <inheritdoc />
        public void OnConnectionClosed(Exception exception)
        {
            // do nothing here
        }

        public async Task<IEnumerable<Task<DicomCGetResponse>>> OnCGetRequestAsync(DicomCGetRequest request)
        {
            return InnerOnCGetRequestAsync();

            IEnumerable<Task<DicomCGetResponse>> InnerOnCGetRequestAsync()
            {
                yield return Task.FromResult(new DicomCGetResponse(request, DicomStatus.Pending));
                yield return Task.FromResult(new DicomCGetResponse(request, DicomStatus.Pending));
                yield return Task.FromResult(new DicomCGetResponse(request, DicomStatus.Success));
            }
        }
    }


    #endregion
}
