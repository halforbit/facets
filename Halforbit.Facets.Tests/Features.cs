using Autofac;
using Halforbit.Facets.Attributes;
using Halforbit.Facets.Autofac.Implementation;
using Halforbit.Facets.Exceptions;
using Halforbit.Facets.Implementation;
using Halforbit.Facets.Interface;
using Moq;
using System;
using Xunit;

namespace Halforbit.Facets.Tests
{
    // TODO:

    // Support DI containers

    // Allow implicitly constructed root types

    // Define behavior and handle facet collision / ambiguity

    public partial class Features
    {
        const string TestRootPath = "c:/test";

        const string TestConfigKey = "config-key";

        [Fact, Trait("Type", "Unit")]
        public void FacetShouldCreateInstance()
        {
            var context = CreateContext<IFacetContext>();

            var serializer = context.Serializer as JsonSerializer;

            Assert.NotNull(serializer);
        }

        [Fact, Trait("Type", "Unit")]
        public void FacetParameterShouldCreateInstance()
        {
            var context = CreateContext<IFacetParameterContext>();

            var storage = context.Storage as LocalFileStorage;

            Assert.NotNull(storage);

            Assert.Equal(TestRootPath, storage.RootPath);
        }

        [Fact, Trait("Type", "Unit")]
        public void NestedFacetDependenciesShouldSelfCompose()
        {
            var context = CreateContext<IComposedContext>();

            var dataStore = context.DataStore as DataStore<string>;

            Assert.NotNull(dataStore);

            Assert.NotNull(dataStore.Serializer as JsonSerializer);

            var storage = dataStore.Storage as LocalFileStorage;

            Assert.NotNull(storage);

            Assert.Equal(TestRootPath, storage.RootPath);

            Assert.NotNull(dataStore.Compressor as GZipCompressor);
        }

        [Fact, Trait("Type", "Unit")]
        public void MissingResultFacetThrowsResultResolutionException()
        {
            var context = CreateContext<IMissingResultFacetContext>();

            Assert.Throws<ResultResolutionException>(() => context.Serializer);
        }

        [Fact, Trait("Type", "Unit")]
        public void MissingParameterFacetThrowsDependencyResolutionException()
        {
            var context = CreateContext<IMissingParameterFacetContext>();

            Assert.Throws<ParameterResolutionException>(() => context.DataStore);
        }

        [Fact, Trait("Type", "Unit")]
        public void FacetShouldCoalesceFromAncestor()
        {
            var context = CreateContext<IFacetAncestorContext>();

            Assert.NotNull(context.Serializer as JsonSerializer);
        }

        [Fact, Trait("Type", "Unit")]
        public void FacetsShouldCoalesceFromSources()
        {
            var context = CreateContext<ISourceContext>();

            var dataStore = context.DataStore as DataStore<byte[]>;

            Assert.NotNull(dataStore);

            Assert.NotNull(dataStore.Serializer as JsonSerializer);

            var storage = dataStore.Storage as LocalFileStorage;

            Assert.NotNull(storage);

            Assert.Equal(TestRootPath, storage.RootPath);

            Assert.NotNull(dataStore.Compressor as GZipCompressor);
        }

        [Fact, Trait("Type", "Unit")]
        public void OptionalFacetParametersAreOptional()
        {
            var context = CreateContext<IOptionalOmittedContext>();

            var dataStore = context.DataStore as DataStore<string>;

            Assert.NotNull(dataStore);

            Assert.NotNull(dataStore.Serializer as JsonSerializer);

            var storage = dataStore.Storage as LocalFileStorage;

            Assert.NotNull(storage);

            Assert.Equal(TestRootPath, storage.RootPath);

            Assert.Null(dataStore.Compressor);
        }

        [Fact, Trait("Type", "Unit")]
        public void ConfigKeyFacetParameterPullsFromConfigurationProvider()
        {
            var configurationProviderMock = new Mock<IConfigurationProvider>(MockBehavior.Strict);

            configurationProviderMock
                .Setup(m => m.GetValue(TestConfigKey))
                .Returns(TestRootPath);

            var context = CreateContext<IConfigKeyContext>(configurationProviderMock.Object);

            var storage = context.Storage as LocalFileStorage;

            Assert.NotNull(storage);

            Assert.Equal(TestRootPath, storage.RootPath);

            configurationProviderMock.Verify(
                m => m.GetValue(TestConfigKey),
                Times.Once);
        }

        [Fact, Trait("Type", "Unit")]
        public void ContextPropertiesAreLazyInstanced()
        {
            var configurationProviderMock = new Mock<IConfigurationProvider>(MockBehavior.Strict);

            configurationProviderMock
                .Setup(m => m.GetValue(TestConfigKey))
                .Returns(TestRootPath);

            var context = CreateContext<IConfigKeyContext>(configurationProviderMock.Object);

            configurationProviderMock.Verify(
                m => m.GetValue(TestConfigKey),
                Times.Never);

            var storage = context.Storage as LocalFileStorage;

            Assert.NotNull(storage);

            Assert.Equal(TestRootPath, storage.RootPath);

            configurationProviderMock.Verify(
                m => m.GetValue(TestConfigKey),
                Times.Once);
        }

