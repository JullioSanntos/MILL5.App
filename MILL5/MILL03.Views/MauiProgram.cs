using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;

namespace MILL03.Views; 
public static class MauiProgram {
    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // 1. Pass MAUI's native ServiceCollection to your Coder-generated extension
        global::MILL03.Views.ServiceCollectionExtensions.AddRegistrations(builder.Services);

        var app = builder.Build();

        // 2. Pass the framework-built provider into your ServiceLocator
        global::MILL80.Infrastructure.ServiceLocator.Initialize(app.Services);

        // Option A: Make it the DEFAULT View for this ViewModel
        // (No string parameter provided)
        MILL03.Views.UIInfrastructure.ViewLocator.Instance.Register<MILL06.ViewModels.CustomersViewModel, Views.CustomersSyncFusion>();


        return app;
    }
}
