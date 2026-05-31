using Microsoft.Extensions.DependencyInjection;

namespace AdminDb.Core;

internal interface IModule
{
    bool Init();
    void OnPostInit(ServiceProvider provider) { }
    void OnAllModulesLoaded(ServiceProvider provider) { }
    void Shutdown() { }
}