        [Fact, Trait("Type", "Unit")]
        public void SubContextsAreLazyInstanced()
        {
            var configurationProviderMock = new Mock<IConfigurationProvider>(MockBehavior.Strict);

            configurationProviderMock
                .Setup(m => m.GetValue(TestConfigKey))
                .Returns(TestRootPath);

            var context = CreateContext<IParentContext>(configurationProviderMock.Object);

            configurationProviderMock.Verify(
                m => m.GetValue(TestConfigKey),
                Times.Never);

            var subContext = context.SubContext;

            var storage = subContext.Storage as LocalFileStorage;

            Assert.NotNull(storage);

            Assert.Equal(TestRootPath, storage.RootPath);

            configurationProviderMock.Verify(
                m => m.GetValue(TestConfigKey),
                Times.Once);
        }

        [Fact, Trait("Type", "Unit")]
        public void UsesDependencyResolver()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<Dependency>().AsImplementedInterfaces();

            var container = builder.Build();

            var context = CreateContext<IDependentContext>(
                dependencyResolver: new AutofacDependencyResolver(container));

            var serializer = context.Serializer as JsonSerializerWithDependency;

            Assert.NotNull(serializer);

            Assert.NotNull(serializer.Dependency);
        }

        [Fact, Trait("Type", "Unit")]
        public void UsesAncestorFacetAndGenericParameters()
        {
            var context = CreateContext<IStoreParentContext>();

            var service = context.Things.Get;

            Assert.NotNull(service);

            Assert.NotNull(service.RequestMapper);

            Assert.Equal("steve", service.RequestMapper.Name);
        }

        [Fact, Trait("Type", "Unit")]
        public void Uses_WhenDependencyNotUsed_ThrowDependencyUnusedException()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<UsesDependency>().AsImplementedInterfaces();

            var container = builder.Build();

            var context = CreateContext<IUsesContext>(
                dependencyResolver: new AutofacDependencyResolver(container));

