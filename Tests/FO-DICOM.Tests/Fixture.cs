﻿// Copyright (c) 2012-2019 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom.Imaging;
using FellowOakDicom.Tests.Network;
using Microsoft.Extensions.DependencyInjection;
using System;
using FellowOakDicom.Network;
using Xunit;

namespace FellowOakDicom.Tests
{
    public class GlobalFixture : IDisposable
    {
        private static readonly IDicomServerRegistry _dicomServerRegistry = new DicomServerRegistry();

        public readonly IServiceProvider ServiceProvider;

        public GlobalFixture()
        {
            var serviceCollection = new ServiceCollection()
                .AddFellowOakDicom()
                .AddSingleton(_dicomServerRegistry); // Use static registry for our tests

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public T GetRequiredService<T>() => ServiceProvider.GetRequiredService<T>();

        public void Dispose()
        {
        }
    }

    public class ImageSharpFixture : IDisposable
    {
        public readonly IServiceProvider ServiceProvider;

        public ImageSharpFixture()
        {
            var serviceCollection = new ServiceCollection()
                .AddFellowOakDicom()
                .AddImageManager<ImageSharpImageManager>();

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public void Dispose()
        {
        }
    }


    public class DependencyFixture : IDisposable
    {
        public readonly IServiceProvider ServiceProvider;

        public DependencyFixture()
        {
            var serviceCollection = new ServiceCollection()
                .AddFellowOakDicom()
                .AddImageManager<ImageSharpImageManager>()
                .AddTransient<ISomeInterface, SomeInterfaceImplementation>();

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public void Dispose()
        {
        }

    }


    [CollectionDefinition("General")]
    public class GeneralCollection : ICollectionFixture<GlobalFixture>
    { }

    [CollectionDefinition("Network")]
    public class NetworkCollection : ICollectionFixture<GlobalFixture>
    { }

    [CollectionDefinition("Imaging")]
    public class ImagingCollection : ICollectionFixture<GlobalFixture>
    { }

    [CollectionDefinition("ImageSharp")]
    public class ImageSharpCollection : ICollectionFixture<ImageSharpFixture>
    {}

    [CollectionDefinition("Dependency")]
    public class DependencyCollection: ICollectionFixture<DependencyFixture>
    {

    }

    [CollectionDefinition("Validation")]
    public class ValidationCollection: ICollectionFixture<GlobalFixture>
    { }


}
