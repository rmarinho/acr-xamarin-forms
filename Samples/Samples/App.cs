﻿using System;
using System.Reflection;
using Acr.XamForms.BarCodeScanner;
using Acr.XamForms.Mobile;
using Acr.XamForms.Settings;
using Acr.XamForms.UserDialogs;
using Acr.XamForms.ViewModels;
using Autofac;
using Samples.Views;
using Xamarin.Forms;


namespace Samples {

    public static class App {
        private static readonly IContainer container;
        private static INavigation navigator;

        static App() {
            var builder = new ContainerBuilder()
                .RegisterViewModels()
                .RegisterXamDependency<IBarCodeScanner>()
                .RegisterXamDependency<IUserDialogService>()
                .RegisterXamDependency<IDeviceInfo>()
                .RegisterXamDependency<INetworkService>()
                .RegisterXamDependency<ISettingsService>()
                .RegisterXamDependency<ILocationService>()
                .RegisterXamDependency<IMailService>()
                .RegisterXamDependency<IPhoneService>()
                .RegisterXamDependency<IPhotoService>();

            builder
                .Register(x => navigator)
                .As<INavigation>()
                .SingleInstance();

            container = builder.Build();
        }


        public static Page GetMainPage() {
            var page = new NavigationPage(new HomeView());
            navigator = page.Navigation;
            return page;
        }


        private static ContainerBuilder RegisterViewModels(this ContainerBuilder builder) {
            var ass = Assembly.Load(new AssemblyName("Samples"));

            builder
                .RegisterAssemblyTypes(ass)
                .Where(x => 
                    x.GetTypeInfo().IsClass &&
                    !x.GetTypeInfo().IsAbstract &&
                    x.Name.EndsWith("ViewModel")
                )
                .InstancePerDependency();
            
            return builder;
        }


        private static ContainerBuilder RegisterXamDependency<T>(this ContainerBuilder builder) where T : class {
            builder.Register(x => DependencyService.Get<T>()).SingleInstance();
            return builder;
        }
 

        public static ContentPage ResolveView<T>(object args = null) where T : IViewModel {
            var viewName = typeof(T)
                .AssemblyQualifiedName
                .Replace("ViewModel", "View");

            var viewModel = container.Resolve<T>();
            viewModel.Init(args);

            var viewType = Type.GetType(viewName);
            if (viewType == null)
                throw new ArgumentException(viewName + " type not exist");

            var page = Activator.CreateInstance(viewType) as ContentPage;
            if (page == null)
                throw new ArgumentException(viewName + " does not inherit from contentpage");

            page.BindingContext = viewModel;
            page.Appearing += async (sender, args1) => await viewModel.Start();
            page.Disappearing += async (sender, args1) => {
                var dispose = viewModel as IDisposable;
                if (dispose != null)
                    dispose.Dispose();
            };
            
            //var nav = DependencyService.Get<INavigation>();
            //return nav.PushAsync(page);
            return page;
        }


        public static void NavigateTo<T>(object args = null) where T : IViewModel {
            var page = ResolveView<T>(args);
            navigator.PushAsync(page);
        }


        public static T Resolve<T>() where T : class {
            return container.Resolve<T>();    
        }
    }
}
