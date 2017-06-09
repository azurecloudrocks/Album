using System.Web.Http;
using System.Web.Mvc;

[assembly: WebActivator.PreApplicationStartMethod(typeof(AzureCloudRocks.CodeSamples.Album.WebApi.App_Start.NinjectWebCommon), "Start")]
[assembly: WebActivator.ApplicationShutdownMethodAttribute(typeof(AzureCloudRocks.CodeSamples.Album.WebApi.App_Start.NinjectWebCommon), "Stop")]

namespace AzureCloudRocks.CodeSamples.Album.WebApi.App_Start
{
    using System;
    using System.Web;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    using Ninject;
    using Ninject.Web.Common;
    using DataAccess;
    using DataAccess.Models;
    using Microsoft.WindowsAzure;
    using Microsoft.Azure;

    public static class NinjectWebCommon
    {
        public static readonly Bootstrapper Bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            Bootstrapper.Initialize(CreateKernel);
        }

        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            Bootstrapper.ShutDown();
        }

        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();

            GlobalConfiguration.Configuration.DependencyResolver = new NinjectDependencyResolver(kernel);

            kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
            kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

            RegisterServices(kernel);
            return kernel;
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            string stroageAccount = CloudConfigurationManager.GetSetting("DataConnectionString");
            var csa = CloudStorageAccount.Parse(stroageAccount);
            kernel.Bind<CloudStorageAccount>().ToConstant(csa);
            kernel.Bind<IBlobHelper>().To<BlobHelper>();
            kernel.Bind<IQueueHelper>().To<QueueHelper>();
            kernel.Bind<IEntityRepository<ImageItemEntity>>().To<EntityRepository<ImageItemEntity>>().WithConstructorArgument(
                "tableName", "images");
        }
    }
}