            Assert.Throws<DependencyUnusedException>(() => context.NonDependant);
        }

        [Fact, Trait("Type", "Unit")]
        public void Uses_WhenUsingConcreteSubtype_Success()
        {
            var builder = new ContainerBuilder();

            var container = builder.Build();

            var context = CreateContext<IUsesConcreteContext>(
                dependencyResolver: new AutofacDependencyResolver(container));

            var dataStore = context.TestDataStore;
        }

        static TContext CreateContext<TContext>(
            IConfigurationProvider configurationProvider = null,
            IDependencyResolver dependencyResolver = null)
            where TContext : class
        {
            return
                new ContextFactory(
                    configurationProvider,
                    dependencyResolver)
                .Create<TContext>();
        }

        // API interface 

        public interface IStorage { }

        public interface ISerializer { }

        public interface ICompressor { }

        public interface IDataStore<TData> { }

        // API implementation

        class LocalFileStorage : IStorage
        {
            public LocalFileStorage(string rootPath)
            {
                RootPath = rootPath;
            }

            public string RootPath { get; }
        }

        class JsonSerializer : ISerializer { }

        class GZipCompressor : ICompressor { }

        class JsonSerializerWithDependency : ISerializer
        {
            public JsonSerializerWithDependency(IDependency dependency)
            {
                Dependency = dependency;
            }

            public IDependency Dependency { get; }
        }

        interface IDependency { }

        class Dependency : IDependency { }

        class DataStore<TData> : IDataStore<TData>
        {
            public DataStore(
                IStorage storage,
                ISerializer serializer,
                [Optional]ICompressor compressor = null)
            {
                Storage = storage;

                Serializer = serializer;

                Compressor = compressor;
            }

            public IStorage Storage { get; }

            public ISerializer Serializer { get; }

            public ICompressor Compressor { get; }
        }

        // API facet attributes

        public class RootPathAttribute : FacetParameterAttribute
        {
            public RootPathAttribute(string value = null, string configKey = null) : base(value, configKey) { }

            public override Type TargetType => typeof(LocalFileStorage);

            public override string ParameterName => "rootPath";
        }

        public class JsonSerializationAttribute : FacetAttribute
        {
            public override Type TargetType => typeof(JsonSerializer);
        }

        public class JsonSerializationWithDependency : FacetAttribute
        {
            public override Type TargetType => typeof(JsonSerializerWithDependency);
        }

        public class DataStoreAttribute : FacetAttribute
        {
            public override Type TargetType => typeof(DataStore<>);
        }

        public class GZipCompressionAttribute : FacetAttribute
        {
            public override Type TargetType => typeof(GZipCompressor);
        }

        // Test contexts

        public interface IFacetContext : IContext
        {
            [JsonSerialization]
            ISerializer Serializer { get; }
        }

        public interface IFacetParameterContext : IContext
        {
            [RootPath(TestRootPath)]
            IStorage Storage { get; }
        }

        public interface IComposedContext : IContext
        {
            [DataStore, RootPath(TestRootPath), JsonSerialization, GZipCompression]
            IDataStore<string> DataStore { get; }
        }

        public interface IMissingResultFacetContext : IContext
        {
            ISerializer Serializer { get; }
        }

        public interface IMissingParameterFacetContext : IContext
        {
            [DataStore, JsonSerialization]
            IDataStore<string> DataStore { get; }
        }

        [JsonSerialization]
        public interface IFacetAncestorContext : IContext
        {
            ISerializer Serializer { get; }
        }

        [GZipCompression]
        public class FacetSource
        {
            [DataStore]
            public class DataStore
            {
                [JsonSerialization]
                public class Json { }
            }

            [RootPath("something-else")]
            public class SomethingElse { }

            [RootPath(TestRootPath)]
            public class LocalStorage { }
        }

        [Source(typeof(FacetSource.LocalStorage))]
        public interface ISourceContext : IContext
        {
            [Source(typeof(FacetSource.DataStore.Json))]
            IDataStore<byte[]> DataStore { get; }
        }

        public interface IOptionalOmittedContext : IContext
        {
            [DataStore, RootPath(TestRootPath), JsonSerialization]
            IDataStore<string> DataStore { get; }
        }

        public interface IConfigKeyContext : IContext
        {
            [RootPath(configKey: TestConfigKey)]
            IStorage Storage { get; }
        }

        public interface ISubContext : IContext
        {
            [RootPath(configKey: TestConfigKey)]
            IStorage Storage { get; }
        }

        public interface IParentContext : IContext
        {
            ISubContext SubContext { get; }
        }

        public interface IDependentContext : IContext
        {
            [JsonSerializationWithDependency]
            ISerializer Serializer { get; }
        }

        public interface IStoreContext<TKey, TValue> : IContext
        {
            [ServiceGet]
            [Uses(typeof(StoreRequestMapper<,>), nameof(TKey), nameof(TValue))]
            IEndpoint<StoreGetCommand<TKey, TValue>, TValue> Get { get; }
        }

        public class StoreGetCommand<TKey, TValue>
        {
            public StoreGetCommand()
            {
                var a = typeof(TKey).Name;

                var b = typeof(TValue).Name;
            }
        }

        public interface IStoreParentContext : IContext
        {
            [StoreName("steve")]
            IStoreContext<Guid, string> Things { get; }
        }

        public interface IRequestMapper
        {
            string Name { get; }
        }

        public class StoreRequestMapper<TKey, TValue> : IRequestMapper
        {
            public StoreRequestMapper(
                string name)
            {
                var a = typeof(TKey).Name;

                var b = typeof(TValue).Name;

                Name = name;
            }

            public string Name { get; }
        }

        public interface IEndpoint<TCommand, TResult>
        {
            IRequestMapper RequestMapper { get; }
        }

        public class Endpoint<TCommand, TResult> : IEndpoint<TCommand, TResult>
        {
            public Endpoint(
                IRequestMapper genericDependency)
            {
                RequestMapper = genericDependency;
            }

            public IRequestMapper RequestMapper { get; }
        }

        public class ServiceGetAttribute : FacetAttribute
        {
            public override Type TargetType => typeof(Endpoint<,>);
        }

        public class StoreNameAttribute : FacetParameterAttribute
        {
            public StoreNameAttribute(string value = null, string configKey = null) :
                base(value, configKey) { }

            public override string ParameterName => "name";

            public override Type TargetType => typeof(StoreRequestMapper<,>);
        }

        // UsesAttribute Test Types ///////////////////////////////////////////

        public interface IUsesDependency { }

        public interface IUsesNonDependant { }

        public class UsesDependency : IUsesDependency { }

        public class UsesNonDependant : IUsesNonDependant { }

        public class UsesNonDependantFacetAttribute : FacetAttribute
        {
            public override Type TargetType => typeof(UsesNonDependant);
        }

        public interface IUsesContext
        {
            [Uses(typeof(IUsesDependency))]
            [UsesNonDependantFacet]
            IUsesNonDependant NonDependant { get; }
        }

        // UsesAttribute Concrete Test Types //////////////////////////////////

        public interface ITestDataStore<TKey, TValue> { }

        public interface ITestValidator<TKey, TValue> { }

        public abstract class TestValidatorBase<TKey, TValue> : ITestValidator<TKey, TValue> { }

        public class TestValidator : TestValidatorBase<Guid, string> { }

        public class TestFileStoreDataStore<TKey, TValue> : ITestDataStore<TKey, TValue>
        {
            public TestFileStoreDataStore(
                [Optional]ITestValidator<TKey, TValue> validator = null)
            {

            }
        }

        public class TestFileStoreAttributeAttribute : FacetAttribute
        {
            public override Type TargetType => typeof(TestFileStoreDataStore<,>);
        }

        public interface IUsesConcreteContext
        {
            [TestFileStoreAttribute]
            [Uses(typeof(TestValidator))]
            ITestDataStore<Guid, string> TestDataStore { get; }
        }
    }
}
